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
                    else
                    {
                        Debug.LogError("[CityManager] Modtog null fra ApiService. Tjek netværksloggen.");
                    }
                }));
            }
            else
            {
                Debug.LogWarning("[CityManager] Kunne ikke påbegynde instansiering: Intet aktivt CityId i ApiService.");
            }
        }

        private void ExecuteRealWorldBuildingPopulationProcess(List<CityControllerGetDetailedCityInformationBuildingDTO> buildingDataList)
        {
            // 1. Destruktion af eksisterende bygninger
            int existingChildCount = buildingParentContainer.childCount;
            foreach (Transform child in buildingParentContainer)
            {
                Destroy(child.gameObject);
            }
            Debug.Log($"[CityManager] Rensede {existingChildCount} gamle bygningsobjekter.");

            if (buildingDataList == null || buildingDataList.Count == 0)
            {
                Debug.LogWarning("[CityManager] Bygningslisten er tom. Ingen bygninger at spawne.");
                return;
            }

            // 2. Instansierings-loop med debugging
            for (int i = 0; i < buildingDataList.Count; i++)
            {
                CityControllerGetDetailedCityInformationBuildingDTO currentBuildingData = buildingDataList[i];
                Debug.Log($"[CityManager] Behandler bygning {i + 1}/{buildingDataList.Count}: {currentBuildingData.BuildingType} (Lvl {currentBuildingData.CurrentLevel})");

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
                    Debug.Log($"[CityManager] Succes: Instansieret {buildingInstance.name} på position {spawnPosition}");

                    ConfigureInstantiatedBuildingMetadata(buildingInstance, currentBuildingData);
                }
                else
                {
                    Debug.LogError($"[CityManager] FEJL: Kunne ikke finde en Prefab til typen '{currentBuildingData.BuildingType}'. Tjek Inspectoren!");
                }
            }

            Debug.Log($"[CityManager] Population proces færdig. {buildingParentContainer.childCount} bygninger aktive i containeren.");
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

        private void ConfigureInstantiatedBuildingMetadata(GameObject instance, CityControllerGetDetailedCityInformationBuildingDTO data)
        {
            CityBuildingInteractionController interactionController = instance.GetComponent<CityBuildingInteractionController>();

            if (interactionController == null)
            {
                interactionController = instance.AddComponent<CityBuildingInteractionController>();
            }

            interactionController.InitializeBuildingInteractionData(data);
        }
    }
}