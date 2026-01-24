using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Project.Network.Manager;

namespace Project.Modules.Auth
{
    [RequireComponent(typeof(UIDocument))]
    public class RegisterWindowController : MonoBehaviour
    {
        private VisualElement _rootVisualElement;

        private TextField _usernameTextField;
        private TextField _emailTextField;
        private TextField _passwordTextField;
        private Button _registerExecutionButton;
        private Button _backToLoginButton;
        private Label _statusFeedbackLabel;

        [Header("Scene Konfiguration")]
        [SerializeField] private string _loginSceneName = "LoginScene";
        [SerializeField] private string _worldSelectionSceneName = "WorldSelection";

        private void OnEnable()
        {
            var uiDocumentComponent = GetComponent<UIDocument>();
            if (uiDocumentComponent == null) return;

            _rootVisualElement = uiDocumentComponent.rootVisualElement;

            InitializeUserInterfaceElements();
            RegisterUserInteractionCallbacks();
        }

        private void InitializeUserInterfaceElements()
        {
            _usernameTextField = _rootVisualElement.Q<TextField>("Input-Username");
            _emailTextField = _rootVisualElement.Q<TextField>("Input-Email");
            _passwordTextField = _rootVisualElement.Q<TextField>("Input-Password");
            _registerExecutionButton = _rootVisualElement.Q<Button>("Button-Execute-Register");
            _backToLoginButton = _rootVisualElement.Q<Button>("Button-Navigate-Login");
            _statusFeedbackLabel = _rootVisualElement.Q<Label>("Label-Status-Feedback");

            if (NetworkManager.Instance == null)
            {
                UpdateStatusFeedbackText("System Error: Network Manager not found.");
                SetInteractionState(false);
            }
        }

        private void RegisterUserInteractionCallbacks()
        {
            if (_registerExecutionButton != null)
                _registerExecutionButton.clicked += HandleRegistrationAttemptRequest;

            if (_backToLoginButton != null)
                _backToLoginButton.clicked += HandleNavigateBackToLoginRequest;
        }

        private void HandleRegistrationAttemptRequest()
        {
            string username = _usernameTextField.value;
            string email = _emailTextField.value;
            string password = _passwordTextField.value;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                UpdateStatusFeedbackText("All fields must be filled out.");
                return;
            }

            SetInteractionState(false);
            UpdateStatusFeedbackText("Creating your profile...");

            NetworkManager.Instance.RegisterUser(email, username, password, (isRegistrationSuccessful) =>
            {
                if (isRegistrationSuccessful)
                {
                    Debug.Log("[RegisterWindow] Registration successful. Redirecting to World Selection.");
                    UpdateStatusFeedbackText("Account created! Logging in...");
                    SceneManager.LoadScene(_worldSelectionSceneName);
                }
                else
                {
                    SetInteractionState(true);
                    UpdateStatusFeedbackText("Registration failed. Email might be in use.");
                }
            });
        }

        private void HandleNavigateBackToLoginRequest()
        {
            SceneManager.LoadScene(_loginSceneName);
        }

        private void SetInteractionState(bool isInteractable)
        {
            _usernameTextField.SetEnabled(isInteractable);
            _emailTextField.SetEnabled(isInteractable);
            _passwordTextField.SetEnabled(isInteractable);
            _registerExecutionButton.SetEnabled(isInteractable);
            _backToLoginButton.SetEnabled(isInteractable);
        }

        private void UpdateStatusFeedbackText(string message)
        {
            if (_statusFeedbackLabel != null)
                _statusFeedbackLabel.text = message;
        }
    }
}