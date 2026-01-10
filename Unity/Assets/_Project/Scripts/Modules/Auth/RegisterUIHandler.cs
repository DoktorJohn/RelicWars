using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

namespace Project.Modules.Auth
{
    public class RegisterUIHandler : MonoBehaviour
    {
        [Header("UI Input Elementer")]
        [SerializeField] private TMP_InputField userNameInputField;
        [SerializeField] private TMP_InputField emailAddressInputField;
        [SerializeField] private TMP_InputField passwordInputField;

        [Header("UI Interaktions Elementer")]
        [SerializeField] private Button registerExecutionButton;
        [SerializeField] private Button backToLoginButton;
        [SerializeField] private TMP_Text statusFeedbackText;

        [Header("Scene Konfiguration")]
        [SerializeField] private string loginSceneName = "LoginScene";
        [SerializeField] private string worldSelectionSceneName = "WorldSelection";

        private void Start()
        {
            InitializeRegistrationUserInterface();
        }

        private void InitializeRegistrationUserInterface()
        {
            if (registerExecutionButton != null)
                registerExecutionButton.onClick.AddListener(HandleRegisterButtonClick);

            if (backToLoginButton != null)
                backToLoginButton.onClick.AddListener(() => SceneManager.LoadScene(loginSceneName));

            if (statusFeedbackText != null)
                statusFeedbackText.text = "Opret en ny profil for at starte.";
        }

        private void HandleRegisterButtonClick()
        {
            if (ApiService.Instance == null) return;

            string userName = userNameInputField.text;
            string email = emailAddressInputField.text;
            string password = passwordInputField.text;

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                UpdateStatusFeedback("Alle felter skal udfyldes.");
                return;
            }

            SetUserInterfaceInteractivity(false);
            UpdateStatusFeedback("Opretter profil...");

            StartCoroutine(ApiService.Instance.ExecutePlayerRegistrationRequest(email, userName, password, (success, message) =>
            {
                SetUserInterfaceInteractivity(true);
                UpdateStatusFeedback(message);

                if (success)
                {
                    Debug.Log("[RegisterUI] Registrering succesfuld. Navigerer videre.");
                    SceneManager.LoadScene(worldSelectionSceneName);
                }
            }));
        }

        private void SetUserInterfaceInteractivity(bool isInteractable)
        {
            if (registerExecutionButton != null) registerExecutionButton.interactable = isInteractable;
            if (backToLoginButton != null) backToLoginButton.interactable = isInteractable;
            userNameInputField.interactable = isInteractable;
            emailAddressInputField.interactable = isInteractable;
            passwordInputField.interactable = isInteractable;
        }

        private void UpdateStatusFeedback(string message)
        {
            if (statusFeedbackText != null) statusFeedbackText.text = message;
        }
    }
}