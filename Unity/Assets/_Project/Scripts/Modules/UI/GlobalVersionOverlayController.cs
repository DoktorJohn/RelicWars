using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.Modules.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class GlobalVersionOverlayController : MonoBehaviour
    {
        private static GlobalVersionOverlayController _internalInstance;

        private UIDocument _overlayDocument;
        private Label _versionLabel;

        private void Awake()
        {
            // Singleton logik: Vi vil kun have ét overlay i hele spillet
            if (_internalInstance == null)
            {
                _internalInstance = this;
                DontDestroyOnLoad(gameObject);

                InitializeUserInterface();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeUserInterface()
        {
            _overlayDocument = GetComponent<UIDocument>();
            VisualElement root = _overlayDocument.rootVisualElement;

            // Find labelen i vores UXML
            _versionLabel = root.Q<Label>("GlobalOverlay-VersionLabel");

            if (_versionLabel != null)
            {
                // Henter automatisk versionen fra Project Settings -> Player -> Version
                string buildVersion = Application.version;
                _versionLabel.text = $"VERSION {buildVersion} - ALPHA BUILD";
            }
            else
            {
                Debug.LogWarning("[GlobalVersionOverlayController] Kunne ikke finde 'GlobalOverlay-VersionLabel' i UXML dokumentet.");
            }
        }
    }
}