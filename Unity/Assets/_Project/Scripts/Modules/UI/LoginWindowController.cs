using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Project.Network.Manager;

namespace Project.Modules.Auth
{
    [RequireComponent(typeof(UIDocument))]
    public class LoginWindowController : MonoBehaviour
    {
        private VisualElement _rootVisualElement;

        private TextField _emailTextField;
        private TextField _passwordTextField;
        private Button _loginExecutionButton;
        private Button _navigateToRegisterButton;
        private Label _statusFeedbackLabel;

        [Header("Scene Konfiguration")]
        [SerializeField] private string _worldSelectionSceneName = "WorldSelection";
        [SerializeField] private string _registrationSceneName = "RegisterScene";

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            _rootVisualElement = uiDocument.rootVisualElement;

            InitializeUserInterfaceElements();
            RegisterUserInteractionCallbacks();
        }

        private void InitializeUserInterfaceElements()
        {
            _emailTextField = _rootVisualElement.Q<TextField>("Input-Email");
            _passwordTextField = _rootVisualElement.Q<TextField>("Input-Password");
            _loginExecutionButton = _rootVisualElement.Q<Button>("Button-Execute-Login");
            _navigateToRegisterButton = _rootVisualElement.Q<Button>("Button-Navigate-Register");
            _statusFeedbackLabel = _rootVisualElement.Q<Label>("Label-Status-Feedback");

            if (NetworkManager.Instance == null)
            {
                UpdateStatusFeedbackText("System Error: Network Manager not found.");
                SetInteractionState(false);
            }
        }

        private void RegisterUserInteractionCallbacks()
        {
            if (_loginExecutionButton != null)
                _loginExecutionButton.clicked += HandleLoginAttemptRequest;

            if (_navigateToRegisterButton != null)
                _navigateToRegisterButton.clicked += HandleNavigateToRegistrationRequest;
        }

        private void HandleLoginAttemptRequest()
        {
            string email = _emailTextField.value;
            string password = _passwordTextField.value;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                UpdateStatusFeedbackText("Please enter both email and password.");
                return;
            }

            SetInteractionState(false);
            UpdateStatusFeedbackText("Authenticating profile...");

            NetworkManager.Instance.AuthenticateUser(email, password, (isAuthenticationSuccessful) =>
            {
                if (isAuthenticationSuccessful)
                {
                    Debug.Log("[LoginWindow] Authentication successful. Transitioning to World Selection.");
                    SceneManager.LoadScene(_worldSelectionSceneName);
                }
                else
                {
                    SetInteractionState(true);
                    UpdateStatusFeedbackText("Login failed. Please check your credentials.");
                }
            });
        }

        private void HandleNavigateToRegistrationRequest()
        {
            SceneManager.LoadScene(_registrationSceneName);
        }

        private void SetInteractionState(bool isInteractable)
        {
            _emailTextField.SetEnabled(isInteractable);
            _passwordTextField.SetEnabled(isInteractable);
            _loginExecutionButton.SetEnabled(isInteractable);
            _navigateToRegisterButton.SetEnabled(isInteractable);
        }

        private void UpdateStatusFeedbackText(string message)
        {
            if (_statusFeedbackLabel != null)
                _statusFeedbackLabel.text = message;
        }
    }
}