using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.Common.UI
{
    [AddComponentMenu("Sunvale/Common/SafeZonePanelScaler")]
    [ExecuteAlways]
    public class SafeZonePanelScaler : UIBehaviour
    {
        [Header("References")]
        [Tooltip("The zone this panel must fit inside (e.g., your Red Safe Zone)")]
        public RectTransform safeZone;

        [Header("Baked Data (Read Only)")]
        public bool isSetupValid = false;
        [SerializeField] private float authoredWidth;
        [SerializeField] private float authoredHeight;
        [SerializeField] private RectTransform myRectTransform;

        private float lastSafeWidth;
        private float lastSafeHeight;

        private DrivenRectTransformTracker tracker;

        #region Lifecycle & Timing Hooks

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!isSetupValid) return;

            // Ensure we don't double subscribe
            Canvas.willRenderCanvases -= UpdateScaleInstant;
            Canvas.willRenderCanvases += UpdateScaleInstant;
            
            // FIX: Reset BOTH cached values so Domain Reload doesn't trick the math
            lastSafeWidth = 0; 
            lastSafeHeight = 0; 
            
            UpdateScaleInstant();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Canvas.willRenderCanvases -= UpdateScaleInstant;
            tracker.Clear();
        }

        #endregion

        #region Runtime Scaling Logic

        private void UpdateScaleInstant()
        {
            
            if (!isSetupValid || safeZone == null || !gameObject.activeSelf) return;

            if (authoredWidth <= 0 || authoredHeight <= 0) return;

           

            float safeWidth = safeZone.rect.width;
            float safeHeight = safeZone.rect.height;

            if (Mathf.Approximately(safeWidth, lastSafeWidth) && Mathf.Approximately(safeHeight, lastSafeHeight))
                return;

            lastSafeWidth = safeWidth;
            lastSafeHeight = safeHeight;

            float scaleX = safeWidth / authoredWidth;
            float scaleY = safeHeight / authoredHeight;
            float finalScale = Mathf.Min(scaleX, scaleY);

            tracker.Clear();
            tracker.Add(this, myRectTransform, DrivenTransformProperties.Scale);

            myRectTransform.localScale = new Vector3(finalScale, finalScale, 1f);

       
        }
        
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            
            // If we are currently active and valid, check if we need to scale
            if (isActiveAndEnabled && isSetupValid)
            {
                UpdateScaleInstant();
            }
        }

        #endregion

        #region Bulletproof Authoring System (Editor Only)

    #if UNITY_EDITOR

        protected override void Reset()
        {
            base.Reset();
            myRectTransform = GetComponent<RectTransform>();
        }

       public void BakeSetup()
        {
            if (myRectTransform == null) myRectTransform = GetComponent<RectTransform>();

            // 1. Validation: References & Self-Assignment
            if (safeZone == null)
            {
                Debug.LogError("[Panel Scaler] FAILED: You must assign the Target Safe Zone RectTransform.", this);
                return;
            }
            if (safeZone == myRectTransform)
            {
                Debug.LogError("[Panel Scaler] FAILED: The Safe Zone cannot be the exact same object as this panel!", this);
                return;
            }

            // 2. Validation: Aspect Ratio
            float currentAspect = (float)Camera.main.pixelWidth / Camera.main.pixelHeight;
            if (currentAspect == 0) InitializeGameViewAspectRatio(out currentAspect);

            float targetAspect = 1920f / 1080f;
            if (Mathf.Abs(currentAspect - targetAspect) > 0.01f)
            {
                Debug.LogError($"[Panel Scaler] FAILED: Game View must be exactly 16:9 (e.g., 1920x1080).", this);
                return;
            }

            // 3. Validation: Dirty Scale
            if (myRectTransform.localScale != Vector3.one)
            {
                Debug.LogError("[Panel Scaler] FAILED: The Panel's localScale is not (1,1,1). Resetting scale to 1. Please verify layout and click Bake again.", this);
                myRectTransform.localScale = Vector3.one;
                return;
            }

            // 4. NEW Validation: Reject Stretch Anchors
            // Rigid scaled panels must have fixed anchors, otherwise Unity's Layout system fights the Scale system.
            if (myRectTransform.anchorMin != myRectTransform.anchorMax)
            {
                Debug.LogError("[Panel Scaler] FAILED: Scaled rigid panels cannot use Stretch Anchors. Please set Anchors to a fixed point (e.g., Center, Bottom-Center, etc).", this);
                return;
            }

            Canvas.ForceUpdateCanvases();
            
            // 5. Cache the physical size
            float trueWidth = myRectTransform.rect.width;
            float trueHeight = myRectTransform.rect.height;

            if (trueWidth <= 0 || trueHeight <= 0)
            {
                Debug.LogError("[Panel Scaler] FAILED: Panel has 0 width or height. Fix your layout before baking.", this);
                return;
            }

            // Note: We completely removed the code that forced anchors/pivots to 0.5.
            // We now respect whatever the UI Artist authored!

            // 6. Serialize Data
            authoredWidth = trueWidth;
            authoredHeight = trueHeight;
            isSetupValid = true;

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"<color=#4CAF50>[Panel Scaler] SUCCESS:</color> Panel locked at {authoredWidth}x{authoredHeight} with custom anchors.", this);

            Canvas.willRenderCanvases -= UpdateScaleInstant;
            Canvas.willRenderCanvases += UpdateScaleInstant;
            lastSafeWidth = 0;
            lastSafeHeight = 0;
            UpdateScaleInstant();
        }

        public void UnbakeSetup()
        {
            isSetupValid = false;
            tracker.Clear();
            Canvas.willRenderCanvases -= UpdateScaleInstant;
            
            if (myRectTransform != null)
                myRectTransform.localScale = Vector3.one;

            
            
            
            authoredWidth = 0;
            authoredHeight = 0;
            lastSafeWidth = 0;
            lastSafeHeight = 0;
            
            

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("<color=#FF9800>[Panel Scaler] UNBAKED:</color> Scaler disengaged. Scale reset to 1. You can now edit freely.", this);
        }

        private void InitializeGameViewAspectRatio(out float aspect)
        {
            Vector2 res = UnityEditor.Handles.GetMainGameViewSize();
            aspect = res.x / res.y;
        }
    #endif

        #endregion
    }

}
