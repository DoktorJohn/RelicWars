using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.AncientRomeUI.Buttons;
using Sunvale.AncientRomeUI.Graphics;


namespace Sunvale.AncientRomeUI.Demos.StrategyTopDown
{
    public class DemoProvincePanelController : MonoBehaviour
    {
        public StrategyTopDownDemoController myManager;
        public DemoConstructionPanelController constructionPanel;

        [Header("Province Header")]
        public TextMeshProUGUI provinceNameTMP;

        [Header("Governor")]
        public Image governorPortraitImage;
        public TextMeshProUGUI governorTitleTMP;
        public TextMeshProUGUI governorNameTMP;
        public TextMeshProUGUI administrationTMP;
        public TextMeshProUGUI influenceTMP;
        public TextMeshProUGUI commandTMP;
        public TextMeshProUGUI treasuryTMP;

        [Header("Trade Routes")]
        public TextMeshProUGUI tradeRoutesHeaderTMP;
        public Sprite navalTradeRouteSprite;
        public Sprite landTradeRouteSprite;
        public List<DemoTradeRouteRowView> tradeRouteRows = new List<DemoTradeRouteRowView>();

        [Header("Public Order")]
        public TextMeshProUGUI publicOrderValueTMP;
        public SimpleFillBar publicOrderFillBar;

        [Header("Infrastructure")]
        public TextMeshProUGUI infrastructureValueTMP;
        public SimpleFillBar infrastructureFillBar;

        [Header("Garrison")]
        public List<DemoUnitCardView> garrisonCards = new List<DemoUnitCardView>();

        [Header("Buildings")]
        public List<BuildingSlotButton> buildingSlotButtons = new List<BuildingSlotButton>();

        [Header("Construction Queue")]
        public List<BuildingSlotButton> constructionQueueButtons = new List<BuildingSlotButton>();

        [NonSerialized] public DemoProvinceData currentProvince;

        private bool wasInitialized;

       

        private void OnDestroy()
        {
            UnregisterBuildingSlotEvents();
        }

        private void InnerInitialization()
        {
            if (wasInitialized)
                return;

            wasInitialized = true;
            RegisterBuildingSlotEvents();
        }

        public void InitializeForProvince(DemoProvinceData province)
        {
            InnerInitialization();
            currentProvince = province;

            if (province == null)
            {
                ClearPanel();
                return;
            }

            RefreshCurrentProvince();
        }

        public void RefreshCurrentProvince()
        {
            if (currentProvince == null)
            {
                ClearPanel();
                return;
            }

            EnsureBuildQueue(currentProvince);

            SetProvinceHeader(currentProvince);
            SetGovernor(currentProvince.governor);
            SetTradeRoutes(currentProvince);
            SetPublicOrder(currentProvince);
            SetInfrastructure(currentProvince);
            SetGarrison(currentProvince);
            SetBuildings(currentProvince);
            SetConstructionQueue(currentProvince);

            if (constructionPanel != null && constructionPanel.gameObject.activeSelf)
            {
                constructionPanel.Refresh();
            }
        }

        public bool NewBuildingConstruction(DemoProvinceBuildingType buildingType)
        {
            if (!CanQueueNewBuilding(buildingType, true))
                return false;

            AddConstructionQueueItem(
                buildingType,
                1,
                true,
                -1
            );

            RefreshCurrentProvince();
            return true;
        }

        public int GetRemainingBuildingSlots()
        {
            return GetRemainingBuildingSlots(currentProvince);
        }

        private int GetRemainingBuildingSlots(DemoProvinceData province)
        {
            if (province == null)
                return 0;

            int builtSlots = province.buildings != null ? province.buildings.Count : 0;
            int queuedNewBuildingSlots = CountQueuedNewBuildingSlots(province);

            return Mathf.Max(0, province.maxBuildingSlots - builtSlots - queuedNewBuildingSlots);
        }

        private int CountQueuedNewBuildingSlots(DemoProvinceData province)
        {
            if (province == null || province.buildQueue == null)
                return 0;

            int count = 0;

            for (int i = 0; i < province.buildQueue.Count; i++)
            {
                if (province.buildQueue[i].constructAsNewBuilding)
                    count++;
            }

            return count;
        }

        private void AddConstructionQueueItem(
            DemoProvinceBuildingType buildingType,
            int targetLevel,
            bool constructAsNewBuilding,
            int sourceBuildingIndex)
        {
            EnsureBuildQueue(currentProvince);

            DemoConstructionQueueItem item = new DemoConstructionQueueItem();
            item.buildingType = buildingType;
            item.targetLevel = Mathf.Clamp(targetLevel, 1, 4);
            item.turnsRemaining = TurnsForBuildingLevel(item.targetLevel);
            item.constructAsNewBuilding = constructAsNewBuilding;
            item.sourceBuildingIndex = sourceBuildingIndex;

            currentProvince.buildQueue.Add(item);
        }

