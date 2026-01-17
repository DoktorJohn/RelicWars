using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Project.Network.Manager;

namespace Project.Modules.WorldSelection
{
    public class WorldSelectionUIHandler : MonoBehaviour
    {
        [Header("Identitets Visning")]
        [SerializeField] private TMP_Text authenticatedPlayerNameDisplay;

        [Header("Overskrift Elementer")]
        [SerializeField] private TMP_Text worldsHeaderLabel;

        [Header("Verdens Liste Konfiguration")]
        [SerializeField] private Transform worldSelectionEntryContainerParent;
        [SerializeField] private GameObject worldEntrySelectionPrefab;

        [Header("Scene Navigation")]
        [SerializeField] private string nextGameplaySceneName = "CityViewScene";

        private void Start()
        {
            ValidateNetworkManagerSession();
            UpdateAuthenticatedPlayerIdentityDisplay();
            ConfigureUserInterfaceLabels();
            InitiateAvailableWorldsLoadingProcess();
        }

        private void ValidateNetworkManagerSession()
        {
            if (NetworkManager.Instance == null)
            {
                Debug.LogError("[WorldSelectionUI] NetworkManager blev ikke fundet. Sørg for at starte fra Bootstrap/Login scenen.");
                return;
            }
        }

        private void UpdateAuthenticatedPlayerIdentityDisplay()
        {
            // RETTELSE: Vi bruger nu 'PlayerName' fra NetworkManager
            if (NetworkManager.Instance != null && authenticatedPlayerNameDisplay != null)
            {
                authenticatedPlayerNameDisplay.text = NetworkManager.Instance.PlayerName;
            }
        }

        private void ConfigureUserInterfaceLabels()
        {
            if (worldsHeaderLabel != null)
            {
                worldsHeaderLabel.text = "Available Worlds:";
            }
        }

        private void InitiateAvailableWorldsLoadingProcess()
        {
            if (NetworkManager.Instance == null) return;

            // RETTELSE: Vi kalder World servicen direkte. 
            // Da servicen returnerer en IEnumerator, skal vi stadig bruge StartCoroutine her i UI'en 
            // for at hente dataen asynkront.
            StartCoroutine(NetworkManager.Instance.World.GetAvailableWorlds((receivedWorldsList) =>
            {
                if (receivedWorldsList != null)
                {
                    PopulateAvailableGameWorldSelectionList(receivedWorldsList);
                }
                else
                {
                    Debug.LogWarning("[WorldSelectionUI] Modtog ingen verdener fra serveren.");
                }
            }));
        }

        private void PopulateAvailableGameWorldSelectionList(List<WorldAvailableResponseDTO> activeWorlds)
        {
            // Ryd den eksisterende liste
            foreach (Transform existingEntry in worldSelectionEntryContainerParent)
            {
                Destroy(existingEntry.gameObject);
            }

            foreach (var worldData in activeWorlds)
            {
                CreateAndConfigureWorldSelectionEntry(worldData);
            }
        }

        private void CreateAndConfigureWorldSelectionEntry(WorldAvailableResponseDTO worldData)
        {
            if (worldEntrySelectionPrefab == null) return;

            GameObject worldItemInstance = Instantiate(worldEntrySelectionPrefab, worldSelectionEntryContainerParent);
            WorldSelectionEntryLinker entryLinker = worldItemInstance.GetComponentInChildren<WorldSelectionEntryLinker>();

            if (entryLinker != null)
            {
                // 1. Konfigurer tekst
                entryLinker.worldNameLabel.text = worldData.WorldName;
                entryLinker.playerCountLabel.text = $"Players: {worldData.CurrentPlayerCount} / {worldData.MaxPlayerCapacity}";

                // 2. Konfigurer knap-tekst
                // Her kan du evt. tilføje logik der tjekker 'IsCurrentPlayerMember' hvis DTO'en har det
                string actionVerb = "ENTER"; // Eller "JOIN" afhængig af logik
                entryLinker.actionButtonLabel.text = actionVerb;

                // 3. Konfigurer knap-logik
                if (Guid.TryParse(worldData.WorldId, out Guid worldIdentifier))
                {
                    entryLinker.actionExecutionButton.onClick.AddListener(() => HandleWorldSelectionExecution(worldIdentifier));
                }
            }
            else
            {
                Debug.LogError($"[WorldSelectionUI] Kritisk fejl: WorldSelectionEntryLinker mangler på prefab.");
            }
        }

        private void HandleWorldSelectionExecution(Guid worldId)
        {
            Debug.Log($"[WorldSelectionUI] Udfører handling for verden: {worldId}");

            // RETTELSE: Her bruger vi NetworkManagerens 'JoinWorld' metode.
            // Denne metode håndterer SELV sin coroutine internt, så vi skal IKKE skrive StartCoroutine her.
            // Vi får blot en 'bool success' tilbage.
            NetworkManager.Instance.JoinWorld(worldId, (success) =>
            {
                if (success)
                {
                    Debug.Log("[WorldSelectionUI] Join succesfuld. Skifter scene.");
                    SceneManager.LoadScene(nextGameplaySceneName);
                }
                else
                {
                    // Her kunne du vise en popup til brugeren med fejlbeskeden
                    Debug.LogError("[WorldSelectionUI] Kunne ikke tilslutte sig verdenen. Tjek logs for detaljer.");
                }
            });
        }
    }
}