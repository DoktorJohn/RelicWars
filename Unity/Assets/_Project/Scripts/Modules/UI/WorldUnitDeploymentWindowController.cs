using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using Project.Modules.UI.Windows;
using Project.Modules.City;
using Project.Scripts.Modules.Map;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;

namespace Project.Modules.UI.Windows.Implementations
{
    public class WorldUnitDeploymentWindowController : BaseWindow
    {
        protected override string WindowName => "WorldUnitDeploymentWindow";
        protected override string VisualContainerName => "Deployment-Window-MainContainer";
        protected override string HeaderName => "Deployment-Window-Header";

        private VisualElement _mainFrame;
        private MapInteractionPayload _payload;

        public override void OnOpen(object dataPayload)
        {
            if (dataPayload is MapInteractionPayload payload)
            {
                _payload = payload;
                _mainFrame = Root.Q<VisualElement>(VisualContainerName);

                PositionWindow();
                SetupEvents();

                Root.Q<Label>("Destination-Label").text = $"TARGET: {_payload.Coordinates.x}, {_payload.Coordinates.y}";
            }
        }

        private void PositionWindow()
        {
            Vector2 clickPos = _payload.ScreenClickPosition;
            _mainFrame.RegisterCallback<GeometryChangedEvent>(evt => {
                float x = Mathf.Clamp(clickPos.x, 0, Screen.width - _mainFrame.layout.width);
                float y = Mathf.Clamp(clickPos.y, 0, Screen.height - _mainFrame.layout.height);
                _mainFrame.style.left = x;
                _mainFrame.style.top = y;
            });
        }

        private void SetupEvents()
        {
            var moveBtn = Root.Q<Button>("Btn-Confirm-Move");
            var cancelBtn = Root.Q<Button>("Btn-Cancel-Command");

            moveBtn.clicked -= ExecuteMove;
            moveBtn.clicked += ExecuteMove;

            cancelBtn.clicked -= CancelAction;
            cancelBtn.clicked += CancelAction;
        }

        private void ExecuteMove()
        {
            Guid? selectedUnitId = WorldMapInteractionHandler.Instance.SelectedDeploymentId;
            if (selectedUnitId.HasValue)
            {
                var request = new MoveUnitRequestDTO
                {
                    UnitDeploymentId = selectedUnitId.Value,
                    TargetX = _payload.Coordinates.x,
                    TargetY = _payload.Coordinates.y
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
        }

        private void CancelAction()
        {
            WorldMapInteractionHandler.Instance.ClearSelection();
            Close();
        }
    }
}