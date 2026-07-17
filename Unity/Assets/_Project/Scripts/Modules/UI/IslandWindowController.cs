using Project.Network.Manager;
using Project.Network.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Project.Modules.UI
{
    public partial class IslandWindowController : BaseWindow
    {
        protected override string WindowName => "IslandWindow";
        protected override string VisualContainerName => "Island-Window-MainContainer";
        protected override string HeaderName => "Island-Window-Header";

        private ScrollView _cityList;
        private Label _statusMessage;
        private Guid _islandId;
        private int _requestVersion;

        public override void OnOpen(object dataPayload)
        {
            var version = BeginDeferredOpen();
            _requestVersion = version;
            InitializeUserInterfaceReferences();

            if (dataPayload is not Guid islandId || islandId == Guid.Empty)
            {
                SetStatusMessage("Invalid island.", true);
                WindowAsyncStateHelper.ShowError(_cityList, "Invalid island.");
                CompleteDeferredOpen(version);
                return;
            }

            _islandId = islandId;
            LoadIsland(version);
        }

        private void OnDisable()
        {
            InvalidateDeferredOpen();
            StopAllCoroutines();
        }

        private void InitializeUserInterfaceReferences()
        {
            _cityList = Root.Q<ScrollView>("Island-City-List");
            _statusMessage = Root.Q<Label>("Island-Status-Message");
        }

        private void LoadIsland(int version)
        {
            if (NetworkManager.Instance == null)
            {
                SetStatusMessage("Network is unavailable.", true);
                WindowAsyncStateHelper.ShowError(_cityList, "Network is unavailable.");
                CompleteDeferredOpen(version);
                return;
            }

            WindowAsyncStateHelper.ShowLoading(_cityList, "Loading island cities...");
            SetStatusMessage("Loading island details...", false);

            StartCoroutine(NetworkManager.Instance.World.GetIslandDetails(
                _islandId,
                NetworkManager.Instance.JwtToken,
                details =>
                {
                    if (!isActiveAndEnabled || version != _requestVersion)
                    {
                        return;
                    }

                    if (details == null)
                    {
                        WindowAsyncStateHelper.ShowError(_cityList, "Could not load the island.", () => LoadIsland(version));
                        SetStatusMessage("Could not load island details.", true);
                        CompleteDeferredOpen(version);
                        return;
                    }

                    RenderIslandDetails(details);
                    SetStatusMessage(string.Empty, false);
                    CompleteDeferredOpen(version);
                }));
        }

        private void RenderIslandDetails(WorldIslandDetailsDTO details)
        {
            var cities = details.Cities?.OrderByDescending(city => city.Points).ThenBy(city => city.CityName).ToList() ?? new List<WorldIslandCityDTO>();

            RenderCities(cities);
        }

        private void RenderCities(List<WorldIslandCityDTO> cities)
        {
            if (_cityList == null)
            {
                return;
            }

            _cityList.Clear();

            if (cities.Count == 0)
            {
                WindowAsyncStateHelper.ShowEmpty(_cityList, "No cities on this island.");
                return;
            }

            foreach (var city in cities)
            {
                var row = new VisualElement();
                row.AddToClassList("island-city-row");

                if (city.IsNPC)
                {
                    var player = new Label("NPC Village");
                    player.AddToClassList("island-city-player");
                    row.Add(player);
                }
                else if (city.WorldPlayerId.HasValue)
                {
                    Guid worldPlayerId = city.WorldPlayerId.Value;
                    var playerLink = new Label(city.WorldPlayerName);
                    playerLink.AddToClassList("island-city-player");
                    playerLink.AddToClassList("btn-entity-link");
                    playerLink.RegisterCallback<ClickEvent>(_ => WindowNavigationHelper.OpenProfile(worldPlayerId));
                    row.Add(playerLink);
                }
                else
                {
                    var player = new Label("-");
                    player.AddToClassList("island-city-player");
                    row.Add(player);
                }

                var cityLink = new Label(city.CityName);
                cityLink.AddToClassList("island-city-name");
                cityLink.AddToClassList("btn-entity-link");
                cityLink.RegisterCallback<ClickEvent>(_ =>
                    WindowNavigationHelper.OpenCityInspection(city.Id, city.X, city.Y));
                row.Add(cityLink);

                if (city.AllianceId.HasValue)
                {
                    Guid allianceId = city.AllianceId.Value;
                    var allianceLink = new Label(city.AllianceName);
                    allianceLink.AddToClassList("island-city-alliance");
                    allianceLink.AddToClassList("btn-entity-link");
                    allianceLink.RegisterCallback<ClickEvent>(_ => WindowNavigationHelper.OpenAlliance(allianceId));
                    row.Add(allianceLink);
                }
                else
                {
                    var alliance = new Label("-");
                    alliance.AddToClassList("island-city-alliance");
                    row.Add(alliance);
                }

                var coordinates = new Label($"{city.X}, {city.Y}");
                coordinates.AddToClassList("island-city-coordinates");
                row.Add(coordinates);

                var points = new Label(city.Points.ToString("N0"));
                points.AddToClassList("island-city-points");
                row.Add(points);

                _cityList.Add(row);
            }
        }

        private void SetStatusMessage(string message, bool isError)
        {
            if (_statusMessage == null)
            {
                return;
            }

            _statusMessage.text = message ?? string.Empty;
            _statusMessage.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
            _statusMessage.EnableInClassList("island-status-message--error", isError);
            _statusMessage.EnableInClassList("island-status-message--info", !isError);
        }
    }
}
