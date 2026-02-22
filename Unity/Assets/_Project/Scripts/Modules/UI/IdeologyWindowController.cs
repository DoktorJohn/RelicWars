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

namespace Assets._Project.Scripts.Modules.UI
{
    public class IdeologyWindowController : BaseWindow
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

        // Liste til at holde styr på vores nedtællinger
        private List<Coroutine> _activeTimers = new List<Coroutine>();

        public override void OnOpen(object dataPayload)
        {
            InitializeUserInterfaceReferences();

            _currentActiveCityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (_currentActiveCityId == Guid.Empty) return;

            // 3. Abonnement: Lyt til state manageren for løbende opdateringer
            if (WorldPlayerStateManager.Instance != null)
            {
                WorldPlayerStateManager.Instance.OnEconomyStateChanged += HandleEconomyStateChanged;

                // Kør den manuelt én gang for at få start-værdien med det samme
                HandleEconomyStateChanged(WorldPlayerStateManager.Instance.CurrentEconomy);
            }

            RequestAndRenderIdeologyData();
        }

        private void OnDisable()
        {
            // VIGTIGT: Afmeld event for at undgå memory leaks når vinduet lukkes
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

            // Dynamisk tjek af knapperne: Hvis vi får nok point, aktiveres 'Enact' automatisk
            if (_focusGridContainer != null)
            {
                var allEnactButtons = _focusGridContainer.Query<Button>("Btn-Enact").ToList();
                foreach (var btn in allEnactButtons)
                {
                    if (btn.userData is double cost)
                    {
                        btn.SetEnabled(_currentAvailablePoints >= cost);
                    }
                }
            }
        }

        private void RequestAndRenderIdeologyData()
        {
            if (_focusGridContainer != null) _focusGridContainer.Clear();
            StopAllActiveTimers(); // Stop gamle timers før vi bygger nye

            string token = NetworkManager.Instance.JwtToken;
            Guid? cityId = NetworkManager.Instance.ActiveCityId;

            StartCoroutine(NetworkManager.Instance.IdeologyFocus.GetIdeologyOverview(cityId ?? Guid.Empty, token, (overviewData) =>
            {
                if (overviewData != null && string.IsNullOrEmpty(overviewData.Message))
                {
                    RenderOverviewSection(overviewData.IdeologyDTO);
                    PopulateFocusGrid(overviewData.IdeologyFocuses);
                }
                else
                {
                    Debug.LogError($"[IdeologyWindow] Fejl ved hentning af data: {overviewData?.Message}");
                }
            }));
        }

        private void RenderOverviewSection(IdeologyDTO ideologyDto)
        {
            if (_labelIdeologyName != null)
                _labelIdeologyName.text = ideologyDto.Name.ToUpper();

            if (_labelIdeologyDescription != null)
                _labelIdeologyDescription.text = ideologyDto.Description;
        }

