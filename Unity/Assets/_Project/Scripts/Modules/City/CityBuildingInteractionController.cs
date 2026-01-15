using UnityEngine;
using UnityEngine.EventSystems; // Required for IPointer interfaces
using Assets.Scripts.Domain.Enums;
using Project.Modules.UI;
using Project.Network.Models; // Required for GlobalWindowManager & WindowTypeEnum

namespace Project.Modules.City
{
    /// <summary>
    /// Controls visual interaction for building objects in CityView.
    /// Implements New Input System compatible interfaces for click and hover effects.
    /// NOW INTEGRATED WITH: Global Window Manager Architecture.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class CityBuildingInteractionController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private CityControllerGetDetailedCityInformationBuildingDTO _associatedBuildingData;
        private Renderer _visualModelRenderer;
        private Color _initialMaterialColor;
        private bool _isControllerSuccessfullyInitialized = false;

        /// <summary>
        /// Links DTO data to the object and locates the renderer in hierarchy.
        /// </summary>
        public void InitializeBuildingInteractionData(CityControllerGetDetailedCityInformationBuildingDTO buildingData)
        {
            _associatedBuildingData = buildingData;

            // Find renderer in children (e.g., House_1)
            _visualModelRenderer = GetComponentInChildren<Renderer>();

            if (_visualModelRenderer != null)
            {
                // Create material instance to avoid modifying asset file
                _initialMaterialColor = _visualModelRenderer.material.color;
                _isControllerSuccessfullyInitialized = true;

                Debug.Log($"<color=cyan>[CityInteraction]</color> Initialized correctly for {gameObject.name} ({_associatedBuildingData.BuildingType}).");
            }
            else
            {
                Debug.LogWarning($"<color=yellow>[CityInteraction]</color> Warning: No Renderer found on {gameObject.name} or children.");
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isControllerSuccessfullyInitialized || _visualModelRenderer == null) return;
            _visualModelRenderer.material.color = Color.yellow; // Highlight
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isControllerSuccessfullyInitialized || _visualModelRenderer == null) return;
            _visualModelRenderer.material.color = _initialMaterialColor; // Restore
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ExecuteInteractionLogic();
        }

        private void ExecuteInteractionLogic()
        {
            Debug.Log($"<color=magenta><b>[INTERACTION TRIGGER]</b></color> Clicked on: {gameObject.name}");

            if (!_isControllerSuccessfullyInitialized || _associatedBuildingData == null)
            {
                Debug.LogError($"<color=red><b>[INTERACTION ERROR]</b></color> Click ignored: Not initialized or missing data.");
                return;
            }

            // --- NEW ARCHITECTURE INTEGRATION START ---

            // Map the specific building enum to a generic WindowTypeEnum
            WindowTypeEnum windowType = MapBuildingTypeToWindowType(_associatedBuildingData.BuildingType);

            if (windowType != WindowTypeEnum.None)
            {
                Debug.Log($"<color=green><b>[UI REQUEST]</b></color> Requesting GlobalWindowManager to open {windowType}.");

                // We pass the CityID if available, otherwise Manager uses active city
                // Assuming we want to open it for the CURRENT active city
                GlobalWindowManager.Instance.OpenWindow(windowType, null);
            }
            else
            {
                Debug.LogWarning($"<color=orange>[UI WARNING]</color> No window type defined for building: {_associatedBuildingData.BuildingType}");
            }

            // --- NEW ARCHITECTURE INTEGRATION END ---
        }

        /// <summary>
        /// Helper method to map the Domain Building Enum to the UI Window Enum.
        /// This decouples the game logic from the UI logic.
        /// </summary>
        private WindowTypeEnum MapBuildingTypeToWindowType(BuildingTypeEnum buildingType)
        {
            switch (buildingType)
            {
                case BuildingTypeEnum.Senate:
                    return WindowTypeEnum.Senate;

                case BuildingTypeEnum.Barracks:
                    return WindowTypeEnum.Barracks;

                case BuildingTypeEnum.Warehouse:
                    return WindowTypeEnum.Warehouse;

                case BuildingTypeEnum.TimberCamp:
                    return WindowTypeEnum.TimberCamp;

                case BuildingTypeEnum.StoneQuarry:
                    return WindowTypeEnum.StoneQuarry;

                case BuildingTypeEnum.MetalMine:
                    return WindowTypeEnum.MetalMine;

                case BuildingTypeEnum.Housing:
                    return WindowTypeEnum.Housing;

                case BuildingTypeEnum.Wall:
                    return WindowTypeEnum.Wall;

                case BuildingTypeEnum.Academy:
                    return WindowTypeEnum.Academy;

                case BuildingTypeEnum.Stable:
                    return WindowTypeEnum.Stable;

                case BuildingTypeEnum.Workshop:
                    return WindowTypeEnum.Workshop;

                // Add other mappings here as you create windows for them
                // case BuildingTypeEnum.TimberCamp: return WindowTypeEnum.ProductionBuilding;

                default:
                    // Return None if we don't have a window for this building yet
                    return WindowTypeEnum.None;
            }
        }
    }
}