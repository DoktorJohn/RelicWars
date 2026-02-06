using Project.Modules.City;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using Project.Scripts.Domain.Enums;
using Project.Scripts.Modules.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.Modules.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class WorldMapUnitDeploymentSideBarController : MonoBehaviour
    {
        private VisualElement _rootVisualElement;
        private VisualElement _mainUnitDeploymentContainer;
        private ScrollView _unitDeploymentScrollView;

        private bool _isSidebarMinimized = false;
        private HashSet<Guid> _expandedUnitDeploymentIds = new HashSet<Guid>();

        private void OnEnable()
        {
            var uiDocumentComponent = GetComponent<UIDocument>();
            if (uiDocumentComponent == null) return;

            _rootVisualElement = uiDocumentComponent.rootVisualElement;
            _mainUnitDeploymentContainer = _rootVisualElement.Q<VisualElement>("UnitDeploymentBar-MainContainer");
            _unitDeploymentScrollView = _rootVisualElement.Q<ScrollView>("UnitDeploymentBar-ScrollView");

            if (_mainUnitDeploymentContainer != null)
            {
                _mainUnitDeploymentContainer.RegisterCallback<PointerEnterEvent>(evt =>
                {
                    WorldMapInteractionHandler.Instance?.SetMouseOverUI(true);
                });

                _mainUnitDeploymentContainer.RegisterCallback<PointerLeaveEvent>(evt =>
                {
                    WorldMapInteractionHandler.Instance?.SetMouseOverUI(false);
                });
            }

            var headerElement = _rootVisualElement.Q<VisualElement>("UnitDeploymentBar-Header");
            if (headerElement != null)
            {
                headerElement.RegisterCallback<ClickEvent>(evt => ExecuteSidebarMinimizeToggle());
            }

            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnDeploymentsStateReceived += HandleUnitDeploymentsStateReceived;
            }

            if (WorldMapStateManager.Instance != null)
            {
                WorldMapStateManager.Instance.OnChunkDataReady += HandleWorldMapChunkDataUpdateReceived;
            }

            RefreshUnitDeploymentList(CityStateManager.Instance?.CurrentActiveDeployments);
        }

        private void OnDisable()
        {
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnDeploymentsStateReceived -= HandleUnitDeploymentsStateReceived;
            }

            if (WorldMapStateManager.Instance != null)
            {
                WorldMapStateManager.Instance.OnChunkDataReady -= HandleWorldMapChunkDataUpdateReceived;
            }

            WorldMapInteractionHandler.Instance?.SetMouseOverUI(false);
        }

        private void ExecuteSidebarMinimizeToggle()
        {
            _isSidebarMinimized = !_isSidebarMinimized;
            if (_isSidebarMinimized)
            {
                _mainUnitDeploymentContainer.AddToClassList("minimized");
                WorldMapInteractionHandler.Instance?.SetMouseOverUI(false);
            }
            else
            {
                _mainUnitDeploymentContainer.RemoveFromClassList("minimized");
            }
        }

        private void HandleUnitDeploymentsStateReceived(List<UnitDeploymentDTO> deployments)
        {
            RefreshUnitDeploymentList(deployments);
        }

        private void HandleWorldMapChunkDataUpdateReceived(WorldMapChunkResponseDTO chunkData)
        {
            if (chunkData == null || CityStateManager.Instance == null) return;

            var globalDeployments = CityStateManager.Instance.CurrentActiveDeployments;
            bool stateChanged = false;

            if (chunkData.UnitDeployments != null)
            {
                foreach (var mapUnit in chunkData.UnitDeployments)
                {
                    if (mapUnit.WorldPlayerId != Guid.Parse(NetworkManager.Instance.WorldPlayerId)) continue;

                    bool isAtHome = mapUnit.Status == UnitDeploymentMovementStatusEnum.Stationed &&
                                   mapUnit.CurrentX == CityStateManager.Instance.HomeCityX &&
                                   mapUnit.CurrentY == CityStateManager.Instance.HomeCityY;

                    var existingUnit = globalDeployments.FirstOrDefault(d => d.Id == mapUnit.Id);

                    if (isAtHome)
                    {
                        if (existingUnit != null)
                        {
                            Debug.Log($"<color=orange>[SideBar Sync]</color> ABSORPTION: Sletter {mapUnit.Id} fra UI (Er nået hjem).");
                            globalDeployments.Remove(existingUnit);
                            WorldMapEntityManager.Instance?.RemoveUnitVisualExplicitly(mapUnit.Id);
                            stateChanged = true;
                        }
                        continue;
                    }

                    if (existingUnit != null)
                    {
                        int index = globalDeployments.IndexOf(existingUnit);
                        globalDeployments[index] = mapUnit;
                        stateChanged = true;
                    }
                    else
                    {
                        globalDeployments.Add(mapUnit);
                        stateChanged = true;
                    }
                }
            }

            var deploymentsToRemove = globalDeployments
                .Where(d => d.CurrentX >= chunkData.ChunkX && d.CurrentX < chunkData.ChunkX + chunkData.Width &&
                            d.CurrentY >= chunkData.ChunkY && d.CurrentY < chunkData.ChunkY + chunkData.Height)
                .Where(d => chunkData.UnitDeployments == null || !chunkData.UnitDeployments.Any(mu => mu.Id == d.Id))
                .ToList();

            foreach (var toRemove in deploymentsToRemove)
            {
                Debug.Log($"<color=red>[SideBar Sync]</color> Server har fjernet hær {toRemove.Id}. Sletter visual.");
                globalDeployments.Remove(toRemove);
                WorldMapEntityManager.Instance?.RemoveUnitVisualExplicitly(toRemove.Id);
                stateChanged = true;
            }

            if (stateChanged)
            {
                RefreshUnitDeploymentList(globalDeployments);
            }
        }

        private void RefreshUnitDeploymentList(List<UnitDeploymentDTO> deployments)
        {
            if (_unitDeploymentScrollView == null) return;
            _unitDeploymentScrollView.Clear();

            if (deployments == null || deployments.Count == 0)
            {
                Label emptyLabel = new Label("NO ACTIVE EXPEDITIONS");
                emptyLabel.AddToClassList("detail-label");
                emptyLabel.style.marginTop = 20;
                emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                _unitDeploymentScrollView.Add(emptyLabel);
                return;
            }

            foreach (var deployment in deployments.OrderBy(d => d.WorldPlayerUserName))
            {
                _unitDeploymentScrollView.Add(CreateUnitDeploymentVisualEntry(deployment));
            }
        }

        private VisualElement CreateUnitDeploymentVisualEntry(UnitDeploymentDTO data)
        {
            VisualElement entryContainer = new VisualElement();
            entryContainer.AddToClassList("UnitDeployment-entry-container");

            VisualElement headerContainer = new VisualElement();
            headerContainer.AddToClassList("UnitDeployment-entry-header");

            int totalUnitStrength = data.UnitStacks.Sum(stack => stack.Quantity);

            // Navnebegrænsning: Maks 30 tegn
            string processedUnitDeploymentName = data.Name.Length > 20
                ? data.Name.Substring(0, 20) + "..."
                : data.Name;

            Label unitNameLabel = new Label(processedUnitDeploymentName.ToUpper());
            unitNameLabel.AddToClassList("UnitDeployment-name-label");

            Label unitSizeBadgeLabel = new Label($"{totalUnitStrength} UNITS");
            unitSizeBadgeLabel.AddToClassList("UnitDeployment-size-badge");

            headerContainer.Add(unitNameLabel);
            headerContainer.Add(unitSizeBadgeLabel);

            VisualElement detailsContainer = new VisualElement();
            detailsContainer.AddToClassList("UnitDeployment-details-container");

            if (_expandedUnitDeploymentIds.Contains(data.Id))
            {
                detailsContainer.AddToClassList("expanded");
            }

            detailsContainer.Add(CreateUnitDeploymentDetailRow("ORIGIN", data.OriginCity?.CityName ?? "UNKNOWN"));
            detailsContainer.Add(CreateUnitDeploymentDetailRow("LOCATION", $"{data.CurrentX}, {data.CurrentY}"));
            detailsContainer.Add(CreateUnitDeploymentDetailRow("TARGET", data.TargetCity?.CityName ?? $"{data.FinalX}, {data.FinalY}"));
            detailsContainer.Add(CreateUnitDeploymentDetailRow("STATUS", data.Status.ToString().ToUpper()));

            detailsContainer.Add(CreateUnitDeploymentDetailRow("NEXT STEP", data.NextStepTime.ToString("HH:mm:ss")));
            string arrivalTimeString = data.ArrivalTime.HasValue ? data.ArrivalTime.Value.ToString("HH:mm:ss") : "--:--:--";
            detailsContainer.Add(CreateUnitDeploymentDetailRow("ARRIVAL", arrivalTimeString));

            if (data.Status == UnitDeploymentMovementStatusEnum.Moving)
            {
                Button abortButton = new Button { text = "ABORT MARCH" };
                abortButton.AddToClassList("btn-global-base");
                abortButton.AddToClassList("btn-imperial-danger");
                abortButton.style.marginTop = 15;
                abortButton.clicked += () => ExecuteAbortMovementRequest(data.Id);
                detailsContainer.Add(abortButton);
            }

            Button returnButton = new Button { text = "RETURN TO CITY" };
            returnButton.AddToClassList("btn-global-base");
            returnButton.AddToClassList("btn-imperial-primary");
            returnButton.style.marginTop = 5;
            returnButton.clicked += () => ExecuteReturnToOriginRequest(data.Id);
            detailsContainer.Add(returnButton);

            VisualElement unitStackListContainer = new VisualElement();
            unitStackListContainer.AddToClassList("unit-stack-list");

            foreach (var stack in data.UnitStacks)
            {
                VisualElement stackRow = new VisualElement();
                stackRow.AddToClassList("unit-stack-row");

                Label typeLabel = new Label(stack.Type.ToString());
                typeLabel.AddToClassList("unit-stack-text");
                Label quantityLabel = new Label(stack.Quantity.ToString());
                quantityLabel.AddToClassList("unit-stack-text");

                stackRow.Add(typeLabel);
                stackRow.Add(quantityLabel);
                unitStackListContainer.Add(stackRow);
            }
            detailsContainer.Add(unitStackListContainer);

            headerContainer.RegisterCallback<ClickEvent>(evt =>
            {
                if (_expandedUnitDeploymentIds.Contains(data.Id))
                {
                    _expandedUnitDeploymentIds.Remove(data.Id);
                    detailsContainer.RemoveFromClassList("expanded");
                }
                else
                {
                    _expandedUnitDeploymentIds.Add(data.Id);
                    detailsContainer.AddToClassList("expanded");
                }
                evt.StopPropagation();
            });

            entryContainer.Add(headerContainer);
            entryContainer.Add(detailsContainer);

            return entryContainer;
        }

        private void ExecuteAbortMovementRequest(Guid deploymentId)
        {
            string authenticationToken = NetworkManager.Instance.JwtToken;
            StartCoroutine(NetworkManager.Instance.UnitDeployment.AbortMovementUnits(deploymentId, authenticationToken, (updatedDeployment) =>
            {
                if (updatedDeployment != null)
                {
                    WorldMapStateManager.Instance.UpdateDeploymentInCache(updatedDeployment);
                }
            }));
        }

        private void ExecuteReturnToOriginRequest(Guid deploymentId)
        {
            string authenticationToken = NetworkManager.Instance.JwtToken;
            Debug.Log($"<color=cyan>[SideBar]</color> Bruger klikkede RETURN for {deploymentId}");

            StartCoroutine(NetworkManager.Instance.UnitDeployment.ReturnToOriginCityUnits(deploymentId, authenticationToken, (updatedDeployment) =>
            {
                if (updatedDeployment != null)
                {
                    bool shouldBeRemoved = updatedDeployment.Status == UnitDeploymentMovementStatusEnum.Stationed &&
                                         updatedDeployment.CurrentX == CityStateManager.Instance.HomeCityX &&
                                         updatedDeployment.CurrentY == CityStateManager.Instance.HomeCityY;

                    if (shouldBeRemoved)
                    {
                        var globalList = CityStateManager.Instance.CurrentActiveDeployments;
                        globalList.RemoveAll(d => d.Id == updatedDeployment.Id);
                        WorldMapEntityManager.Instance?.RemoveUnitVisualExplicitly(updatedDeployment.Id);
                        RefreshUnitDeploymentList(globalList);
                    }
                    else
                    {
                        WorldMapStateManager.Instance.UpdateDeploymentInCache(updatedDeployment);
                    }
                }
            }));
        }

        private VisualElement CreateUnitDeploymentDetailRow(string labelText, string valueText)
        {
            VisualElement rowContainer = new VisualElement();
            rowContainer.AddToClassList("detail-row");

            Label titleLabel = new Label(labelText);
            titleLabel.AddToClassList("detail-label");

            Label valueLabel = new Label(valueText);
            valueLabel.AddToClassList("detail-value");

            rowContainer.Add(titleLabel);
            rowContainer.Add(valueLabel);
            return rowContainer;
        }
    }
}