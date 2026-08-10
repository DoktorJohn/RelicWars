using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.AncientRomeUI.Buttons;


namespace Sunvale.AncientRomeUI.Demos.RPGTopDown
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Sunvale/RPG/DemoRPGBuffIconView")]
    public class DemoRPGBuffIconView : MonoBehaviour
    {
        private static readonly int BgTexId = Shader.PropertyToID("_BgTex");
        private static readonly int OverlayTexId = Shader.PropertyToID("_OverlayTex");

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BgTintId = Shader.PropertyToID("_BgTint");
        private static readonly int BgTintStrengthId = Shader.PropertyToID("_BgTintStrength");
        private static readonly int IconTintId = Shader.PropertyToID("_IconTint");
        private static readonly int IconOpacityId = Shader.PropertyToID("_IconOpacity");

        private static readonly int IconScaleId = Shader.PropertyToID("_IconScale");
        private static readonly int IconOffsetXId = Shader.PropertyToID("_IconOffsetX");
        private static readonly int IconOffsetYId = Shader.PropertyToID("_IconOffsetY");
        private static readonly int IconBrightnessId = Shader.PropertyToID("_IconBrightness");
        private static readonly int IconSaturationId = Shader.PropertyToID("_IconSaturation");
        private static readonly int IconContrastId = Shader.PropertyToID("_IconContrast");

        private static readonly int BgScaleId = Shader.PropertyToID("_BgScale");
        private static readonly int BgOffsetXId = Shader.PropertyToID("_BgOffsetX");
        private static readonly int BgOffsetYId = Shader.PropertyToID("_BgOffsetY");
        private static readonly int BgBrightnessId = Shader.PropertyToID("_BgBrightness");
        private static readonly int BgContrastId = Shader.PropertyToID("_BgContrast");
        private static readonly int BgSaturationId = Shader.PropertyToID("_BgSaturation");

        private static readonly int OverlayEnabledId = Shader.PropertyToID("_OverlayEnabled");
        private static readonly int OverlayTintId = Shader.PropertyToID("_OverlayTint");
        private static readonly int OverlayOpacityId = Shader.PropertyToID("_OverlayOpacity");
        private static readonly int OverlayScaleId = Shader.PropertyToID("_OverlayScale");
        private static readonly int OverlayOffsetXId = Shader.PropertyToID("_OverlayOffsetX");
        private static readonly int OverlayOffsetYId = Shader.PropertyToID("_OverlayOffsetY");

        private static readonly int IconStrokeColorId = Shader.PropertyToID("_IconStrokeColor");
        private static readonly int IconStrokeSizeId = Shader.PropertyToID("_IconStrokeSize");
        private static readonly int IconStrokeOpacityId = Shader.PropertyToID("_IconStrokeOpacity");

        private static readonly int IconShadowColorId = Shader.PropertyToID("_IconShadowColor");
        private static readonly int IconShadowOpacityId = Shader.PropertyToID("_IconShadowOpacity");
        private static readonly int IconShadowOffsetXId = Shader.PropertyToID("_IconShadowOffsetX");
        private static readonly int IconShadowOffsetYId = Shader.PropertyToID("_IconShadowOffsetY");
        private static readonly int IconShadowSoftnessId = Shader.PropertyToID("_IconShadowSoftness");

        private static readonly int CooldownEnabledId = Shader.PropertyToID("_CooldownEnabled");
        private static readonly int CooldownProgressId = Shader.PropertyToID("_CooldownProgress");
        private static readonly int GreyscaleDisabledId = Shader.PropertyToID("_GreyscaleDisabled");
        private static readonly int GreyscaleDisabledDarknessId = Shader.PropertyToID("_GreyscaleDisabledDarkness");
        private static readonly int SweepHighlightEnabledId = Shader.PropertyToID("_SweepHighlightEnabled");
        private static readonly int SweepHighlightOpacityId = Shader.PropertyToID("_SweepHighlightOpacity");

        [Header("References")]
        public RectTransform myRectTransform;
        public Image frameImage;
        public Image coreImage;
        public Image shadowImage;

        [Header("Source Copy")]
        public bool copyCoreSprite = true;
        public bool copyCoreImageColor = true;
        public bool copyFrameSprite = true;
        public bool copyFrameImageColor = true;
        public bool copyShadowSprite = false;
        public bool copyShadowImageColor = false;

        [Tooltip("Only copies variable art/color properties from the skill button material. The prefab keeps shape, bevel, frame, and cooldown style settings.")]
        public bool copyVariableShaderProperties = true;

        [Header("Icon Emphasis")]
        [SerializeField, Min(0.01f)] private float iconScaleMultiplier = 1.15f;

        [Header("Timer")]
        [SerializeField] private bool useUnscaledTime;
        [SerializeField] private bool hideWhenFinished = true;

        private Material runtimeCoreMaterial;
        private float durationSeconds = 1f;
        private float remainingSeconds;
        private bool isRunning;

        public float IconScaleMultiplier
        {
            get => iconScaleMultiplier;
            set => iconScaleMultiplier = Mathf.Max(0.01f, value);
        }

        public float DurationSeconds => durationSeconds;
        public float RemainingSeconds => remainingSeconds;
        public bool IsRunning => isRunning;

        private void Reset()
        {
            myRectTransform = transform as RectTransform;

            if (coreImage == null)
                coreImage = GetComponent<Image>();
        }

    #if UNITY_EDITOR
        private void OnValidate()
        {
            if (myRectTransform == null)
                myRectTransform = transform as RectTransform;

            iconScaleMultiplier = Mathf.Max(0.01f, iconScaleMultiplier);
        }
    #endif

        private void Awake()
        {
            if (myRectTransform == null)
                myRectTransform = transform as RectTransform;

            EnsureRuntimeCoreMaterial(null);
            SetTimerShaderState(active: false, progress01: 1f);
        }

        private void OnDisable()
        {
            isRunning = false;
            remainingSeconds = 0f;
        }

        private void OnDestroy()
        {
            if (runtimeCoreMaterial == null)
                return;

            if (Application.isPlaying)
                Destroy(runtimeCoreMaterial);
            else
                DestroyImmediate(runtimeCoreMaterial);

            runtimeCoreMaterial = null;
        }

        private void Update()
        {
            if (!isRunning)
                return;

            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            if (deltaTime <= 0f)
                return;

            remainingSeconds -= deltaTime;
            remainingSeconds = Mathf.Max(0f, remainingSeconds);

            float progress01 = 1f - remainingSeconds / Mathf.Max(0.01f, durationSeconds);
            progress01 = Mathf.Clamp01(progress01);

            SetTimerShaderState(active: remainingSeconds > 0f, progress01: progress01);

            if (remainingSeconds > 0f)
                return;

            isRunning = false;

            if (hideWhenFinished)
                DespawnInstant();
        }

        public void SpawnFrom(RPGSkillButton sourceButton, float duration)
        {
            if (sourceButton == null)
            {
                DespawnInstant();
                return;
            }

            gameObject.SetActive(true);

            durationSeconds = Mathf.Max(0.01f, duration);
            remainingSeconds = durationSeconds;
            isRunning = true;

            CopyFromSourceButton(sourceButton);
            SetTimerShaderState(active: true, progress01: 0f);
        }

        public void DespawnInstant()
        {
            isRunning = false;
            durationSeconds = Mathf.Max(0.01f, durationSeconds);
            remainingSeconds = 0f;

            SetTimerShaderState(active: false, progress01: 1f);
            gameObject.SetActive(false);
        }

        private void CopyFromSourceButton(RPGSkillButton sourceButton)
        {
            Image sourceCoreImage = sourceButton.coreImage;
            Image sourceFrameImage = sourceButton.frameImage;

            if (sourceCoreImage != null && coreImage != null)
            {
                if (copyCoreSprite)
                {
                    coreImage.sprite = sourceCoreImage.sprite;
                    coreImage.overrideSprite = sourceCoreImage.overrideSprite;
                    coreImage.preserveAspect = sourceCoreImage.preserveAspect;
                    coreImage.type = sourceCoreImage.type;
                    coreImage.fillCenter = sourceCoreImage.fillCenter;
                }

                if (copyCoreImageColor)
                    coreImage.color = sourceCoreImage.color;
            }

            if (sourceFrameImage != null && frameImage != null)
            {
                if (copyFrameSprite)
                {
                    frameImage.sprite = sourceFrameImage.sprite;
                    frameImage.overrideSprite = sourceFrameImage.overrideSprite;
                    frameImage.preserveAspect = sourceFrameImage.preserveAspect;
                    frameImage.type = sourceFrameImage.type;
                    frameImage.fillCenter = sourceFrameImage.fillCenter;
                }

                if (copyFrameImageColor)
                    frameImage.color = sourceFrameImage.color;
            }

            if (sourceFrameImage != null && shadowImage != null)
            {
                if (copyShadowSprite)
                {
                    shadowImage.sprite = sourceFrameImage.sprite;
                    shadowImage.overrideSprite = sourceFrameImage.overrideSprite;
                    shadowImage.preserveAspect = sourceFrameImage.preserveAspect;
                    shadowImage.type = sourceFrameImage.type;
                    shadowImage.fillCenter = sourceFrameImage.fillCenter;
                }

                if (copyShadowImageColor)
                    shadowImage.color = sourceFrameImage.color;
            }

            if (!copyVariableShaderProperties)
                return;

            Material sourceMaterial = sourceCoreImage != null ? sourceCoreImage.material : null;
            Material targetMaterial = EnsureRuntimeCoreMaterial(sourceMaterial);

            if (sourceMaterial == null || targetMaterial == null)
                return;

            CopyTextureIfExists(sourceMaterial, targetMaterial, BgTexId);
            CopyTextureIfExists(sourceMaterial, targetMaterial, OverlayTexId);

            CopyColorIfExists(sourceMaterial, targetMaterial, ColorId);
            CopyColorIfExists(sourceMaterial, targetMaterial, BgTintId);
            CopyColorIfExists(sourceMaterial, targetMaterial, IconTintId);
            CopyColorIfExists(sourceMaterial, targetMaterial, OverlayTintId);
            CopyColorIfExists(sourceMaterial, targetMaterial, IconStrokeColorId);
            CopyColorIfExists(sourceMaterial, targetMaterial, IconShadowColorId);

            CopyFloatIfExists(sourceMaterial, targetMaterial, BgTintStrengthId);
            CopyFloatIfExists(sourceMaterial, targetMaterial, IconOpacityId);

            CopyFloatIfExists(sourceMaterial, targetMaterial, IconOffsetXId);
            CopyFloatIfExists(sourceMaterial, targetMaterial, IconOffsetYId);
            CopyFloatIfExists(sourceMaterial, targetMaterial, IconBrightnessId);
            CopyFloatIfExists(sourceMaterial, targetMaterial, IconSaturationId);
            CopyFloatIfExists(sourceMaterial, targetMaterial, IconContrastId);

            CopyFloatIfExists(sourceMaterial, targetMaterial, BgScaleId);
            CopyFloatIfExists(sourceMaterial, targetMaterial, BgOffsetXId);
            CopyFloatIfExists(sourceMaterial, targetMaterial, BgOffsetYId);
            CopyFloatIfExists(sourceMaterial, targetMaterial, BgBrightnessId);
            // CopyFloatIfExists(sourceMaterial, targetMaterial, BgContrastId);
            // CopyFloatIfExists(sourceMaterial, targetMaterial, BgSaturationId);

            CopyFloatIfExists(sourceMaterial, targetMaterial, OverlayEnabledId);
            CopyFloatIfExists(sourceMaterial, targetMaterial, OverlayOpacityId);
            CopyFloatIfExists(sourceMaterial, targetMaterial, OverlayScaleId);
            CopyFloatIfExists(sourceMaterial, targetMaterial, OverlayOffsetXId);
            CopyFloatIfExists(sourceMaterial, targetMaterial, OverlayOffsetYId);

            CopyFloatIfExists(sourceMaterial, targetMaterial, IconStrokeSizeId);
            CopyFloatIfExists(sourceMaterial, targetMaterial, IconStrokeOpacityId);
            CopyFloatIfExists(sourceMaterial, targetMaterial, IconShadowOpacityId);
            CopyFloatIfExists(sourceMaterial, targetMaterial, IconShadowOffsetXId);
            CopyFloatIfExists(sourceMaterial, targetMaterial, IconShadowOffsetYId);
            CopyFloatIfExists(sourceMaterial, targetMaterial, IconShadowSoftnessId);

            float sourceIconScale = GetFloatIfExists(sourceMaterial, IconScaleId, GetFloatIfExists(targetMaterial, IconScaleId, 1f));
            SetFloatIfExists(targetMaterial, IconScaleId, sourceIconScale * iconScaleMultiplier);

            SetFloatIfExists(targetMaterial, GreyscaleDisabledId, 0f);
            SetFloatIfExists(targetMaterial, GreyscaleDisabledDarknessId, 0f);
            SetFloatIfExists(targetMaterial, SweepHighlightEnabledId, 0f);
            SetFloatIfExists(targetMaterial, SweepHighlightOpacityId, 0f);
        }

        private Material EnsureRuntimeCoreMaterial(Material fallbackSourceMaterial)
        {
            if (coreImage == null)
                return null;

            if (runtimeCoreMaterial != null)
                return runtimeCoreMaterial;

            Material sourceMaterial = coreImage.material;

            if (sourceMaterial == null)
                sourceMaterial = fallbackSourceMaterial;

            if (sourceMaterial == null)
                return null;

            runtimeCoreMaterial = new Material(sourceMaterial);
            runtimeCoreMaterial.name = sourceMaterial.name + " - Global Buff Visual Instance";
            runtimeCoreMaterial.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

            coreImage.material = runtimeCoreMaterial;
            coreImage.SetMaterialDirty();

            return runtimeCoreMaterial;
        }

        private void SetTimerShaderState(bool active, float progress01)
        {
            Material material = EnsureRuntimeCoreMaterial(null);

            if (material == null)
                return;

            SetFloatIfExists(material, CooldownEnabledId, active ? 1f : 0f);
            SetFloatIfExists(material, CooldownProgressId, Mathf.Clamp01(progress01));
        }

        private static void CopyTextureIfExists(Material source, Material target, int propertyId)
        {
            if (source == null || target == null)
                return;

            if (!source.HasProperty(propertyId) || !target.HasProperty(propertyId))
                return;

            target.SetTexture(propertyId, source.GetTexture(propertyId));
        }

        private static void CopyColorIfExists(Material source, Material target, int propertyId)
        {
            if (source == null || target == null)
                return;

            if (!source.HasProperty(propertyId) || !target.HasProperty(propertyId))
                return;

            target.SetColor(propertyId, source.GetColor(propertyId));
        }

        private static void CopyFloatIfExists(Material source, Material target, int propertyId)
        {
            if (source == null || target == null)
                return;

            if (!source.HasProperty(propertyId) || !target.HasProperty(propertyId))
                return;

            target.SetFloat(propertyId, source.GetFloat(propertyId));
        }

        private static float GetFloatIfExists(Material material, int propertyId, float fallback)
        {
            if (material == null)
                return fallback;

            if (!material.HasProperty(propertyId))
                return fallback;

            return material.GetFloat(propertyId);
        }

        private static void SetFloatIfExists(Material material, int propertyId, float value)
        {
            if (material == null)
                return;

            if (!material.HasProperty(propertyId))
                return;

            material.SetFloat(propertyId, value);
        }
    }

}
