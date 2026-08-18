using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Project.Network.Manager;
using Project.Network.Models;
using Project.Scripts.Domain.DTOs;
using Sunvale.AncientRomeUI.Buttons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public sealed class UguiProfileWindowController : MonoBehaviour
    {
        [SerializeField] private UguiCityPlayerProfileDataRowView cityRowPrefab;

        private TMP_Text _playerName;
        private TMP_Text _allianceName;
        private TMP_Text _rank;
        private TMP_Text _points;
        private TMP_Text _cities;
        private TMP_Text _descriptionText;
        private TMP_InputField _descriptionInput;
        private Image _descriptionBackground;
        private Color _descriptionBackgroundColor;
        private Transform _cityRows;
        private CarvedPressButton _editButton;
        private Guid _worldPlayerId;
        private int _requestVersion;
        private bool _editing;
        private bool _saving;
        private string _savedDescription = string.Empty;

        private void Awake()
        {
            _playerName = FindComponent<TMP_Text>("PlayerName Text");
            _allianceName = FindComponent<TMP_Text>("AllianceName Text");
            Transform rankValue = FindTransform(transform, "RankValue");
            _rank = FindTransform(rankValue, "RankText")?.GetComponent<TMP_Text>();
            _points = FindComponent<TMP_Text>("PointsValue");
            _cities = FindComponent<TMP_Text>("CitiesValue");
            _descriptionText = FindComponent<TMP_Text>("DescriptionText");
            _editButton = FindComponent<CarvedPressButton>("EditBtn");

            ScrollRect cityScroll = FindTransform(transform, "CityList")?.GetComponentInChildren<ScrollRect>(true);
            _cityRows = cityScroll != null ? cityScroll.content : null;
            EnsureDescriptionInput();
        }

        private void OnEnable()
        {
            int version = ++_requestVersion;
            if (_editButton != null) _editButton.OnButtonActivatedClicked += BeginEditing;
            if (_descriptionInput != null) _descriptionInput.onDeselect.AddListener(SaveOnFocusLost);
            Load(version);
        }

        private void OnDisable()
        {
            _requestVersion++;
            if (_editButton != null) _editButton.OnButtonActivatedClicked -= BeginEditing;
            if (_descriptionInput != null) _descriptionInput.onDeselect.RemoveListener(SaveOnFocusLost);
            StopAllCoroutines();
            _editing = false;
            _saving = false;
        }

        private void Load(int version)
        {
            NetworkManager network = NetworkManager.Instance;
            if (network == null || network.WorldPlayer == null ||
                !Guid.TryParse(network.WorldPlayerId, out _worldPlayerId))
            {
                Debug.LogError("[UguiProfileWindowController] No active world player is available.", this);
                return;
            }

            StartCoroutine(network.WorldPlayer.GetPlayerProfile(_worldPlayerId, network.JwtToken, profile =>
            {
                if (!CanApply(version) || profile == null) return;
                Render(profile);
            }));
        }

        private void Render(WorldPlayerProfileDTO profile)
        {
            _savedDescription = profile.Description ?? string.Empty;
            SetText(_playerName, string.IsNullOrWhiteSpace(profile.UserName) ? "Unknown" : profile.UserName);
            SetText(_allianceName,
                profile.AllianceId != Guid.Empty && HasAllianceName(profile.AllianceName)
                    ? profile.AllianceName
                    : string.Empty);
            SetText(_rank, profile.Ranking.ToString("N0"));
            SetText(_points, profile.TotalPoints.ToString("N0"));
            SetText(_cities, profile.CityCount.ToString("N0"));
            SetDescription(_savedDescription);
            RenderCities(profile.Cities);
        }

        private void RenderCities(IEnumerable<CityDTO> cities)
        {
            if (_cityRows == null || cityRowPrefab == null) return;
            for (int index = _cityRows.childCount - 1; index >= 0; index--)
                Destroy(_cityRows.GetChild(index).gameObject);

            foreach (CityDTO city in (cities ?? Enumerable.Empty<CityDTO>())
                         .OrderByDescending(item => item.Points)
                         .ThenBy(item => item.CityName))
            {
                UguiCityPlayerProfileDataRowView row = Instantiate(cityRowPrefab, _cityRows, false);
                row.Bind(city, OpenCity);
            }
        }

        private static void OpenCity(CityDTO city)
        {
            WindowNavigationHelper.OpenCityInspection(city.Id, city.X, city.Y);
        }

        private void BeginEditing(CarvedPressButton _)
        {
            if (_saving || _descriptionInput == null) return;
            _editing = true;
            _descriptionInput.interactable = true;
            _descriptionInput.readOnly = false;
            _descriptionInput.SetTextWithoutNotify(_savedDescription);
            ApplyDescriptionInputVisual(true);
            StartCoroutine(FocusDescriptionAfterEditClick());
        }

        private IEnumerator FocusDescriptionAfterEditClick()
        {
            // The edit button's pointer event must finish before focus moves. Otherwise
            // its pointer-up immediately deselects the input and triggers an unwanted save.
            yield return new WaitForEndOfFrame();
            if (!_editing || _descriptionInput == null) yield break;
            _descriptionInput.Select();
            _descriptionInput.ActivateInputField();
        }

        private void SaveOnFocusLost(string value)
        {
            if (!_editing || _saving) return;
            _editing = false;
            string description = (value ?? string.Empty).Trim();
            if (description.Length > 500)
            {
                description = description.Substring(0, 500);
                _descriptionInput.SetTextWithoutNotify(description);
            }

            if (description == _savedDescription)
            {
                EndEditing();
                return;
            }

            NetworkManager network = NetworkManager.Instance;
            if (network == null || network.WorldPlayer == null)
            {
                _descriptionInput.SetTextWithoutNotify(_savedDescription);
                EndEditing();
                return;
            }

            _saving = true;
            int version = _requestVersion;
            StartCoroutine(network.WorldPlayer.UpdatePlayerDescription(
                _worldPlayerId, description, network.JwtToken, profile =>
                {
                    if (!CanApply(version)) return;
                    _saving = false;
                    if (profile == null)
                    {
                        Debug.LogError("[UguiProfileWindowController] Description could not be saved.", this);
                        _descriptionInput.SetTextWithoutNotify(_savedDescription);
                        EndEditing();
                        return;
                    }

                    _savedDescription = profile.Description ?? string.Empty;
                    SetDescription(_savedDescription);
                    EndEditing();
                }));
        }

        private void EndEditing()
        {
            _editing = false;
            if (_descriptionInput != null)
            {
                // Keep the Selectable out of its disabled visual state; TMP_InputField
                // otherwise dims the authored DescriptionText through targetGraphic.
                _descriptionInput.interactable = true;
                _descriptionInput.readOnly = true;
            }
            ApplyDescriptionInputVisual(false);
        }

        private void SetDescription(string description)
        {
            string visible = string.IsNullOrWhiteSpace(description) ? "No description available." : description;
            if (_descriptionInput != null) _descriptionInput.SetTextWithoutNotify(visible);
            else SetText(_descriptionText, visible);
            EndEditing();
        }

        private void EnsureDescriptionInput()
        {
            if (_descriptionText == null) return;
            Transform inputRoot = _descriptionText.transform.parent;
            _descriptionBackground = FindTransform(inputRoot, "Background")?.GetComponent<Image>();
            if (_descriptionBackground != null)
                _descriptionBackgroundColor = _descriptionBackground.color;
            _descriptionInput = inputRoot.GetComponent<TMP_InputField>();
            if (_descriptionInput == null) _descriptionInput = inputRoot.gameObject.AddComponent<TMP_InputField>();
            _descriptionInput.textViewport = inputRoot as RectTransform;
            _descriptionInput.textComponent = _descriptionText;
            _descriptionInput.targetGraphic = _descriptionBackground != null
                ? _descriptionBackground
                : _descriptionText;
            _descriptionInput.lineType = TMP_InputField.LineType.MultiLineNewline;
            _descriptionInput.characterLimit = 500;
            _descriptionInput.interactable = true;
            _descriptionInput.readOnly = true;
            _descriptionInput.richText = false;
            ApplyDescriptionInputVisual(false);
        }

        private void ApplyDescriptionInputVisual(bool editing)
        {
            if (_descriptionBackground == null) return;
            _descriptionBackground.color = editing
                ? new Color(1f, 0.93f, 0.72f, 0.72f)
                : _descriptionBackgroundColor;
        }

        private static bool HasAllianceName(string allianceName)
        {
            if (string.IsNullOrWhiteSpace(allianceName)) return false;
            string normalized = allianceName.Trim();
            return !normalized.Equals("Ingen alliance", StringComparison.OrdinalIgnoreCase) &&
                   !normalized.Equals("No alliance", StringComparison.OrdinalIgnoreCase);
        }

        private bool CanApply(int version) => isActiveAndEnabled && version == _requestVersion;

        private T FindComponent<T>(string objectName) where T : Component
        {
            Transform target = FindTransform(transform, objectName);
            return target != null ? target.GetComponent<T>() : null;
        }

        private static Transform FindTransform(Transform root, string objectName)
        {
            if (root == null) return null;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);
                if (child.name.Equals(objectName, StringComparison.Ordinal)) return child;
                Transform nested = FindTransform(child, objectName);
                if (nested != null) return nested;
            }
            return null;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null) text.text = value;
        }
    }
}
