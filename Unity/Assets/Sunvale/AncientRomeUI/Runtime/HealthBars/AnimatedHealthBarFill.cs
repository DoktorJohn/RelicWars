using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Tweening;


namespace Sunvale.AncientRomeUI.HealthBars
{
    [AddComponentMenu("Sunvale/AncientRomeUI/AnimatedHealthBarFill")]
    public class AnimatedHealthBarFill : MonoBehaviour, ITweenClient
    {
        [System.Serializable]
        public struct FillBarGradient
        {
            public Color top;
            public Color bottom;
        }

        public enum TweenMode
        {
            None,
            Damage,
            Heal,
            Custom
        }

        public enum EaseType
        {
            Linear,
            EaseOutQuad,
            EaseOutCubic,
            EaseOutQuart,
            EaseInOutQuad
        }

        [Header("References")]
        [SerializeField] private Graphic coreGraphic;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TextMeshProUGUI tmpNumber;

        [Header("Tiling")]
        [SerializeField] private Vector2 pixelsPerTile = new Vector2(128f, 64f);

        [Header("Main Fill Colors")]
        [SerializeField]
        private FillBarGradient mainGradient = new FillBarGradient
        {
            top = new Color(0.92f, 0.20f, 0.18f, 1f),
            bottom = new Color(0.38f, 0.03f, 0.03f, 1f)
        };

        [Header("Damage Ghost Colors")]
        [SerializeField]
        private FillBarGradient damageGhostGradient = new FillBarGradient
        {
            top = new Color(1.00f, 0.72f, 0.34f, 1f),
            bottom = new Color(0.58f, 0.22f, 0.06f, 1f)
        };

        [Header("Heal Ghost Colors")]
        [SerializeField]
        private FillBarGradient healGhostGradient = new FillBarGradient
        {
            top = new Color(0.65f, 1.00f, 0.55f, 1f),
            bottom = new Color(0.12f, 0.45f, 0.12f, 1f)
        };

        [Header("Animation Durations")]
        [SerializeField] private float damageDelay = 0.08f;
        [SerializeField] private float damageFillDuration = 0.28f;

        [SerializeField] private float healDelay = 0.04f;
        [SerializeField] private float healFillDuration = 0.22f;

        [Header("Animation Easing")]
        [SerializeField] private EaseType damageEase = EaseType.EaseOutCubic;
        [SerializeField] private EaseType healEase = EaseType.EaseOutCubic;

        [Header("Damage Flash")]
        [SerializeField] private bool flashOnDamage = true;

        [SerializeField] private float flashDuration = 0.16f;
        [SerializeField] private float flashBrightness = 1.28f;
        [SerializeField] private float flashSaturation = 0.95f;
        [SerializeField] private float flashContrast = 1.18f;
        [SerializeField] private EaseType flashEase = EaseType.EaseOutQuad;

        [Header("Number Text")]
        [SerializeField] private bool tweenNumberText = true;
        [SerializeField] private bool roundNumberText = true;
        [SerializeField] private string numberSeparator = "/";

        [Header("Runtime State")]
        [SerializeField, Range(0f, 1f)] private float mainFill = 1f;
        [SerializeField, Range(0f, 1f)] private float ghostFill = 1f;

        private Material runtimeMaterial;

        private int myTweenNumber;

        private bool isTweenRegistered;

        private bool fillTweenActive;
        private bool flashTweenActive;
        private bool numberTweenActive;

        

        private float fillDelay;
        private float fillDuration;
        private float fillElapsed;
        private float fillStartValue;
        private float fillTargetValue;
        private EaseType fillEase;

        private float flashElapsed;

        private bool numberTextInitialized;
        private float numberDisplayedCurrent;
        private float numberDisplayedMax;
        private float numberStartCurrent;
        private float numberStartMax;
        private float numberTargetCurrent;
        private float numberTargetMax;
        private float numberDelay;
        private float numberDuration;
        private float numberElapsed;
        private EaseType numberEase;

        private static readonly int MainFill = Shader.PropertyToID("_MainFill");
        private static readonly int GhostFill = Shader.PropertyToID("_GhostFill");

        private static readonly int RectSize = Shader.PropertyToID("_RectSize");
        private static readonly int PixelsPerTile = Shader.PropertyToID("_PixelsPerTile");

