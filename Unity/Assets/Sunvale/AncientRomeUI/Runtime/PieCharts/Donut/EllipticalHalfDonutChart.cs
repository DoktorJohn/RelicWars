using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.PieCharts.Donut
{
    public class EllipticalHalfDonutChart : MonoBehaviour
    {
        [System.Serializable]
        public class DonutChartCategory
        {
            public string name;
            public float value;
            public Color color = Color.white;
            public Material material;
        }

        [Header("Setup")]
        public RectTransform container;
        public List<DonutChartCategory> categories = new List<DonutChartCategory>();

        [Header("Shape Settings")]
        [Range(0f, 10f)] public float spacingDegrees = 2f; 
        [Range(0.1f, 0.9f)] public float innerHoleSize = 0.4f;
        
        [Tooltip("Shrinks the pie slices by this many pixels so they hide under the borders.")]
        public float sliceInsetPixels = 1.5f;
        
        [Header("Border Settings")]
        public bool showBorders = true;
        public float borderThickness = 4f;
        [Range(0f, 1f)]
        [Tooltip("0 = Inside the slice (hides slice edges), 0.5 = Centered, 1 = Outside the slice")]
        public float borderAlignment = 0f;
        public Color borderColor = new Color(0.8f, 0.7f, 0.2f, 1f); 
        public Material borderMaterial; 
        
        [SerializeField, HideInInspector] private List<EllipticalDonutSliceGraphic> m_SlicePool = new List<EllipticalDonutSliceGraphic>();
        [SerializeField, HideInInspector] private List<EllipticalDonutBorderGraphic> m_BorderPool = new List<EllipticalDonutBorderGraphic>();

        [ContextMenu("Generate Chart")]
        public void GenerateChart()
        {
            if (container == null) return;

            float totalValue = 0f;
            int validCategoryCount = 0;
            foreach (var cat in categories)
            {
                if (cat.value > 0.001f) { totalValue += cat.value; validCategoryCount++; }
            }

            m_SlicePool.RemoveAll(item => item == null);
            m_BorderPool.RemoveAll(item => item == null);

            if (totalValue <= 0)
            {
                foreach (var s in m_SlicePool) s.gameObject.SetActive(false);
                foreach (var b in m_BorderPool) b.gameObject.SetActive(false);
                return;
            }

            while (m_SlicePool.Count < validCategoryCount) m_SlicePool.Add(CreateNewSlice());
            while (m_BorderPool.Count < validCategoryCount) m_BorderPool.Add(CreateNewBorder());

            float totalAvailableDegrees = 180f;
            float totalGapDegrees = spacingDegrees * Mathf.Max(0, validCategoryCount - 1);
            if (totalGapDegrees >= totalAvailableDegrees) { totalGapDegrees = 0; spacingDegrees = 0; }

            float degreesForData = totalAvailableDegrees - totalGapDegrees;
            float currentStartPercent = 0f;
            int activeIndex = 0;

            for (int i = 0; i < categories.Count; i++)
            {
                DonutChartCategory cat = categories[i];
                if (cat.value <= 0.001f) continue;

                float sliceFillPercent = (cat.value / totalValue * degreesForData) / 180f;

                // Update Slice
                var slice = m_SlicePool[activeIndex];
                slice.gameObject.SetActive(true);
                slice.gameObject.name = $"Slice_{i}_{cat.name}";
                slice.startPercent = currentStartPercent;
                slice.fillPercent = sliceFillPercent;
                slice.innerHoleSize = innerHoleSize;
                slice.sliceInset = sliceInsetPixels; // Apply the inset here!
                slice.color = cat.color;
                slice.material = cat.material;
                slice.SetVerticesDirty();

                // Update Border
                var border = m_BorderPool[activeIndex];
                if (showBorders)
                {
                    border.gameObject.SetActive(true);
                    border.gameObject.name = $"Border_{i}_{cat.name}";
                    border.borderAlignment = borderAlignment;
                    border.startPercent = currentStartPercent;
                    border.fillPercent = sliceFillPercent;
                    border.innerHoleSize = innerHoleSize;
                    border.borderThickness = borderThickness;
                    border.color = borderColor;
                    border.material = borderMaterial;
                    border.SetVerticesDirty();
                }
                else border.gameObject.SetActive(false);

                currentStartPercent += sliceFillPercent + (spacingDegrees / 180f);
                activeIndex++;
            }

            for (int i = activeIndex; i < m_SlicePool.Count; i++) m_SlicePool[i].gameObject.SetActive(false);
            for (int i = activeIndex; i < m_BorderPool.Count; i++) m_BorderPool[i].gameObject.SetActive(false);

            foreach (var slice in m_SlicePool) if (slice.gameObject.activeSelf) slice.transform.SetAsLastSibling();
            foreach (var border in m_BorderPool) if (border.gameObject.activeSelf) border.transform.SetAsLastSibling();
        }

        private EllipticalDonutSliceGraphic CreateNewSlice()
        {
            GameObject obj = new GameObject("Slice_New");
            obj.transform.SetParent(container, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0f); 
            return obj.AddComponent<EllipticalDonutSliceGraphic>();
        }

        private EllipticalDonutBorderGraphic CreateNewBorder()
        {
            GameObject obj = new GameObject("Border_New");
            obj.transform.SetParent(container, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0f); 
            var border = obj.AddComponent<EllipticalDonutBorderGraphic>();
            border.raycastTarget = false; 
            return border;
        }

    #if UNITY_EDITOR
        private void OnValidate() { UnityEditor.EditorApplication.delayCall += () => { if (this != null && gameObject.activeInHierarchy) GenerateChart(); }; }
    #endif
    }
}