        private bool CanQueueNewBuilding(DemoProvinceBuildingType buildingType, bool logFailure)
        {
            if (currentProvince == null)
                return false;

            if (buildingType == DemoProvinceBuildingType.noneExistingNull)
                return false;

            if (GetRemainingBuildingSlots(currentProvince) <= 0)
            {
                if (logFailure)
                    Debug.Log("No building slots available.");

                return false;
            }

            if (IsQueueFull(currentProvince))
            {
                if (logFailure)
                    Debug.Log("Construction queue is full.");

                return false;
            }

            return true;
        }

        private bool CanQueueUpgrade(DemoProvinceData province, int buildingIndex, DemoProvinceBuildingData building, bool logFailure)
        {
            if (province == null || building == null)
                return false;

            if (building.level >= 4)
            {
                if (logFailure)
                    Debug.Log("Building is already at max level.");

                return false;
            }

            if (IsQueueFull(province))
            {
                if (logFailure)
                    Debug.Log("Construction queue is full.");

                return false;
            }

            if (FindQueuedUpgradeIndexForBuildingSlot(province, buildingIndex) >= 0)
            {
                if (logFailure)
                    Debug.Log("That building already has an upgrade in the construction queue.");

                return false;
            }

            return true;
        }

        private bool IsQueueFull(DemoProvinceData province)
        {
            if (province == null)
                return true;

            EnsureBuildQueue(province);

            int queueLimit = constructionQueueButtons != null && constructionQueueButtons.Count > 0
                ? constructionQueueButtons.Count
                : 4;

            return province.buildQueue.Count >= queueLimit;
        }

        private void EnsureBuildQueue(DemoProvinceData province)
        {
            if (province.buildQueue == null)
            {
                province.buildQueue = new List<DemoConstructionQueueItem>();
            }
        }

        private void SetProvinceHeader(DemoProvinceData province)
        {
            SetTMP(provinceNameTMP, ToDisplayTitle(province.provinceName));
        }

        private void SetGovernor(DemoGovernorData governor)
        {
            if (governor == null)
            {
                SetImage(governorPortraitImage, null, false);
                SetTMP(governorTitleTMP, string.Empty);
                SetTMP(governorNameTMP, string.Empty);
                SetTMP(administrationTMP, string.Empty);
                SetTMP(influenceTMP, string.Empty);
                SetTMP(commandTMP, string.Empty);
                SetTMP(treasuryTMP, string.Empty);
                return;
            }

            SetImage(governorPortraitImage, governor.portraitSprite, false);

            string title = governor.mainTitle;

            if (!string.IsNullOrWhiteSpace(governor.extraTitle))
            {
                title += ", " + governor.extraTitle;
            }

            SetTMP(governorTitleTMP, title);
            SetTMP(governorNameTMP, governor.characterName);

            SetTMP(administrationTMP, governor.administration.ToString());
            SetTMP(influenceTMP, governor.influence.ToString());
            SetTMP(commandTMP, governor.command.ToString());
            SetTMP(treasuryTMP, governor.treasury.ToString());
        }

        private void SetTradeRoutes(DemoProvinceData province)
        {
            int routeCount = province.tradeRoutes != null ? province.tradeRoutes.Count : 0;
            int maxRoutes = Mathf.Max(0, province.maxTradeRoutes);

            SetTMP(tradeRoutesHeaderTMP, $"Trade routes {routeCount}/{maxRoutes}:");

            for (int i = 0; i < tradeRouteRows.Count; i++)
            {
                DemoTradeRouteRowView row = tradeRouteRows[i];

                if (row == null)
                    continue;

                if (province.tradeRoutes != null && i < province.tradeRoutes.Count)
                {
                    DemoTradeRouteData route = province.tradeRoutes[i];
                    Sprite routeSprite = GetTradeRouteSprite(route.routeType);

                    row.SetVisible(true);
                    row.SetData(route, routeSprite);
                }
                else
                {
                    row.SetVisible(false);
                }
            }
        }

        private void SetPublicOrder(DemoProvinceData province)
        {
            SetTMP(publicOrderValueTMP, FormatSignedValue(province.publicOrder));

            if (publicOrderFillBar != null)
            {
                publicOrderFillBar.SetNormalizedValue(province.publicOrder / 100f);
            }
        }

