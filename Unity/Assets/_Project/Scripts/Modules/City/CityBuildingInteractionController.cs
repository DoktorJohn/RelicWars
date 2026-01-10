using UnityEngine;
using Project.Network.Models;

namespace Project.Modules.City
{
    /// <summary>
    /// Styrer den visuelle interaktion for bygningsobjekter i CityView.
    /// Kravet om Renderer på dette objekt er fjernet, da renderen nu findes i børnene (FBX).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class CityBuildingInteractionController : MonoBehaviour
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

            // Da vi har fjernet Renderen fra Parent, leder vi nu i House_1 (Child)
            _visualModelRenderer = GetComponentInChildren<Renderer>();

            if (_visualModelRenderer != null)
            {
                // Vi bruger .material (instans) fremfor .sharedMaterial for ikke at ændre projekt-filen
                _initialMaterialColor = _visualModelRenderer.material.color;
                _isControllerSuccessfullyInitialized = true;
                Debug.Log($"[CityInteraction] Initialiseret korrekt for {gameObject.name}.");
            }
            else
            {
                Debug.LogWarning($"[CityInteraction] Advarsel: Ingen Renderer fundet i børnene af {gameObject.name}. " +
                                 "Sørg for at dit hus (FBX) har en MeshRenderer.");
            }
        }

        private void OnMouseEnter()
        {
            if (!_isControllerSuccessfullyInitialized || _visualModelRenderer == null) return;
            _visualModelRenderer.material.color = Color.yellow;
        }

        private void OnMouseExit()
        {
            if (!_isControllerSuccessfullyInitialized || _visualModelRenderer == null) return;
            _visualModelRenderer.material.color = _initialMaterialColor;
        }

        private void OnMouseDown()
        {
            if (!_isControllerSuccessfullyInitialized || _associatedBuildingData == null) return;
            Debug.Log($"[CityView] Valgt bygning: {_associatedBuildingData.BuildingType} | Level: {_associatedBuildingData.CurrentLevel}");
        }
    }
}