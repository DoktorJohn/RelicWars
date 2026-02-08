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

        // Liste over alle renderere under dette prefab
        private SpriteRenderer[] _allChildSpriteRenderers;

        // Vi gemmer de oprindelige farver for at kunne nulstille præcist
        private readonly Dictionary<SpriteRenderer, Color> _originalRendererColors = new Dictionary<SpriteRenderer, Color>();

        private bool _isControllerSuccessfullyInitialized = false;
        private bool _isCurrentlyHighlighted = false;

        [Header("Highlight Settings")]
        [SerializeField] private Color _highlightColorTint = new Color(0.85f, 0.95f, 1.0f, 1.0f); // En ren, kold hvid/blålig farve
        [SerializeField] private bool _resetHighlightOnDisable = true;

        /// <summary>
        /// Collects all child renderers and maps their initial states.
        /// </summary>
        public void InitializeBuildingInteractionData(CityControllerGetDetailedCityInformationBuildingDTO buildingData)
        {
            _associatedBuildingData = buildingData;

            // Find ALLE SpriteRenderere i hierarkiet
            _allChildSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();

            if (_allChildSpriteRenderers != null && _allChildSpriteRenderers.Length > 0)
            {
                _originalRendererColors.Clear();

                foreach (var renderer in _allChildSpriteRenderers)
                {
                    if (renderer != null && !_originalRendererColors.ContainsKey(renderer))
                    {
                        _originalRendererColors.Add(renderer, renderer.color);
                    }
                }

                _isControllerSuccessfullyInitialized = true;
                _isCurrentlyHighlighted = false;
            }
            else
            {
                Debug.LogError($"<color=red>[CityInteraction]</color> CRITICAL: No SpriteRenderers found on {gameObject.name}.");
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
            // Sikkerhed: Fjern highlight når vi klikker, da et vindue åbner og kan "låse" hover-staten
            ApplyHighlightEffect(false);
            ExecuteInteractionLogic();
        }

        private void OnDisable()
        {
            // Hvis objektet deaktiveres (f.eks. ved refresh eller sceneskift), sikrer vi at det ikke er gult næste gang
            if (_resetHighlightOnDisable)
            {
                ApplyHighlightEffect(false);
            }
        }

        /// <summary>
        /// Iterates through all tracked renderers and applies or removes the highlight tint.
        /// </summary>
        private void ApplyHighlightEffect(bool shouldEnableHighlight)
        {
            if (_allChildSpriteRenderers == null || _isCurrentlyHighlighted == shouldEnableHighlight) return;

            foreach (var renderer in _allChildSpriteRenderers)
            {
                if (renderer == null) continue;

                if (shouldEnableHighlight)
                {
                    renderer.color = _highlightColorTint;
                }
                else
                {
                    if (_originalRendererColors.TryGetValue(renderer, out Color originalColor))
                    {
                        renderer.color = originalColor;
                    }
                }
            }

            _isCurrentlyHighlighted = shouldEnableHighlight;
        }

        private void ExecuteInteractionLogic()
        {
            if (!_isControllerSuccessfullyInitialized || _associatedBuildingData == null)
            {
                return;
            }

            WindowTypeEnum targetWindowType = MapBuildingTypeToWindowType(_associatedBuildingData.BuildingType);

            if (targetWindowType != WindowTypeEnum.None)
            {
                Debug.Log($"<color=green>[UI REQUEST]</color> Opening {targetWindowType}");
                GlobalWindowManager.Instance.OpenWindow(targetWindowType, null);
            }
        }

        private WindowTypeEnum MapBuildingTypeToWindowType(BuildingTypeEnum buildingType)
        {
            return buildingType switch
            {
                BuildingTypeEnum.TownHall => WindowTypeEnum.TownHall,
                BuildingTypeEnum.Barracks => WindowTypeEnum.Barracks,
                BuildingTypeEnum.Warehouse => WindowTypeEnum.Warehouse,
                BuildingTypeEnum.TimberCamp => WindowTypeEnum.TimberCamp,
                BuildingTypeEnum.StoneQuarry => WindowTypeEnum.StoneQuarry,
                BuildingTypeEnum.MetalMine => WindowTypeEnum.MetalMine,
                BuildingTypeEnum.Housing => WindowTypeEnum.Housing,
                BuildingTypeEnum.Wall => WindowTypeEnum.Wall,
                BuildingTypeEnum.University => WindowTypeEnum.University,
                BuildingTypeEnum.Stable => WindowTypeEnum.Stable,
                BuildingTypeEnum.Workshop => WindowTypeEnum.Workshop,
                BuildingTypeEnum.MarketPlace => WindowTypeEnum.MarketPlace,
                _ => WindowTypeEnum.None
            };
        }
    }
}