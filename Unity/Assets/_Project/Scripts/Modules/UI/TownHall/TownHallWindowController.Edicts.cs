using System;
using System.Collections;
using Assets._Project.Scripts.Domain.Enums;
using Project.Modules.City;
using Project.Modules.UI;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.Modules.UI
{
    public partial class TownHallWindowController
    {
        private VisualElement _buildingsPanel, _edictsPanel;
        private ScrollView _edictList;
        private Label _edictStatus;
        private Button _buildingsTab, _edictsTab;
        private EdictOverviewDTO _edictOverview;
        private bool _edictRequestInFlight;
        private Coroutine _edictCountdown;

        private void InitializeEdictInterface()
        {
            _buildingsPanel = Root.Q<VisualElement>("TownHall-Buildings-Panel"); _edictsPanel = Root.Q<VisualElement>("TownHall-Edicts-Panel");
            _edictList = Root.Q<ScrollView>("TownHall-Edict-List"); _edictStatus = Root.Q<Label>("TownHall-Edict-Status");
            _buildingsTab = Root.Q<Button>("TownHall-Buildings-Tab"); _edictsTab = Root.Q<Button>("TownHall-Edicts-Tab");
            if (_buildingsTab != null) _buildingsTab.clicked += () => ShowEdictTab(false);
            if (_edictsTab != null) _edictsTab.clicked += () => ShowEdictTab(true);
            ShowEdictTab(false);
        }

        private void ShowEdictTab(bool show)
        {
            if (_buildingsPanel != null) _buildingsPanel.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;
            if (_edictsPanel != null) _edictsPanel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            _buildingsTab?.EnableInClassList("window-tab-active", !show);
            _edictsTab?.EnableInClassList("window-tab-active", show);
            if (show && _edictOverview == null) RequestEdicts();
        }

        private void RequestEdicts()
        {
            if (_edictRequestInFlight || NetworkManager.Instance == null) return;
            _edictRequestInFlight = true; WindowAsyncStateHelper.ShowLoading(_edictList, "Loading edicts...");
            StartCoroutine(NetworkManager.Instance.City.GetEdicts(_activeCityId, NetworkManager.Instance.JwtToken, result =>
            { _edictRequestInFlight = false; if (!isActiveAndEnabled) return; if (result == null) { WindowAsyncStateHelper.ShowError(_edictList, "Could not load edicts.", RequestEdicts); return; } RenderEdicts(result); }));
        }

        private void RenderEdicts(EdictOverviewDTO overview)
        {
            _edictOverview = overview; _edictList.Clear();
            foreach (var option in overview.Options)
            {
                var card = new VisualElement(); card.AddToClassList("edict-card");
                if (overview.ActiveEdict == option.EdictType) card.AddToClassList("edict-card-active");
                var header = new VisualElement(); header.AddToClassList("edict-card-header");
                var name = new Label(option.Name); name.AddToClassList("edict-name"); header.Add(name);
                if (overview.ActiveEdict == option.EdictType) { var active = new Label("ACTIVE"); active.AddToClassList("edict-active"); header.Add(active); }
                var meta = new Label($"IN USE {option.UsageCount}/{option.UsageLimit}"); meta.AddToClassList("edict-meta"); header.Add(meta);
                var action = new Button(() => Enact(option.EdictType)); action.AddToClassList("btn-global-base"); action.AddToClassList("btn-imperial-success"); action.AddToClassList("edict-action"); action.text = option.AvailabilityReason == EdictAvailabilityReasonEnum.AlreadyActive ? "ACTIVE" : option.AvailabilityReason == EdictAvailabilityReasonEnum.UsageLimitReached ? "LIMIT REACHED" : overview.ActiveEdict.HasValue ? "RE-ENACT" : "ENACT"; action.SetEnabled(option.CanEnact && !_edictRequestInFlight); header.Add(action); card.Add(header);
                var effects = new VisualElement(); effects.AddToClassList("edict-effects");
                var benefit = new Label($"+ {option.BenefitDescription}{(option.BenefitImplemented ? "" : "  • COMING SOON")}"); benefit.AddToClassList("edict-benefit"); effects.Add(benefit);
                var downside = new Label($"− {option.DownsideDescription}{(option.DownsideImplemented ? "" : "  • COMING SOON")}"); downside.AddToClassList("edict-downside"); effects.Add(downside); card.Add(effects);
                _edictList.Add(card);
            }
            UpdateEdictStatus(); if (_edictCountdown != null) StopCoroutine(_edictCountdown); _edictCountdown = StartCoroutine(EdictCountdown());
        }

        private void Enact(EdictTypeEnum type)
        {
            if (_edictRequestInFlight) return; _edictRequestInFlight = true; RenderEdicts(_edictOverview);
            StartCoroutine(NetworkManager.Instance.City.EnactEdict(_activeCityId, new EnactEdictRequestDTO { EdictType = type }, NetworkManager.Instance.JwtToken, result =>
            { _edictRequestInFlight = false; if (!isActiveAndEnabled) return; if (result == null) { _edictStatus.text = "Edict enactment failed. Refresh and try again."; RequestEdicts(); return; } RenderEdicts(result); RequestTownHallAvailableBuildingsRefresh(_activeCityId, _requestVersion, false); CityStateManager.Instance?.RequestImmediateRefresh(_activeCityId); }));
        }

        private IEnumerator EdictCountdown() { while (isActiveAndEnabled && _edictOverview != null) { UpdateEdictStatus(); if (_edictOverview.CooldownEndsAtUtc.HasValue && DateTime.UtcNow >= _edictOverview.CooldownEndsAtUtc.Value) { _edictOverview = null; RequestEdicts(); yield break; } yield return new WaitForSeconds(1); } }
        private void UpdateEdictStatus() { if (_edictStatus == null || _edictOverview == null) return; if (!_edictOverview.ActiveEdict.HasValue) { _edictStatus.text = "No edict enacted. Your first enactment is immediate."; return; } var activeOption = _edictOverview.Options.Find(option => option.EdictType == _edictOverview.ActiveEdict.Value); var activeName = activeOption?.Name ?? _edictOverview.ActiveEdict.Value.ToString(); var remaining = (_edictOverview.CooldownEndsAtUtc ?? DateTime.UtcNow) - DateTime.UtcNow; _edictStatus.text = remaining > TimeSpan.Zero ? $"ACTIVE: {activeName}  •  CHANGE AVAILABLE IN {remaining:hh\\:mm\\:ss}" : $"ACTIVE: {activeName}  •  READY TO CHANGE"; }
    }
}
