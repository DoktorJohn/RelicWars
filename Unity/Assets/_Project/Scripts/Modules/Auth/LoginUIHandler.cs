using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

namespace Project.Modules.Auth
{
    public class LoginUIHandler : MonoBehaviour
    {
        [Header("UI Input Elementer")]
        [SerializeField] private TMP_InputField emailAddressInputField;
        [SerializeField] private TMP_InputField passwordInputField;

        [Header("UI Interaktions Elementer")]
        [SerializeField] private Button loginExecutionButton;
        [SerializeField] private Button navigateToRegistrationButton;
        [SerializeField] private TMP_Text statusFeedbackText;

        [Header("Scene Konfiguration")]
        [SerializeField] private string worldSelectionSceneName = "WorldSelection";
        [SerializeField] private string registrationSceneName = "RegisterScene";

        private void Start()
        {
            InitializeLoginUserInterface();
        }

        private void InitializeLoginUserInterface()
        {
            if (loginExecutionButton != null)
                loginExecutionButton.onClick.AddListener(HandleLoginButtonClick);

            if (navigateToRegistrationButton != null)
                navigateToRegistrationButton.onClick.AddListener(HandleNavigateToRegistrationClick);

            if (statusFeedbackText != null)
                statusFeedbackText.text = string.Empty;
        }

        private void HandleLoginButtonClick()
        {
            if (ApiService.Instance == null) return;

            string playerEmail = emailAddressInputField.text;
            string playerPassword = passwordInputField.text;

            if (string.IsNullOrEmpty(playerEmail) || string.IsNullOrEmpty(playerPassword))
            {
                UpdateStatusFeedback("Venligst indtast både email og adgangskode.");
                return;
            }

            SetUserInterfaceInteractivity(false);
            UpdateStatusFeedback("Godkender profiloplysninger...");

            StartCoroutine(ApiService.Instance.ExecutePlayerAuthenticationRequest(playerEmail, playerPassword, (authenticationResponse) =>
            {
                SetUserInterfaceInteractivity(true);

                if (authenticationResponse != null && authenticationResponse.IsAuthenticated)
                {
                    SceneManager.LoadScene(worldSelectionSceneName);
                }
                else
                {
                    UpdateStatusFeedback(authenticationResponse?.FeedbackMessage ?? "Forbindelsen fejlede.");
                }
            }));
        }

        private void HandleNavigateToRegistrationClick()
        {
            SceneManager.LoadScene(registrationSceneName);
        }

        private void SetUserInterfaceInteractivity(bool isInteractable)
        {
            if (loginExecutionButton != null) loginExecutionButton.interactable = isInteractable;
            if (navigateToRegistrationButton != null) navigateToRegistrationButton.interactable = isInteractable;
            if (emailAddressInputField != null) emailAddressInputField.interactable = isInteractable;
            if (passwordInputField != null) passwordInputField.interactable = isInteractable;
        }

        private void UpdateStatusFeedback(string message)
        {
            if (statusFeedbackText != null) statusFeedbackText.text = message;
        }
    }
}