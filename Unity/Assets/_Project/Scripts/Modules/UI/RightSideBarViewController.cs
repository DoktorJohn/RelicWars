using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Project.Modules.City;
using Project.Network.Models;
using Assets.Scripts.Domain.Enums;

namespace Project.Modules.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class RightSideBarViewController : MonoBehaviour
    {
        private VisualElement _rootVisualElement;
        private VisualElement _enactFocusesButton;
        private ScrollView _unitCardsScrollContainer;

        private void OnEnable()
        {
            InitializeUserInterfaceRoots();
            RegisterButtonCallbacks();
            SubscribeToCityStateEvents();

            if (CityStateManager.Instance != null && CityStateManager.Instance.CurrentStationedUnits != null)
            {
                SynchronizeTroopDisplay(CityStateManager.Instance.CurrentStationedUnits);
            }
        }

        private void OnDisable()
        {
            UnregisterButtonCallbacks();
            UnsubscribeFromCityStateEvents();
        }

        private void InitializeUserInterfaceRoots()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument != null)
            {
                _rootVisualElement = uiDocument.rootVisualElement;

                _enactFocusesButton = _rootVisualElement.Q<VisualElement>("Button-Enact-Focuses");
                _unitCardsScrollContainer = _rootVisualElement.Q<ScrollView>("Container-Unit-Cards");

                ValidateInterfaceReferences();
            }
        }

        private void ValidateInterfaceReferences()
        {
            if (_enactFocusesButton == null) Debug.LogError("[HUD-Bottom] Enact Focuses Button reference missing.");
            if (_unitCardsScrollContainer == null) Debug.LogError("[HUD-Bottom] Unit Cards ScrollContainer reference missing.");
        }

        private void SubscribeToCityStateEvents()
        {
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnTroopsStateReceived += SynchronizeTroopDisplay;
            }
        }

        private void UnsubscribeFromCityStateEvents()
        {
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnTroopsStateReceived -= SynchronizeTroopDisplay;
            }
        }

        private void RegisterButtonCallbacks()
        {
            _enactFocusesButton?.RegisterCallback<ClickEvent>(OnEnactFocusesButtonClicked);
        }

        private void UnregisterButtonCallbacks()
        {
            _enactFocusesButton?.UnregisterCallback<ClickEvent>(OnEnactFocusesButtonClicked);
        }

        private void OnEnactFocusesButtonClicked(ClickEvent clickEvent)
        {
            if (GlobalWindowManager.Instance != null)
            {
                GlobalWindowManager.Instance.OpenWindow(WindowTypeEnum.IdeologyFocus);
            }
        }

        /// <summary>
        /// Transformerer troppe-data til individuelle visuelle kort i bunden af skærmen.
        /// </summary>
        public void SynchronizeTroopDisplay(List<UnitStackDTO> troops)
        {
            if (_unitCardsScrollContainer == null) return;

            _unitCardsScrollContainer.Clear();

            if (troops == null || troops.Count == 0) return;

            foreach (var unitStack in troops)
            {
                if (unitStack.Quantity <= 0) continue;

                VisualElement unitCard = CreateUnitDisplayCard(unitStack);
                _unitCardsScrollContainer.Add(unitCard);
            }
        }

        /// <summary>
        /// Konstruerer et visuelt enheds-kort med ikon, navn og antal.
        /// </summary>
        private VisualElement CreateUnitDisplayCard(UnitStackDTO unitData)
        {
            // Hovedkort
            VisualElement card = new VisualElement();
            card.AddToClassList("hud-card");

            // Ikon
            VisualElement icon = new VisualElement();
            icon.AddToClassList("hud-card-icon");
            icon.AddToClassList("icon-unit-default"); // Her kan du mappe til specifikke ikoner senere

            // Enhedsnavn
            Label nameLabel = new Label(unitData.Type.ToString());
            nameLabel.AddToClassList("hud-card-label");

            // Quantity Badge (Flydende oppe i hjørnet)
            VisualElement badge = new VisualElement();
            badge.AddToClassList("hud-card-quantity-badge");

            Label countLabel = new Label(unitData.Quantity.ToString("N0"));
            countLabel.AddToClassList("hud-card-quantity-text");

            badge.Add(countLabel);
            card.Add(badge);
            card.Add(icon);
            card.Add(nameLabel);

            return card;
        }
    }
}