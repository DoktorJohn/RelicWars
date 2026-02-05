using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using Project.Modules.UI.Windows;
using Project.Modules.City;
using Project.Scripts.Modules.Map;
using Project.Scripts.Domain.DTOs;
using Project.Network.Manager;
using Assets.Scripts.Domain.Enums;

namespace Project.Modules.UI.Windows.Implementations
{
    public class MapInteractionPayload
    {
        public Vector2Int Coordinates;
        public string BiomeName;
        public Guid? DeploymentIdOnTile;
        public bool IsPlayerOwned;
        public Vector2 ScreenClickPosition;
    }

    public class WorldHexagonWindowController : BaseWindow
    {
        protected override string WindowName => "WorldHexagonWindow";
        protected override string VisualContainerName => "Hexagon-Window-MainContainer";
        protected override string HeaderName => "Hexagon-Window-Header";

        [SerializeField] private VisualTreeAsset _unitRowTemplate;

        private VisualElement _deployView;
        private VisualElement _actionContainer;
        private ScrollView _unitListContainer;

        private MapInteractionPayload _currentPayload;
        private List<UnitSelectionDTO> _selectedUnits = new List<UnitSelectionDTO>();

        public override void OnOpen(object dataPayload)
        {
            if (dataPayload is MapInteractionPayload payload)
            {
                _currentPayload = payload;

                CacheRequiredVisualElements();
                InitialPositioning();
                UpdateDisplayInfo();
                RefreshContextualButtons();
                ResetDeploymentView();
            }
        }

        private void CacheRequiredVisualElements()
        {
            _deployView = Root.Q<VisualElement>("Deploy-Unit-View");
            _actionContainer = Root.Q<VisualElement>("Action-Container");
            _unitListContainer = Root.Q<ScrollView>("Unit-List-Container");

            // Header-Close-Button bliver automatisk håndteret af din BaseWindow.Initialize 
            // så længe navnet i UXML matcher {WindowName}-Close-Button.
        }

        private void InitialPositioning()
        {
            // Sætter vinduet til klik-positionen én gang ved åbning.
            // Din DragManipulator vil derefter overtage styringen herfra.
            Vector2 clickPos = _currentPayload.ScreenClickPosition;

            // Vi sikrer at vinduet ikke spawner uden for skærmen
            float x = Mathf.Clamp(clickPos.x, 0, Screen.width - 350);
            float y = Mathf.Clamp(clickPos.y, 0, Screen.height - 400);

            MainContainer.style.left = x;
            MainContainer.style.top = y;
        }

        private void UpdateDisplayInfo()
        {
            Root.Q<Label>("Target-Coordinates-Label").text = $"{_currentPayload.Coordinates.x} , {_currentPayload.Coordinates.y}";
            Root.Q<Label>("Biome-Type-Label").text = _currentPayload.BiomeName.Replace("_", " ").ToUpper();
        }

        private void RefreshContextualButtons()
        {
            var selectBtn = Root.Q<Button>("Btn-Select-Unit");
            var moveBtn = Root.Q<Button>("Btn-Move-Here");
            var deployTriggerBtn = Root.Q<Button>("Btn-Open-Deploy-View");

            if (_currentPayload.DeploymentIdOnTile.HasValue && _currentPayload.IsPlayerOwned)
            {
                selectBtn.RemoveFromClassList("hidden");
                selectBtn.clicked -= HandleSelectUnit;
                selectBtn.clicked += HandleSelectUnit;
            }
            else selectBtn.AddToClassList("hidden");

            if (WorldMapInteractionHandler.Instance.HasActiveSelection && !_currentPayload.DeploymentIdOnTile.HasValue)
            {
                moveBtn.RemoveFromClassList("hidden");
                moveBtn.clicked -= HandleMoveOrder;
                moveBtn.clicked += HandleMoveOrder;
            }
            else moveBtn.AddToClassList("hidden");

            deployTriggerBtn.clicked -= SwitchToDeployMode;
            deployTriggerBtn.clicked += SwitchToDeployMode;
        }

