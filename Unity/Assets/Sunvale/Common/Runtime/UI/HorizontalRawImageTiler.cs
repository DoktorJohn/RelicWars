
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.Common.UI
{
        [ExecuteAlways] 
        [RequireComponent(typeof(RawImage))]
        [AddComponentMenu("Sunvale/Common/HorizontalRawImageTiler")]
        public class HorizontalRawImageTiler : UIBehaviour
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
                    m_TextureScale = Mathf.Max(0.001f, value); // Prevent division by zero
                    UpdateTiling();
                }
            }
        }

        [Tooltip("If enabled, the texture will not stretch/deform when the UI height changes. It maintains its original aspect ratio visually.")]
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
            // Ensure scale doesn't hit 0 in the inspector
            m_TextureScale = Mathf.Max(0.001f, m_TextureScale); 
            UpdateTiling();
        }
        
        
        [System.NonSerialized] private Texture m_LastTexture;
        protected virtual void Update()
        {
            // Do not run this polling loop in the actual game
            if (Application.isPlaying) return;

            if (rawImage != null && rawImage.texture != m_LastTexture)
            {
                m_LastTexture = rawImage.texture;
                UpdateTiling();
                
                // Forces the Editor to redraw the view immediately so it doesn't wait for a mouse click
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

            // Prevent math errors if texture or rect is collapsed
            if (texW <= 0 || texH <= 0 || rectH <= 0) return;

            Rect uvs = rawImage.uvRect;
            
            // Always stretch vertically (no vertical tiling)
            uvs.height = 1f; 

            float effectiveTexW = texW;

            if (m_PreserveAspect)
            {
                // Calculate how much the UI height has stretched compared to the raw texture height
                float heightStretchRatio = rectH / texH;
                
                // Apply that exact same stretch to the width calculation so it doesn't deform!
                effectiveTexW = texW * heightStretchRatio;
            }

            // Apply UV width based on Rect size, effective width, and the user's custom scale multiplier
            uvs.width = (rectW / effectiveTexW) * m_TextureScale;

            // Apply
            rawImage.uvRect = uvs;
        }
        }

}
