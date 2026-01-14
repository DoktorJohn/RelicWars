using UnityEngine;
using System.Collections.Generic;
using System;
using Project.Network.Models;
using Assets.Scripts.Domain.Enums;
using Assets._Project.Scripts.Domain.DTOs;
using Project.Network.Manager;

namespace Project.Modules.City
{
    public class CityManager : MonoBehaviour
    {
        // Singleton Instance
        public static CityManager Instance { get; private set; }

        [Header("Bygnings Præfab Referencer")]
        [SerializeField] private GameObject barracksPrefab;
        [SerializeField] private GameObject senatePrefab;
        [SerializeField] private GameObject timberCampPrefab;
        [SerializeField] private GameObject stoneQuarryPrefab;
        [SerializeField] private GameObject metalMinePrefab;
        [SerializeField] private GameObject warehousePrefab;
        [SerializeField] private GameObject housingPrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject workshopPrefab;
        [SerializeField] private GameObject academyPrefab;
        [SerializeField] private GameObject stablePrefab;

        [Header("Layout Konfiguration")]
        [SerializeField] private float horizontalSpacingBetweenBuildingInstances = 5.0f;
        [SerializeField] private Transform buildingParentContainer;

        private void Awake()
        {
            // Singleton setup
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            ValidateInitializationRequirements();

            // VIGTIGT: Vi abonnerer på eventet fra CityResourceService.
            // Når servicen henter nye data (hvert 30. sek eller ved force refresh), kører HandleBuildingUpdate.
            if (CityResourceService.Instance != null)
            {
                CityResourceService.Instance.OnBuildingStateReceived += HandleBuildingUpdateFromService;
            }
            else
            {
                Debug.LogError("[CityManager] CityResourceService ikke fundet i Start! Sørg for eksekveringsrækkefølge.");
            }
        }

        private void OnDestroy()
        {
            // Husk altid at afmelde events for at undgå memory leaks
            if (CityResourceService.Instance != null)
            {
                CityResourceService.Instance.OnBuildingStateReceived -= HandleBuildingUpdateFromService;
            }
        }

        /// <summary>
        /// Denne metode kaldes automatisk via Event, når CityResourceService har hentet data.
        /// </summary>
        private void HandleBuildingUpdateFromService(List<CityControllerGetDetailedCityInformationBuildingDTO> buildings)
        {
            Debug.Log($"[CityManager] Modtog opdateret bygningsliste ({buildings.Count} bygninger) fra Service. Opdaterer 3D verden.");
            ExecuteRealWorldBuildingPopulationProcess(buildings);
        }

        /// <summary>
        /// Kan kaldes udefra (f.eks. fra Senatet eller en Timer) for at tvinge en opdatering af hele byen.
        /// </summary>
        public void RefreshCityArchitecture()
        {
            if (NetworkManager.Instance != null && NetworkManager.Instance.ActiveCityId.HasValue)
            {
                Debug.Log("[CityManager] Anmoder om Force Refresh via CityResourceService.");
                // Vi beder servicen om at polle nu, hvilket vil trigge eventet når det er færdigt
                if (CityResourceService.Instance != null)
                {
                    CityResourceService.Instance.InitiateResourceRefresh(NetworkManager.Instance.ActiveCityId.Value);
                }
            }
        }

        private void ValidateInitializationRequirements()
        {
            if (NetworkManager.Instance == null)
            {
                Debug.LogError("[CityManager] Kritisk fejl: NetworkManager blev ikke fundet i scenen.");
                return;
            }

            if (buildingParentContainer == null)
            {
                Debug.LogError("[CityManager] Fejl: Building Parent Container (Transform) er ikke tildelt i Inspectoren.");
            }
        }

        private void ExecuteRealWorldBuildingPopulationProcess(List<CityControllerGetDetailedCityInformationBuildingDTO> buildingDataList)
        {
            // 1. Slet eksisterende bygninger
            foreach (Transform child in buildingParentContainer)
            {
                Destroy(child.gameObject);
            }

            // 2. Spawn nye bygninger baseret på listen
            for (int i = 0; i < buildingDataList.Count; i++)
            {
                CityControllerGetDetailedCityInformationBuildingDTO currentBuildingData = buildingDataList[i];

                GameObject prefabToInstantiate = GetPrefabForSpecificBuildingType(currentBuildingData.BuildingType);

                if (prefabToInstantiate != null)
                {
                    Vector3 spawnPosition = new Vector3(i * horizontalSpacingBetweenBuildingInstances, 0.5f, 0f);

                    GameObject buildingInstance = Instantiate(
                        prefabToInstantiate,
                        spawnPosition,
                        Quaternion.identity,
                        buildingParentContainer
                    );

                    buildingInstance.name = $"{currentBuildingData.BuildingType}_Level_{currentBuildingData.CurrentLevel}";

                    ConfigureInstantiatedBuildingMetadata(buildingInstance, currentBuildingData);
                }
            }
        }

        private void ConfigureInstantiatedBuildingMetadata(GameObject instance, CityControllerGetDetailedCityInformationBuildingDTO data)
        {
            Collider buildingCollider = instance.GetComponentInChildren<Collider>();

            if (buildingCollider == null)
            {
                Debug.LogWarning($"[CityManager] Advarsel: Ingen Collider fundet på {instance.name}.");
                return;
            }

            GameObject targetGameObject = buildingCollider.gameObject;
            CityBuildingInteractionController interactionController = targetGameObject.GetComponent<CityBuildingInteractionController>();

            if (interactionController == null)
            {
                interactionController = targetGameObject.AddComponent<CityBuildingInteractionController>();
            }

            interactionController.InitializeBuildingInteractionData(data);
        }

        private GameObject GetPrefabForSpecificBuildingType(BuildingTypeEnum type)
        {
            return type switch
            {
                BuildingTypeEnum.Senate => senatePrefab,
                BuildingTypeEnum.Warehouse => warehousePrefab,
                BuildingTypeEnum.Housing => housingPrefab,
                BuildingTypeEnum.Barracks => barracksPrefab,
                BuildingTypeEnum.TimberCamp => timberCampPrefab,
                BuildingTypeEnum.StoneQuarry => stoneQuarryPrefab,
                BuildingTypeEnum.MetalMine => metalMinePrefab,
                BuildingTypeEnum.Wall => wallPrefab,
                BuildingTypeEnum.Workshop => workshopPrefab,
                BuildingTypeEnum.Academy => academyPrefab,
                BuildingTypeEnum.Stable => stablePrefab,
                _ => null
            };
        }
    }
}