using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLauncher : MonoBehaviour
{
    private void Start()
    {
        // Indlæs UI-scenen additivt (så den ligger som et lag ovenpå)
        // Vi tjekker om den allerede er indlæst for at undgå dubletter
        if (!SceneManager.GetSceneByName("GlobalUIScene").isLoaded)
        {
            SceneManager.LoadSceneAsync("GlobalUIScene", LoadSceneMode.Additive);
        }
    }
}