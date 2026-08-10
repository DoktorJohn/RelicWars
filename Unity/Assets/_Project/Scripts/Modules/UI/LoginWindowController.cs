using System.Collections;
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
    public class LoginWindowController : MonoBehaviour
    {
        private const string DefaultLoginButtonText = "ENTER WORLD";
        private const string LoadingLoginButtonText = "ENTERING...";
        private const string GenericFailureMessage = "Unable to reach the realm. Please try again.";

        [Header("Scene Configuration")]
        [SerializeField] private string _worldSelectionSceneName = "WorldSelectionScene";
        [SerializeField] private string _registrationSceneName = "RegisterScene";

        [Header("Scene-authored Login View")]
        [SerializeField] private TMP_InputField _emailInput;
        [SerializeField] private TMP_InputField _passwordInput;
        [SerializeField] private Toggle _rememberLoginToggle;
        [SerializeField] private CarvedPressButton _loginButton;
        [SerializeField] private CarvedPressButton _registerButton;
        [SerializeField] private TMP_Text _statusText;

        private bool _isUiActive;
        private bool _isRequestInFlight;
        private int _lifecycleVersion;

        private void OnEnable()
        {
            EnsureEventSystem();

            if (!HasCompleteViewBinding())
            {
                Debug.LogError("[LoginWindow] Login Menu view references are incomplete.", this);
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
            SetLoginButtonText(DefaultLoginButtonText);
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
            return _emailInput != null
                && _passwordInput != null
                && _rememberLoginToggle != null
                && _loginButton != null
                && _registerButton != null
                && _statusText != null;
        }

        private void ConfigureInputFields()
        {
            _emailInput.contentType = TMP_InputField.ContentType.EmailAddress;
            _emailInput.lineType = TMP_InputField.LineType.SingleLine;
            _emailInput.characterLimit = 256;

            _passwordInput.contentType = TMP_InputField.ContentType.Password;
            _passwordInput.lineType = TMP_InputField.LineType.SingleLine;
            _passwordInput.asteriskChar = '\u2022';
            _passwordInput.ForceLabelUpdate();
        }

        private void RegisterUserInteractionCallbacks()
        {
            _loginButton.buttonActivatedClicked.AddListener(HandleLoginAttemptRequest);
            _registerButton.buttonActivatedClicked.AddListener(HandleNavigateToRegistrationRequest);
            _emailInput.onValueChanged.AddListener(HandleEmailChanged);
            _passwordInput.onValueChanged.AddListener(HandlePasswordChanged);
            _passwordInput.onSubmit.AddListener(HandlePasswordSubmitted);
        }

        private void UnregisterUserInteractionCallbacks()
        {
            _loginButton?.buttonActivatedClicked.RemoveListener(HandleLoginAttemptRequest);
            _registerButton?.buttonActivatedClicked.RemoveListener(HandleNavigateToRegistrationRequest);
            _emailInput?.onValueChanged.RemoveListener(HandleEmailChanged);
            _passwordInput?.onValueChanged.RemoveListener(HandlePasswordChanged);
            _passwordInput?.onSubmit.RemoveListener(HandlePasswordSubmitted);
        }

        private void HandleLoginAttemptRequest()
        {
            if (_isRequestInFlight || !_isUiActive)
            {
                return;
            }

            string email = (_emailInput.text ?? string.Empty).Trim();
            string password = _passwordInput.text ?? string.Empty;

            if (!ValidateForm(email, password))
            {
                return;
            }

            if (NetworkManager.Instance == null)
            {
                SetStatusFeedback(GenericFailureMessage, true);
                SetInteractionState(false);
                return;
            }

            _emailInput.SetTextWithoutNotify(email);
            _isRequestInFlight = true;
            SetInteractionState(false);
            SetLoginButtonText(LoadingLoginButtonText);
            SetStatusFeedback("Authenticating your profile...", false);

            int requestVersion = _lifecycleVersion;
            NetworkManager.Instance.AuthenticateUser(email, password, _rememberLoginToggle.isOn, response =>
            {
                if (!_isUiActive || requestVersion != _lifecycleVersion)
                {
                    return;
                }

                _isRequestInFlight = false;
                if (response != null && response.IsAuthenticated)
                {
                    Debug.Log("[LoginWindow] Authentication successful. Transitioning to World Selection.");
                    StartCoroutine(LoadWorldSelectionSeamlessly());
                    return;
                }

                SetLoginButtonText(DefaultLoginButtonText);
                SetInteractionState(true);
                SetStatusFeedback(GetFailureMessage(response), true);
            });
        }

        private void HandleNavigateToRegistrationRequest()
        {
            if (!_isRequestInFlight)
            {
                SceneManager.LoadScene(_registrationSceneName);
            }
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
            HandleLoginAttemptRequest();
        }

        private bool ValidateForm(string email, string password)
        {
            ClearValidationFeedback();

            if (string.IsNullOrWhiteSpace(email))
            {
                ShowFieldError(_emailInput);
                SetStatusFeedback("Email is required.", true);
                FocusField(_emailInput);
                return false;
            }

            if (!HasValidEmailFormat(email))
            {
                ShowFieldError(_emailInput);
                SetStatusFeedback("Enter a valid email address.", true);
                FocusField(_emailInput);
                return false;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowFieldError(_passwordInput);
                SetStatusFeedback("Password is required.", true);
                FocusField(_passwordInput);
                return false;
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
            _emailInput.interactable = isInteractable;
            _passwordInput.interactable = isInteractable;
            _rememberLoginToggle.interactable = isInteractable;
            SetButtonInteraction(_loginButton, isInteractable);
            SetButtonInteraction(_registerButton, isInteractable);
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

        private void SetLoginButtonText(string value)
        {
            _loginButton.SetTextOnLabel(value);
        }

        private IEnumerator LoadWorldSelectionSeamlessly()
        {
            SetStatusFeedback("Opening your worlds...", false);

            AsyncOperation sceneLoadOperation = SceneManager.LoadSceneAsync(_worldSelectionSceneName);
            if (sceneLoadOperation == null)
            {
                _isRequestInFlight = false;
                SetLoginButtonText(DefaultLoginButtonText);
                SetInteractionState(true);
                SetStatusFeedback("Unable to open world selection. Please try again.", true);
                yield break;
            }

            sceneLoadOperation.allowSceneActivation = false;
            while (sceneLoadOperation.progress < 0.9f)
            {
                yield return null;
            }

            // Keep the completed login composition visible until World Selection is ready to render.
            yield return null;
            sceneLoadOperation.allowSceneActivation = true;
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
