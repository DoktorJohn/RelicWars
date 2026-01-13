using UnityEngine;
using System.Collections.Generic;
using System;
using Project.Network.Models;
using Assets.Scripts.Domain.Enums;

namespace Project.Modules.City
{
    public class CityManager : MonoBehaviour
    {
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

        [Header("Layout Konfiguration")]
        [SerializeField] private float horizontalSpacingBetweenBuildingInstances = 5.0f;
        [SerializeField] private Transform buildingParentContainer;

        private void Start()
        {
            ValidateInitializationRequirements();
            InitiateCityBuildingInstantiationSequence();
        }

        private void ValidateInitializationRequirements()
        {
            if (ApiService.Instance == null)
            {
                Debug.LogError("[CityManager] Kritisk fejl: ApiService Instance blev ikke fundet i scenen.");
                return;
            }

            if (buildingParentContainer == null)
            {
                Debug.LogError("[CityManager] Fejl: Building Parent Container (Transform) er ikke tildelt i Inspectoren.");
            }
        }

        private void InitiateCityBuildingInstantiationSequence()
        {
            Guid? activeCityId = ApiService.Instance.CurrentlySelectedActiveCityId;

            if (activeCityId.HasValue)
            {
                Debug.Log($"[CityManager] Påbegynder hentning af bygnings-data for CityId: {activeCityId.Value}");

                StartCoroutine(ApiService.Instance.RetrieveDetailedCityInformationByCityIdentifier(activeCityId.Value, (cityInformation) =>
                {
                    if (cityInformation != null)
                    {
                        Debug.Log($"[CityManager] Modtog data for '{cityInformation.CityName}'. Sender {cityInformation.BuildingList.Count} bygninger til population.");
                        ExecuteRealWorldBuildingPopulationProcess(cityInformation.BuildingList);
                    }
                }));
            }
        }

        private void ExecuteRealWorldBuildingPopulationProcess(List<CityControllerGetDetailedCityInformationBuildingDTO> buildingDataList)
        {
            foreach (Transform child in buildingParentContainer)
            {
                Destroy(child.gameObject);
            }

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

                    // Vi konfigurerer nu metadataen på det objekt, der har collideren
                    ConfigureInstantiatedBuildingMetadata(buildingInstance, currentBuildingData);
                }
            }
        }

        private void ConfigureInstantiatedBuildingMetadata(GameObject instance, CityControllerGetDetailedCityInformationBuildingDTO data)
        {
            // OBJEKTIV FIX: Vi leder efter den collider, som musen rent faktisk skal ramme
            Collider buildingCollider = instance.GetComponentInChildren<Collider>();

            if (buildingCollider == null)
            {
                Debug.LogWarning($"[CityManager] Advarsel: Ingen Collider fundet på {instance.name} eller dens børn. Interaktion vil ikke virke!");
                return;
            }

            // Vi tager fat i det specifikke GameObject, hvor collideren bor
            GameObject targetGameObject = buildingCollider.gameObject;

            CityBuildingInteractionController interactionController = targetGameObject.GetComponent<CityBuildingInteractionController>();

            if (interactionController == null)
            {
                // Vi tilføjer scriptet til SAMME objekt som collideren
                interactionController = targetGameObject.AddComponent<CityBuildingInteractionController>();
            }

            interactionController.InitializeBuildingInteractionData(data);
            Debug.Log($"[CityManager] Initialiserede {data.BuildingType} på {targetGameObject.name} (Hvor collideren findes).");
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
                _ => null
            };
        }
    }
}