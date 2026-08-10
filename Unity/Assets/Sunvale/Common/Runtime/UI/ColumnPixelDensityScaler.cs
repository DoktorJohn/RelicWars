using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.Common.UI
{
    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Sunvale/Common/ColumnPixelDensityScaler")]
    public class ColumnPixelDensityScaler : UIBehaviour
    {
        [Tooltip("Keep this enabled if your UI changes size during gameplay...")]
        public bool updateAtRuntime = false;

        [SerializeField, HideInInspector]  private Image img;
        [SerializeField, HideInInspector]  private RectTransform rect;
        [SerializeField, HideInInspector]  private float nativeWidth;

        [SerializeField, HideInInspector] 
        private float lastCalculatedWidth = -1f;

        #if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            img = GetComponent<Image>();
            rect = GetComponent<RectTransform>();
            
            if (img != null && img.sprite != null)
            {
                nativeWidth = img.sprite.rect.width; 
            }
        }

    #endif

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            if (!Application.isPlaying || updateAtRuntime)
            {
                UpdateMultiplier();
            }
        }

        // We make this public so the Editor button can call it
        public void ForceRecalculate()
        {
            // Reset the cached width so the script is forced to update
            lastCalculatedWidth = -1f; 
            UpdateMultiplier();
        }

        private void UpdateMultiplier()
        {
            if (img == null || img.sprite == null || nativeWidth <= 0.01f || rect == null) return;

            float currentWidth = rect.rect.width;
            if (currentWidth <= 0.01f) return;
            if (Mathf.Abs(currentWidth - lastCalculatedWidth) < 0.01f) return;
            lastCalculatedWidth = currentWidth;
            float scaleRatio = currentWidth / nativeWidth;
            img.pixelsPerUnitMultiplier = 1f / scaleRatio;
    #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                EditorUtility.SetDirty(img);
            }
    #endif
        }
    }

}