        private void SetInfrastructure(DemoProvinceData province)
        {
            SetTMP(infrastructureValueTMP, FormatSignedValue(province.infrastructure));

            if (infrastructureFillBar != null)
            {
                infrastructureFillBar.SetNormalizedValue(province.infrastructure / 100f);
            }
        }

        private void SetGarrison(DemoProvinceData province)
        {
            for (int i = 0; i < garrisonCards.Count; i++)
            {
                DemoUnitCardView card = garrisonCards[i];

                if (card == null)
                    continue;

                bool hasUnit = province.garrison != null && i < province.garrison.Count;
                card.gameObject.SetActive(hasUnit);

                if (!hasUnit)
                    continue;

                DemoGarrisonUnitData unit = province.garrison[i];

                card.SetIconSprite(unit.sprite);
                card.SetFillbarNormalized(unit.health / 100f);
                card.SetCounterLabel(unit.unitCount.ToString());
            }
        }

        private void SetBuildings(DemoProvinceData province)
        {
            EnsureBuildQueue(province);

            int builtSlots = province.buildings != null ? province.buildings.Count : 0;
            List<int> queuedNewBuildingIndexes = GetQueuedNewBuildingIndexes(province);

            for (int i = 0; i < buildingSlotButtons.Count; i++)
            {
                BuildingSlotButton slotButton = buildingSlotButtons[i];

                if (slotButton == null)
                    continue;

                slotButton.ClearRadial();

                bool hasRealBuilding = province.buildings != null && i < province.buildings.Count;

                if (hasRealBuilding)
                {
                    DemoProvinceBuildingData building = province.buildings[i];

                    slotButton.SetBuildingSlot(building.sprite, ToRoman(building.level));
                    slotButton.SetLevelLabelVisible(true);

                    int queuedUpgradeIndex = FindQueuedUpgradeIndexForBuildingSlot(province, i);

                    if (queuedUpgradeIndex >= 0)
                    {
                        ApplyQueueCooldownToSlot(slotButton, province, queuedUpgradeIndex);
                        slotButton.SetInteractable(false);
                    }
                    else
                    {
                        slotButton.SetInteractable(CanQueueUpgrade(province, i, building, false));
                    }

                    continue;
                }

                int queuedNewOffset = i - builtSlots;
                bool hasQueuedNewBuilding =
                    queuedNewOffset >= 0 &&
                    queuedNewOffset < queuedNewBuildingIndexes.Count;

                if (hasQueuedNewBuilding)
                {
                    int queueIndex = queuedNewBuildingIndexes[queuedNewOffset];
                    DemoConstructionQueueItem item = province.buildQueue[queueIndex];

                    int targetLevel = GetTargetLevel(item);
                    Sprite icon = ResolveBuildingSprite(item.buildingType, targetLevel);

                    slotButton.SetBuildingSlot(icon, ToRoman(targetLevel));
                    slotButton.SetLevelLabelVisible(true);
                    ApplyQueueCooldownToSlot(slotButton, province, queueIndex);
                    slotButton.SetInteractable(false);

                    continue;
                }

                slotButton.SetEmptySlotVisual();
                slotButton.SetLevelLabelVisible(false);

                bool canUseEmptySlot =
                    i < province.maxBuildingSlots &&
                    GetRemainingBuildingSlots(province) > 0 &&
                    !IsQueueFull(province);

                slotButton.SetInteractable(canUseEmptySlot);
            }
        }

        private void SetConstructionQueue(DemoProvinceData province)
        {
            EnsureBuildQueue(province);

            for (int i = 0; i < constructionQueueButtons.Count; i++)
            {
                BuildingSlotButton slotButton = constructionQueueButtons[i];

                if (slotButton == null)
                    continue;

                bool hasQueueItem = province.buildQueue != null && i < province.buildQueue.Count;

                if (hasQueueItem)
                {
                    DemoConstructionQueueItem item = province.buildQueue[i];

                    int targetLevel = GetTargetLevel(item);
                    Sprite icon = ResolveBuildingSprite(item.buildingType, targetLevel);

                    slotButton.SetBuildingSlot(icon, string.Empty);
                    slotButton.SetLevelLabelVisible(false);

                    ApplyQueueCooldownToSlot(slotButton, province, i);

                    slotButton.SetInteractable(false);
                }
                else
                {
                    slotButton.SetEmptySlotVisual();
                    slotButton.SetLevelLabelVisible(false);
                    slotButton.ClearRadial();
                    slotButton.SetInteractable(false);
                }
            }
        }

