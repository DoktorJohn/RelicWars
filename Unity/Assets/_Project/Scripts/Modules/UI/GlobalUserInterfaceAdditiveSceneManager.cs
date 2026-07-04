using Project.Scripts.Modules.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Modules.UI
{
    public class GlobalUserInterfaceAdditiveSceneManager : MonoBehaviour
    {
        public static GlobalUserInterfaceAdditiveSceneManager Instance { get; private set; }

        [Header("Scene Navngivning")]
        [SerializeField] private string _topBarSceneName = "TopBarHUD";
        [SerializeField] private string _leftBarSceneName = "LeftSideBarHUD";
        [SerializeField] private string _UnitStackIdeologySceneName = "UnitStackIdeologyHUD";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                SceneManager.sceneLoaded += OnSceneLoadedInternal;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoadedInternal;
        }

        private void OnSceneLoadedInternal(Scene scene, LoadSceneMode mode)
        {
            // Ignorer HUD scenerne selv
            if (IsSceneAHudComponent(scene.name)) return;

            // Find konfigurationen i den nye scene
            UserInterfaceSceneConfiguration config = FindFirstObjectByType<UserInterfaceSceneConfiguration>();

            if (config == null)
            {
                Debug.LogWarning($"[UI-Manager] Ingen UserInterfaceSceneConfiguration fundet i {scene.name}. Stopper load.");
                return;
            }

            ExecuteInterfaceSyncProcess(config);
        }

        private void ExecuteInterfaceSyncProcess(UserInterfaceSceneConfiguration config)
        {
            // Vi håndterer hver bar individuelt: Load hvis nødvendig, Unload hvis ikke.
            ManageHudComponentStatus(_topBarSceneName, config.NeedTopBar);
            ManageHudComponentStatus(_leftBarSceneName, config.NeedLeftSideBar);
            ManageHudComponentStatus(_UnitStackIdeologySceneName, config.NeedUnitStackIdeology);
        }

        private void ManageHudComponentStatus(string hudSceneName, bool shouldBeLoaded)
        {
            bool isLoaded = IsSpecificSceneAlreadyLoaded(hudSceneName);

            if (shouldBeLoaded && !isLoaded)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[UI-Manager] Indlæser additivt: {hudSceneName}");
#endif
                SceneManager.LoadScene(hudSceneName, LoadSceneMode.Additive);
            }
            else if (!shouldBeLoaded && isLoaded)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[UI-Manager] Fjerner (Unloader): {hudSceneName}");
#endif
                SceneManager.UnloadSceneAsync(hudSceneName);
            }
        }

        private bool IsSpecificSceneAlreadyLoaded(string sceneNameIdentifier)
        {
            Scene specificScene = SceneManager.GetSceneByName(sceneNameIdentifier);
            return specificScene.isLoaded;
        }

        private bool IsSceneAHudComponent(string sceneName)
        {
            return sceneName == _topBarSceneName ||
                   sceneName == _leftBarSceneName ||
                   sceneName == _UnitStackIdeologySceneName;
        }
    }
}