        private void ResetDeploymentView()
        {
            _deployView.AddToClassList("hidden");
            _actionContainer.RemoveFromClassList("hidden");
        }

        private void SwitchToDeployMode()
        {
            _actionContainer.AddToClassList("hidden");
            _deployView.RemoveFromClassList("hidden");
            PopulateUnitList();
        }

        private void PopulateUnitList()
        {
            _unitListContainer.Clear();
            _selectedUnits.Clear();

            var availableStacks = CityStateManager.Instance.CurrentStationedUnits;

            if (availableStacks == null || availableStacks.Count == 0)
            {
                // FIX: Korrekt måde at tilføje classList i Unity C#
                Label emptyLabel = new Label("No reserves available in city.");
                emptyLabel.AddToClassList("info-text");
                emptyLabel.style.marginTop = 20;
                emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                _unitListContainer.Add(emptyLabel);
                return;
            }

            foreach (var stack in availableStacks)
            {
                VisualElement row = _unitRowTemplate.Instantiate();
                row.Q<Label>("Unit-Name").text = stack.Type.ToString();
                row.Q<Label>("Available-Amount").text = $"In City: {stack.Quantity}";

                var inputField = row.Q<TextField>("Input-Amount");

                // Gør det lettere at bruge: Vælg alt tekst når man klikker
                inputField.RegisterCallback<FocusEvent>(evt => inputField.SelectAll());

                inputField.RegisterValueChangedCallback(evt => {
                    if (int.TryParse(evt.newValue, out int amount))
                    {
                        amount = Mathf.Clamp(amount, 0, stack.Quantity);
                        _selectedUnits.RemoveAll(u => u.Type == stack.Type);
                        if (amount > 0) _selectedUnits.Add(new UnitSelectionDTO { Type = stack.Type, Quantity = amount });
                    }
                });

                _unitListContainer.Add(row);
            }

            var confirmBtn = Root.Q<Button>("Btn-Confirm-Deployment");
            confirmBtn.clicked -= HandleDeployConfirm;
            confirmBtn.clicked += HandleDeployConfirm;
        }

        private void HandleSelectUnit()
        {
            WorldMapInteractionHandler.Instance.SetSelectedDeployment(_currentPayload.DeploymentIdOnTile.Value);
            Close();
        }

        private void HandleMoveOrder()
        {
            var unitId = WorldMapInteractionHandler.Instance.SelectedDeploymentId.Value;
            var request = new MoveUnitRequestDTO
            {
                UnitDeploymentId = unitId,
                TargetX = _currentPayload.Coordinates.x,
                TargetY = _currentPayload.Coordinates.y
            };

            StartCoroutine(NetworkManager.Instance.UnitDeployment.MoveUnits(request, NetworkManager.Instance.JwtToken, (res) => {
                if (res != null)
                {
                    WorldMapStateManager.Instance.UpdateDeploymentInCache(res);
                    WorldMapInteractionHandler.Instance.ClearSelection();
                    Close();
                }
            }));
        }

        private void HandleDeployConfirm()
        {
            if (!_selectedUnits.Any()) return;

            var request = new DeployUnitRequestDTO
            {
                OriginCityId = CityStateManager.Instance.CityId,
                TargetX = _currentPayload.Coordinates.x,
                TargetY = _currentPayload.Coordinates.y,
                UnitsToDeploy = _selectedUnits,
                WorldPlayerId = Guid.Parse(NetworkManager.Instance.WorldPlayerId)
            };

            StartCoroutine(NetworkManager.Instance.UnitDeployment.DeployUnits(request, NetworkManager.Instance.JwtToken, (res) => {
                if (res != null)
                {
                    WorldMapStateManager.Instance.RequestWorldMapChunkData((short)res.CurrentX, (short)res.CurrentY, 50, 50, true);
                    Close();
                }
            }));
        }
    }
}