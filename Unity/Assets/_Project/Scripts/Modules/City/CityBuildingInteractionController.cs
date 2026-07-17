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
using UnityEngine.InputSystem;

namespace Project.Modules.City
{
    /// <summary>
    /// Kontrollerer visuel interaktion for komplekse bygnings-prefabs.
    /// Opdateret til at bruge Unity Input System pakken for at undgå InvalidOperationException.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class CityBuildingInteractionController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private CityControllerGetDetailedCityInformationBuildingDTO _associatedBuildingData;
        private SpriteRenderer[] _allChildSpriteRenderers;
        private readonly Dictionary<SpriteRenderer, Color> _originalRendererColors = new Dictionary<SpriteRenderer, Color>();

        private bool _isControllerSuccessfullyInitialized = false;
        private bool _isCurrentlyHighlighted = false;

        [Header("Highlight Settings")]
        [SerializeField] private Color _highlightColorTint = new Color(0.85f, 0.95f, 1.0f, 1.0f);
        [SerializeField] private bool _resetHighlightOnDisable = true;

        public void InitializeBuildingInteractionData(CityControllerGetDetailedCityInformationBuildingDTO buildingData)
        {
            _associatedBuildingData = buildingData;
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
        }

        private void Update()
        {
            if (!_isControllerSuccessfullyInitialized || !_isCurrentlyHighlighted) return;

            // Hvis IsPointerOverGameObject er true pga. HUD eller vinduer, tjekker vi om vi skal slukke highlight
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (IsPointerOverBlockingWindow())
                {
                    ApplyHighlightEffect(false);
                }
            }
        }

        /// <summary>
        /// Hjælpemetode til at afgøre om musen er over et UI-vindue.
        /// Bruger det nye Input System til at læse musens position.
        /// </summary>
        private bool IsPointerOverBlockingWindow()
        {
            if (EventSystem.current == null) return false;

            // FIX: Vi læser positionen fra det nye Input System i stedet for den gamle Input-klasse
            Vector2 currentMousePosition = Vector2.zero;

            if (Mouse.current != null)
            {
                currentMousePosition = Mouse.current.position.ReadValue();
            }
            else if (Pointer.current != null)
            {
                currentMousePosition = Pointer.current.position.ReadValue();
            }

            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = currentMousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

            foreach (var result in results)
            {
                // Vi tjekker om navnet indikerer et blokerende element
                if (result.gameObject.name.Contains("Window") || result.gameObject.name.Contains("Panel"))
                {
                    return true;
                }
            }
            return false;
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
            if (!_isControllerSuccessfullyInitialized) return;

            ApplyHighlightEffect(false);
            ExecuteInteractionLogic();
        }

        private void OnDisable()
        {
            if (_resetHighlightOnDisable)
            {
                ApplyHighlightEffect(false);
            }
        }

        private void ApplyHighlightEffect(bool shouldEnableHighlight)
        {
            if (_allChildSpriteRenderers == null || _isCurrentlyHighlighted == shouldEnableHighlight) return;

            foreach (var renderer in _allChildSpriteRenderers)
            {
                if (renderer == null) continue;
                renderer.color = shouldEnableHighlight ? _highlightColorTint : _originalRendererColors[renderer];
            }

            _isCurrentlyHighlighted = shouldEnableHighlight;
        }

        private void ExecuteInteractionLogic()
        {
            if (_associatedBuildingData == null) return;

            WindowTypeEnum targetWindowType = MapBuildingTypeToWindowType(_associatedBuildingData.BuildingType);
            if (targetWindowType != WindowTypeEnum.None)
            {
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
                BuildingTypeEnum.Harbor => WindowTypeEnum.Harbor,
                BuildingTypeEnum.MarketPlace => WindowTypeEnum.MarketPlace,
                _ => WindowTypeEnum.None
            };
        }
    }
}
