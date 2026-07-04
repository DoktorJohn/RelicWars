using Project.Modules.City;
using Project.Modules.UI;
using Project.Network.Manager;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets._Project.Scripts.Modules.UI
{
    public partial class CityOverviewWindowController : BaseWindow
    {
        protected override string WindowName => "Overview";
        protected override string VisualContainerName => "Overview-Window-MainContainer";
        protected override string HeaderName => "Overview-Window-Header";

        private readonly Color _darkTextColor = new Color(0.17f, 0.11f, 0.06f, 1.0f);

        private VisualElement _economyResourceGridContainer;
        private VisualElement _populationUsageBarFill;
        private Label _labelPopulationStatisticalDetails;
        private Label _labelResistanceDetails;
        private Label _labelStatusTownHall;
        private Label _labelStatusBarracks;
        private int _requestVersion;

        public override void OnOpen(object dataPayload)
        {
            var version = BeginDeferredOpen();
            _requestVersion = version;
            InitializeUserInterfaceComponentReferences();

            if (Root != null)
            {
                Root.pickingMode = PickingMode.Ignore;
            }

            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnResourceStateChanged += HandleCityResourceStateCalculated;

                CityStateManager.Instance.OnBuildingQueueChanged += HandleAnyQueueChanged;
                CityStateManager.Instance.OnBarracksQueueChanged += HandleAnyQueueChanged;
                CityStateManager.Instance.OnStableQueueChanged += HandleAnyQueueChanged;
                CityStateManager.Instance.OnWorkshopQueueChanged += HandleAnyQueueChanged;

                UpdateCityUserInterfaceElements(CityStateManager.Instance.CurrentResources);
                UpdateAllActivityStatuses();
            }

            if (NetworkManager.Instance == null)
            {
                CompleteDeferredOpen(version);
                return;
            }

            Guid activeCityIdentifier = (dataPayload is Guid cityGuid)
                ? cityGuid
                : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;

            if (activeCityIdentifier == Guid.Empty)
            {
                CompleteDeferredOpen(version);
                return;
            }

            ExecuteCityOverviewDataRequest(activeCityIdentifier, version);
        }

        private void OnDisable()
        {
            InvalidateDeferredOpen();
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnResourceStateChanged -= HandleCityResourceStateCalculated;
                CityStateManager.Instance.OnBuildingQueueChanged -= HandleAnyQueueChanged;
                CityStateManager.Instance.OnBarracksQueueChanged -= HandleAnyQueueChanged;
                CityStateManager.Instance.OnStableQueueChanged -= HandleAnyQueueChanged;
                CityStateManager.Instance.OnWorkshopQueueChanged -= HandleAnyQueueChanged;
            }
        }

        private void InitializeUserInterfaceComponentReferences()
        {
            var headerCloseButton = Root.Q<Button>("Header-Close-Button");
            if (headerCloseButton != null)
            {
                headerCloseButton.clicked -= Close;
                headerCloseButton.clicked += Close;
            }

            _economyResourceGridContainer = Root.Q<VisualElement>("Economy-Grid-Container");

            _populationUsageBarFill = Root.Q<VisualElement>("Population-Bar-Used");
            _labelPopulationStatisticalDetails = Root.Q<Label>("Label-Pop-Details");
            _labelResistanceDetails = Root.Q<Label>("Label-Resistance-Details");
            _labelStatusTownHall = Root.Q<Label>("Status-TownHall");
            _labelStatusBarracks = Root.Q<Label>("Status-Barracks");
        }

        private void ExecuteCityOverviewDataRequest(Guid cityIdentifier, int version)
        {
            string authenticationToken = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.City.GetCityOverviewHUD(cityIdentifier, authenticationToken, (cityOverviewData) =>
            {
                if (!isActiveAndEnabled || version != _requestVersion)
                {
                    return;
                }

                if (cityOverviewData != null)
                {
                    PopulateUserInterfaceWithDataModel(cityOverviewData);
                }

                CompleteDeferredOpen(version);
            }));
        }
    }
}
