using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Project.Network.Models;
using Assets.Scripts.Domain.Enums;
using Project.Network.Manager;

namespace Project.Modules.City
{
    public class CityManager : MonoBehaviour
    {
        // Singleton Instance til global adgang
        public static CityManager Instance { get; private set; }

        [Header("Bygnings Præfab Referencer")]
        [SerializeField] private GameObject barracksPrefab;
        [SerializeField] private GameObject townHallPrefab;
        [SerializeField] private GameObject timberCampPrefab;
        [SerializeField] private GameObject stoneQuarryPrefab;
        [SerializeField] private GameObject metalMinePrefab;
        [SerializeField] private GameObject warehousePrefab;
        [SerializeField] private GameObject housingPrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject workshopPrefab;
        [SerializeField] private GameObject universityPrefab;
        [SerializeField] private GameObject stablePrefab;
        [SerializeField] private GameObject marketPlacePrefab;

        [Header("Special Præfabs")]
        [Tooltip("En visuel markør eller platform der vises, når en bygning er i level 0.")]
        [SerializeField] private GameObject constructionGhostPrefab;

        [Header("Hierarki Konfiguration")]
        [Tooltip("Containeren som alle de aktive bygnings-instanser bliver lagt under.")]
        [SerializeField] private Transform buildingInstanceParent;

        private void Awake()
        {
            InitializeSingletonPattern();
        }

        private void Start()
        {
            ValidateInitializationRequirements();
            SubscribeToBuildingDataEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromBuildingDataEvents();
        }

        private void InitializeSingletonPattern()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void SubscribeToBuildingDataEvents()
        {
            if (CityResourceService.Instance != null)
            {
                CityResourceService.Instance.OnBuildingStateReceived += HandleBuildingUpdateFromService;
            }
            else
            {
                Debug.LogError("[CityManager] CityResourceService ikke fundet! Sørg for at den findes i scenen.");
            }
        }

        private void UnsubscribeFromBuildingDataEvents()
        {
            if (CityResourceService.Instance != null)
            {
                CityResourceService.Instance.OnBuildingStateReceived -= HandleBuildingUpdateFromService;
            }
        }

        private void HandleBuildingUpdateFromService(List<CityControllerGetDetailedCityInformationBuildingDTO> buildingDataList)
        {
            Debug.Log($"[CityManager] Synkroniserer {buildingDataList.Count} bygninger med scenens anchors.");
            ExecuteAnchorBasedBuildingPopulationProcess(buildingDataList);
        }

        /// <summary>
        /// Gennemløber alle anchors i scenen og placerer den korrekte model baseret på server-data.
        /// </summary>
        private void ExecuteAnchorBasedBuildingPopulationProcess(List<CityControllerGetDetailedCityInformationBuildingDTO> buildingDataList)
        {
            ClearExistingBuildingInstances();

            // Find alle anchors der er placeret manuelt i scenen
            CityBuildingAnchor[] sceneAnchors = FindObjectsByType<CityBuildingAnchor>(FindObjectsSortMode.None);

            if (sceneAnchors.Length == 0)
            {
                Debug.LogWarning("[CityManager] Ingen CityBuildingAnchors fundet i scenen! Byen kan ikke bygges.");
                return;
            }

            foreach (CityBuildingAnchor anchor in sceneAnchors)
            {
                // Find den data fra serveren der matcher denne anchors type
                CityControllerGetDetailedCityInformationBuildingDTO matchingData = buildingDataList
                    .FirstOrDefault(data => data.BuildingType == anchor.BuildingType);

                if (matchingData != null && matchingData.CurrentLevel > 0)
                {
                    // Bygningen er konstrueret - Spawn den rigtige præfab
                    GameObject prefabToInstantiate = GetPrefabForSpecificBuildingType(anchor.BuildingType);
                    SpawnBuildingAtAnchorLocation(anchor, matchingData, prefabToInstantiate);
                }
                else
                {
                    // Bygningen er i level 0 - Spawn et 'Ghost' hvis muligt
                    if (constructionGhostPrefab != null)
                    {
                        // Vi opretter en 'tom' DTO til ghostet så man stadig kan klikke på det
                        CityControllerGetDetailedCityInformationBuildingDTO ghostData = matchingData ?? new CityControllerGetDetailedCityInformationBuildingDTO
                        {
                            BuildingType = anchor.BuildingType,
                            CurrentLevel = 0
                        };
                        SpawnBuildingAtAnchorLocation(anchor, ghostData, constructionGhostPrefab);
                    }
                }
            }
        }

