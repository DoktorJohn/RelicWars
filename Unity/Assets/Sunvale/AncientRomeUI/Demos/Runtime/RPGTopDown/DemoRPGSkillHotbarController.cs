using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.UI;
using Sunvale.Common.Sound;
using Sunvale.AncientRomeUI.Buttons;


namespace Sunvale.AncientRomeUI.Demos.RPGTopDown
{
    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    public class DemoRPGSkillHotbarController : MonoBehaviour
    {
        [Serializable]
        public struct ButtonProfile
        {
            public bool spawnArrow;
            public bool hasCooldown;

            [Tooltip("When true, this skill publishes a timed global-buff visual to RPGDemoController side cards.")]
            public bool publishGlobalBuff;

            [Tooltip("Manager-owned timer value. Used for cooldowns and for published global-buff visuals.")]
            public float cooldownDuration;
        }

        [Serializable]
        public class HotbarSkillSet
        {
            [Tooltip("Optional parent object for this hotbar page. If assigned, it will be activated/deactivated.")]
            public GameObject optionalRoot;

            public List<RPGSkillButton> buttons = new List<RPGSkillButton>();

            [Tooltip("Must match the Buttons list by index.")]
            public List<ButtonProfile> buttonProfiles = new List<ButtonProfile>();
        }

        private class RuntimeCooldown
        {
            public RPGSkillButton button;
            public float duration;
            public float remaining;
            public bool readyHighlightPlayed;
        }

        private const int HotBarCount = 4;

        [Header("References")] [SerializeField]
        public RPGDemoController myManager;
        [SerializeField] private Canvas canvas;
        [SerializeField] private SkillTargetingArrowGraphic targetArrowGraphic;
        [SerializeField] private TextMeshProUGUI hotbarCounterTMP;

        [Header("Sounds")]
        public UISoundConfig skillUseSound;

        public UISoundConfig buttonNotReadySound;

        [Header("Hot Bar Switch Buttons")]
        public SimpleButton upArrowButton;
        public SimpleButton downArrow;

        [Header("Hot Bar Skill Sets")]
        [SerializeField] private HotbarSkillSet[] hotBars = new HotbarSkillSet[HotBarCount];

        [Header("Startup")]
        [SerializeField, Range(0, HotBarCount - 1)] private int startingHotBarIndex = 0;

        [Header("Cooldown Behaviour")]
        [Tooltip("For this UI demo, this is usually cleaner. Switching pages resets cooldown visuals.")]
        [SerializeField] private bool clearCooldownsWhenChangingHotBar = true;

        [Tooltip("When a skill is clicked again, stop its old ready shine before starting cooldown.")]
        [SerializeField] private bool stopReadyHighlightWhenSkillUsed = true;

        [Header("Cooldown Text")]
        [SerializeField] private bool showCooldownTimerText = true;

        [Tooltip("TMP mspace tag keeps the timer width stable while digits change.")]
        [SerializeField] private string cooldownTimerMspaceTag = "0.65em";

        private readonly List<RuntimeCooldown> activeCooldowns = new List<RuntimeCooldown>();

        private int currentHotBarIndex;

        private bool isTargeting;
        private RPGSkillButton targetingButton;
        private ButtonProfile targetingProfile;

        private void Reset()
        {
            canvas = GetComponentInParent<Canvas>();
            EnsureHotBarsExist();
        }

    #if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureHotBarsExist();

            startingHotBarIndex = Mathf.Clamp(startingHotBarIndex, 0, HotBarCount - 1);

            for (int i = 0; i < hotBars.Length; i++)
            {
                HotbarSkillSet set = hotBars[i];

                if (set == null)
                    continue;

                for (int p = 0; p < set.buttonProfiles.Count; p++)
                {
                    ButtonProfile profile = set.buttonProfiles[p];

                    if (profile.cooldownDuration <= 0f)
                        profile.cooldownDuration = 1f;

                    set.buttonProfiles[p] = profile;
                }
            }
        }

        [ContextMenu("Hot Bar/Collect Buttons From Optional Roots")]
        private void ContextCollectButtonsFromOptionalRoots()
        {
            EditorCollectButtonsFromOptionalRoots();
        }