        private static readonly int MainTopColor = Shader.PropertyToID("_MainTopColor");
        private static readonly int MainBottomColor = Shader.PropertyToID("_MainBottomColor");

        private static readonly int GhostTopColor = Shader.PropertyToID("_GhostTopColor");
        private static readonly int GhostBottomColor = Shader.PropertyToID("_GhostBottomColor");

        private static readonly int FXBrightness = Shader.PropertyToID("_FXBrightness");
        private static readonly int FXSaturation = Shader.PropertyToID("_FXSaturation");
        private static readonly int FXContrast = Shader.PropertyToID("_FXContrast");

        private void Reset()
        {
            coreGraphic = GetComponent<Graphic>();
            rectTransform = GetComponent<RectTransform>();
            tmpNumber = GetComponentInChildren<TextMeshProUGUI>();
        }

        private void Awake()
        {
            SetupMaterialInstance();
            PushAllToMaterial();
        }

        private void OnDisable()
        {
            StopTweening();
        }

        private void OnRectTransformDimensionsChange()
        {
            PushRectSize();
        }

        private void SetupMaterialInstance()
        {
            runtimeMaterial = new Material(coreGraphic.material);
            runtimeMaterial.name = coreGraphic.material.name + " Runtime";
            coreGraphic.material = runtimeMaterial;
        }

        private void PushAllToMaterial()
        {
            if (runtimeMaterial == null)
                return;

            PushRectSize();
            PushTiling();

            runtimeMaterial.SetFloat(MainFill, mainFill);
            runtimeMaterial.SetFloat(GhostFill, ghostFill);

            PushMainGradient();
            ResetFX();
        }

        private void PushRectSize()
        {
            if (runtimeMaterial == null || rectTransform == null)
                return;

            Rect rect = rectTransform.rect;
            runtimeMaterial.SetVector(RectSize, new Vector4(rect.width, rect.height, 0f, 0f));
        }

        private void PushTiling()
        {
            if (runtimeMaterial == null)
                return;

            runtimeMaterial.SetVector(PixelsPerTile, new Vector4(pixelsPerTile.x, pixelsPerTile.y, 0f, 0f));
        }

        private void PushMainGradient()
        {
            if (runtimeMaterial == null)
                return;

            runtimeMaterial.SetColor(MainTopColor, mainGradient.top);
            runtimeMaterial.SetColor(MainBottomColor, mainGradient.bottom);
        }

        private void PushGhostGradient(FillBarGradient gradient)
        {
            if (runtimeMaterial == null)
                return;

            runtimeMaterial.SetColor(GhostTopColor, gradient.top);
            runtimeMaterial.SetColor(GhostBottomColor, gradient.bottom);
        }

        public void SetHealthInstant(float currentHealth, float maxHealth)
        {
            float max = SanitizeMax(maxHealth);
            float current = ClampCurrent(currentHealth, max);

            SetNormalizedInstantInternal(Normalize(current, max), false);
            SetNumberInstant(current, max);
        }

        public void SetNormalizedInstant(float normalizedHealth)
        {
            SetNormalizedInstantInternal(normalizedHealth, true);
        }

        private void SetNormalizedInstantInternal(float normalizedHealth, bool updateNumberFromKnownMax)
        {
            SetupMaterialInstance();

            float value = Mathf.Clamp01(normalizedHealth);

            StopTweening();

            mainFill = value;
            ghostFill = value;

            if (runtimeMaterial != null)
            {
                runtimeMaterial.SetFloat(MainFill, mainFill);
                runtimeMaterial.SetFloat(GhostFill, ghostFill);
                ResetFX();
            }

            if (updateNumberFromKnownMax)
                SetKnownNormalizedNumberInstant(value);
        }

        public void AnimateToHealth(float currentHealth, float maxHealth)
        {
            float max = SanitizeMax(maxHealth);
            float current = ClampCurrent(currentHealth, max);
            float target = Normalize(current, max);

            if (Mathf.Approximately(target, mainFill))
            {
                SetHealthInstant(current, max);
                return;
            }

            if (target < mainFill)
                TakeDamageToHealth(current, max);
            else
                HealToHealth(current, max);
        }

        public void AnimateToNormalized(float targetNormalizedHealth)
        {
            float target = Mathf.Clamp01(targetNormalizedHealth);

            if (Mathf.Approximately(target, mainFill))
            {
                SetNormalizedInstant(target);
                return;
            }

            if (target < mainFill)
            {
                TakeDamageToNormalized(target);
            }
            else
            {
                HealToNormalized(target);
            }
        }

