using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.Common.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(RawImage))]
    [AddComponentMenu("Sunvale/Common/VerticalRawImageTiler")]
    public class VerticalRawImageTiler : UIBehaviour
    {
        private RawImage m_RawImage;
        private RawImage rawImage
        {
            get
            {
                if (m_RawImage == null) m_RawImage = GetComponent<RawImage>();
                return m_RawImage;
            }
        }

        [Tooltip("Zooms the texture in or out. 1 is default, 2 tiles twice as often (smaller), 0.5 tiles half as often (larger).")]
        [SerializeField] private float m_TextureScale = 1f;
        public float textureScale
        {
            get => m_TextureScale;
            set
            {
                if (m_TextureScale != value)
                {
                    m_TextureScale = Mathf.Max(0.001f, value);
                    UpdateTiling();
                }
            }
        }

        [Tooltip("If enabled, the texture will not stretch/deform when the UI width changes. It maintains its original aspect ratio visually.")]
        [SerializeField] private bool m_PreserveAspect = false;
        public bool preserveAspect
        {
            get => m_PreserveAspect;
            set
            {
                if (m_PreserveAspect != value)
                {
                    m_PreserveAspect = value;
                    UpdateTiling();
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateTiling();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            UpdateTiling();
        }

    #if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            m_TextureScale = Mathf.Max(0.001f, m_TextureScale);
            UpdateTiling();
        }

        [System.NonSerialized] private Texture m_LastTexture;
        protected virtual void Update()
        {
            if (Application.isPlaying) return;

            if (rawImage != null && rawImage.texture != m_LastTexture)
            {
                m_LastTexture = rawImage.texture;
                UpdateTiling();

                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
    #endif

        public void UpdateTiling()
        {
            if (!IsActive() || rawImage == null || rawImage.texture == null)
                return;

            float texW = rawImage.texture.width;
            float texH = rawImage.texture.height;
            float rectW = rawImage.rectTransform.rect.width;
            float rectH = rawImage.rectTransform.rect.height;

            if (texW <= 0 || texH <= 0 || rectW <= 0) return;

            Rect uvs = rawImage.uvRect;

            // Always stretch horizontally (no horizontal tiling)
            uvs.width = 1f;

            float effectiveTexH = texH;

            if (m_PreserveAspect)
            {
                // Calculate how much the UI width has stretched compared to the raw texture width
                float widthStretchRatio = rectW / texW;

                // Apply that exact same stretch to the height calculation so it doesn't deform
                effectiveTexH = texH * widthStretchRatio;
            }

            // Apply UV height based on Rect size, effective height, and the user's custom scale multiplier
            uvs.height = (rectH / effectiveTexH) * m_TextureScale;

            rawImage.uvRect = uvs;
        }
    }

}
