using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Project.Network.Manager;
using Project.Network.Models;
using Project.Scripts.Domain.Enums;

namespace Project.Modules.WorldSelection
{
    [RequireComponent(typeof(UIDocument))]
    public class WorldSelectionWindowController : MonoBehaviour
    {
        private VisualElement _rootVisualElement;
        private ScrollView _worldListScrollView;
        private Label _playerNameLabel;

        [Header("Data Skabelon")]
        [SerializeField] private VisualTreeAsset _worldEntryTemplate;

        [Header("Scene Konfiguration")]
        [SerializeField] private string _nextGameplaySceneName = "CityViewScene";
        [SerializeField] private string _ideologySelectionSceneName = "IdeologySelectionScene";

        private void OnEnable()
        {
            var uiDocumentComponent = GetComponent<UIDocument>();
            if (uiDocumentComponent == null) return;

            _rootVisualElement = uiDocumentComponent.rootVisualElement;

            InitializeUserInterfaceElements();
            SynchronizePlayerIdentityDisplay();
            StartAvailableWorldsLoadingProcess();
        }

        private void InitializeUserInterfaceElements()
        {
            _worldListScrollView = _rootVisualElement.Q<ScrollView>("Scroll-World-List");
            _playerNameLabel = _rootVisualElement.Q<Label>("Label-Player-Name");

            if (NetworkManager.Instance == null)
            {
                Debug.LogError("[WorldSelection] NetworkManager session not found. Return to Bootstrap.");
            }
        }

        private void SynchronizePlayerIdentityDisplay()
        {
            if (NetworkManager.Instance != null && _playerNameLabel != null)
            {
                _playerNameLabel.text = NetworkManager.Instance.PlayerName;
            }
        }

        private void StartAvailableWorldsLoadingProcess()
        {
            if (NetworkManager.Instance == null) return;

            // Vi bruger coroutine her da GetAvailableWorlds returnerer IEnumerator i din arkitektur
            StartCoroutine(NetworkManager.Instance.World.GetAvailableWorlds((receivedWorldsList) =>
            {
                if (receivedWorldsList != null)
                {
                    PopulateWorldSelectionList(receivedWorldsList);
                }
                else
                {
                    Debug.LogWarning("[WorldSelection] No worlds received from server.");
                }
            }));
        }

        private void PopulateWorldSelectionList(List<WorldAvailableResponseDTO> activeWorlds)
        {
            _worldListScrollView.Clear();

            foreach (var worldData in activeWorlds)
            {
                VisualElement worldEntryInstance = _worldEntryTemplate.CloneTree();

                // Konfigurer Labels
                Label nameLabel = worldEntryInstance.Q<Label>("World-Name");
                Label statsLabel = worldEntryInstance.Q<Label>("World-Stats");
                Button enterButton = worldEntryInstance.Q<Button>("Button-Enter");

                nameLabel.text = worldData.WorldName;
                statsLabel.text = $"Players: {worldData.CurrentPlayerCount} / {worldData.MaxPlayerCapacity}";

                // Registrer Click Callback
                if (Guid.TryParse(worldData.WorldId, out Guid worldIdentifier))
                {
                    enterButton.clicked += () => HandleWorldSelectionRequest(worldIdentifier);
                }

                _worldListScrollView.Add(worldEntryInstance);
            }
        }

        private void HandleWorldSelectionRequest(Guid worldIdentifier)
        {
            Debug.Log($"[WorldSelection] Attempting to join realm: {worldIdentifier}");

            // Vi modtager nu både succes-status OG den valgte ideologi
            NetworkManager.Instance.JoinWorld(worldIdentifier, (isJoinSuccessful, selectedIdeology) =>
            {
                if (isJoinSuccessful)
                {
                    Debug.Log($"[WorldSelection] Join successful. Ideology is: {selectedIdeology}");

                    // LOGIK: Hvis spilleren ikke har valgt en ideologi endnu, send dem til valg-scenen
                    if (selectedIdeology == IdeologyTypeEnum.None)
                    {
                        Debug.Log("[WorldSelection] New player detected. Redirecting to Ideology Selection.");
                        SceneManager.LoadScene(_ideologySelectionSceneName);
                    }
                    else
                    {
                        Debug.Log("[WorldSelection] Returning player. Proceeding to City View.");
                        SceneManager.LoadScene(_nextGameplaySceneName);
                    }
                }
                else
                {
                    Debug.LogError("[WorldSelection] Failed to join realm.");
                }
            });
        }
    }
}