        public void TakeDamageToHealth(float currentHealth, float maxHealth)
        {
            float max = SanitizeMax(maxHealth);
            float current = ClampCurrent(currentHealth, max);
            float target = Normalize(current, max);

            TakeDamageToNormalizedCore(target);
            StartNumberTween(current, max, damageDelay, damageFillDuration, damageEase);
            RegisterTweenIfNeeded();
        }

        public void TakeDamageToNormalized(float targetNormalizedHealth)
        {
            float target = Mathf.Clamp01(targetNormalizedHealth);

            TakeDamageToNormalizedCore(target);
            StartKnownNormalizedNumberTween(target, damageDelay, damageFillDuration, damageEase);
            RegisterTweenIfNeeded();
        }

        private void TakeDamageToNormalizedCore(float targetNormalizedHealth)
        {
            float target = Mathf.Clamp01(targetNormalizedHealth);

            SetupMaterialInstance();
            StopTweeningButKeepValues();
            PushGhostGradient(damageGhostGradient);
            ghostFill = target;
            runtimeMaterial.SetFloat(GhostFill, ghostFill);

            StartFillTween(
                from: mainFill,
                to: target,
                delay: damageDelay,
                duration: damageFillDuration,
                ease: damageEase
            );

            if (flashOnDamage)
                StartFlashTween();
        }

        public void HealToHealth(float currentHealth, float maxHealth)
        {
            float max = SanitizeMax(maxHealth);
            float current = ClampCurrent(currentHealth, max);
            float target = Normalize(current, max);

            HealToNormalizedCore(target);
            StartNumberTween(current, max, healDelay, healFillDuration, healEase);
            RegisterTweenIfNeeded();
        }

        public void HealToNormalized(float targetNormalizedHealth)
        {
            float target = Mathf.Clamp01(targetNormalizedHealth);

            HealToNormalizedCore(target);
            StartKnownNormalizedNumberTween(target, healDelay, healFillDuration, healEase);
            RegisterTweenIfNeeded();
        }

        private void HealToNormalizedCore(float targetNormalizedHealth)
        {
            float target = Mathf.Clamp01(targetNormalizedHealth);

            SetupMaterialInstance();
            StopTweeningButKeepValues();
            PushGhostGradient(healGhostGradient);
            ghostFill = target;
            runtimeMaterial.SetFloat(GhostFill, ghostFill);

            StartFillTween(
                from: mainFill,
                to: target,
                delay: healDelay,
                duration: healFillDuration,
                ease: healEase
            );
        }

        public void AnimateCustomToNormalized(
            float targetNormalizedHealth,
            FillBarGradient ghostGradient,
            bool useFlash,
            float delay,
            float duration,
            EaseType ease)
        {
            float target = Mathf.Clamp01(targetNormalizedHealth);

            SetupMaterialInstance();
            StopTweeningButKeepValues();
            PushGhostGradient(ghostGradient);
            ghostFill = target;
            runtimeMaterial.SetFloat(GhostFill, ghostFill);

            StartFillTween(
                from: mainFill,
                to: target,
                delay: delay,
                duration: duration,
                ease: ease
            );

            StartKnownNormalizedNumberTween(target, delay, duration, ease);

            if (useFlash)
                StartFlashTween();

            RegisterTweenIfNeeded();
        }

        private void StartFillTween(float from, float to, float delay, float duration, EaseType ease)
        {
            fillTweenActive = true;

            fillDelay = Mathf.Max(0f, delay);
            fillDuration = Mathf.Max(0.0001f, duration);
            fillElapsed = 0f;

            fillStartValue = from;
            fillTargetValue = to;
            fillEase = ease;
        }

        private void StartFlashTween()
        {
            flashTweenActive = true;
            flashElapsed = 0f;
        }

        private void SetNumberInstant(float currentValue, float maxValue)
        {
            if (tmpNumber == null)
                return;

            maxValue = SanitizeMax(maxValue);
            currentValue = ClampCurrent(currentValue, maxValue);

            numberTextInitialized = true;
            numberTweenActive = false;

            numberDisplayedCurrent = currentValue;
            numberDisplayedMax = maxValue;

            numberStartCurrent = currentValue;
            numberStartMax = maxValue;
            numberTargetCurrent = currentValue;
            numberTargetMax = maxValue;

            PushNumberText();
        }

