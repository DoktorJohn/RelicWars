using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Project.Modules.City;
using Project.Network.Models;
using Assets.Scripts.Domain.Enums;

namespace Project.Modules.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class UnitStackIdeologyController : MonoBehaviour
    {
        private VisualElement _rootVisualElement;
        private VisualElement _enactFocusesButton;
        private ScrollView _unitCardsScrollContainer;
        private Label _currentCityLabel;

        private void OnEnable()
        {
            InitializeUserInterfaceRoots();
            RegisterButtonCallbacks();
            SubscribeToCityStateEvents();

            if (CityStateManager.Instance != null && CityStateManager.Instance.CurrentStationedUnits != null)
            {
                SynchronizeTroopDisplay(CityStateManager.Instance.CurrentStationedUnits);
            }

            UpdateCurrentCityLabel(CityStateManager.Instance?.CurrentCityName);
        }

        private void OnDisable()
        {
            ResponsiveUiStateManager.UnregisterRoot(_rootVisualElement);
            UnregisterButtonCallbacks();
            UnsubscribeFromCityStateEvents();
        }

        private void InitializeUserInterfaceRoots()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument != null)
            {
                _rootVisualElement = uiDocument.rootVisualElement;
                ResponsiveUiStateManager.RegisterRoot(_rootVisualElement);

                _enactFocusesButton = _rootVisualElement.Q<VisualElement>("Button-Enact-Focuses");
                _unitCardsScrollContainer = _rootVisualElement.Q<ScrollView>("Container-Unit-Cards");
                _currentCityLabel = _rootVisualElement.Q<Label>("City-Command-CurrentCity-Label");

                ValidateInterfaceReferences();
            }
        }

        private void ValidateInterfaceReferences()
        {
            if (_enactFocusesButton == null) Debug.LogError("[HUD-Bottom] Enact Focuses Button reference missing.");
            if (_unitCardsScrollContainer == null) Debug.LogError("[HUD-Bottom] Unit Cards ScrollContainer reference missing.");
            if (_currentCityLabel == null) Debug.LogError("[HUD-Bottom] Current City Label reference missing.");
        }

        private void SubscribeToCityStateEvents()
        {
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnTroopsStateReceived += SynchronizeTroopDisplay;
                CityStateManager.Instance.OnCityNameChanged += UpdateCurrentCityLabel;
            }
        }

        private void UnsubscribeFromCityStateEvents()
        {
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnTroopsStateReceived -= SynchronizeTroopDisplay;
                CityStateManager.Instance.OnCityNameChanged -= UpdateCurrentCityLabel;
            }
        }

        private void UpdateCurrentCityLabel(string cityName)
        {
            if (_currentCityLabel != null)
            {
                _currentCityLabel.text = cityName ?? string.Empty;
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

        public void SynchronizeTroopDisplay(List<UnitStackDTO> troops)
        {
            if (_unitCardsScrollContainer == null) return;

            _unitCardsScrollContainer.Clear();

            if (troops == null || troops.TrueForAll(unitStack => unitStack.Quantity <= 0))
            {
                var emptyLabel = new Label("No units stationed");
                emptyLabel.AddToClassList("city-unit-list-empty");
                _unitCardsScrollContainer.Add(emptyLabel);
                return;
            }

            foreach (var unitStack in troops)
            {
                if (unitStack.Quantity <= 0) continue;

                _unitCardsScrollContainer.Add(CreateUnitDisplayRow(unitStack));
            }
        }

        private VisualElement CreateUnitDisplayRow(UnitStackDTO unitData)
        {
            var row = new VisualElement();
            row.AddToClassList("city-unit-row");

            var marker = new VisualElement();
            marker.AddToClassList("city-unit-row-marker");

            var nameLabel = new Label(unitData.Type.ToString());
            nameLabel.AddToClassList("city-unit-row-name");

            var countLabel = new Label(unitData.Quantity.ToString("N0"));
            countLabel.AddToClassList("city-unit-row-quantity");

            row.Add(marker);
            row.Add(nameLabel);
            row.Add(countLabel);
            return row;
        }
    }
}
