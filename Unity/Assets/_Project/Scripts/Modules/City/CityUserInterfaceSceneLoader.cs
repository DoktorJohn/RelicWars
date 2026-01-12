using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Modules.City
{
    /// <summary>
    /// Ansvarlig for at indlæse HUD og UI scener additivt ovenpå spilscener.
    /// </summary>
    public class CityUserInterfaceSceneLoader : MonoBehaviour
    {
        [SerializeField] private string _hudSceneName = "TopBarHUD";

        private void Start()
        {
            LoadUserInterfaceAdditive();
        }

        public void LoadUserInterfaceAdditive()
        {
            // Tjek om scenen allerede er indlæst for at undgå dubletter
            if (!SceneManager.GetSceneByName(_hudSceneName).isLoaded)
            {
                // LoadSceneMode.Additive sikrer, at CityViewScene IKKE lukkes
                SceneManager.LoadScene(_hudSceneName, LoadSceneMode.Additive);
            }
        }
    }
}