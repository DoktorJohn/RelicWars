using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using Assets._Project.Scripts.Domain.Enums;
using Project.Scripts.Modules.UI;
using Assets.Scripts.Domain.Enums;
using Project.Network.Manager;
using Project.Modules.UI;
using Project.Network.Models;

namespace Project.Modules.City
{
    /// <summary>
    /// Controls visual interaction for complex building objects consisting of multiple sprites.
    /// Manages highlighting of all child renderers simultaneously.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class CityBuildingInteractionController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private CityControllerGetDetailedCityInformationBuildingDTO _associatedBuildingData;

        // Liste over alle renderere under dette prefab (huse, træer, detaljer osv.)
        private SpriteRenderer[] _allChildSpriteRenderers;

        // Vi gemmer de oprindelige farver, så vi ikke ødelægger evt. unik toning pr. sprite
        private Dictionary<SpriteRenderer, Color> _originalRendererColors = new Dictionary<SpriteRenderer, Color>();

        private bool _isControllerSuccessfullyInitialized = false;

        // Highlight farve - kan gøres SerializeField hvis ønsket
        private readonly Color _highlightColorTint = Color.yellow;

        /// <summary>
        /// Collects all child renderers and maps their initial states.
        /// </summary>
        public void InitializeBuildingInteractionData(CityControllerGetDetailedCityInformationBuildingDTO buildingData)
        {
            _associatedBuildingData = buildingData;

            // Find ALLE SpriteRenderere i hele hierarkiet under dette objekt
            _allChildSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();

            if (_allChildSpriteRenderers != null && _allChildSpriteRenderers.Length > 0)
            {
                _originalRendererColors.Clear();

                foreach (var renderer in _allChildSpriteRenderers)
                {
                    // Gem den originale farve for hver enkelt del
                    if (!_originalRendererColors.ContainsKey(renderer))
                    {
                        _originalRendererColors.Add(renderer, renderer.color);
                    }
                }

                _isControllerSuccessfullyInitialized = true;
                Debug.Log($"<color=cyan>[CityInteraction]</color> Initialized {gameObject.name} with {_allChildSpriteRenderers.Length} sprites as {buildingData.BuildingType}");
            }
            else
            {
                Debug.LogError($"<color=red>[CityInteraction]</color> CRITICAL: No SpriteRenderers found on {gameObject.name} or its children.");
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isControllerSuccessfullyInitialized) return;
            ApplyHighlightEffect(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isControllerSuccessfullyInitialized) return;
            ApplyHighlightEffect(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ExecuteInteractionLogic();
        }

        /// <summary>
        /// Iterates through all tracked renderers and applies or removes the highlight tint.
        /// </summary>
        private void ApplyHighlightEffect(bool shouldEnableHighlight)
        {
            foreach (var renderer in _allChildSpriteRenderers)
            {
                if (renderer == null) continue;

                if (shouldEnableHighlight)
                {
                    // Vi multiplicerer med gul for at bevare evt. alpha-kanal (gennemsigtighed)
                    renderer.color = _highlightColorTint;
                }
                else
                {
                    // Gendan den præcise farve spriten havde før hover
                    if (_originalRendererColors.TryGetValue(renderer, out Color originalColor))
                    {
                        renderer.color = originalColor;
                    }
                }
            }
        }

        private void ExecuteInteractionLogic()
        {
            if (!_isControllerSuccessfullyInitialized || _associatedBuildingData == null)
            {
                Debug.LogError($"<color=red>[INTERACTION ERROR]</color> {gameObject.name} click ignored: Initialization failed.");
                return;
            }

            WindowTypeEnum targetWindowType = MapBuildingTypeToWindowType(_associatedBuildingData.BuildingType);

            if (targetWindowType != WindowTypeEnum.None)
            {
                Debug.Log($"<color=green>[UI REQUEST]</color> Opening {targetWindowType} for City: {NetworkManager.Instance.ActiveCityId}");
                GlobalWindowManager.Instance.OpenWindow(targetWindowType, null);
            }
        }

        private WindowTypeEnum MapBuildingTypeToWindowType(BuildingTypeEnum buildingType)
        {
            switch (buildingType)
            {
                case BuildingTypeEnum.TownHall: return WindowTypeEnum.TownHall;
                case BuildingTypeEnum.Barracks: return WindowTypeEnum.Barracks;
                case BuildingTypeEnum.Warehouse: return WindowTypeEnum.Warehouse;
                case BuildingTypeEnum.TimberCamp: return WindowTypeEnum.TimberCamp;
                case BuildingTypeEnum.StoneQuarry: return WindowTypeEnum.StoneQuarry;
                case BuildingTypeEnum.MetalMine: return WindowTypeEnum.MetalMine;
                case BuildingTypeEnum.Housing: return WindowTypeEnum.Housing;
                case BuildingTypeEnum.Wall: return WindowTypeEnum.Wall;
                case BuildingTypeEnum.University: return WindowTypeEnum.University;
                case BuildingTypeEnum.Stable: return WindowTypeEnum.Stable;
                case BuildingTypeEnum.Workshop: return WindowTypeEnum.Workshop;
                case BuildingTypeEnum.MarketPlace: return WindowTypeEnum.MarketPlace;
                default: return WindowTypeEnum.None;
            }
        }
    }
}