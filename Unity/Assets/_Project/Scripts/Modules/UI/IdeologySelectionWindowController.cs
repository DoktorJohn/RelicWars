using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Project.Network.Manager;
using Project.Scripts.Domain.Enums;

namespace Project.Modules.IdeologySelection
{
    public class IdeologySelectionSceneController : MonoBehaviour
    {
        private const string DefaultInformationText = "The choice is reversible, but comes with great cost.";

        [Header("uGUI View")]
        [SerializeField] private GameObject _ideologySelectionWindowPrefab;
        [SerializeField] private Canvas _sceneCanvas;

        [Header("Scene Configuration")]
        [SerializeField] private string _nextGameplaySceneName = "CityViewScene";

        private readonly List<Button> _ideologyButtons = new List<Button>();
        private readonly List<UguiIdeologyCardHover> _cardHoverEffects = new List<UguiIdeologyCardHover>();
        private GameObject _canvasRoot;
        private GameObject _windowInstance;
        private bool _ownsCanvasRoot;
        private TMP_Text _informationText;
        private bool _isUiActive;
        private bool _isSelectionInFlight;
        private int _lifecycleVersion;

        private void OnEnable()
        {
            _isUiActive = true;
            _isSelectionInFlight = false;
            _lifecycleVersion++;

            EnsureEventSystem();

            if (_ideologySelectionWindowPrefab == null)
            {
                Debug.LogError("[IdeologySelection] IdeologySelectionWindow prefab is not assigned.", this);
                enabled = false;
                return;
            }

            CreateWindowView();
            BindIdeologyCard("Feudalism", IdeologyTypeEnum.Feudalism);
            BindIdeologyCard("Monarchy", IdeologyTypeEnum.Monarchy);
            BindIdeologyCard("Oligarchy", IdeologyTypeEnum.Oligarchy);
            BindIdeologyCard("Democracy", IdeologyTypeEnum.Democracy);
            BindIdeologyCard("Military junta", IdeologyTypeEnum.MilitaryJunta);

            Transform informationTransform = FindDescendant(_windowInstance.transform, "InfoTitle");
            _informationText = informationTransform != null ? informationTransform.GetComponent<TMP_Text>() : null;
            SetInformation(DefaultInformationText);

            if (_ideologyButtons.Count != 5)
            {
                Debug.LogError($"[IdeologySelection] Expected 5 ideology cards, but bound {_ideologyButtons.Count}.", this);
                SetInformation("The ideology selection view is incomplete.");
                SetInteractionState(false);
                return;
            }

            if (NetworkManager.Instance == null)
            {
                Debug.LogError("[IdeologySelection] NetworkManager instance not found. Return to Bootstrap.", this);
                SetInformation("The realm service is unavailable. Return to login and try again.");
                SetInteractionState(false);
            }
        }

        private void OnDisable()
        {
            _isUiActive = false;
            _isSelectionInFlight = false;
            _lifecycleVersion++;

            foreach (Button button in _ideologyButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                }
            }

            _ideologyButtons.Clear();
            _cardHoverEffects.Clear();
            _informationText = null;

            if (_windowInstance != null)
            {
                Destroy(_windowInstance);
                _windowInstance = null;
            }

            if (_ownsCanvasRoot && _canvasRoot != null)
            {
                Destroy(_canvasRoot);
            }

            _canvasRoot = null;
            _ownsCanvasRoot = false;
        }