        private void PopulateFocusGrid(List<IdeologyFocusDTO> focuses)
        {
            if (_focusGridContainer == null || _focusCardTemplate == null) return;
            _focusGridContainer.Clear();

            foreach (var focus in focuses)
            {
                VisualElement cardInstance = _focusCardTemplate.Instantiate();
                VisualElement actualCard = cardInstance.Q<VisualElement>(null, "focus-card");

                var nameLbl = actualCard.Q<Label>("Card-Name");
                var descLbl = actualCard.Q<Label>("Card-Description");
                var costInfoLbl = actualCard.Q<Label>("Card-CostInfo");

                if (nameLbl != null) nameLbl.text = focus.Name.ToFriendlyName().ToUpper();
                if (descLbl != null) descLbl.text = focus.Description;

                var enactBtn = actualCard.Q<Button>("Btn-Enact");
                var statusLbl = actualCard.Q<Label>("Lbl-Status");

                // Gemmer prisen i knappen, så vi kan slå den til/fra i HandleResourceStateChanged
                enactBtn.userData = focus.IdeologyFocusPointCost;

                if (focus.AlreadyEnacted)
                {
                    enactBtn.style.display = DisplayStyle.None;
                    statusLbl.style.display = DisplayStyle.Flex;

                    if (costInfoLbl != null)
                        costInfoLbl.text = $"{focus.IdeologyFocusPointCost} PTS";

                    // KUN start nedtælling, hvis der er en udløbsdato (altså hvis ActiveTime ikke var null)
                    if (focus.ActiveTime.HasValue)
                    {
                        var timerCoroutine = StartCoroutine(CountdownTimerRoutine(statusLbl, focus.ExpirationTime));
                        _activeTimers.Add(timerCoroutine);
                    }
                    else
                    {
                        // Hvis den er null, er det en Instant buff. Den er allerede udført, så vi viser bare at den er brugt.
                        // (Hvis du hellere vil have knappen frem igen med det samme, skal du lave logik i backenden 
                        // der lader instant-focuses blive slettet med det samme).
                        statusLbl.text = "ENACTED";
                    }
                }
                else
                {
                    statusLbl.style.display = DisplayStyle.None;
                    enactBtn.style.display = DisplayStyle.Flex;

                    if (focus.ActiveTime.HasValue)
                    {
                        string timeString = "";
                        if (focus.ActiveTime.Value.TotalHours >= 1) timeString += $"{(int)focus.ActiveTime.Value.TotalHours}H ";
                        if (focus.ActiveTime.Value.Minutes > 0) timeString += $"{focus.ActiveTime.Value.Minutes}M";
                        timeString = timeString.Trim();

                        if (costInfoLbl != null)
                            costInfoLbl.text = $"{focus.IdeologyFocusPointCost} PTS | TIME: {timeString}";
                    }
                    else
                    {
                        // Instant Fokus der ikke er købt endnu
                        if (costInfoLbl != null)
                            costInfoLbl.text = $"{focus.IdeologyFocusPointCost} PTS | INSTANT";
                    }

                    enactBtn.SetEnabled(_currentAvailablePoints >= focus.IdeologyFocusPointCost);
                    enactBtn.clicked += () => ExecuteEnactFocus(focus.Name, enactBtn);
                }

                _focusGridContainer.Add(actualCard);
            }
        }

        private IEnumerator CountdownTimerRoutine(Label targetLabel, DateTime expirationTime)
        {
            while (true)
            {
                TimeSpan remaining = expirationTime - DateTime.UtcNow;

                if (remaining.TotalSeconds <= 0)
                {
                    targetLabel.text = "EXPIRED";
                    // Når den udløber, re-loader vi vinduet så buffen forsvinder og knappen vender tilbage
                    RequestAndRenderIdeologyData();
                    yield break;
                }

                // Format: ACTIVE \n 02:15:30
                targetLabel.text = string.Format("ACTIVE\n{0:D2}:{1:D2}:{2:D2}",
                    (int)remaining.TotalHours, remaining.Minutes, remaining.Seconds);

                yield return new WaitForSeconds(1f);
            }
        }

        private void StopAllActiveTimers()
        {
            foreach (var timer in _activeTimers)
            {
                if (timer != null) StopCoroutine(timer);
            }
            _activeTimers.Clear();
        }

        private void ExecuteEnactFocus(IdeologyFocusNameEnum focusName, Button clickedButton)
        {
            clickedButton.SetEnabled(false);
            string token = NetworkManager.Instance.JwtToken;

            // Hent prisen fra knappen, som vi gemte tidligere
            double pointCost = (double)clickedButton.userData;

            // 1. Træk pointene fra skærmen LOKALT og ØJEBLIKKELIGT
            if (WorldPlayerStateManager.Instance != null)
            {
                WorldPlayerStateManager.Instance.DeductResourcesLocally(0, 0, pointCost);
            }

            var requestDto = new IdeologyFocusRequestDTO();
            requestDto.CityId = _currentActiveCityId;
            requestDto.IdeologyFocusName = focusName;

            // 2. Send request til backend
            StartCoroutine(NetworkManager.Instance.IdeologyFocus.EnactIdeologyFocus(requestDto, token, (result) =>
            {
                if (result != null && result.Success)
                {
                    RequestAndRenderIdeologyData();
                    
                    if (Guid.TryParse(NetworkManager.Instance.WorldPlayerId, out Guid wpId) && WorldPlayerStateManager.Instance != null)
                    {
                        WorldPlayerStateManager.Instance.InitiateEconomyRefresh(wpId);
                    }
                }
                else
                {
                    if (WorldPlayerStateManager.Instance != null)
                    {
                        WorldPlayerStateManager.Instance.DeductResourcesLocally(0, 0, -pointCost);
                    }

                    Debug.LogError($"[IdeologyWindow] Enact failed: {result?.Message}");
                    clickedButton.SetEnabled(true);
                }
            }));
        }
    }
}
