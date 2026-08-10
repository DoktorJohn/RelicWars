using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Graphics
{
    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public class SimpleFillBar : MonoBehaviour
    {
        [Range(0f, 1f)]
        public float sliderValue = 1f;

        [SerializeField] private Material sourceMaterial;

        private static readonly int FillId = Shader.PropertyToID("_Fill");
        private static readonly int RectSizeId = Shader.PropertyToID("_RectSize");

        private Graphic graphic;
        private RectTransform rectTransform;

        private Material runtimeMaterial;
        private Material editorPreviewMaterial;

    #if UNITY_EDITOR
        private bool editorPreviewQueued;
    #endif

        private void Awake()
        {
            CacheComponents();

            if (Application.isPlaying)
            {
                CreateRuntimeMaterialInstance();
                SetNormalizedValue(sliderValue);
            }
        }

        private void OnEnable()
        {
            CacheComponents();

    #if UNITY_EDITOR
            if (!Application.isPlaying)
                QueueEditorPreview();
    #endif
        }

        public void SetNormalizedValue(float normalizedValue)
        {
            sliderValue = Mathf.Clamp01(normalizedValue);

            CacheComponents();

            if (Application.isPlaying)
            {
                CreateRuntimeMaterialInstance();
                ApplyToMaterial(runtimeMaterial);
                graphic.SetMaterialDirty();
            }
    #if UNITY_EDITOR
            else
            {
                QueueEditorPreview();
            }
    #endif
        }

        private void CacheComponents()
        {
            graphic = GetComponent<Graphic>();
            rectTransform = GetComponent<RectTransform>();
        }

        private void CreateRuntimeMaterialInstance()
        {
            if (runtimeMaterial != null)
                return;

            runtimeMaterial = new Material(GetSourceMaterial());
            runtimeMaterial.name = GetSourceMaterial().name + " Runtime Instance";

            graphic.material = runtimeMaterial;
        }

        private Material GetSourceMaterial()
        {
            return sourceMaterial != null ? sourceMaterial : graphic.material;
        }

        private void ApplyToMaterial(Material material)
        {
            Rect rect = rectTransform.rect;

            material.SetFloat(FillId, sliderValue);
            material.SetVector(RectSizeId, new Vector4(rect.width, rect.height, 0f, 0f));
        }

        private void OnRectTransformDimensionsChange()
        {
            CacheComponents();

            if (Application.isPlaying)
            {
                SetNormalizedValue(sliderValue);
            }
    #if UNITY_EDITOR
            else
            {
                QueueEditorPreview();
            }
    #endif
        }

    #if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
                QueueEditorPreview();
            else
                SetNormalizedValue(sliderValue);
        }

        private void QueueEditorPreview()
        {
            if (editorPreviewQueued)
                return;

            editorPreviewQueued = true;
            EditorApplication.delayCall += ApplyEditorPreviewDelayed;
        }

        private void ApplyEditorPreviewDelayed()
        {
            editorPreviewQueued = false;

            if (this == null || !isActiveAndEnabled)
                return;

            ApplyEditorPreview();
        }

        private void ApplyEditorPreview()
        {
            CacheComponents();
            CreateEditorPreviewMaterialInstance();
            ApplyToMaterial(editorPreviewMaterial);

            graphic.canvasRenderer.SetMaterial(editorPreviewMaterial, graphic.mainTexture);
            graphic.SetVerticesDirty();
        }

        private void CreateEditorPreviewMaterialInstance()
        {
            Material currentSource = GetSourceMaterial();

            if (editorPreviewMaterial != null && editorPreviewMaterial.shader == currentSource.shader)
                return;

            DestroyEditorPreviewMaterial();

            editorPreviewMaterial = new Material(currentSource);
            editorPreviewMaterial.name = currentSource.name + " Editor Preview Instance";
            editorPreviewMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        private void DestroyEditorPreviewMaterial()
        {
            if (editorPreviewMaterial == null)
                return;

            DestroyImmediate(editorPreviewMaterial);
            editorPreviewMaterial = null;
        }
    #endif

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(runtimeMaterial);
                else
                    DestroyImmediate(runtimeMaterial);
            }

    #if UNITY_EDITOR
            DestroyEditorPreviewMaterial();
    #endif
        }
    }
}
