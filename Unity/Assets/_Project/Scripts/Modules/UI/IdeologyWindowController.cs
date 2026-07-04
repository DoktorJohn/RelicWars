using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using UnityEngine;
using Project.Modules.UI;
using Project.Network.Manager;
using Project.Modules.WorldPlayer;
using Assets.Scripts.Domain.State;
using Project.Scripts.Domain.DTOs;
using Assets._Project.Scripts.Domain.Enums;
using System.Collections.Generic;
using Project.Modules.City;

namespace Assets._Project.Scripts.Modules.UI
{
    public partial class IdeologyWindowController : BaseWindow
    {
        protected override string WindowName => "Ideology";
        protected override string VisualContainerName => "Ideology-Window-MainContainer";
        protected override string HeaderName => "Ideology-Window-Header";

        // UI Referencer - Overview
        private Label _labelIdeologyName;
        private Label _labelIdeologyDescription;
        private Label _labelAvailablePoints;
        private Label _labelPointsProduction;

        // UI Referencer - Grid Container 
        private VisualElement _focusGridContainer;

        [Header("Template Configuration")]
        [SerializeField] private VisualTreeAsset _focusCardTemplate;

        private Guid _currentActiveCityId;
        private double _currentAvailablePoints;
        private int _requestVersion;

        // Liste til at holde styr på vores nedtællinger
        private List<Coroutine> _activeTimers = new List<Coroutine>();

        public override void OnOpen(object dataPayload)
        {
            var version = BeginDeferredOpen();
            _requestVersion = version;
            InitializeUserInterfaceReferences();

            if (NetworkManager.Instance == null)
            {
                CompleteDeferredOpen(version);
                return;
            }

            _currentActiveCityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (_currentActiveCityId == Guid.Empty)
            {
                CompleteDeferredOpen(version);
                return;
            }

            if (WorldPlayerStateManager.Instance != null)
            {
                WorldPlayerStateManager.Instance.OnEconomyStateChanged += HandleEconomyStateChanged;

                HandleEconomyStateChanged(WorldPlayerStateManager.Instance.CurrentEconomy);
            }

            RequestAndRenderIdeologyData(version);
        }

        private void OnDisable()
        {
            InvalidateDeferredOpen();
            if (WorldPlayerStateManager.Instance != null)
            {
                WorldPlayerStateManager.Instance.OnEconomyStateChanged -= HandleEconomyStateChanged;
            }
            StopAllActiveTimers();
        }

        private void InitializeUserInterfaceReferences()
        {
            var closeWindowButton = Root.Q<Button>("Header-Close-Button");
            if (closeWindowButton != null)
            {
                closeWindowButton.clicked -= Close;
                closeWindowButton.clicked += Close;
            }

            _labelIdeologyName = Root.Q<Label>("Lbl-IdeologyName");
            _labelIdeologyDescription = Root.Q<Label>("Lbl-IdeologyDescription");
            _labelAvailablePoints = Root.Q<Label>("Lbl-AvailablePoints");
            _labelPointsProduction = Root.Q<Label>("Lbl-PointsProduction");

            _focusGridContainer = Root.Q<VisualElement>("Focus-Grid-Container");
        }

        private void HandleEconomyStateChanged(WorldPlayerState state)
        {
            _currentAvailablePoints = state.IdeologyFocusPointsAmount;

            if (_labelAvailablePoints != null)
                _labelAvailablePoints.text = $"{_currentAvailablePoints:N0} POINTS";

            if (_labelPointsProduction != null)
                _labelPointsProduction.text = $"+{state.IdeologyFocusPointsProductionPerHour:N1} / HR";

            if (_focusGridContainer != null)
            {
                var allEnactButtons = _focusGridContainer.Query<Button>("Btn-Enact").ToList();
                foreach (var btn in allEnactButtons)
                {
                    if (btn.userData is FocusButtonState focusState)
                    {
                        btn.SetEnabled(focusState.IsAvailable && _currentAvailablePoints >= focusState.Cost);
                    }
                }
            }
        }

        private void RequestAndRenderIdeologyData(int version)
        {
            if (_focusGridContainer != null) _focusGridContainer.Clear();
            StopAllActiveTimers();

            string token = NetworkManager.Instance.JwtToken;
            Guid? cityId = NetworkManager.Instance.ActiveCityId;

            StartCoroutine(NetworkManager.Instance.IdeologyFocus.GetIdeologyOverview(cityId ?? Guid.Empty, token, (overviewData) =>
            {
                if (!isActiveAndEnabled || version != _requestVersion)
                {
                    return;
                }

                if (overviewData != null && string.IsNullOrEmpty(overviewData.Message))
                {
                    RenderOverviewSection(overviewData.IdeologyDTO);
                    PopulateFocusGrid(overviewData.IdeologyFocuses);
                    CompleteDeferredOpen(version);
                }
                else
                {
                    Debug.LogError($"[IdeologyWindow] Error loading data: {overviewData?.Message}");
                    CompleteDeferredOpen(version);
                }
            }));
        }

    }
}
