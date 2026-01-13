using UnityEngine;
using UnityEngine.EventSystems; // Påkrævet for IPointer interfaces
using Project.Network.Models;
using Project.Modules.CityView.UI;
using Assets.Scripts.Domain.Enums;

namespace Project.Modules.City
{
    /// <summary>
    /// Styrer den visuelle interaktion for bygningsobjekter i CityView.
    /// Implementerer New Input System kompatible interfaces for klik og hover-effekter.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class CityBuildingInteractionController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private CityControllerGetDetailedCityInformationBuildingDTO _associatedBuildingData;
        private Renderer _visualModelRenderer;
        private Color _initialMaterialColor;
        private bool _isControllerSuccessfullyInitialized = false;

        /// <summary>
        /// Forbinder DTO-data med objektet og lokaliserer renderen i hierarkiet.
        /// </summary>
        public void InitializeBuildingInteractionData(CityControllerGetDetailedCityInformationBuildingDTO buildingData)
        {
            _associatedBuildingData = buildingData;

            // Finder renderen i barnet (f.eks. House_1)
            _visualModelRenderer = GetComponentInChildren<Renderer>();

            if (_visualModelRenderer != null)
            {
                // Vi bruger .material for at skabe en instans, så vi ikke ændrer selve asset-filen permanent
                _initialMaterialColor = _visualModelRenderer.material.color;
                _isControllerSuccessfullyInitialized = true;

                Debug.Log($"<color=cyan>[CityInteraction]</color> Initialiseret korrekt for {gameObject.name} ({_associatedBuildingData.BuildingType}).");
            }
            else
            {
                Debug.LogWarning($"<color=yellow>[CityInteraction]</color> Advarsel: Ingen Renderer fundet på {gameObject.name} eller dens børn.");
            }
        }

        /// <summary>
        /// New Input System: Kaldes når musen bevæger sig ind over objektets collider.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isControllerSuccessfullyInitialized || _visualModelRenderer == null) return;

            // Highlight effekt
            _visualModelRenderer.material.color = Color.yellow;
        }

        /// <summary>
        /// New Input System: Kaldes når musen forlader objektets collider.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isControllerSuccessfullyInitialized || _visualModelRenderer == null) return;

            // Gendan farve
            _visualModelRenderer.material.color = _initialMaterialColor;
        }

        /// <summary>
        /// New Input System: Kaldes ved klik på objektets collider.
        /// Erstatter OnMouseDown i projekter med New Input System.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            ExecuteInteractionLogic();
        }

        private void ExecuteInteractionLogic()
        {
            // LOG: Bekræftelse på at input-systemet har fanget klikket
            Debug.Log($"<color=magenta><b>[5. INTERACTION TRIGGER]</b></color> Input System fangede klik på: {gameObject.name}");

            if (!_isControllerSuccessfullyInitialized)
            {
                Debug.LogError($"<color=red><b>[6. INIT ERROR]</b></color> Klik på {gameObject.name} ignoreret: Ikke initialiseret.");
                return;
            }

            if (_associatedBuildingData == null)
            {
                Debug.LogError($"<color=red><b>[6. DATA ERROR]</b></color> Klik på {gameObject.name} ignoreret: Data mangler.");
                return;
            }

            Debug.Log($"<color=white><b>[7. DATA CHECK]</b></color> Bygningstype: {_associatedBuildingData.BuildingType}. Er det Senate? " +
                      (_associatedBuildingData.BuildingType == BuildingTypeEnum.Senate ? "JA" : "NEJ"));

            if (_associatedBuildingData.BuildingType == BuildingTypeEnum.Senate)
            {
                ExecuteSenateWindowOpenRequest();
            }
        }

        private void ExecuteSenateWindowOpenRequest()
        {
            if (SenateWindowController.Instance != null)
            {
                Debug.Log("<color=green><b>[8. UI CALL]</b></color> Forsøger at åbne Senat-vinduet via Singleton.");
                SenateWindowController.Instance.OpenWindow();
            }
            else
            {
                Debug.LogError("<color=red><b>[8. SINGLETON ERROR]</b></color> SenateWindowController.Instance er NULL! Er scriptet i scenen?");
            }
        }
    }
}