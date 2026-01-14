using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using Assets.Scripts.Domain.Enums;

namespace Project.Modules.UI
{
    public class GlobalWindowManager : MonoBehaviour
    {
        public static GlobalWindowManager Instance { get; private set; }

        [Header("Window Configuration")]
        [SerializeField] private List<WindowPrefabConfig> _windowPrefabs;

        // State tracking
        private Dictionary<WindowTypeEnum, BaseWindow> _openWindows = new Dictionary<WindowTypeEnum, BaseWindow>();
        private int _currentSortOrder = 100; // Start z-index

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // THIS IS KEY: It survives scene changes
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// The main entry point to open ANY window in the game.
        /// </summary>
        public void OpenWindow(WindowTypeEnum type, object payload = null)
        {
            // 1. Check if already open
            if (_openWindows.ContainsKey(type))
            {
                var existingWindow = _openWindows[type];
                existingWindow.Focus();
                existingWindow.OnOpen(payload); // Re-inject data if needed
                return;
            }

            // 2. Find Prefab
            var config = _windowPrefabs.FirstOrDefault(x => x.Type == type);
            if (config == null || config.Prefab == null)
            {
                Debug.LogError($"[WindowManager] No prefab found for window type: {type}");
                return;
            }

            // 3. Instantiate as Child of Manager (so it is also DontDestroyOnLoad)
            GameObject windowInstance = Instantiate(config.Prefab, transform);
            windowInstance.name = $"Window_{type}";

            // 4. Initialize
            var controller = windowInstance.GetComponent<BaseWindow>();
            if (controller != null)
            {
                controller.Initialize(this, type);
                controller.OnOpen(payload); // Inject Data
                _openWindows.Add(type, controller);
            }
            else
            {
                Debug.LogError($"[WindowManager] Prefab for {type} missing BaseWindow component!");
            }
        }

        public void CloseWindow(WindowTypeEnum type)
        {
            if (_openWindows.ContainsKey(type))
            {
                _openWindows.Remove(type);
            }
        }

        public void NotifyWindowFocused(BaseWindow window)
        {
            // Logic if you need to track the "Active" window
        }

        public int GetNextSortingOrder()
        {
            _currentSortOrder += 10;
            return _currentSortOrder;
        }
    }

    [Serializable]
    public class WindowPrefabConfig
    {
        public WindowTypeEnum Type;
        public GameObject Prefab; // Must contain UIDocument + SpecificWindowScript
    }
}