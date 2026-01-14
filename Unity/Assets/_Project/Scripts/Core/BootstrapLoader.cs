using Project.Network.Manager;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    [Header("Scene Konfiguration")]
    [SerializeField] private string targetInitialLoginSceneName = "LoginScene";

    [Header("Forsinkelse (Valgfri)")]
    [SerializeField] private float minimalBootstrapDisplayDuration = 0.5f;

    private void Start()
    {
        ExecuteApplicationBootstrappingSequence();
    }

    private void ExecuteApplicationBootstrappingSequence()
    {
        Debug.Log("[Bootstrap] Påbegynder initialisering af globale systemer...");

        // RETTELSE: Vi tjekker nu om NetworkManager er til stede i scenen.
        // Det er vigtigt, at du har trukket dit 'NetworkManager' prefab ind i Bootstrap scenen.
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("[Bootstrap] KRITISK: NetworkManager blev ikke fundet! " +
                           "Husk at tilføje NetworkManager prefab til Bootstrap scenen.");
        }
        else
        {
            Debug.Log("[Bootstrap] NetworkManager fundet og klar.");
        }

        StartCoroutine(PerformSceneTransitionAfterValidation());
    }

    private IEnumerator PerformSceneTransitionAfterValidation()
    {
        // Giv systemet et øjeblik til at vågne helt op (og vise splash screen)
        yield return new WaitForSeconds(minimalBootstrapDisplayDuration);

        if (ApplicationCanPathToTargetScene(targetInitialLoginSceneName))
        {
            Debug.Log($"[Bootstrap] Skifter nu til indlæsnings-scene: {targetInitialLoginSceneName}");
            SceneManager.LoadScene(targetInitialLoginSceneName);
        }
        else
        {
            Debug.LogError($"[Bootstrap] FATAL FEJL: Scenen '{targetInitialLoginSceneName}' kunne ikke findes! " +
                           "Tjek venligst Build Settings (File -> Build Settings) og verificer navngivningen.");
        }
    }

    private bool ApplicationCanPathToTargetScene(string sceneName)
    {
        // Denne hjælpefunktion tjekker om scenen er registreret i Build Settings
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string nameOnly = System.IO.Path.GetFileNameWithoutExtension(path);

            if (nameOnly == sceneName)
            {
                return true;
            }
        }
        return false;
    }
}