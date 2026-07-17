using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Project.Modules.UI;
using Project.Network.Manager;

namespace Project.Modules.Auth
{
    [RequireComponent(typeof(UIDocument))]
    public class RegisterWindowController : MonoBehaviour
    {
        private const string DefaultRegisterButtonText = "CREATE ACCOUNT";
        private const string LoadingRegisterButtonText = "CREATING...";
        private const string GenericFailureMessage = "Unable to reach the realm. Please try again.";

        private VisualElement _rootVisualElement;
        private VisualElement _safeAreaElement;
        private TextField _usernameTextField;
        private TextField _emailTextField;
        private TextField _passwordTextField;
        private Label _usernameErrorLabel;
        private Label _emailErrorLabel;
        private Label _passwordErrorLabel;
        private Label _statusFeedbackLabel;
        private Button _registerExecutionButton;
        private Button _backToLoginButton;

        private bool _isUiActive;
        private bool _isRequestInFlight;
        private int _lifecycleVersion;

        [Header("Scene Configuration")]
        [SerializeField] private string _loginSceneName = "LoginScene";
        [SerializeField] private string _worldSelectionSceneName = "WorldSelectionScene";

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                return;
            }

            _rootVisualElement = uiDocument.rootVisualElement;
            _isUiActive = true;
            _isRequestInFlight = false;
            _lifecycleVersion++;

            ResponsiveUiStateManager.RegisterRoot(_rootVisualElement);
            InitializeUserInterfaceElements();
            RegisterUserInteractionCallbacks();
            ResponsiveUiStateManager.LayoutChanged += ApplyResponsiveLayout;
            ApplyResponsiveLayout(ResponsiveUiStateManager.CurrentSnapshot);

