using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Project.Network.Models;
using Assets.Scripts.Domain.Enums;
using Project.Network.Manager;

namespace Project.Modules.City
{
    /// <summary>
    /// Styrer synlighed og data-opdatering for manuelt placerede bygningsobjekter i scenen.
    /// Denne klasse fjerner behovet for dynamisk instantiation til fordel for en statisk scene-konfiguration.
    /// </summary>
    public class CityManager : MonoBehaviour
    {
        public static CityManager Instance { get; private set; }

        [System.Serializable]
        public class BygningsReferenceKobling
        {
            public BuildingTypeEnum BygningsType;
            public GameObject BygningsObjektIScenene;
            [Tooltip("Valgfrit: Et objekt der viser en byggeplads, hvis bygningen er i level 0.")]
            public GameObject KonstruktionsGhostObjekt;
        }

        [Header("Statisk Bygnings Konfiguration")]
        [SerializeField] private List<BygningsReferenceKobling> _identificeredeBygningsReferencer = new List<BygningsReferenceKobling>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            SubscribeToBuildingDataEvents();
        }

        private void OnDestroy()
        {
            if (CityResourceService.Instance != null)
            {
                CityResourceService.Instance.OnBuildingStateReceived -= HandleBuildingUpdateFromService;
            }
        }

        private void SubscribeToBuildingDataEvents()
        {
            if (CityResourceService.Instance != null)
            {
                CityResourceService.Instance.OnBuildingStateReceived += HandleBuildingUpdateFromService;
            }
        }

        /// <summary>
        /// Modtager data fra serveren og opdaterer de statiske objekter i scenen.
        /// </summary>
        private void HandleBuildingUpdateFromService(List<CityControllerGetDetailedCityInformationBuildingDTO> buildingDataList)
        {
            Debug.Log($"[CityManager] Opdaterer tilstand for {buildingDataList.Count} statiske bygningsreferencer.");

            foreach (var bygningsData in buildingDataList)
            {
                var kobling = _identificeredeBygningsReferencer.FirstOrDefault(x => x.BygningsType == bygningsData.BuildingType);

                if (kobling != null)
                {
                    OpdaterBygningsVisuelTilstand(kobling, bygningsData);
                }
            }
        }

        private void OpdaterBygningsVisuelTilstand(BygningsReferenceKobling kobling, CityControllerGetDetailedCityInformationBuildingDTO data)
        {
            bool erBygningKonstrueret = data.CurrentLevel > 0;

            // Aktivér selve bygningen hvis level > 0
            if (kobling.BygningsObjektIScenene != null)
            {
                kobling.BygningsObjektIScenene.SetActive(erBygningKonstrueret);
            }

            // Aktivér ghost/byggeplads hvis level == 0
            if (kobling.KonstruktionsGhostObjekt != null)
            {
                kobling.KonstruktionsGhostObjekt.SetActive(!erBygningKonstrueret);
            }

            // Opdatér interaktionsdata hvis bygningen er synlig
            if (erBygningKonstrueret)
            {
                var interactionController = kobling.BygningsObjektIScenene.GetComponentInChildren<CityBuildingInteractionController>();
                if (interactionController != null)
                {
                    interactionController.InitializeBuildingInteractionData(data);
                }
            }
        }
    }
}