        private List<int> GetQueuedNewBuildingIndexes(DemoProvinceData province)
        {
            List<int> indexes = new List<int>();

            if (province == null || province.buildQueue == null)
                return indexes;

            for (int i = 0; i < province.buildQueue.Count; i++)
            {
                if (province.buildQueue[i].constructAsNewBuilding)
                {
                    indexes.Add(i);
                }
            }

            return indexes;
        }

        private int FindQueuedUpgradeIndexForBuildingSlot(DemoProvinceData province, int buildingIndex)
        {
            if (province == null || province.buildQueue == null)
                return -1;

            for (int i = 0; i < province.buildQueue.Count; i++)
            {
                DemoConstructionQueueItem item = province.buildQueue[i];

                if (item.constructAsNewBuilding)
                    continue;

                if (item.sourceBuildingIndex == buildingIndex)
                    return i;
            }

            return -1;
        }

        private void ApplyQueueCooldownToSlot(BuildingSlotButton slotButton, DemoProvinceData province, int queueIndex)
        {
            int turnsUntilFinished = GetQueueTurnsUntilFinished(province, queueIndex);
            float progress = GetQueueCooldownProgress01(province, queueIndex);

            slotButton.SetRadialProgress(progress, turnsUntilFinished.ToString(), true);
        }

        private int GetQueueTurnsUntilFinished(DemoProvinceData province, int queueIndex)
        {
            if (province == null || province.buildQueue == null)
                return 0;

            int turns = 0;
            int safeLastIndex = Mathf.Clamp(queueIndex, 0, province.buildQueue.Count - 1);

            for (int i = 0; i <= safeLastIndex; i++)
            {
                turns += GetSafeTurnsRemaining(province.buildQueue[i]);
            }

            return turns;
        }

        private float GetQueueCooldownProgress01(DemoProvinceData province, int queueIndex)
        {
            if (province == null || province.buildQueue == null)
                return 0f;

            int remainingTurns = 0;
            int totalTurns = 0;
            int safeLastIndex = Mathf.Clamp(queueIndex, 0, province.buildQueue.Count - 1);

            for (int i = 0; i <= safeLastIndex; i++)
            {
                DemoConstructionQueueItem item = province.buildQueue[i];

                remainingTurns += GetSafeTurnsRemaining(item);
                totalTurns += TurnsForBuildingLevel(GetTargetLevel(item));
            }

            if (totalTurns <= 0)
                return 0f;

            return Mathf.Clamp01((float)remainingTurns / totalTurns);
        }

        private int GetSafeTurnsRemaining(DemoConstructionQueueItem item)
        {
            if (item == null)
                return 0;

            int totalTurns = TurnsForBuildingLevel(GetTargetLevel(item));

            if (item.turnsRemaining <= 0)
                return totalTurns;

            return Mathf.Clamp(item.turnsRemaining, 1, totalTurns);
        }

        private int GetTargetLevel(DemoConstructionQueueItem item)
        {
            if (item == null)
                return 1;

            return Mathf.Clamp(item.targetLevel, 1, 4);
        }

        private void ClearPanel()
        {
            SetTMP(provinceNameTMP, string.Empty);

            SetGovernor(null);

            SetTMP(tradeRoutesHeaderTMP, "Trade routes 0/0:");

            for (int i = 0; i < tradeRouteRows.Count; i++)
            {
                if (tradeRouteRows[i] != null)
                    tradeRouteRows[i].SetVisible(false);
            }

            SetTMP(publicOrderValueTMP, string.Empty);
            SetTMP(infrastructureValueTMP, string.Empty);

            if (publicOrderFillBar != null)
                publicOrderFillBar.SetNormalizedValue(0f);

            if (infrastructureFillBar != null)
                infrastructureFillBar.SetNormalizedValue(0f);

            for (int i = 0; i < garrisonCards.Count; i++)
            {
                if (garrisonCards[i] != null)
                    garrisonCards[i].gameObject.SetActive(false);
            }

            ClearSlotButtons(buildingSlotButtons);
            ClearSlotButtons(constructionQueueButtons);
        }

        private void ClearSlotButtons(List<BuildingSlotButton> buttons)
        {
            if (buttons == null)
                return;

            for (int i = 0; i < buttons.Count; i++)
            {
                BuildingSlotButton button = buttons[i];

                if (button == null)
                    continue;

                button.SetEmptySlotVisual();
                button.SetLevelLabelVisible(false);
                button.ClearRadial();
                button.SetInteractable(false);
            }
        }

        private void RegisterBuildingSlotEvents()
        {
            RegisterButtonList(buildingSlotButtons);
            RegisterButtonList(constructionQueueButtons);
        }