            if (NetworkManager.Instance == null)
            {
                SetStatusFeedback("The realm service is unavailable. Start from the Bootstrap scene.", true);
                SetInteractionState(false);
            }
        }

        private void OnDisable()
        {
            _isUiActive = false;
            _isRequestInFlight = false;
            _lifecycleVersion++;

            ResponsiveUiStateManager.LayoutChanged -= ApplyResponsiveLayout;
            ResponsiveUiStateManager.UnregisterRoot(_rootVisualElement);
            UnregisterUserInteractionCallbacks();
        }

        private void InitializeUserInterfaceElements()
        {
            _safeAreaElement = _rootVisualElement.Q<VisualElement>("Auth-SafeArea");
            _usernameTextField = _rootVisualElement.Q<TextField>("Input-Username");
            _emailTextField = _rootVisualElement.Q<TextField>("Input-Email");
            _passwordTextField = _rootVisualElement.Q<TextField>("Input-Password");
            _usernameErrorLabel = _rootVisualElement.Q<Label>("Error-Username");
            _emailErrorLabel = _rootVisualElement.Q<Label>("Error-Email");
            _passwordErrorLabel = _rootVisualElement.Q<Label>("Error-Password");
            _statusFeedbackLabel = _rootVisualElement.Q<Label>("Label-Status-Feedback");
            _registerExecutionButton = _rootVisualElement.Q<Button>("Button-Execute-Register");
            _backToLoginButton = _rootVisualElement.Q<Button>("Button-Navigate-Login");

            _usernameTextField.tabIndex = 0;
            _emailTextField.tabIndex = 1;
            _passwordTextField.tabIndex = 2;
            _registerExecutionButton.tabIndex = 3;
            _backToLoginButton.tabIndex = 4;

            _passwordTextField.isPasswordField = true;
            _registerExecutionButton.text = DefaultRegisterButtonText;
            ClearValidationFeedback();
            SetStatusFeedback(string.Empty, false);
        }

        private void RegisterUserInteractionCallbacks()
        {
            _registerExecutionButton.clicked += HandleRegistrationAttemptRequest;
            _backToLoginButton.clicked += HandleNavigateBackToLoginRequest;
            _usernameTextField.RegisterValueChangedCallback(HandleUsernameChanged);
            _emailTextField.RegisterValueChangedCallback(HandleEmailChanged);
            _passwordTextField.RegisterValueChangedCallback(HandlePasswordChanged);
            _passwordTextField.RegisterCallback<KeyDownEvent>(HandlePasswordKeyDown);
        }

        private void UnregisterUserInteractionCallbacks()
        {
            if (_registerExecutionButton != null)
            {
                _registerExecutionButton.clicked -= HandleRegistrationAttemptRequest;
            }

            if (_backToLoginButton != null)
            {
                _backToLoginButton.clicked -= HandleNavigateBackToLoginRequest;
            }

            _usernameTextField?.UnregisterValueChangedCallback(HandleUsernameChanged);
            _emailTextField?.UnregisterValueChangedCallback(HandleEmailChanged);
            _passwordTextField?.UnregisterValueChangedCallback(HandlePasswordChanged);
            _passwordTextField?.UnregisterCallback<KeyDownEvent>(HandlePasswordKeyDown);
        }

        private void HandleRegistrationAttemptRequest()
        {
            if (_isRequestInFlight || !_isUiActive)
            {
                return;
            }

            string username = (_usernameTextField.value ?? string.Empty).Trim();
            string email = (_emailTextField.value ?? string.Empty).Trim();
            string password = _passwordTextField.value ?? string.Empty;

            if (!ValidateForm(username, email, password))
            {
                SetStatusFeedback("Please correct the highlighted fields.", true);
                return;
            }

            if (NetworkManager.Instance == null)
            {
                SetStatusFeedback(GenericFailureMessage, true);
                SetInteractionState(false);
                return;
            }

            _usernameTextField.SetValueWithoutNotify(username);
            _emailTextField.SetValueWithoutNotify(email);
            _isRequestInFlight = true;
            SetInteractionState(false);
            _registerExecutionButton.text = LoadingRegisterButtonText;
            SetStatusFeedback("Creating your profile...", false);

            int requestVersion = _lifecycleVersion;
            NetworkManager.Instance.RegisterUser(email, username, password, response =>
            {
                if (!_isUiActive || requestVersion != _lifecycleVersion)
                {
                    return;
                }

                _isRequestInFlight = false;

                if (response != null && response.IsAuthenticated)
                {
                    Debug.Log("[RegisterWindow] Registration successful. Transitioning to World Selection.");
                    SceneManager.LoadScene(_worldSelectionSceneName);
                    return;
                }

                _registerExecutionButton.text = DefaultRegisterButtonText;
                SetInteractionState(true);
                SetStatusFeedback(GetFailureMessage(response), true);
            });
        }

        private void HandleNavigateBackToLoginRequest()
        {
            if (!_isRequestInFlight)
            {
                SceneManager.LoadScene(_loginSceneName);
            }
        }

        private void HandleUsernameChanged(ChangeEvent<string> changeEvent)
        {
            ClearFieldError(_usernameTextField, _usernameErrorLabel);
        }

        private void HandleEmailChanged(ChangeEvent<string> changeEvent)
        {
            ClearFieldError(_emailTextField, _emailErrorLabel);
        }

        private void HandlePasswordChanged(ChangeEvent<string> changeEvent)
        {
            ClearFieldError(_passwordTextField, _passwordErrorLabel);
        }

        private void HandlePasswordKeyDown(KeyDownEvent keyDownEvent)
        {
            if (keyDownEvent.keyCode != KeyCode.Return && keyDownEvent.keyCode != KeyCode.KeypadEnter)
            {
                return;
            }

            keyDownEvent.StopPropagation();
            HandleRegistrationAttemptRequest();
        }

        private bool ValidateForm(string username, string email, string password)
        {
            ClearValidationFeedback();
            bool isValid = true;

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowFieldError(_usernameTextField, _usernameErrorLabel, "Username is required.");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                ShowFieldError(_emailTextField, _emailErrorLabel, "Email is required.");
                isValid = false;
            }
            else if (!HasValidEmailFormat(email))
            {
                ShowFieldError(_emailTextField, _emailErrorLabel, "Enter a valid email address.");
                isValid = false;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowFieldError(_passwordTextField, _passwordErrorLabel, "Password is required.");
                isValid = false;
            }

            if (!isValid)
            {
                if (!string.IsNullOrEmpty(_usernameErrorLabel.text))
                {
                    _usernameTextField.Focus();
                }
                else if (!string.IsNullOrEmpty(_emailErrorLabel.text))
                {
                    _emailTextField.Focus();
                }
                else
                {
                    _passwordTextField.Focus();
                }
            }

            return isValid;
        }

        private static bool HasValidEmailFormat(string email)
        {
            int atIndex = email.IndexOf('@');
            int lastAtIndex = email.LastIndexOf('@');
            int dotIndex = email.LastIndexOf('.');

            return atIndex > 0
                && atIndex == lastAtIndex
                && dotIndex > atIndex + 1
                && dotIndex < email.Length - 1
                && email.IndexOf(' ') < 0;
        }

        private static string GetFailureMessage(AuthenticationResponse response)
        {
            return string.IsNullOrWhiteSpace(response?.FeedbackMessage)
                ? GenericFailureMessage
                : response.FeedbackMessage.Trim();
        }

        private void ClearValidationFeedback()
        {
            ClearFieldError(_usernameTextField, _usernameErrorLabel);
            ClearFieldError(_emailTextField, _emailErrorLabel);
            ClearFieldError(_passwordTextField, _passwordErrorLabel);
        }

        private static void ShowFieldError(TextField field, Label errorLabel, string message)
        {
            field.AddToClassList("auth-field-invalid");
            errorLabel.text = message;
        }

        private static void ClearFieldError(TextField field, Label errorLabel)
        {
            field?.RemoveFromClassList("auth-field-invalid");
            if (errorLabel != null)
            {
                errorLabel.text = string.Empty;
            }
        }

        private void SetInteractionState(bool isInteractable)
        {
            _usernameTextField?.SetEnabled(isInteractable);
            _emailTextField?.SetEnabled(isInteractable);
            _passwordTextField?.SetEnabled(isInteractable);
            _registerExecutionButton?.SetEnabled(isInteractable);
            _backToLoginButton?.SetEnabled(isInteractable);
        }

        private void SetStatusFeedback(string message, bool isError)
        {
            if (_statusFeedbackLabel == null)
            {
                return;
            }

            _statusFeedbackLabel.text = message;
            _statusFeedbackLabel.EnableInClassList("auth-status-error", isError);
        }

        private void ApplyResponsiveLayout(FrontendLayoutSnapshot snapshot)
        {
            if (_safeAreaElement == null)
            {
                return;
            }

            Vector4 safeAreaInsets = ResponsiveUiStateManager.GetSafeAreaInsets();
            _safeAreaElement.style.paddingLeft = safeAreaInsets.x;
            _safeAreaElement.style.paddingTop = safeAreaInsets.y;
            _safeAreaElement.style.paddingRight = safeAreaInsets.z;
            _safeAreaElement.style.paddingBottom = safeAreaInsets.w;
        }
    }
}
