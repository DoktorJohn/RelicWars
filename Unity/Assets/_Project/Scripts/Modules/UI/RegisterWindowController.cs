using Project.Network.Manager;
using Sunvale.AncientRomeUI.Buttons;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project.Modules.Auth
{
    public class RegisterWindowController : MonoBehaviour
    {
        private const string DefaultRegisterButtonText = "CREATE ACCOUNT";
        private const string LoadingRegisterButtonText = "CREATING...";
        private const string GenericFailureMessage = "Unable to reach the realm. Please try again.";

        [Header("Scene Configuration")]
        [SerializeField] private string _loginSceneName = "LoginScene";
        [SerializeField] private string _worldSelectionSceneName = "WorldSelectionScene";

        [Header("Scene-authored Register View")]
        [SerializeField] private TMP_InputField _usernameInput;
        [SerializeField] private TMP_InputField _emailInput;
        [SerializeField] private TMP_InputField _passwordInput;
        [SerializeField] private CarvedPressButton _registerButton;
        [SerializeField] private CarvedPressButton _backToLoginButton;
        [SerializeField] private TMP_Text _statusText;

        private bool _isUiActive;
        private bool _isRequestInFlight;
        private int _lifecycleVersion;

        private void OnEnable()
        {
            EnsureEventSystem();

            if (!HasCompleteViewBinding())
            {
                Debug.LogError("[RegisterWindow] Register Menu view references are incomplete.", this);
                enabled = false;
                return;
            }

            _isUiActive = true;
            _isRequestInFlight = false;
            _lifecycleVersion++;

            ConfigureInputFields();
            RegisterUserInteractionCallbacks();
            ClearValidationFeedback();
            SetStatusFeedback(string.Empty, false);
            SetRegisterButtonText(DefaultRegisterButtonText);
            SetInteractionState(true);

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
            UnregisterUserInteractionCallbacks();
        }

        private bool HasCompleteViewBinding()
        {
            return _usernameInput != null
                && _emailInput != null
                && _passwordInput != null
                && _registerButton != null
                && _backToLoginButton != null
                && _statusText != null;
        }

        private void ConfigureInputFields()
        {
            _usernameInput.contentType = TMP_InputField.ContentType.Standard;
            _usernameInput.lineType = TMP_InputField.LineType.SingleLine;
            _usernameInput.characterLimit = 20;

            _emailInput.contentType = TMP_InputField.ContentType.EmailAddress;
            _emailInput.lineType = TMP_InputField.LineType.SingleLine;
            _emailInput.characterLimit = 256;

            _passwordInput.contentType = TMP_InputField.ContentType.Password;
            _passwordInput.lineType = TMP_InputField.LineType.SingleLine;
            _passwordInput.characterLimit = 128;
            _passwordInput.asteriskChar = '\u2022';
            _passwordInput.ForceLabelUpdate();
        }

        private void RegisterUserInteractionCallbacks()
        {
            _registerButton.buttonActivatedClicked.AddListener(HandleRegistrationAttemptRequest);
            _backToLoginButton.buttonActivatedClicked.AddListener(HandleNavigateBackToLoginRequest);
            _usernameInput.onValueChanged.AddListener(HandleUsernameChanged);
            _emailInput.onValueChanged.AddListener(HandleEmailChanged);
            _passwordInput.onValueChanged.AddListener(HandlePasswordChanged);
            _passwordInput.onSubmit.AddListener(HandlePasswordSubmitted);
        }

        private void UnregisterUserInteractionCallbacks()
        {
            _registerButton?.buttonActivatedClicked.RemoveListener(HandleRegistrationAttemptRequest);
            _backToLoginButton?.buttonActivatedClicked.RemoveListener(HandleNavigateBackToLoginRequest);
            _usernameInput?.onValueChanged.RemoveListener(HandleUsernameChanged);
            _emailInput?.onValueChanged.RemoveListener(HandleEmailChanged);
            _passwordInput?.onValueChanged.RemoveListener(HandlePasswordChanged);
            _passwordInput?.onSubmit.RemoveListener(HandlePasswordSubmitted);
        }

        private void HandleRegistrationAttemptRequest()
        {
            if (_isRequestInFlight || !_isUiActive)
            {
                return;
            }

            string username = (_usernameInput.text ?? string.Empty).Trim();
            string email = (_emailInput.text ?? string.Empty).Trim();
            string password = _passwordInput.text ?? string.Empty;

            if (!ValidateForm(username, email, password))
            {
                return;
            }

            if (NetworkManager.Instance == null)
            {
                SetStatusFeedback(GenericFailureMessage, true);
                SetInteractionState(false);
                return;
            }

            _usernameInput.SetTextWithoutNotify(username);
            _emailInput.SetTextWithoutNotify(email);
            _isRequestInFlight = true;
            SetInteractionState(false);
            SetRegisterButtonText(LoadingRegisterButtonText);
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

                SetRegisterButtonText(DefaultRegisterButtonText);
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

        private void HandleUsernameChanged(string value)
        {
            ClearFieldError(_usernameInput);
        }

        private void HandleEmailChanged(string value)
        {
            ClearFieldError(_emailInput);
        }

        private void HandlePasswordChanged(string value)
        {
            ClearFieldError(_passwordInput);
        }

        private void HandlePasswordSubmitted(string value)
        {
            HandleRegistrationAttemptRequest();
        }

        private bool ValidateForm(string username, string email, string password)
        {
            ClearValidationFeedback();

            if (string.IsNullOrWhiteSpace(username))
            {
                return RejectField(_usernameInput, "Username is required.");
            }

            if (username.Length < 3 || username.Length > 20)
            {
                return RejectField(_usernameInput, "Username must contain 3 to 20 characters.");
            }

            if (!HasValidUsernameCharacters(username))
            {
                return RejectField(_usernameInput, "Use only letters, numbers, hyphens, and underscores.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                return RejectField(_emailInput, "Email is required.");
            }

            if (!HasValidEmailFormat(email))
            {
                return RejectField(_emailInput, "Enter a valid email address.");
            }

            if (string.IsNullOrEmpty(password))
            {
                return RejectField(_passwordInput, "Password is required.");
            }

            if (password.Length < 8)
            {
                return RejectField(_passwordInput, "Password must contain at least 8 characters.");
            }

            return true;
        }

        private bool RejectField(TMP_InputField field, string message)
        {
            ShowFieldError(field);
            SetStatusFeedback(message, true);
            FocusField(field);
            return false;
        }

        private static bool HasValidUsernameCharacters(string username)
        {
            foreach (char character in username)
            {
                bool isAsciiLetter = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z';
                bool isDigit = character >= '0' && character <= '9';
                if (!isAsciiLetter && !isDigit && character != '-' && character != '_')
                {
                    return false;
                }
            }

            return true;
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
            ClearFieldError(_usernameInput);
            ClearFieldError(_emailInput);
            ClearFieldError(_passwordInput);
        }

        private static void ShowFieldError(TMP_InputField field)
        {
            if (field?.image != null)
            {
                field.image.color = new Color(0.72f, 0.34f, 0.27f, 1f);
            }
        }

        private static void ClearFieldError(TMP_InputField field)
        {
            if (field?.image != null)
            {
                field.image.color = Color.white;
            }
        }

        private static void FocusField(TMP_InputField field)
        {
            field.Select();
            field.ActivateInputField();
        }

        private void SetInteractionState(bool isInteractable)
        {
            _usernameInput.interactable = isInteractable;
            _emailInput.interactable = isInteractable;
            _passwordInput.interactable = isInteractable;
            SetButtonInteraction(_registerButton, isInteractable);
            SetButtonInteraction(_backToLoginButton, isInteractable);
        }

        private static void SetButtonInteraction(CarvedPressButton button, bool isInteractable)
        {
            button.enabled = isInteractable;
            if (button.coreImage != null)
            {
                button.coreImage.raycastTarget = isInteractable;
            }
        }

        private void SetStatusFeedback(string message, bool isError)
        {
            _statusText.text = message;
            _statusText.color = isError
                ? new Color(0.62f, 0.10f, 0.07f, 1f)
                : new Color(0.33f, 0.25f, 0.19f, 0.9f);
        }

        private void SetRegisterButtonText(string value)
        {
            _registerButton.SetTextOnLabel(value);
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