        private void UnregisterBuildingSlotEvents()
        {
            UnregisterButtonList(buildingSlotButtons);
            UnregisterButtonList(constructionQueueButtons);
        }

        private void RegisterButtonList(List<BuildingSlotButton> buttons)
        {
            if (buttons == null)
                return;

            for (int i = 0; i < buttons.Count; i++)
            {
                BuildingSlotButton button = buttons[i];

                if (button == null)
                    continue;

                button.OnButtonActivatedClicked -= HandleBuildingSlotClicked;
                button.OnButtonActivatedClicked += HandleBuildingSlotClicked;
            }
        }

        private void UnregisterButtonList(List<BuildingSlotButton> buttons)
        {
            if (buttons == null)
                return;

            for (int i = 0; i < buttons.Count; i++)
            {
                BuildingSlotButton button = buttons[i];

                if (button == null)
                    continue;

                button.OnButtonActivatedClicked -= HandleBuildingSlotClicked;
            }
        }

        private void HandleBuildingSlotClicked(BuildingSlotButton clickedButton)
        {
            int constructionQueueIndex = constructionQueueButtons.IndexOf(clickedButton);

            if (constructionQueueIndex >= 0)
            {
                HandleConstructionQueueSlotClicked(constructionQueueIndex, clickedButton);
                return;
            }

            int buildingIndex = buildingSlotButtons.IndexOf(clickedButton);

            if (buildingIndex < 0)
                return;

            if (currentProvince != null && currentProvince.buildings != null && buildingIndex < currentProvince.buildings.Count)
            {
                HandleExistingBuildingSlotClicked(buildingIndex, clickedButton);
            }
            else
            {
                HandleEmptyBuildingSlotClicked(buildingIndex, clickedButton);
            }
        }

        private void HandleExistingBuildingSlotClicked(int buildingIndex, BuildingSlotButton clickedButton)
        {
            if (currentProvince == null || currentProvince.buildings == null)
                return;

            if (buildingIndex < 0 || buildingIndex >= currentProvince.buildings.Count)
                return;

            DemoProvinceBuildingData building = currentProvince.buildings[buildingIndex];

            if (!CanQueueUpgrade(currentProvince, buildingIndex, building, true))
                return;

            int targetLevel = Mathf.Clamp(building.level + 1, 1, 4);

            AddConstructionQueueItem(
                building.buildingType,
                targetLevel,
                false,
                buildingIndex
            );

            RefreshCurrentProvince();
        }

        private void HandleEmptyBuildingSlotClicked(int buildingIndex, BuildingSlotButton clickedButton)
        {
            if (currentProvince == null)
                return;

            if (GetRemainingBuildingSlots() <= 0)
                return;

            if (IsQueueFull(currentProvince))
                return;

            if (constructionPanel != null)
            {
                constructionPanel.Initialize();
            }
        }

        private void HandleConstructionQueueSlotClicked(int constructionQueueIndex, BuildingSlotButton clickedButton)
        {
            // Queue slots are display-only in this demo.
            // Construction progresses when the turn ends.
        }

        private Sprite ResolveBuildingSprite(DemoProvinceBuildingType buildingType, int level)
        {
            if (myManager == null)
                return null;

            return myManager.GetBuildingSpriteForLevel(buildingType, level);
        }

        private Sprite GetTradeRouteSprite(DemoTradeRouteType routeType)
        {
            switch (routeType)
            {
                case DemoTradeRouteType.Naval:
                    return navalTradeRouteSprite;

                case DemoTradeRouteType.Land:
                    return landTradeRouteSprite;

                default:
                    return null;
            }
        }

        private int TurnsForBuildingLevel(int level)
        {
            return Mathf.Clamp(level + 1, 2, 5);
        }

        private string ToDisplayTitle(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value.ToUpperInvariant();
        }

        private string FormatSignedValue(int value)
        {
            return value >= 0 ? "+" + value : value.ToString();
        }

        private string ToRoman(int value)
        {
            switch (Mathf.Clamp(value, 1, 4))
            {
                case 1:
                    return "I";

                case 2:
                    return "II";

                case 3:
                    return "III";

                case 4:
                    return "IV";

                default:
                    return string.Empty;
            }
        }

        private void SetTMP(TextMeshProUGUI tmp, string value)
        {
            if (tmp != null)
                tmp.SetText(value);
        }

        private void SetImage(Image image, Sprite sprite, bool disableWhenNull)
        {
            if (image == null)
                return;

            image.sprite = sprite;

            if (disableWhenNull)
                image.enabled = sprite != null;
        }
    }
}
