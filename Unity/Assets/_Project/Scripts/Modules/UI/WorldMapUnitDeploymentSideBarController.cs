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
    /// <summary>
    /// Controller til sidebaren på verdenskortet, der viser aktive ekspeditioner (hære).
    /// Benytter de granulære events fra StateManagers til automatisk UI-opdatering.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class WorldMapUnitDeploymentSideBarController : MonoBehaviour
    {
        private VisualElement _rootVisualElement;
        private VisualElement _mainUnitDeploymentContainer;
        private ScrollView _unitDeploymentScrollView;

        private bool _isSidebarMinimized = false;
        private readonly HashSet<Guid> _expandedUnitDeploymentIds = new();

        private void OnEnable()
        {
            InitializeUserInterface();
            SubscribeToStateEvents();

            // Initial render baseret på hvad managerne ved lige nu
            RefreshUnitDeploymentList(CityStateManager.Instance?.CurrentActiveDeployments);
        }

        private void OnDisable()
        {
            UnsubscribeFromStateEvents();
            WorldMapInteractionHandler.Instance?.SetMouseOverUI(false);
        }

        private void InitializeUserInterface()
        {
            var uiDocumentComponent = GetComponent<UIDocument>();
            if (uiDocumentComponent == null) return;

            _rootVisualElement = uiDocumentComponent.rootVisualElement;
            _mainUnitDeploymentContainer = _rootVisualElement.Q<VisualElement>("UnitDeploymentBar-MainContainer");
            _unitDeploymentScrollView = _rootVisualElement.Q<ScrollView>("UnitDeploymentBar-ScrollView");

            if (_mainUnitDeploymentContainer != null)
            {
                _mainUnitDeploymentContainer.RegisterCallback<PointerEnterEvent>(evt => WorldMapInteractionHandler.Instance?.SetMouseOverUI(true));
                _mainUnitDeploymentContainer.RegisterCallback<PointerLeaveEvent>(evt => WorldMapInteractionHandler.Instance?.SetMouseOverUI(false));
            }

            var headerElement = _rootVisualElement.Q<VisualElement>("UnitDeploymentBar-Header");
            headerElement?.RegisterCallback<ClickEvent>(evt => ExecuteSidebarMinimizeToggle());
        }

        private void SubscribeToStateEvents()
        {
            // Vi lytter nu KUN på WorldMapStateManager
            if (WorldMapStateManager.Instance != null)
            {
                WorldMapStateManager.Instance.OnUnitDeploymentsStateChanged += HandleMapDeploymentsUpdated;
            }
        }

        private void UnsubscribeFromStateEvents()
        {
            if (WorldMapStateManager.Instance != null)
            {
                WorldMapStateManager.Instance.OnUnitDeploymentsStateChanged -= HandleMapDeploymentsUpdated;
            }
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

        /// <summary>
        /// Modtager opdateringer for spillerens egne hære fra CityStateManager.
        /// </summary>
        private void HandleUnitDeploymentsStateReceived(List<UnitDeploymentDTO> deployments)
        {
            RefreshUnitDeploymentList(deployments);
        }

        /// <summary>
        /// Modtager realtids-opdateringer for alle synlige hære på kortet.
        /// Vi filtrerer her for at sikre, at vi kun viser spillerens egne hære i ekspeditions-listen.
        /// </summary>
        private void HandleMapDeploymentsUpdated(List<UnitDeploymentDTO> mapDeployments)
        {
            if (mapDeployments == null) return;

            string myPlayerId = NetworkManager.Instance.WorldPlayerId;

            // OBJEKTIV LOGIK: Vi viser kun hære i sidebaren der tilhører spilleren selv
            var myVisibleArmies = mapDeployments
                .Where(d => d.WorldPlayerId.ToString() == myPlayerId)
                .ToList();

            // Hvis manageren har opdaget ændringer i mine hære på kortet, opdaterer vi listen
            if (myVisibleArmies.Count > 0)
            {
                // Vi bruger WorldMapStateManagerens AllVisibleDeployments som 'Master List' 
                // for at sikre at vi ikke mister hære der er uden for den nuværende chunk-opdatering.
                var allMyArmies = WorldMapStateManager.Instance.AllVisibleDeployments
                    .Where(d => d.WorldPlayerId.ToString() == myPlayerId)
                    .OrderBy(d => d.Name)
                    .ToList();

                RefreshUnitDeploymentList(allMyArmies);
            }
        }

        private void RefreshUnitDeploymentList(List<UnitDeploymentDTO> deployments)
        {
            if (_unitDeploymentScrollView == null) return;
            _unitDeploymentScrollView.Clear();

            // Filtrering: Vi viser ikke hære der er 'Stationed' i hjembyen i sidebaren (de er inde i kasernen)
            var activeExpeditions = deployments?
                .Where(d => d.Status != UnitDeploymentMovementStatusEnum.Stationed ||
                            d.CurrentX != CityStateManager.Instance.HomeCityX ||
                            d.CurrentY != CityStateManager.Instance.HomeCityY)
                .OrderBy(d => d.Name)
                .ToList();

            if (activeExpeditions == null || activeExpeditions.Count == 0)
            {
                RenderEmptyState();
                return;
            }

            foreach (var deployment in activeExpeditions)
            {
                _unitDeploymentScrollView.Add(CreateUnitDeploymentVisualEntry(deployment));
            }
        }

        private void RenderEmptyState()
        {
            Label emptyLabel = new Label("NO ACTIVE EXPEDITIONS");
            emptyLabel.AddToClassList("detail-label");
            emptyLabel.style.marginTop = 20;
            emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _unitDeploymentScrollView.Add(emptyLabel);
        }

        private VisualElement CreateUnitDeploymentVisualEntry(UnitDeploymentDTO data)
        {
            VisualElement entryContainer = new VisualElement();
            entryContainer.AddToClassList("UnitDeployment-entry-container");

            // Header
            VisualElement headerContainer = new VisualElement();
            headerContainer.AddToClassList("UnitDeployment-entry-header");

            int totalUnits = data.UnitStacks.Sum(s => s.Quantity);
            string displayName = data.Name.Length > 20 ? data.Name[..20] + "..." : data.Name;

            headerContainer.Add(new Label(displayName.ToUpper()) { name = "UnitDeployment-name-label" });
            headerContainer.Add(new Label($"{totalUnits} UNITS") { name = "UnitDeployment-size-badge" });

            // Details Container
            VisualElement detailsContainer = new VisualElement();
            detailsContainer.AddToClassList("UnitDeployment-details-container");
            if (_expandedUnitDeploymentIds.Contains(data.Id)) detailsContainer.AddToClassList("expanded");

            // Data Rækker
            detailsContainer.Add(CreateUnitDeploymentDetailRow("ORIGIN", data.OriginCity?.CityName ?? "UNKNOWN"));
            detailsContainer.Add(CreateUnitDeploymentDetailRow("LOCATION", $"{data.CurrentX}, {data.CurrentY}"));
            detailsContainer.Add(CreateUnitDeploymentDetailRow("TARGET", data.TargetCity?.CityName ?? $"{data.FinalX}, {data.FinalY}"));
            detailsContainer.Add(CreateUnitDeploymentDetailRow("STATUS", data.Status.ToString().ToUpper()));

            string arrivalStr = data.ArrivalTime.HasValue ? data.ArrivalTime.Value.ToString("HH:mm:ss") : "--:--:--";
            detailsContainer.Add(CreateUnitDeploymentDetailRow("ARRIVAL", arrivalStr));

            // Controls
            if (data.Status == UnitDeploymentMovementStatusEnum.Moving)
            {
                Button abortBtn = new Button(() => ExecuteAbortMovementRequest(data.Id)) { text = "ABORT MARCH" };
                abortBtn.AddToClassList("btn-imperial-danger");
                detailsContainer.Add(abortBtn);
            }

            Button returnBtn = new Button(() => ExecuteReturnToOriginRequest(data.Id)) { text = "RETURN TO CITY" };
            returnBtn.AddToClassList("btn-imperial-primary");
            detailsContainer.Add(returnBtn);

            // Unit Composition
            VisualElement stackList = new VisualElement();
            stackList.AddToClassList("unit-stack-list");
            foreach (var stack in data.UnitStacks)
            {
                VisualElement row = new VisualElement();
                row.AddToClassList("unit-stack-row");
                row.Add(new Label(stack.Type.ToString()) { name = "unit-stack-text" });
                row.Add(new Label(stack.Quantity.ToString()) { name = "unit-stack-text" });
                stackList.Add(row);
            }
            detailsContainer.Add(stackList);

            // Click to Expand
            headerContainer.RegisterCallback<ClickEvent>(evt =>
            {
                if (_expandedUnitDeploymentIds.Contains(data.Id)) _expandedUnitDeploymentIds.Remove(data.Id);
                else _expandedUnitDeploymentIds.Add(data.Id);

                detailsContainer.ToggleInClassList("expanded");
                evt.StopPropagation();
            });

            entryContainer.Add(headerContainer);
            entryContainer.Add(detailsContainer);
            return entryContainer;
        }

        private void ExecuteAbortMovementRequest(Guid id)
        {
            StartCoroutine(NetworkManager.Instance.UnitDeployment.AbortMovementUnits(id, NetworkManager.Instance.JwtToken, (updated) =>
            {
                if (updated != null) WorldMapStateManager.Instance.UpdateDeploymentInCache(updated);
            }));
        }

        private void ExecuteReturnToOriginRequest(Guid id)
        {
            StartCoroutine(NetworkManager.Instance.UnitDeployment.ReturnToOriginCityUnits(id, NetworkManager.Instance.JwtToken, (updated) =>
            {
                if (updated == null) return;

                // Tjek om enheden er nået hjem med det samme (f.eks. hvis den stod lige uden for)
                bool isHome = updated.Status == UnitDeploymentMovementStatusEnum.Stationed &&
                             updated.CurrentX == CityStateManager.Instance.HomeCityX &&
                             updated.CurrentY == CityStateManager.Instance.HomeCityY;

                if (isHome) WorldMapStateManager.Instance.RemoveDeploymentFromCacheExplicitly(updated.Id);
                else WorldMapStateManager.Instance.UpdateDeploymentInCache(updated);
            }));
        }

        private VisualElement CreateUnitDeploymentDetailRow(string label, string value)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("detail-row");
            row.Add(new Label(label) { name = "detail-label" });
            row.Add(new Label(value) { name = "detail-value" });
            return row;
        }
    }
}