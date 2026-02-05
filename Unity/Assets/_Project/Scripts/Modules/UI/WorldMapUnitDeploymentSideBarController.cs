using Project.Modules.City;
using Project.Scripts.Domain.DTOs;
using Project.Scripts.Domain.Enums;
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
        private ScrollView _unitDeploymentScrollView;

        // Vi holder styr på hvilke hære der er "foldet ud" i UI'en
        private HashSet<Guid> _expandedDeploymentIds = new HashSet<Guid>();

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            _rootVisualElement = uiDocument.rootVisualElement;
            _unitDeploymentScrollView = _rootVisualElement.Q<ScrollView>("UnitDeploymentBar-ScrollView");

            if (CityStateManager.Instance != null)
            {
                // Vi lytter på opdateringer af spillerens deployments
                CityStateManager.Instance.OnDeploymentsStateReceived += HandleDeploymentsUpdated;

                // Initial tegn hvis data allerede findes
                RefreshUnitDeploymentList(CityStateManager.Instance.CurrentActiveDeployments);
            }
        }

        private void OnDisable()
        {
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnDeploymentsStateReceived -= HandleDeploymentsUpdated;
            }
        }

        private void HandleDeploymentsUpdated(List<UnitDeploymentDTO> deployments)
        {
            RefreshUnitDeploymentList(deployments);
        }

        private void RefreshUnitDeploymentList(List<UnitDeploymentDTO> deployments)
        {
            if (_unitDeploymentScrollView == null) return;

            _unitDeploymentScrollView.Clear();

            if (deployments == null || deployments.Count == 0)
            {
                Label emptyLabel = new Label("No active expeditions.");
                emptyLabel.AddToClassList("detail-label");
                emptyLabel.style.marginTop = 20;
                emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                _unitDeploymentScrollView.Add(emptyLabel);
                return;
            }

            foreach (var deployment in deployments)
            {
                _unitDeploymentScrollView.Add(CreateUnitDeploymentEntry(deployment));
            }
        }

        private VisualElement CreateUnitDeploymentEntry(UnitDeploymentDTO data)
        {
            // 1. Container
            VisualElement entryContainer = new VisualElement();
            entryContainer.AddToClassList("UnitDeployment-entry-container");

            // 2. Header (Den linje man altid ser)
            VisualElement headerContainer = new VisualElement();
            headerContainer.AddToClassList("UnitDeployment-entry-header");

            int totalStrength = data.UnitStacks.Sum(s => s.Quantity);
            Label nameLabel = new Label(data.WorldPlayerUserName.ToUpper());
            nameLabel.AddToClassList("UnitDeployment-name-label");

            Label sizeLabel = new Label($"{totalStrength} Units");
            sizeLabel.AddToClassList("UnitDeployment-size-badge");

            headerContainer.Add(nameLabel);
            headerContainer.Add(sizeLabel);

            // 3. Details (Det der kan foldes ud)
            VisualElement detailsContainer = new VisualElement();
            detailsContainer.AddToClassList("UnitDeployment-details-container");

            if (_expandedDeploymentIds.Contains(data.Id))
            {
                detailsContainer.AddToClassList("expanded");
            }

            // Info rækker
            detailsContainer.Add(CreateDetailRow("ORIGIN", data.OriginCity?.CityName ?? "Unknown"));
            detailsContainer.Add(CreateDetailRow("TARGET", data.TargetCity?.CityName ?? $"{data.FinalX}, {data.FinalY}"));
            detailsContainer.Add(CreateDetailRow("STATUS", data.Status.ToString()));

            if (data.Status == UnitDeploymentMovementStatusEnum.Moving)
            {
                detailsContainer.Add(CreateDetailRow("NEXT STEP", data.NextStepTime.ToLocalTime().ToString("HH:mm:ss")));
                detailsContainer.Add(CreateDetailRow("ARRIVAL", data.ArrivalTime?.ToLocalTime().ToString("HH:mm:ss") ?? "--"));
            }

            // Unit Stacks Liste
            VisualElement stacksListContainer = new VisualElement();
            stacksListContainer.AddToClassList("unit-stack-list");
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
                stacksListContainer.Add(stackRow);
            }
            detailsContainer.Add(stacksListContainer);

            // 4. Click Event for Toggle
            headerContainer.RegisterCallback<ClickEvent>(evt => {
                if (_expandedDeploymentIds.Contains(data.Id))
                {
                    _expandedDeploymentIds.Remove(data.Id);
                    detailsContainer.RemoveFromClassList("expanded");
                }
                else
                {
                    _expandedDeploymentIds.Add(data.Id);
                    detailsContainer.AddToClassList("expanded");
                }
            });

            entryContainer.Add(headerContainer);
            entryContainer.Add(detailsContainer);

            return entryContainer;
        }

        private VisualElement CreateDetailRow(string labelText, string valueText)
        {
            VisualElement rowContainer = new VisualElement();
            rowContainer.AddToClassList("detail-row");

            Label label = new Label(labelText);
            label.AddToClassList("detail-label");

            Label value = new Label(valueText);
            value.AddToClassList("detail-value");

            rowContainer.Add(label);
            rowContainer.Add(value);
            return rowContainer;
        }
    }
}