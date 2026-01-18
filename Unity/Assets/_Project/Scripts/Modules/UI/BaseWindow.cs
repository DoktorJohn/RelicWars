using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.Domain.Enums;

namespace Project.Modules.UI
{
    [RequireComponent(typeof(UIDocument))]
    public abstract class BaseWindow : MonoBehaviour
    {
        protected VisualElement Root;
        protected VisualElement MainContainer;
        protected UIDocument MyUiDocument;

        // Abstract: Every window must define its visual names
        protected abstract string WindowName { get; } // e.g., "TownHall"
        protected abstract string VisualContainerName { get; } // e.g., "TownHall-MainContainer"
        protected abstract string HeaderName { get; } // e.g., "TownHall-Header"

        public WindowTypeEnum Type { get; private set; }
        private GlobalWindowManager _manager;

        public void Initialize(GlobalWindowManager manager, WindowTypeEnum type)
        {
            _manager = manager;
            Type = type;

            MyUiDocument = GetComponent<UIDocument>();
            Root = MyUiDocument.rootVisualElement;
            MainContainer = Root.Q<VisualElement>(VisualContainerName);

            // 1. Setup Dragging
            var header = Root.Q<VisualElement>(HeaderName);
            if (header != null && MainContainer != null)
            {
                var dragger = new CityUserInterfaceWindowDragManipulator(MainContainer);
                header.AddManipulator(dragger);
            }

            // 2. Setup Close Button (Standardized naming recommended)
            var closeBtn = Root.Q<Button>($"{WindowName}-Close-Button");
            if (closeBtn != null) closeBtn.clicked += Close;

            // 3. Focus Logic (Clicking brings to front)
            MainContainer?.RegisterCallback<MouseDownEvent>(evt => Focus());

            // 4. Set Sort Order (Bring to front immediately)
            Focus();
        }

        public void Focus()
        {
            if (MyUiDocument != null)
            {
                _manager.NotifyWindowFocused(this);
                // We change the 'Sort Order' on the UIDocument component to bring it visually to the front
                MyUiDocument.sortingOrder = _manager.GetNextSortingOrder();
            }
        }

        public void Close()
        {
            _manager.CloseWindow(Type);
            Destroy(gameObject); // We destroy the GameObject to clean up
        }

        // Child classes implement this to receive data (e.g., CityId, EnemyId)
        public abstract void OnOpen(object dataPayload);
    }
}