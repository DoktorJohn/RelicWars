using System;
using Project.Modules.UI.Windows.Implementations;
using Project.Network.Manager;
using Project.Network.Models;
using Project.Scripts.Modules.Map;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.Modules.UI
{
    public sealed class UguiCityInspectionWindowController : MonoBehaviour, IUguiWindowPayloadReceiver
    {
        private TMP_Text _cityNameLabel;
        private TMP_Text _cityPointsLabel;
        private TMP_Text _playerNameLabel;
        private TMP_Text _playerPointsLabel;
        private TMP_Text _playerAllianceLabel;
        private TMP_Text _terrainLabel;
        private TMP_Text _locationLabel;
        private Transform _inspectCityButton;
        private EventTrigger.Entry _inspectCityReleaseEntry;
        private CityInspectionDTO _inspection;
        private CityInspectionPayload _payload;
        private int _requestVersion;

        private void Awake()
        {
            _cityNameLabel = FindDescendant(transform, "CityName label")?.GetComponent<TMP_Text>();
            _cityPointsLabel = FindDescendant(transform, "CityPoints label")?.GetComponent<TMP_Text>();
            _playerNameLabel = FindDescendant(transform, "PlayerName Label")?.GetComponent<TMP_Text>();
            _playerPointsLabel = FindDescendant(transform, "PlayerTotalPoints Label")?.GetComponent<TMP_Text>();
            _playerAllianceLabel = FindDescendant(transform, "PlayerAlliance Label")?.GetComponent<TMP_Text>();
            _terrainLabel = FindDescendant(transform, "TerrainLabel")?.GetComponent<TMP_Text>();
            _locationLabel = FindDescendant(transform, "LocationLabel")?.GetComponent<TMP_Text>();
            _inspectCityButton = FindDescendant(transform, "InspectCityBtn");
        }

        private void OnEnable()
        {
            BindInspectCityButton();
        }

        private void OnDisable()
        {
            _requestVersion++;
            StopAllCoroutines();
            UnbindInspectCityButton();
        }

        public void OnOpen(object payload)
        {
            if (payload is not CityInspectionPayload cityPayload || cityPayload.CityId == Guid.Empty)
            {
                Debug.LogError("[UguiCityInspectionWindowController] Invalid city inspection payload.", this);
                return;
            }

            _payload = cityPayload;
            _inspection = null;
            RenderPayloadPreview();
            LoadInspection();
        }

        private void LoadInspection()
        {
            NetworkManager network = NetworkManager.Instance;
            if (network == null || network.World == null)
            {
                Debug.LogError("[UguiCityInspectionWindowController] Network is unavailable.", this);
                return;
            }

            int version = ++_requestVersion;
            StartCoroutine(network.World.GetCityInspection(_payload.CityId, network.JwtToken, inspection =>
            {
                if (!isActiveAndEnabled || version != _requestVersion) return;
                if (inspection == null)
                {
                    Debug.LogError($"[UguiCityInspectionWindowController] City {_payload.CityId} could not be loaded.", this);
                    return;
                }

                _inspection = inspection;
                RenderInspection(inspection);
            }));
        }

        private void RenderPayloadPreview()
        {
            SetText(_locationLabel, $"{_payload.Coordinates.x}, {_payload.Coordinates.y}");
            SetText(_terrainLabel, FormatTerrain(_payload.TerrainName));
        }

        private void RenderInspection(CityInspectionDTO inspection)
        {
            SetText(_cityNameLabel, ValueOrDash(inspection.CityName));
            SetText(_cityPointsLabel, $"{inspection.Points:N0} points");
            SetText(_playerNameLabel, inspection.IsNPC ? "NPC Village" : ValueOrDash(inspection.WorldPlayerName));
            int? playerTotalPoints = inspection.PlayerTotalPoints;
            if (!playerTotalPoints.HasValue && (inspection.IsNPC || inspection.WorldPlayerId.HasValue))
            {
                // Backward compatibility while a pre-PlayerTotalPoints backend is still running.
                playerTotalPoints = inspection.Points;
            }

            SetText(_playerPointsLabel, playerTotalPoints?.ToString("N0") ?? "-");
            SetText(_playerAllianceLabel, inspection.AllianceId.HasValue
                ? ValueOrDash(inspection.AllianceName)
                : "-");
            SetText(_locationLabel, $"{inspection.X}, {inspection.Y}");
            SetText(_terrainLabel, FormatTerrain(_payload.TerrainName));
        }

        private void BindInspectCityButton()
        {
            if (_inspectCityButton == null) return;
            EventTrigger trigger = _inspectCityButton.GetComponent<EventTrigger>()
                ?? _inspectCityButton.gameObject.AddComponent<EventTrigger>();
            trigger.triggers ??= new System.Collections.Generic.List<EventTrigger.Entry>();
            _inspectCityReleaseEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            _inspectCityReleaseEntry.callback.AddListener(_ => MoveMapToInspectedCity());
            trigger.triggers.Add(_inspectCityReleaseEntry);
        }

        private void UnbindInspectCityButton()
        {
            if (_inspectCityButton == null || _inspectCityReleaseEntry == null) return;
            EventTrigger trigger = _inspectCityButton.GetComponent<EventTrigger>();
            trigger?.triggers?.Remove(_inspectCityReleaseEntry);
            _inspectCityReleaseEntry = null;
        }

        private void MoveMapToInspectedCity()
        {
            int x = _inspection?.X ?? _payload?.Coordinates.x ?? 0;
            int y = _inspection?.Y ?? _payload?.Coordinates.y ?? 0;
            FindFirstObjectByType<WorldMapRenderer>()?.CenterCameraOnCoordinates(x, y);
        }

        private static string FormatTerrain(string terrain) =>
            string.IsNullOrWhiteSpace(terrain) ? "-" : terrain.Replace("_", " ");

        private static string ValueOrDash(string value) =>
            string.IsNullOrWhiteSpace(value) ? "-" : value;

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null) label.text = value;
        }

        private static Transform FindDescendant(Transform root, string objectName)
        {
            if (root == null) return null;
            if (root.name.Equals(objectName, StringComparison.OrdinalIgnoreCase)) return root;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform found = FindDescendant(root.GetChild(index), objectName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
