using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project.Modules.World
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
            ValidateApiServiceAndSession();
            UpdateAuthenticatedPlayerIdentityDisplay();
            ConfigureUserInterfaceLabels();
            InitiateAvailableWorldsLoadingProcess();
        }

        private void ValidateApiServiceAndSession()
        {
            if (ApiService.Instance == null)
            {
                Debug.LogError("[WorldSelectionUI] ApiService blev ikke fundet. Sørg for at starte fra Bootstrap.");
            }
        }

        private void UpdateAuthenticatedPlayerIdentityDisplay()
        {
            if (ApiService.Instance != null && authenticatedPlayerNameDisplay != null)
            {
                authenticatedPlayerNameDisplay.text = ApiService.Instance.AuthenticatedPlayerUserName;
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
            StartCoroutine(ApiService.Instance.FetchAvailableActiveGameWorlds((receivedWorldsList) =>
            {
                if (receivedWorldsList != null)
                {
                    PopulateAvailableGameWorldSelectionList(receivedWorldsList);
                }
            }));
        }

        private void PopulateAvailableGameWorldSelectionList(List<GameWorldAvailableResponseDTO> activeWorlds)
        {
            foreach (Transform existingEntry in worldSelectionEntryContainerParent)
            {
                Destroy(existingEntry.gameObject);
            }

            foreach (var worldData in activeWorlds)
            {
                CreateAndConfigureWorldSelectionEntry(worldData);
            }
        }

        private void CreateAndConfigureWorldSelectionEntry(GameWorldAvailableResponseDTO worldData)
        {
            if (worldEntrySelectionPrefab == null) return;

            GameObject worldItemInstance = Instantiate(worldEntrySelectionPrefab, worldSelectionEntryContainerParent);

            // OBJEKTIV FIX: Vi bruger GetComponentInChildren for at undgå null-referencer 
            // hvis scriptet er placeret på et underobjekt i præfab-hierarkiet.
            WorldSelectionEntryLinker entryLinker = worldItemInstance.GetComponentInChildren<WorldSelectionEntryLinker>();

            if (entryLinker != null)
            {
                // 1. Konfigurer tekst via linkeren
                entryLinker.worldNameLabel.text = worldData.WorldName;
                entryLinker.playerCountLabel.text = $"Players: {worldData.CurrentPlayerCount} / {worldData.MaxPlayerCapacity}";

                // 2. Konfigurer knap-tekst baseret på medlemskab
                string actionVerb = worldData.IsCurrentPlayerMember ? "ENTER" : "ENROLL";
                entryLinker.actionButtonLabel.text = actionVerb;

                // 3. Konfigurer knap-logik
                if (Guid.TryParse(worldData.WorldId, out Guid worldIdentifier))
                {
                    entryLinker.actionExecutionButton.onClick.AddListener(() => HandleWorldSelectionExecution(worldIdentifier));
                }
            }
            else
            {
                Debug.LogError($"[WorldSelectionUI] Kritisk fejl: WorldSelectionEntryLinker kunne ikke findes på {worldItemInstance.name} eller dens børn.");
            }
        }

        private void HandleWorldSelectionExecution(Guid worldId)
        {
            Debug.Log($"[WorldSelectionUI] Udfører handling for verden: {worldId}");

            StartCoroutine(ApiService.Instance.SubmitRequestToJoinSelectedGameWorld(worldId, (joinResponse) =>
            {
                if (joinResponse != null && joinResponse.ConnectionSuccessful)
                {
                    SceneManager.LoadScene(nextGameplaySceneName);
                }
                else
                {
                    Debug.LogError("[WorldSelectionUI] Kunne ikke tilslutte sig eller træde ind i verdenen.");
                }
            }));
        }
    }
}