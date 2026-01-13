using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using Project.Network.Models;
using Newtonsoft.Json;
using Assets.Scripts.Domain.Enums;
using Assets._Project.Scripts.Domain.DTOs;
using Project.Modules.CityView.UI.Manipulators; // Påkrævet for drag-logik

namespace Project.Modules.CityView.UI
{
    /// <summary>
    /// Controller ansvarlig for Senatets brugerflade. 
    /// Håndterer visning af bygningsliste, opgraderings-ordrer og vindues-manipulation (drag/luk).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SenateWindowController : MonoBehaviour
    {
        public static SenateWindowController Instance { get; private set; }

        [Header("UI Skabeloner")]
        [SerializeField] private VisualTreeAsset _buildingRowTemplateAsset;

        private VisualElement _rootVisualElement;
        private VisualElement _windowOverlayContainer;
        private VisualElement _windowMainContainer;
        private VisualElement _windowDragHeader;
        private ScrollView _buildingListScrollView;
        private Button _closeSenateWindowButton;

        private readonly string _apiBaseUrl = "https://127.0.0.1:55286/api/City";

        private void Awake()
        {
            InitializeSingletonPattern();
        }

        private void OnEnable()
        {
            InitializeUserInterfaceReferences();
        }

        private void InitializeSingletonPattern()
        {
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("[Senate-DEBUG] Singleton instans oprettet.");
            }
            else
            {
                Debug.LogWarning("[Senate-DEBUG] Dublet af SenateWindowController fundet og destrueret.");
                Destroy(gameObject);
            }
        }

        private void InitializeUserInterfaceReferences()
        {
            Debug.Log("[Senate-DEBUG] Initialiserer UI referencer og drag-logik.");

            _rootVisualElement = GetComponent<UIDocument>().rootVisualElement;

            // Find elementer baseret på de navne vi definerede i UXML
            _windowOverlayContainer = _rootVisualElement.Q<VisualElement>("Senate-Window-Overlay");
            _windowMainContainer = _rootVisualElement.Q<VisualElement>("Senate-Window-MainContainer");
            _windowDragHeader = _rootVisualElement.Q<VisualElement>("Senate-Window-Header");
            _buildingListScrollView = _rootVisualElement.Q<ScrollView>("Senate-Building-List");
            _closeSenateWindowButton = _rootVisualElement.Q<Button>("Senate-Close-Button");

            // Validering af kritiske referencer
            if (_windowOverlayContainer == null) Debug.LogError("[Senate-DEBUG] KRITISK: 'Senate-Window-Overlay' ikke fundet.");
            if (_windowMainContainer == null) Debug.LogError("[Senate-DEBUG] KRITISK: 'Senate-Window-MainContainer' ikke fundet.");
            if (_buildingListScrollView == null) Debug.LogError("[Senate-DEBUG] KRITISK: 'Senate-Building-List' ikke fundet.");

            // Bind lukkeknap
            if (_closeSenateWindowButton != null)
            {
                _closeSenateWindowButton.clicked += ExecuteHideSenateWindowSequence;
            }

            // Implementér Drag-funktionalitet på Headeren
            if (_windowDragHeader != null && _windowMainContainer != null)
            {
                var dragManipulator = new CityUserInterfaceWindowDragManipulator(_windowMainContainer);
                _windowDragHeader.AddManipulator(dragManipulator);
                Debug.Log("[Senate-DEBUG] Drag-manipulator tilføjet til Header.");
            }

            // Skjul vinduet som standard ved opstart
            ExecuteHideSenateWindowSequence();
        }

        public void OpenWindow()
        {
            Debug.Log("[Senate-DEBUG] OpenWindow kaldt.");

            if (_windowMainContainer == null)
            {
                Debug.LogError("[Senate-DEBUG] Kan ikke åbne vindue: MainContainer er NULL.");
                return;
            }

            _windowMainContainer.style.display = DisplayStyle.Flex;
            _windowMainContainer.BringToFront();

            Debug.Log("[Senate-DEBUG] Vindue sat til DisplayStyle.Flex.");

            StartCoroutine(FetchAvailableBuildingsFromBackendRoutine());
        }

