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
        private Button _backToLoginButton;

        [Header("Data Skabelon")]
        [SerializeField] private VisualTreeAsset _worldEntryTemplate;

        [Header("Scene Konfiguration")]
        [SerializeField] private string _nextGameplaySceneName = "CityViewScene";
        [SerializeField] private string _ideologySelectionSceneName = "IdeologySelectionScene";
        [SerializeField] private string _loginSceneName = "LoginScene";

        private void OnEnable()
        {
            var uiDocumentComponent = GetComponent<UIDocument>();
            if (uiDocumentComponent == null) return;

            _rootVisualElement = uiDocumentComponent.rootVisualElement;

            InitializeUserInterfaceElements();
            SynchronizePlayerIdentityDisplay();
            StartAvailableWorldsLoadingProcess();
        }

        private void OnDisable()
        {
            StopAllCoroutines();

            if (_backToLoginButton != null)
            {
                _backToLoginButton.clicked -= HandleBackToLoginRequested;
            }
        }

        private void InitializeUserInterfaceElements()
        {
            _worldListScrollView = _rootVisualElement.Q<ScrollView>("Scroll-World-List");
            _playerNameLabel = _rootVisualElement.Q<Label>("Label-Player-Name");
            _backToLoginButton = _rootVisualElement.Q<Button>("Button-Back-To-Login");

            if (_backToLoginButton != null)
            {
                _backToLoginButton.clicked -= HandleBackToLoginRequested;
                _backToLoginButton.clicked += HandleBackToLoginRequested;
            }

            if (NetworkManager.Instance == null)
            {
                Debug.LogError("[WorldSelection] NetworkManager session not found. Return to Bootstrap.");
            }
        }

        private void HandleBackToLoginRequested()
        {
            NetworkManager.Instance?.ClearSession();
            SceneManager.LoadScene(_loginSceneName);
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
                if (!isActiveAndEnabled)
                {
                    return;
                }

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
            if (_worldListScrollView == null || _worldEntryTemplate == null)
            {
                return;
            }

            _worldListScrollView.Clear();

            foreach (var worldData in activeWorlds)
            {
                VisualElement worldEntryInstance = _worldEntryTemplate.CloneTree();

                // Konfigurer Labels
                Label nameLabel = worldEntryInstance.Q<Label>("World-Name");
                Label statsLabel = worldEntryInstance.Q<Label>("World-Stats");
                Button enterButton = worldEntryInstance.Q<Button>("Button-Enter");

                nameLabel.text = worldData.WorldName;
                statsLabel.text = $"Players: {worldData.CurrentPlayerCount}";

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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[WorldSelection] Attempting to join realm: {worldIdentifier}");
#endif

            if (NetworkManager.Instance == null)
            {
                return;
            }

            // Vi modtager nu både succes-status OG den valgte ideologi
            NetworkManager.Instance.JoinWorld(worldIdentifier, (isJoinSuccessful, selectedIdeology) =>
            {
                if (!isActiveAndEnabled)
                {
                    return;
                }

                if (isJoinSuccessful)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"[WorldSelection] Join successful. Ideology is: {selectedIdeology}");
#endif

                    // LOGIK: Hvis spilleren ikke har valgt en ideologi endnu, send dem til valg-scenen
                    if (selectedIdeology == IdeologyTypeEnum.None)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.Log("[WorldSelection] New player detected. Redirecting to Ideology Selection.");
#endif
                        SceneManager.LoadScene(_ideologySelectionSceneName);
                    }
                    else
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.Log("[WorldSelection] Returning player. Proceeding to City View.");
#endif
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