        private void SetKnownNormalizedNumberInstant(float normalizedValue)
        {
            if (tmpNumber == null || !numberTextInitialized)
                return;

            float max = numberDisplayedMax;
            SetNumberInstant(Mathf.Clamp01(normalizedValue) * max, max);
        }

        private void StartKnownNormalizedNumberTween(
            float targetNormalized,
            float delay,
            float duration,
            EaseType ease)
        {
            if (tmpNumber == null || !numberTextInitialized)
                return;

            float max = numberDisplayedMax;
            float current = Mathf.Clamp01(targetNormalized) * max;

            StartNumberTween(current, max, delay, duration, ease);
        }

        private void StartNumberTween(
            float targetCurrent,
            float targetMax,
            float delay,
            float duration,
            EaseType ease)
        {
            if (tmpNumber == null)
                return;

            targetMax = SanitizeMax(targetMax);
            targetCurrent = ClampCurrent(targetCurrent, targetMax);

            if (!numberTextInitialized)
            {
                numberTextInitialized = true;

                numberDisplayedMax = targetMax;
                numberDisplayedCurrent = ClampCurrent(mainFill * targetMax, targetMax);

                numberStartCurrent = numberDisplayedCurrent;
                numberStartMax = numberDisplayedMax;
                numberTargetCurrent = numberDisplayedCurrent;
                numberTargetMax = numberDisplayedMax;

                PushNumberText();
            }

            if (!tweenNumberText)
            {
                SetNumberInstant(targetCurrent, targetMax);
                return;
            }

            if (Mathf.Approximately(numberDisplayedCurrent, targetCurrent) &&
                Mathf.Approximately(numberDisplayedMax, targetMax))
            {
                SetNumberInstant(targetCurrent, targetMax);
                return;
            }

            numberTweenActive = true;

            numberDelay = Mathf.Max(0f, delay);
            numberDuration = Mathf.Max(0.0001f, duration);
            numberElapsed = 0f;

            numberStartCurrent = numberDisplayedCurrent;
            numberStartMax = numberDisplayedMax;

            numberTargetCurrent = targetCurrent;
            numberTargetMax = targetMax;

            numberEase = ease;
        }

        public void CustomUpdate(float deltaTime)
        {
            if (runtimeMaterial == null)
            {
                UnregisterTweenIfNeeded();
                return;
            }

            if (fillTweenActive)
                UpdateFillTween(deltaTime);

            if (numberTweenActive)
                UpdateNumberTween(deltaTime);

            if (flashTweenActive)
                UpdateFlashTween(deltaTime);

            if (!fillTweenActive && !flashTweenActive && !numberTweenActive)
            {
                ResetFX();
                UnregisterTweenIfNeeded();
            }
        }

        private void UpdateFillTween(float deltaTime)
        {
            fillElapsed += deltaTime;

            if (fillElapsed < fillDelay)
                return;

            float localTime = fillElapsed - fillDelay;
            float t = Mathf.Clamp01(localTime / fillDuration);
            float eased = GetEasedTime(t, fillEase);

            mainFill = Mathf.LerpUnclamped(fillStartValue, fillTargetValue, eased);
            runtimeMaterial.SetFloat(MainFill, mainFill);

            if (t >= 1f)
            {
                mainFill = fillTargetValue;
                ghostFill = fillTargetValue;

                runtimeMaterial.SetFloat(MainFill, mainFill);
                runtimeMaterial.SetFloat(GhostFill, ghostFill);

                fillTweenActive = false;
            }
        }

        private void UpdateNumberTween(float deltaTime)
        {
            numberElapsed += deltaTime;

            if (numberElapsed < numberDelay)
                return;

            float localTime = numberElapsed - numberDelay;
            float t = Mathf.Clamp01(localTime / numberDuration);
            float eased = GetEasedTime(t, numberEase);

            numberDisplayedCurrent = Mathf.LerpUnclamped(numberStartCurrent, numberTargetCurrent, eased);
            numberDisplayedMax = Mathf.LerpUnclamped(numberStartMax, numberTargetMax, eased);

            PushNumberText();

            if (t >= 1f)
            {
                numberDisplayedCurrent = numberTargetCurrent;
                numberDisplayedMax = numberTargetMax;

                PushNumberText();

                numberTweenActive = false;
            }
        }