        private IEnumerator FetchAvailableBuildingsFromBackendRoutine()
        {
            Debug.Log("[Senate-DEBUG] Starter netværks-anmodning for bygningsliste.");

            _buildingListScrollView.Clear();
            _buildingListScrollView.Add(new Label("Henter arkitekttegninger...") { style = { color = Color.white, marginTop = 20 } });

            Guid? activeCityId = ApiService.Instance.CurrentlySelectedActiveCityId;

            if (!activeCityId.HasValue)
            {
                Debug.LogError("[Senate-DEBUG] FEJL: ApiService har intet aktivt CityId.");
                yield break;
            }

            string requestUrl = $"{_apiBaseUrl}/{activeCityId.Value}/senate/available-buildings";
            Debug.Log($"[Senate-DEBUG] Sender GET til: {requestUrl}");

            using (UnityWebRequest webRequest = UnityWebRequest.Get(requestUrl))
            {
                webRequest.certificateHandler = new BypassCertificate();

                if (!string.IsNullOrEmpty(ApiService.Instance.AuthenticationJwtToken))
                {
                    webRequest.SetRequestHeader("Authorization", $"Bearer {ApiService.Instance.AuthenticationJwtToken}");
                    Debug.Log("[Senate-DEBUG] JWT Token tilføjet til anmodning.");
                }

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[Senate-DEBUG] Netværk succes! Modtog: " + webRequest.downloadHandler.text);
                    try
                    {
                        var availableBuildings = JsonConvert.DeserializeObject<List<AvailableBuildingDTO>>(webRequest.downloadHandler.text);
                        Debug.Log($"[Senate-DEBUG] Deserialisering succesfuld. Fandt {availableBuildings.Count} bygninger.");
                        PopulateBuildingListInScrollView(availableBuildings);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Senate-DEBUG] Deserialiserings-fejl: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"[Senate-DEBUG] Netværks-fejl ({webRequest.responseCode}): {webRequest.error}");
                }
            }
        }

        private void PopulateBuildingListInScrollView(List<AvailableBuildingDTO> buildingDataList)
        {
            Debug.Log("[Senate-DEBUG] Påbegynder generering af UI-rækker.");
            _buildingListScrollView.Clear();

            foreach (var building in buildingDataList)
            {
                if (_buildingRowTemplateAsset == null)
                {
                    Debug.LogError("[Senate-DEBUG] Template Asset mangler i Inspectoren!");
                    return;
                }

                VisualElement buildingRowInstance = _buildingRowTemplateAsset.Instantiate();

                buildingRowInstance.Q<Label>("Building-Name").text = building.BuildingName;
                buildingRowInstance.Q<Label>("Building-Level").text = $"Niveau {building.CurrentLevel}";
                buildingRowInstance.Q<Label>("Cost-Wood").text = $"W: {building.WoodCost:N0}";
                buildingRowInstance.Q<Label>("Cost-Stone").text = $"S: {building.StoneCost:N0}";
                buildingRowInstance.Q<Label>("Cost-Metal").text = $"M: {building.MetalCost:N0}";

                Button upgradeButton = buildingRowInstance.Q<Button>("Upgrade-Button");

                if (building.IsCurrentlyUpgrading)
                {
                    upgradeButton.text = "BYGGER...";
                    upgradeButton.SetEnabled(false);
                }
                else
                {
                    upgradeButton.SetEnabled(building.MeetsRequirements);
                    if (!building.CanAfford) upgradeButton.text = "FOR DYRT";
                }

                upgradeButton.clicked += () => {
                    Debug.Log($"[Senate-DEBUG] Bruger klikkede på OPGRADER for {building.BuildingType}");
                    ExecuteBuildingUpgradeRequest(building.BuildingType);
                };

                _buildingListScrollView.Add(buildingRowInstance);
            }
            Debug.Log("[Senate-DEBUG] UI-population færdig.");
        }

        private void ExecuteBuildingUpgradeRequest(BuildingTypeEnum buildingType)
        {
            Guid? activeCityId = ApiService.Instance.CurrentlySelectedActiveCityId;
            if (!activeCityId.HasValue) return;

            var upgradeRequest = new { CityId = activeCityId.Value, BuildingType = buildingType };
            StartCoroutine(PostBuildingUpgradeRoutine(upgradeRequest));
        }

        private IEnumerator PostBuildingUpgradeRoutine(object requestData)
        {
            string url = $"{_apiBaseUrl}/upgrade-building";
            string jsonData = JsonConvert.SerializeObject(requestData);
            Debug.Log($"[Senate-DEBUG] Sender POST til {url} med data: {jsonData}");

            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.certificateHandler = new BypassCertificate();

                if (!string.IsNullOrEmpty(ApiService.Instance.AuthenticationJwtToken))
                    webRequest.SetRequestHeader("Authorization", $"Bearer {ApiService.Instance.AuthenticationJwtToken}");

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[Senate-DEBUG] POST succes! Genindlæser liste.");
                    StartCoroutine(FetchAvailableBuildingsFromBackendRoutine());
                }
                else
                {
                    Debug.LogError($"[Senate-DEBUG] POST fejl ({webRequest.responseCode}): {webRequest.downloadHandler.text}");
                }
            }
        }

        public void ExecuteHideSenateWindowSequence()
        {
            if (_windowMainContainer == null) return;
            Debug.Log("[Senate-DEBUG] Lukker vindue.");
            _windowMainContainer.style.display = DisplayStyle.None;
        }

        // Shortcut til den gamle metode for kompatibilitet med eksisterende events
        public void CloseSenateWindow() => ExecuteHideSenateWindowSequence();
    }
}