        private void CreateWindowView()
        {
            Canvas canvas = _sceneCanvas;
            _ownsCanvasRoot = canvas == null;

            if (_ownsCanvasRoot)
            {
                _canvasRoot = new GameObject(
                    "IdeologySelectionCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));

                canvas = _canvasRoot.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = _canvasRoot.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }
            else
            {
                _canvasRoot = canvas.gameObject;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            _windowInstance = Instantiate(_ideologySelectionWindowPrefab, _canvasRoot.transform, false);
            RectTransform windowRect = _windowInstance.GetComponent<RectTransform>();
            if (windowRect != null)
            {
                windowRect.anchorMin = new Vector2(0.5f, 0.5f);
                windowRect.anchorMax = new Vector2(0.5f, 0.5f);
                windowRect.anchoredPosition = Vector2.zero;
                windowRect.localScale = Vector3.one;
            }
        }

        private void BindIdeologyCard(string cardName, IdeologyTypeEnum ideology)
        {
            Transform cardRoot = FindDescendant(_windowInstance.transform, cardName);
            if (cardRoot == null)
            {
                Debug.LogError($"[IdeologySelection] Card '{cardName}' was not found in the prefab.", this);
                return;
            }

            Image hitSurface = cardRoot.GetComponent<Image>();
            if (hitSurface == null)
            {
                hitSurface = cardRoot.gameObject.AddComponent<Image>();
                hitSurface.color = new Color(1f, 1f, 1f, 0f);
            }

            hitSurface.raycastTarget = true;

            Button button = cardRoot.GetComponent<Button>();
            if (button == null)
            {
                button = cardRoot.gameObject.AddComponent<Button>();
            }

            button.targetGraphic = hitSurface;
            button.transition = Selectable.Transition.None;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => HandleIdeologySelectionRequest(ideology));
            _ideologyButtons.Add(button);

            UguiIdeologyCardHover hoverEffect = cardRoot.GetComponent<UguiIdeologyCardHover>();
            if (hoverEffect == null)
            {
                hoverEffect = cardRoot.gameObject.AddComponent<UguiIdeologyCardHover>();
            }

            hoverEffect.Initialize(hitSurface);
            _cardHoverEffects.Add(hoverEffect);
        }

        private void HandleIdeologySelectionRequest(IdeologyTypeEnum selectedIdeology)
        {
            if (!_isUiActive || _isSelectionInFlight || NetworkManager.Instance == null)
            {
                return;
            }

            _isSelectionInFlight = true;
            SetInteractionState(false);
            SetInformation($"Enacting {FormatIdeologyName(selectedIdeology)}...");

            int requestVersion = _lifecycleVersion;
            NetworkManager.Instance.SelectIdeology(selectedIdeology, isSelectionSuccessful =>
            {
                if (!_isUiActive || requestVersion != _lifecycleVersion)
                {
                    return;
                }

                if (isSelectionSuccessful)
                {
                    Debug.Log($"[IdeologySelection] {selectedIdeology} confirmed. Loading {_nextGameplaySceneName}.");
                    SceneManager.LoadScene(_nextGameplaySceneName);
                    return;
                }

                _isSelectionInFlight = false;
                SetInformation("The realm rejected that ideology. Please try again.");
                SetInteractionState(true);
                Debug.LogError($"[IdeologySelection] Failed to enact {selectedIdeology}. Server rejected request.", this);
            });
        }

        private void SetInteractionState(bool isInteractable)
        {
            foreach (Button button in _ideologyButtons)
            {
                if (button != null)
                {
                    button.interactable = isInteractable;
                }
            }

            foreach (UguiIdeologyCardHover hoverEffect in _cardHoverEffects)
            {
                if (hoverEffect != null)
                {
                    hoverEffect.SetInteractable(isInteractable);
                }
            }
        }

        private void SetInformation(string value)
        {
            if (_informationText != null)
            {
                _informationText.text = value;
            }
        }

        private static string FormatIdeologyName(IdeologyTypeEnum ideology)
        {
            return ideology == IdeologyTypeEnum.MilitaryJunta ? "Military Junta" : ideology.ToString();
        }

        private static Transform FindDescendant(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            foreach (Transform descendant in root.GetComponentsInChildren<Transform>(true))
            {
                if (descendant.name == objectName)
                {
                    return descendant;
                }
            }

            return null;
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