        private void SpawnBuildingAtAnchorLocation(CityBuildingAnchor anchor, CityControllerGetDetailedCityInformationBuildingDTO data, GameObject prefab)
        {
            if (prefab == null) return;

            GameObject buildingInstance = Instantiate(
                prefab,
                anchor.transform.position,
                anchor.transform.rotation,
                buildingInstanceParent
            );

            // Navngivning for bedre hierarki-overblik
            buildingInstance.name = data.CurrentLevel > 0
                ? $"Building_{data.BuildingType}_Level{data.CurrentLevel}"
                : $"Ghost_{data.BuildingType}";

            ConfigureInstantiatedBuildingMetadata(buildingInstance, data);
        }

        private void ClearExistingBuildingInstances()
        {
            if (buildingInstanceParent == null) return;

            foreach (Transform child in buildingInstanceParent)
            {
                Destroy(child.gameObject);
            }
        }

        private void ConfigureInstantiatedBuildingMetadata(GameObject buildingInstance, CityControllerGetDetailedCityInformationBuildingDTO data)
        {
            Collider interactionCollider = buildingInstance.GetComponentInChildren<Collider>();

            if (interactionCollider == null)
            {
                Debug.LogWarning($"[CityManager] Ingen Collider fundet på {buildingInstance.name}. Interaktion vil ikke virke.");
                return;
            }

            GameObject targetInteractionObject = interactionCollider.gameObject;
            CityBuildingInteractionController interactionController = targetInteractionObject.GetComponent<CityBuildingInteractionController>();

            if (interactionController == null)
            {
                interactionController = targetInteractionObject.AddComponent<CityBuildingInteractionController>();
            }

            interactionController.InitializeBuildingInteractionData(data);
        }

        private void ValidateInitializationRequirements()
        {
            if (NetworkManager.Instance == null)
            {
                Debug.LogError("[CityManager] NetworkManager mangler i scenen.");
            }

            if (buildingInstanceParent == null)
            {
                Debug.LogError("[CityManager] Building Instance Parent er ikke tildelt i Inspectoren.");
            }
        }

        private GameObject GetPrefabForSpecificBuildingType(BuildingTypeEnum type)
        {
            return type switch
            {
                BuildingTypeEnum.TownHall => townHallPrefab,
                BuildingTypeEnum.Warehouse => warehousePrefab,
                BuildingTypeEnum.Housing => housingPrefab,
                BuildingTypeEnum.Barracks => barracksPrefab,
                BuildingTypeEnum.TimberCamp => timberCampPrefab,
                BuildingTypeEnum.StoneQuarry => stoneQuarryPrefab,
                BuildingTypeEnum.MetalMine => metalMinePrefab,
                BuildingTypeEnum.Wall => wallPrefab,
                BuildingTypeEnum.Workshop => workshopPrefab,
                BuildingTypeEnum.University => universityPrefab,
                BuildingTypeEnum.Stable => stablePrefab,
                BuildingTypeEnum.MarketPlace => marketPlacePrefab,
                _ => null
            };
        }

        /// <summary>
        /// Tvinger en genindlæsning af byens visuelle tilstand.
        /// </summary>
        public void RefreshCityArchitecture()
        {
            if (NetworkManager.Instance != null && NetworkManager.Instance.ActiveCityId.HasValue)
            {
                if (CityResourceService.Instance != null)
                {
                    CityResourceService.Instance.InitiateResourceRefresh(NetworkManager.Instance.ActiveCityId.Value);
                }
            }
        }
    }
}