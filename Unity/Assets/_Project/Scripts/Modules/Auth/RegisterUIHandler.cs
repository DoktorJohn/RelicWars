using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using Project.Network.Manager;

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
            // Validering af NetworkManager
            if (NetworkManager.Instance == null)
            {
                Debug.LogError("[RegisterUI] NetworkManager mangler! Start fra Bootstrap scenen.");
                UpdateStatusFeedback("Systemfejl: Netværk ikke klar.");
                SetUserInterfaceInteractivity(false);
                return;
            }

            if (registerExecutionButton != null)
                registerExecutionButton.onClick.AddListener(HandleRegisterButtonClick);

            if (backToLoginButton != null)
                backToLoginButton.onClick.AddListener(() => SceneManager.LoadScene(loginSceneName));

            if (statusFeedbackText != null)
                statusFeedbackText.text = "Opret en ny profil for at starte.";
        }

        private void HandleRegisterButtonClick()
        {
            if (NetworkManager.Instance == null) return;

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

            // RETTELSE: Vi bruger nu NetworkManager.RegisterUser.
            // Vi fjerner StartCoroutine, da Manageren klarer det.
            NetworkManager.Instance.RegisterUser(email, userName, password, (success) =>
            {
                SetUserInterfaceInteractivity(true);

                if (success)
                {
                    Debug.Log("[RegisterUI] Registrering succesfuld. Navigerer videre.");
                    UpdateStatusFeedback("Succes! Logger ind...");
                    SceneManager.LoadScene(worldSelectionSceneName);
                }
                else
                {
                    // Da bool success kun er true/false, giver vi en generisk fejl her.
                    // Tjek Unity Console for den specifikke API fejl.
                    UpdateStatusFeedback("Registrering fejlede. Emailen kan være i brug.");
                    Debug.LogError("[RegisterUI] Registrering fejlede i backend.");
                }
            });
        }

        private void SetUserInterfaceInteractivity(bool isInteractable)
        {
            if (registerExecutionButton != null) registerExecutionButton.interactable = isInteractable;
            if (backToLoginButton != null) backToLoginButton.interactable = isInteractable;
            if (userNameInputField != null) userNameInputField.interactable = isInteractable;
            if (emailAddressInputField != null) emailAddressInputField.interactable = isInteractable;
            if (passwordInputField != null) passwordInputField.interactable = isInteractable;
        }

        private void UpdateStatusFeedback(string message)
        {
            if (statusFeedbackText != null) statusFeedbackText.text = message;
        }
    }
}