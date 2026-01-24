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
            InitialiserScenensBygningsTilstand();
            SubscribeToBuildingDataEvents();
        }

        private void InitialiserScenensBygningsTilstand()
        {
            foreach (var kobling in _identificeredeBygningsReferencer)
            {
                if (kobling.BygningsObjektIScenene != null)
                {
                    kobling.BygningsObjektIScenene.SetActive(false);
                }

                // Vis byggepladser (Ghosts) som standard, hvis de findes
                if (kobling.KonstruktionsGhostObjekt != null)
                {
                    kobling.KonstruktionsGhostObjekt.SetActive(true);
                }
            }
        }

        private void OnDestroy()
        {
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnBuildingStateReceived -= HandleBuildingUpdateFromService;
            }
        }

        private void SubscribeToBuildingDataEvents()
        {
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnBuildingStateReceived += HandleBuildingUpdateFromService;
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

            if (kobling.BygningsObjektIScenene != null)
            {
                kobling.BygningsObjektIScenene.SetActive(erBygningKonstrueret);
            }

            if (kobling.KonstruktionsGhostObjekt != null)
            {
                kobling.KonstruktionsGhostObjekt.SetActive(!erBygningKonstrueret);
            }

            if (erBygningKonstrueret)
            {
                // DEBUG: Finder vi overhovedet controlleren?
                var interactionController = kobling.BygningsObjektIScenene.GetComponentInChildren<CityBuildingInteractionController>();

                if (interactionController != null)
                {
                    interactionController.InitializeBuildingInteractionData(data);
                }
                else
                {
                    Debug.LogError($"<color=red>[CityManager ERROR]</color> Fant bygnings-objekt for {data.BuildingType}, men kunne ikke finde CityBuildingInteractionController på det eller dets børn!");
                }
            }
        }
    }
}