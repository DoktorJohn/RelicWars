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

        // Her kan vi validere om ApiService er klar
        if (ApiService.Instance == null)
        {
            Debug.LogWarning("[Bootstrap] Advarsel: ApiService blev ikke fundet i Bootstrap scenen.");
        }

        StartCoroutine(PerformSceneTransitionAfterValidation());
    }

    private IEnumerator PerformSceneTransitionAfterValidation()
    {
        // Giv systemet et øjeblik til at vågne helt op
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