        public void EditorCollectButtonsFromOptionalRoots()
        {
            EnsureHotBarsExist();

            Undo.RecordObject(this, "Collect Hotbar Buttons From Optional Roots");

            for (int i = 0; i < hotBars.Length; i++)
            {
                HotbarSkillSet set = hotBars[i];

                if (set == null)
                {
                    set = new HotbarSkillSet();
                    hotBars[i] = set;
                }

                if (set.optionalRoot == null)
                {
                    Debug.LogWarning(
                        $"Hotbar {i} has no optionalRoot assigned. Skipping collection.",
                        this
                    );
                    continue;
                }

                List<RPGSkillButton> oldButtons = set.buttons != null
                    ? new List<RPGSkillButton>(set.buttons)
                    : new List<RPGSkillButton>();

                List<ButtonProfile> oldProfiles = set.buttonProfiles != null
                    ? new List<ButtonProfile>(set.buttonProfiles)
                    : new List<ButtonProfile>();

                RPGSkillButton[] foundButtons = set.optionalRoot.GetComponentsInChildren<RPGSkillButton>(true);

                Array.Sort(foundButtons, CompareByHierarchyOrder);

                set.buttons.Clear();
                set.buttonProfiles.Clear();

                for (int b = 0; b < foundButtons.Length; b++)
                {
                    RPGSkillButton button = foundButtons[b];

                    if (button == null)
                        continue;

                    set.buttons.Add(button);

                    ButtonProfile profile;

                    if (!TryFindExistingEditorProfile(button, oldButtons, oldProfiles, out profile))
                    {
                        profile = new ButtonProfile
                        {
                            spawnArrow = false,
                            hasCooldown = false,
                            publishGlobalBuff = false,
                            cooldownDuration = 1f
                        };
                    }

                    if (profile.cooldownDuration <= 0f)
                        profile.cooldownDuration = 1f;

                    set.buttonProfiles.Add(profile);
                }

                Debug.Log(
                    $"Collected {set.buttons.Count} RPGSkillButton references for Hotbar {i} from root '{set.optionalRoot.name}'.",
                    this
                );
            }

            EditorUtility.SetDirty(this);
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }

        private static bool TryFindExistingEditorProfile(
            RPGSkillButton button,
            List<RPGSkillButton> oldButtons,
            List<ButtonProfile> oldProfiles,
            out ButtonProfile profile)
        {
            profile = new ButtonProfile();

            if (button == null || oldButtons == null || oldProfiles == null)
                return false;

            int oldIndex = oldButtons.IndexOf(button);

            if (oldIndex < 0 || oldIndex >= oldProfiles.Count)
                return false;

            profile = oldProfiles[oldIndex];
            return true;
        }

        private static int CompareByHierarchyOrder(RPGSkillButton a, RPGSkillButton b)
        {
            if (a == null && b == null)
                return 0;

            if (a == null)
                return 1;

            if (b == null)
                return -1;

            string pathA = GetHierarchySortPath(a.transform);
            string pathB = GetHierarchySortPath(b.transform);

            return string.CompareOrdinal(pathA, pathB);
        }

