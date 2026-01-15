using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Modules.UI
{
    /// <summary>
    /// Denne manager er ansvarlig for at indlæse de nødvendige HUD-scener additivt ovenpå CityViewScene.
    /// Den sikrer, at UI-komponenter som TopBar og SideBar altid er tilgængelige uden at dublere dem.
    /// </summary>
    public class CityUserInterfaceAdditiveSceneManager : MonoBehaviour
    {
        [Header("Scene Navngivning")]
        [SerializeField] private string _topHorizontalNavigationHudSceneName = "TopBarHUD";
        [SerializeField] private string _leftVerticalNavigationHudSceneName = "LeftSideBarHUD";

        private void Start()
        {
            ExecuteAdditiveUserInterfaceSceneLoadingProcess();
        }

        /// <summary>
        /// Kontrollerer om HUD-scenerne er aktive, og indlæser dem hvis de mangler.
        /// </summary>
        public void ExecuteAdditiveUserInterfaceSceneLoadingProcess()
        {
            // Vi tjekker hver scene uafhængigt for at sikre maksimal robusthed.

            if (!IsSpecificSceneAlreadyLoaded(_topHorizontalNavigationHudSceneName))
            {
                Debug.Log($"[UI-Loader] Indlæser additiv HUD scene: {_topHorizontalNavigationHudSceneName}");
                SceneManager.LoadScene(_topHorizontalNavigationHudSceneName, LoadSceneMode.Additive);
            }

            if (!IsSpecificSceneAlreadyLoaded(_leftVerticalNavigationHudSceneName))
            {
                Debug.Log($"[UI-Loader] Indlæser additiv HUD scene: {_leftVerticalNavigationHudSceneName}");
                SceneManager.LoadScene(_leftVerticalNavigationHudSceneName, LoadSceneMode.Additive);
            }
        }

        /// <summary>
        /// Hjælpemetode der tjekker om en scene med et specifikt navn findes i den nuværende session.
        /// </summary>
        private bool IsSpecificSceneAlreadyLoaded(string sceneNameIdentifier)
        {
            Scene specificScene = SceneManager.GetSceneByName(sceneNameIdentifier);
            return specificScene.isLoaded;
        }
    }
}