        private void UpdateFlashTween(float deltaTime)
        {
            flashElapsed += deltaTime;

            float safeDuration = Mathf.Max(0.0001f, flashDuration);
            float halfDuration = safeDuration * 0.5f;

            float brightness;
            float saturation;
            float contrast;

            if (flashElapsed <= halfDuration)
            {
                float t = Mathf.Clamp01(flashElapsed / halfDuration);
                float eased = GetEasedTime(t, flashEase);

                brightness = Mathf.Lerp(1f, flashBrightness, eased);
                saturation = Mathf.Lerp(1f, flashSaturation, eased);
                contrast = Mathf.Lerp(1f, flashContrast, eased);
            }
            else
            {
                float t = Mathf.Clamp01((flashElapsed - halfDuration) / halfDuration);
                float eased = GetEasedTime(t, flashEase);

                brightness = Mathf.Lerp(flashBrightness, 1f, eased);
                saturation = Mathf.Lerp(flashSaturation, 1f, eased);
                contrast = Mathf.Lerp(flashContrast, 1f, eased);
            }

            runtimeMaterial.SetFloat(FXBrightness, brightness);
            runtimeMaterial.SetFloat(FXSaturation, saturation);
            runtimeMaterial.SetFloat(FXContrast, contrast);

            if (flashElapsed >= safeDuration)
            {
                flashTweenActive = false;
                ResetFX();
            }
        }

        private void PushNumberText()
        {
            if (roundNumberText)
            {
                tmpNumber.text =
                    Mathf.RoundToInt(numberDisplayedCurrent) +
                    numberSeparator +
                    Mathf.RoundToInt(numberDisplayedMax);
            }
            else
            {
                tmpNumber.text =
                    numberDisplayedCurrent.ToString("0.#") +
                    numberSeparator +
                    numberDisplayedMax.ToString("0.#");
            }
        }

        private void StopTweening()
        {
            fillTweenActive = false;
            flashTweenActive = false;
            numberTweenActive = false;
            ResetFX();
            UnregisterTweenIfNeeded();
        }

        private void StopTweeningButKeepValues()
        {
            fillTweenActive = false;
            flashTweenActive = false;
            numberTweenActive = false;
            ResetFX();
            UnregisterTweenIfNeeded();
        }

        private void RegisterTweenIfNeeded()
        {
            if (isTweenRegistered)
                return;

            SimpleTweenManager.RegisterTween(this);
            isTweenRegistered = true;
        }

        private void UnregisterTweenIfNeeded()
        {
            if (!isTweenRegistered)
                return;

            SimpleTweenManager.UnregisterTween(this);
            isTweenRegistered = false;
        }

        private void ResetFX()
        {
            if (runtimeMaterial == null)
                return;

            runtimeMaterial.SetFloat(FXBrightness, 1f);
            runtimeMaterial.SetFloat(FXSaturation, 1f);
            runtimeMaterial.SetFloat(FXContrast, 1f);
        }

        private static float Normalize(float currentHealth, float maxHealth)
        {
            if (maxHealth <= 0f)
                return 0f;

            return Mathf.Clamp01(currentHealth / maxHealth);
        }

        private static float SanitizeMax(float maxValue)
        {
            return Mathf.Max(0f, maxValue);
        }

        private static float ClampCurrent(float currentValue, float maxValue)
        {
            maxValue = SanitizeMax(maxValue);

            if (maxValue <= 0f)
                return 0f;

            return Mathf.Clamp(currentValue, 0f, maxValue);
        }

        private static float GetEasedTime(float t, EaseType easeType)
        {
            t = Mathf.Clamp01(t);

            switch (easeType)
            {
                case EaseType.Linear:
                    return t;

                case EaseType.EaseOutQuad:
                    return 1f - (1f - t) * (1f - t);

                case EaseType.EaseOutCubic:
                {
                    float inv = 1f - t;
                    return 1f - inv * inv * inv;
                }

                case EaseType.EaseOutQuart:
                {
                    float inv = 1f - t;
                    return 1f - inv * inv * inv * inv;
                }

                case EaseType.EaseInOutQuad:
                    return t < 0.5f
                        ? 2f * t * t
                        : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;

                default:
                    return t;
            }
        }

        public void SetIndexNumber(int number)
        {
            myTweenNumber = number;
        }

        public int GetIndexNumber()
        {
            return myTweenNumber;
        }
    }
}
