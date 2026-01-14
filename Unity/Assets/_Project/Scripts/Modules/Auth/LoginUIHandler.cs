using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using Project.Network.Manager;

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
            // Tjekker om NetworkManager findes (Vigtigt i den nye arkitektur)
            if (NetworkManager.Instance == null)
            {
                Debug.LogError("[LoginUI] NetworkManager mangler! Start fra Bootstrap scenen.");
                UpdateStatusFeedback("System fejl: Netværk mangler.");
                SetUserInterfaceInteractivity(false);
                return;
            }

            if (loginExecutionButton != null)
                loginExecutionButton.onClick.AddListener(HandleLoginButtonClick);

            if (navigateToRegistrationButton != null)
                navigateToRegistrationButton.onClick.AddListener(HandleNavigateToRegistrationClick);

            if (statusFeedbackText != null)
                statusFeedbackText.text = string.Empty;
        }

        private void HandleLoginButtonClick()
        {
            if (NetworkManager.Instance == null) return;

            string playerEmail = emailAddressInputField.text;
            string playerPassword = passwordInputField.text;

            if (string.IsNullOrEmpty(playerEmail) || string.IsNullOrEmpty(playerPassword))
            {
                UpdateStatusFeedback("Venligst indtast både email og adgangskode.");
                return;
            }

            SetUserInterfaceInteractivity(false);
            UpdateStatusFeedback("Godkender profiloplysninger...");

            // RETTELSE: Vi bruger nu NetworkManager.Instance.AuthenticateUser.
            // Vi fjerner 'StartCoroutine', da Manageren håndterer det internt.
            NetworkManager.Instance.AuthenticateUser(playerEmail, playerPassword, (success) =>
            {
                // UI logik skal køre uanset udfaldet
                SetUserInterfaceInteractivity(true);

                if (success)
                {
                    Debug.Log("[LoginUI] Login godkendt. Skifter scene.");
                    SceneManager.LoadScene(worldSelectionSceneName);
                }
                else
                {
                    // Da NetworkManager returnerer bool, giver vi en generisk fejlbesked her.
                    // (Tjek Unity konsollen for den specifikke fejl fra ClientAuthService)
                    UpdateStatusFeedback("Login fejlede. Tjek email og kodeord.");
                }
            });
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