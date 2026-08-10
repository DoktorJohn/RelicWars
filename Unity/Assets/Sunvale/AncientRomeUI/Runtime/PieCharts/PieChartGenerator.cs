using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.UI;


namespace Sunvale.AncientRomeUI.PieCharts
{
    public class PieChartGenerator : MonoBehaviour
    {
        [System.Serializable]
        public class PieChartCategory
        {
            public string name;
            public float value;
            public Color color = Color.white;
            public Material material;
        }

        [Header("Setup")]
        public RectTransform container;
        public List<PieChartCategory> categories = new List<PieChartCategory>();

        [Header("Global Tiling Settings")]
        public float globalScaleMultiplier = 1000f;
        public Vector2 textureScale = new Vector2(1f, 1f);
        public Vector2 textureOffset = Vector2.zero;

        [Header("Dividers")]
        public bool showDividers = true;
        public Texture dividerTexture;
        public float dividerThickness = 8f;
        [Tooltip("1.0 goes exactly to the edge. 1.05 sticks out slightly.")]
        public float dividerLengthMultiplier = 1.02f;
        public float tilerScaleMultiplier = 1f;
        public Material dividerMaterial;
        
        
        // --- Explicit Object Pools ---
        // HideInInspector so they don't clutter the UI, but they serialize to survive editor reloads
        [SerializeField, HideInInspector] private List<PieChartSliceGraphic> m_SlicePool = new List<PieChartSliceGraphic>();
        [SerializeField, HideInInspector] private List<RectTransform> m_DividerPool = new List<RectTransform>();


        public void SetNewCategoriesMakesACopyOfList(List<PieChartCategory> newCategoryList)
        {
            categories.Clear();
            for (var i = 0; i < newCategoryList.Count; i++)
            {
                var newCategory = newCategoryList[i];
                categories.Add(newCategory);
            }
        }
        
        
        [ContextMenu("Generate Pie Chart")]
        public void GenerateChart()
        {
            if (container == null)
            {
                Debug.LogError("Please assign a container RectTransform.");
                return;
            }

            // 1. Calculate Total and count valid categories (skip 0 value slices)
            float totalValue = 0f;
            int validCategoryCount = 0;
            foreach (var category in categories)
            {
                if (category.value > 0.001f)
                {
                    totalValue += category.value;
                    validCategoryCount++;
                }
            }

            // 2. Clean Pools (in case user manually deleted a generated child in the editor)
            m_SlicePool.RemoveAll(item => item == null);
            m_DividerPool.RemoveAll(item => item == null);

            // If no data, hide everything and abort
            if (totalValue <= 0)
            {
                foreach (var slice in m_SlicePool) slice.gameObject.SetActive(false);
                foreach (var div in m_DividerPool) div.gameObject.SetActive(false);
                return;
            }

            // 3. Ensure Pools are large enough
            while (m_SlicePool.Count < validCategoryCount)
                m_SlicePool.Add(CreateNewSlice());

            while (m_DividerPool.Count < validCategoryCount)
                m_DividerPool.Add(CreateNewDivider());

            // 4. Update Data
            float currentStartPercent = 0f;
            float radius = Mathf.Min(container.rect.width, container.rect.height) / 2f;
            int activeIndex = 0; // Tracks which pool item we are using

            for (int i = 0; i < categories.Count; i++)
            {
                PieChartCategory cat = categories[i];
                if (cat.value <= 0.001f) continue; // Skip empty categories

                float fillPercent = cat.value / totalValue;

                // --- Update Slice ---
                PieChartSliceGraphic sliceGraphic = m_SlicePool[activeIndex];
                sliceGraphic.gameObject.SetActive(true);
                sliceGraphic.gameObject.name = $"Slice_{i}_{cat.name}";

                sliceGraphic.startPercent = currentStartPercent;
                sliceGraphic.fillPercent = fillPercent;
                sliceGraphic.color = cat.color;
                sliceGraphic.material = cat.material;
                sliceGraphic.SetVerticesDirty();

                // Refresh Tiling Component
                if (sliceGraphic.TryGetComponent<UIGlobalTextureTiling>(out var tiling))
                {
                    tiling.shaderTarget = UIGlobalTextureTiling.TilingShaderTarget.StandardUIShader;
                    tiling.globalScaleMultiplier = globalScaleMultiplier;
                    tiling.textureScale = textureScale;
                    tiling.textureOffset = textureOffset;
                    tiling.enabled = false;
                    tiling.enabled = true;
                }

                // --- Update Divider ---
                RectTransform divRt = m_DividerPool[activeIndex];
                if (showDividers)
                {
                    divRt.gameObject.SetActive(true);
                    divRt.gameObject.name = $"Divider_{i}_{cat.name}";

                    float startAngleDeg = currentStartPercent * 360f;
                    divRt.sizeDelta = new Vector2(radius * dividerLengthMultiplier, dividerThickness);
                    divRt.localEulerAngles = new Vector3(0, 0, -startAngleDeg + 90f);

                    if (divRt.TryGetComponent<RawImage>(out var rawImg))
                        rawImg.texture = dividerTexture;

                    if (divRt.TryGetComponent<HorizontalRawImageTiler>(out var tiler))
                    {
                        tiler.textureScale = tilerScaleMultiplier;
                        tiler.preserveAspect = true;
                        tiler.UpdateTiling();
                    }
                }
                else
                {
                    divRt.gameObject.SetActive(false);
                }

                currentStartPercent += fillPercent;
                activeIndex++;
            }

            // 5. Hide excess items in pools
            for (int i = activeIndex; i < m_SlicePool.Count; i++)
                m_SlicePool[i].gameObject.SetActive(false);

            for (int i = activeIndex; i < m_DividerPool.Count; i++)
                m_DividerPool[i].gameObject.SetActive(false);

            // 6. Fix Draw Order (Do this safely after all hierarchy modifications are done!)
            foreach (var slice in m_SlicePool)
                if (slice.gameObject.activeSelf) slice.transform.SetAsLastSibling();

            foreach (var div in m_DividerPool)
                if (div.gameObject.activeSelf) div.transform.SetAsLastSibling();
        }

        // --- Helper Methods for Clean Instantiation ---
        private PieChartSliceGraphic CreateNewSlice()
        {
            GameObject obj = new GameObject("Slice_New");
            obj.transform.SetParent(container, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            PieChartSliceGraphic slice = obj.AddComponent<PieChartSliceGraphic>();
            obj.AddComponent<UIGlobalTextureTiling>();
            
            return slice;
        }

        private RectTransform CreateNewDivider()
        {
            GameObject obj = new GameObject("Divider_New");
            obj.transform.SetParent(container, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            RawImage img = obj.AddComponent<RawImage>();
            img.raycastTarget = false;
            obj.AddComponent<HorizontalRawImageTiler>();
            img.material = dividerMaterial;
            obj.AddComponent<HorizontalRawImageTiler>();

            return rt;
        }

    #if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && gameObject.activeInHierarchy) GenerateChart();
            };
        }
    #endif
    }
}