        private static string GetHierarchySortPath(Transform transform)
        {
            if (transform == null)
                return string.Empty;

            string path = transform.GetSiblingIndex().ToString("D4") + "_" + transform.name;

            Transform parent = transform.parent;

            while (parent != null)
            {
                path = parent.GetSiblingIndex().ToString("D4") + "_" + parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    #endif

        private void Awake()
        {
            EnsureHotBarsExist();

            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();

            currentHotBarIndex = Mathf.Clamp(startingHotBarIndex, 0, HotBarCount - 1);

            if (targetArrowGraphic != null)
                targetArrowGraphic.SetAimingVisible(false);

            SetHotBarSkills(currentHotBarIndex);
        }

        private void OnEnable()
        {
            SubscribeToButtons();

            if (targetArrowGraphic != null)
                targetArrowGraphic.OnTargetClicked += HandleTargetArrowClicked;

            SetHotBarSkills(currentHotBarIndex, false);
            UpdateHotbarCounterText();
        }

        private void OnDisable()
        {
            UnsubscribeFromButtons();

            if (targetArrowGraphic != null)
                targetArrowGraphic.OnTargetClicked -= HandleTargetArrowClicked;

            CancelTargeting(false);
        }

        private void Update()
        {
            TickCooldowns(Time.deltaTime);

            if (!isTargeting)
                return;

            if (targetingButton == null || !targetingButton.gameObject.activeInHierarchy)
            {
                CancelTargeting(false);
                return;
            }

            UpdateTargetArrow();
        }

        public void SetHotBarSkills(int index)
        {
            SetHotBarSkills(index, clearCooldownsWhenChangingHotBar);
            UpdateHotbarCounterText();
        }

        private void SetHotBarSkills(int index, bool clearCooldowns)
        {
            index = WrapHotBarIndex(index);

            CancelTargeting(false);

            if (clearCooldowns)
                ClearManagedCooldowns();

            currentHotBarIndex = index;

            for (int i = 0; i < hotBars.Length; i++)
            {
                HotbarSkillSet set = hotBars[i];

                if (set == null)
                    continue;

                bool active = i == currentHotBarIndex;

                if (set.optionalRoot != null)
                    set.optionalRoot.SetActive(active);

                for (int b = 0; b < set.buttons.Count; b++)
                {
                    RPGSkillButton button = set.buttons[b];

                    if (button == null)
                        continue;

                    button.gameObject.SetActive(active);
                    button.SetAsDeselected(false);
                }
            }
        }

        public void ShowPreviousHotBar()
        {
            SetHotBarSkills(currentHotBarIndex - 1);
        }

        public void ShowNextHotBar()
        {
            SetHotBarSkills(currentHotBarIndex + 1);
        }

        private int WrapHotBarIndex(int index)
        {
            index %= HotBarCount;

            if (index < 0)
                index += HotBarCount;

            return index;
        }

        private void UpdateHotbarCounterText()
        {
            if (hotbarCounterTMP == null)
                return;

            switch (currentHotBarIndex)
            {
                case 0:
                    hotbarCounterTMP.SetText("I");
                    break;

                case 1:
                    hotbarCounterTMP.SetText("II");
                    break;

                case 2:
                    hotbarCounterTMP.SetText("III");
                    break;

                case 3:
                    hotbarCounterTMP.SetText("IV");
                    break;

                default:
                    hotbarCounterTMP.SetText((currentHotBarIndex + 1).ToString(CultureInfo.InvariantCulture));
                    break;
            }
        }

        private void HandleUpArrowClicked(SimpleButton button)
        {
            ShowPreviousHotBar();
        }

        private void HandleDownArrowClicked(SimpleButton button)
        {
            ShowNextHotBar();
        }

        private void HandleSkillButtonPointerUp(RPGSkillButton button, PointerEventData eventData)
        {
            if (button == null)
                return;

            if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!button.IsHovered)
                return;

            if (button.IsCooldownActive)
            {
                SimpleSoundManager.Play(buttonNotReadySound);
                return;
            }

            if (!TryGetButtonProfile(button, out ButtonProfile profile))
                return;

            if (profile.spawnArrow)
            {
                BeginTargeting(button, profile);
            }
            else
            {
                UseSkill(button, profile, eventData != null ? eventData.position : (Vector2)Input.mousePosition);
            }
        }

        private void BeginTargeting(RPGSkillButton button, ButtonProfile profile)
        {
            CancelTargeting(false);

            isTargeting = true;
            targetingButton = button;
            targetingProfile = profile;

            targetingButton.StopRollingHighlightAnimation();
            targetingButton.SetAsSelected(true);

            if (targetArrowGraphic != null)
            {
                targetArrowGraphic.transform.SetAsLastSibling();
                targetArrowGraphic.SetAimingVisible(true);
            }

            UpdateTargetArrow();
        }

        private void HandleTargetArrowClicked(PointerEventData eventData)
        {
            if (!isTargeting)
                return;

            if (eventData == null)
            {
                ConfirmTargeting(Input.mousePosition);
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                CancelTargeting(false);
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            ConfirmTargeting(eventData.position);
        }

        private void ConfirmTargeting(Vector2 screenPosition)
        {
            RPGSkillButton usedButton = targetingButton;
            ButtonProfile usedProfile = targetingProfile;

            isTargeting = false;
            targetingButton = null;

            if (targetArrowGraphic != null)
            {
                targetArrowGraphic.SetAimingVisible(false);
                targetArrowGraphic.PlayClickPulse(canvas, screenPosition);
            }

            UseSkill(usedButton, usedProfile, screenPosition);
        }

        private void CancelTargeting(bool playPulse)
        {
            if (targetingButton != null)
                targetingButton.SetAsDeselected(true);

            isTargeting = false;
            targetingButton = null;

            if (targetArrowGraphic != null)
                targetArrowGraphic.SetAimingVisible(false);

            if (playPulse && targetArrowGraphic != null)
                targetArrowGraphic.PlayClickPulse(canvas, Input.mousePosition);
        }

        private void UseSkill(RPGSkillButton button, ButtonProfile profile, Vector2 screenPosition)
        {
            if (button == null)
                return;

            button.SetAsDeselected(true);

            if (profile.hasCooldown)
            {
                StartManagedCooldown(button, profile.cooldownDuration);
            }
            else
            {
                if (stopReadyHighlightWhenSkillUsed)
                    button.StopRollingHighlightAnimation();
            }

            PublishGlobalBuffIfRequested(button, profile);

            SimpleSoundManager.Play(skillUseSound);
        }


        private void PublishGlobalBuffIfRequested(RPGSkillButton button, ButtonProfile profile)
        {
            if (!profile.publishGlobalBuff)
                return;

            if (button == null)
                return;

            if (myManager == null)
            {
                Debug.LogWarning(
                    $"{nameof(DemoRPGSkillHotbarController)} on {gameObject.name}: Cannot publish global buff because {nameof(myManager)} is not assigned.",
                    this
                );
                return;
            }

            float duration = Mathf.Max(0.01f, profile.cooldownDuration);
            myManager.PublishGlobalBuff(button, duration);
        }

        private void StartManagedCooldown(RPGSkillButton button, float duration)
        {
            if (button == null)
                return;

            float safeDuration = Mathf.Max(0.01f, duration);

            RemoveManagedCooldown(button);

            if (stopReadyHighlightWhenSkillUsed)
                button.StopRollingHighlightAnimation();

            button.StartCooldown(safeDuration);
            SetManagedCooldownText(button, safeDuration);

            RuntimeCooldown runtime = new RuntimeCooldown
            {
                button = button,
                duration = safeDuration,
                remaining = safeDuration,
                readyHighlightPlayed = false
            };

            activeCooldowns.Add(runtime);
        }

        private void TickCooldowns(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            for (int i = activeCooldowns.Count - 1; i >= 0; i--)
            {
                RuntimeCooldown runtime = activeCooldowns[i];

                if (runtime == null || runtime.button == null)
                {
                    activeCooldowns.RemoveAt(i);
                    continue;
                }

                runtime.remaining -= deltaTime;
                runtime.remaining = Mathf.Max(0f, runtime.remaining);

                runtime.button.TickCooldown(deltaTime);

                if (runtime.remaining > 0f)
                {
                    SetManagedCooldownText(runtime.button, runtime.remaining);
                    continue;
                }

                runtime.button.FinishCooldown();
                ClearManagedCooldownText(runtime.button);

                if (!runtime.readyHighlightPlayed)
                {
                    runtime.readyHighlightPlayed = true;
                    runtime.button.StartRollingHighlightAnimation();
                }

                activeCooldowns.RemoveAt(i);
            }
        }

        private void SetManagedCooldownText(RPGSkillButton button, float remaining)
        {
            if (button == null)
                return;

            if (!showCooldownTimerText || remaining <= 0f)
            {
                ClearManagedCooldownText(button);
                return;
            }

            button.EnableCooldownTextLabel();

            string value = Mathf.Max(0f, remaining).ToString("0.0", CultureInfo.InvariantCulture);

            if (!string.IsNullOrWhiteSpace(cooldownTimerMspaceTag))
                button.SetCooldownTextLabel($"<mspace={cooldownTimerMspaceTag}>{value}</mspace>");
            else
                button.SetCooldownTextLabel(value);
        }

        private void ClearManagedCooldownText(RPGSkillButton button)
        {
            if (button == null)
                return;

            button.SetCooldownTextLabel(string.Empty);
            button.DisableCooldownTextLabel();
        }

        private void ClearManagedCooldowns()
        {
            for (int i = 0; i < activeCooldowns.Count; i++)
            {
                RuntimeCooldown runtime = activeCooldowns[i];

                if (runtime == null || runtime.button == null)
                    continue;

                runtime.button.FinishCooldown();
                runtime.button.StopRollingHighlightAnimation();
                ClearManagedCooldownText(runtime.button);
            }

            activeCooldowns.Clear();
        }

        private void RemoveManagedCooldown(RPGSkillButton button)
        {
            if (button == null)
                return;

            for (int i = activeCooldowns.Count - 1; i >= 0; i--)
            {
                RuntimeCooldown runtime = activeCooldowns[i];

                if (runtime == null || runtime.button == null)
                {
                    activeCooldowns.RemoveAt(i);
                    continue;
                }

                if (runtime.button == button)
                {
                    ClearManagedCooldownText(runtime.button);
                    activeCooldowns.RemoveAt(i);
                }
            }
        }

        private void UpdateTargetArrow()
        {
            if (targetArrowGraphic == null)
                return;

            if (targetingButton == null)
                return;

            RectTransform sourceRect = targetingButton.myRectTransform;

            if (sourceRect == null)
                sourceRect = targetingButton.transform as RectTransform;

            if (sourceRect == null)
                return;

            Vector3 worldCenter = sourceRect.TransformPoint(sourceRect.rect.center);
            Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(), worldCenter);
            Vector2 endScreen = Input.mousePosition;

            targetArrowGraphic.SetScreenPoints(canvas, startScreen, endScreen);
        }

        private bool TryGetButtonProfile(RPGSkillButton button, out ButtonProfile profile)
        {
            profile = new ButtonProfile();

            if (button == null)
                return false;

            if (hotBars == null)
                return false;

            if (currentHotBarIndex < 0 || currentHotBarIndex >= hotBars.Length)
                return false;

            HotbarSkillSet set = hotBars[currentHotBarIndex];

            if (set == null)
                return false;

            int index = set.buttons.IndexOf(button);

            if (index < 0)
                return false;

            if (index >= set.buttonProfiles.Count)
            {
                Debug.LogWarning($"No ButtonProfile assigned for {button.name} in hotbar {currentHotBarIndex}.", this);
                return false;
            }

            profile = set.buttonProfiles[index];

            if (profile.cooldownDuration <= 0f)
                profile.cooldownDuration = 1f;

            return true;
        }

        private void SubscribeToButtons()
        {
            UnsubscribeFromButtons();

            if (hotBars != null)
            {
                for (int i = 0; i < hotBars.Length; i++)
                {
                    HotbarSkillSet set = hotBars[i];

                    if (set == null)
                        continue;

                    for (int b = 0; b < set.buttons.Count; b++)
                    {
                        RPGSkillButton button = set.buttons[b];

                        if (button == null)
                            continue;

                        button.OnPointerUpEvent += HandleSkillButtonPointerUp;
                    }
                }
            }

            if (upArrowButton != null)
                upArrowButton.OnButtonActivatedClicked += HandleUpArrowClicked;

            if (downArrow != null)
                downArrow.OnButtonActivatedClicked += HandleDownArrowClicked;
        }

        private void UnsubscribeFromButtons()
        {
            if (hotBars != null)
            {
                for (int i = 0; i < hotBars.Length; i++)
                {
                    HotbarSkillSet set = hotBars[i];

                    if (set == null)
                        continue;

                    for (int b = 0; b < set.buttons.Count; b++)
                    {
                        RPGSkillButton button = set.buttons[b];

                        if (button == null)
                            continue;

                        button.OnPointerUpEvent -= HandleSkillButtonPointerUp;
                    }
                }
            }

            if (upArrowButton != null)
                upArrowButton.OnButtonActivatedClicked -= HandleUpArrowClicked;

            if (downArrow != null)
                downArrow.OnButtonActivatedClicked -= HandleDownArrowClicked;
        }

        private Camera GetCanvasCamera()
        {
            if (canvas == null)
                return null;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera;
        }

        private void EnsureHotBarsExist()
        {
            if (hotBars == null || hotBars.Length != HotBarCount)
            {
                HotbarSkillSet[] newArray = new HotbarSkillSet[HotBarCount];

                if (hotBars != null)
                {
                    int copyCount = Mathf.Min(hotBars.Length, newArray.Length);

                    for (int i = 0; i < copyCount; i++)
                        newArray[i] = hotBars[i];
                }

                hotBars = newArray;
            }

            for (int i = 0; i < hotBars.Length; i++)
            {
                if (hotBars[i] == null)
                    hotBars[i] = new HotbarSkillSet();

                if (hotBars[i].buttons == null)
                    hotBars[i].buttons = new List<RPGSkillButton>();

                if (hotBars[i].buttonProfiles == null)
                    hotBars[i].buttonProfiles = new List<ButtonProfile>();
            }
        }
    }
}
