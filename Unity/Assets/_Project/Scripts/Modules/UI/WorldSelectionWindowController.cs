using System;
using System.Collections.Generic;
using Project.Network.Manager;
using Project.Network.Models;
using Project.Scripts.Domain.Enums;
using Sunvale.AncientRomeUI.Buttons;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project.Modules.WorldSelection
{
    public class WorldSelectionWindowController : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private string _nextGameplaySceneName = "CityViewScene";
        [SerializeField] private string _ideologySelectionSceneName = "IdeologySelectionScene";
        [SerializeField] private string _loginSceneName = "LoginScene";

        [Header("Scene-authored World Selection View")]
        [SerializeField] private TMP_Text _playerNameText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private ScrollRect _worldListScrollRect;
        [SerializeField] private RectTransform _worldListContent;
        [SerializeField] private GameObject _worldEntryPrefab;
        [SerializeField] private CarvedPressButton _backToLoginButton;

        private readonly List<GameObject> _worldEntries = new List<GameObject>();
        private readonly List<CarvedPressButton> _enterRealmButtons = new List<CarvedPressButton>();
        private bool _isUiActive;
        private bool _isJoinInFlight;
        private int _lifecycleVersion;

        private void OnEnable()
        {
            EnsureEventSystem();

            if (!HasCompleteViewBinding())
            {
                Debug.LogError("[WorldSelection] World Selection Menu view references are incomplete.", this);
                enabled = false;
                return;
            }

            _isUiActive = true;
            _isJoinInFlight = false;
            _lifecycleVersion++;
            _backToLoginButton.buttonActivatedClicked.AddListener(HandleBackToLoginRequested);
            SynchronizePlayerIdentityDisplay();
            ClearWorldEntries();

            if (NetworkManager.Instance == null)
            {
                SetStatus("The realm service is unavailable. Return to login and try again.", true);
                SetBackButtonEnabled(true);
                return;
            }

            StartAvailableWorldsLoadingProcess();
        }

        private void OnDisable()
        {
            _isUiActive = false;
            _isJoinInFlight = false;
            _lifecycleVersion++;
            StopAllCoroutines();
            _backToLoginButton?.buttonActivatedClicked.RemoveListener(HandleBackToLoginRequested);
            ClearWorldEntries();
        }

        private bool HasCompleteViewBinding()
        {
            return _playerNameText != null
                && _statusText != null
                && _worldListScrollRect != null
                && _worldListContent != null
                && _worldEntryPrefab != null
                && _backToLoginButton != null;
        }

        private void HandleBackToLoginRequested()
        {
            if (_isJoinInFlight)
            {
                return;
            }

            NetworkManager.Instance?.ClearSession();
            SceneManager.LoadScene(_loginSceneName);
        }

        private void SynchronizePlayerIdentityDisplay()
        {
            _playerNameText.text = NetworkManager.Instance != null
                ? NetworkManager.Instance.PlayerName
                : string.Empty;
        }

        private void StartAvailableWorldsLoadingProcess()
        {
            SetStatus("Loading available worlds...", false);
            SetBackButtonEnabled(true);
            int requestVersion = _lifecycleVersion;

            StartCoroutine(NetworkManager.Instance.World.GetAvailableWorlds(receivedWorldsList =>
            {
                if (!_isUiActive || requestVersion != _lifecycleVersion)
                {
                    return;
                }

                if (receivedWorldsList == null)
                {
                    Debug.LogWarning("[WorldSelection] No worlds received from server.");
                    SetStatus("Unable to load worlds. Please return to login and try again.", true);
                    return;
                }

                PopulateWorldSelectionList(receivedWorldsList);
            }));
        }

        private void PopulateWorldSelectionList(List<WorldAvailableResponseDTO> activeWorlds)
        {
            ClearWorldEntries();

            if (activeWorlds.Count == 0)
            {
                SetStatus("No worlds are currently available.", false);
                return;
            }

            SetStatus("Choose a world to continue.", false);

            foreach (WorldAvailableResponseDTO worldData in activeWorlds)
            {
                GameObject worldEntry = Instantiate(_worldEntryPrefab, _worldListContent);
                worldEntry.SetActive(true);
                _worldEntries.Add(worldEntry);

                TMP_Text worldNameText = worldEntry.transform.Find("World Name")?.GetComponent<TMP_Text>();
                TMP_Text playerCountText = worldEntry.transform.Find("Player Count")?.GetComponent<TMP_Text>();
                CarvedPressButton enterRealmButton = worldEntry.GetComponentInChildren<CarvedPressButton>(true);
                if (worldNameText == null || playerCountText == null || enterRealmButton == null)
                {
                    Debug.LogError("[WorldSelection] World Entry prefab is missing a required binding.", worldEntry);
                    Destroy(worldEntry);
                    _worldEntries.Remove(worldEntry);
                    continue;
                }

                worldNameText.text = string.IsNullOrWhiteSpace(worldData.WorldName)
                    ? "Unnamed Realm"
                    : worldData.WorldName.Trim();
                playerCountText.text = $"Players: {worldData.CurrentPlayerCount}";
                enterRealmButton.SetTextOnLabel("ENTER REALM");
                _enterRealmButtons.Add(enterRealmButton);

                if (!Guid.TryParse(worldData.WorldId, out Guid worldIdentifier))
                {
                    Debug.LogError($"[WorldSelection] Realm '{worldData.WorldName}' has an invalid identifier.");
                    SetButtonEnabled(enterRealmButton, false);
                    continue;
                }

                enterRealmButton.buttonActivatedClicked.AddListener(
                    () => HandleWorldSelectionRequest(worldIdentifier));
            }

            Canvas.ForceUpdateCanvases();
            _worldListScrollRect.verticalNormalizedPosition = 1f;
        }

        private void ClearWorldEntries()
        {
            foreach (GameObject worldEntry in _worldEntries)
            {
                if (worldEntry != null)
                {
                    Destroy(worldEntry);
                }
            }

            _worldEntries.Clear();
            _enterRealmButtons.Clear();
        }

        private void HandleWorldSelectionRequest(Guid worldIdentifier)
        {
            if (_isJoinInFlight || !_isUiActive || NetworkManager.Instance == null)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[WorldSelection] Attempting to join realm: {worldIdentifier}");
#endif

            _isJoinInFlight = true;
            SetEnterButtonsEnabled(false);
            SetBackButtonEnabled(false);
            SetStatus("Entering world...", false);
            int requestVersion = _lifecycleVersion;

            NetworkManager.Instance.JoinWorld(worldIdentifier, (isJoinSuccessful, selectedIdeology) =>
            {
                if (!_isUiActive || requestVersion != _lifecycleVersion)
                {
                    return;
                }

                if (isJoinSuccessful)
                {
                    SceneManager.LoadScene(selectedIdeology == IdeologyTypeEnum.None
                        ? _ideologySelectionSceneName
                        : _nextGameplaySceneName);
                    return;
                }

                Debug.LogError("[WorldSelection] Failed to join realm.");
                _isJoinInFlight = false;
                SetEnterButtonsEnabled(true);
                SetBackButtonEnabled(true);
                SetStatus("Unable to enter that world. Please try again.", true);
            });
        }

        private void SetStatus(string message, bool isError)
        {
            _statusText.text = message;
            _statusText.color = isError
                ? new Color(0.62f, 0.10f, 0.07f, 1f)
                : new Color(0.33f, 0.25f, 0.19f, 0.9f);
        }

        private void SetEnterButtonsEnabled(bool isInteractable)
        {
            foreach (CarvedPressButton enterRealmButton in _enterRealmButtons)
            {
                SetButtonEnabled(enterRealmButton, isInteractable);
            }
        }

        private void SetBackButtonEnabled(bool isInteractable)
        {
            SetButtonEnabled(_backToLoginButton, isInteractable);
        }

        private static void SetButtonEnabled(CarvedPressButton button, bool isInteractable)
        {
            if (button == null)
            {
                return;
            }

            button.enabled = isInteractable;
            if (button.coreImage != null)
            {
                button.coreImage.raycastTarget = isInteractable;
            }
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }
    }
}
