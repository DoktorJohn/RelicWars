using System;
using System.Collections.Generic;
using Project.Modules.City;
using Project.Network.Manager;
using Project.Network.Models;
using Sunvale.AncientRomeUI.Buttons;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public partial class CityTopBarViewController
    {
        [Header("City selector")]
        [SerializeField] private IconTextSidebarButton cityNameTrigger;
        [SerializeField] private TMP_Text cityNameLabel;
        [SerializeField] private GameObject citySelectorPopup;
        [SerializeField] private RectTransform cityListContent;
        [SerializeField] private CityTopBarCityRowView cityRowTemplate;
        [SerializeField] private Button previousCityButton;
        [SerializeField] private Button nextCityButton;

        private readonly List<CityTopBarCityRowView> _cityRows = new();

        private void InitializeCitySelector()
        {
            if (cityNameTrigger != null) cityNameTrigger.OnButtonActivatedClicked += HandleCityNameClicked;
            if (previousCityButton != null) previousCityButton.onClick.AddListener(OnPreviousCityClicked);
            if (nextCityButton != null) nextCityButton.onClick.AddListener(OnNextCityClicked);

            if (cityRowTemplate != null) cityRowTemplate.gameObject.SetActive(false);
            HideCitySelectorPopup();
            PopulateCitySelectorPopup();
            UpdateNavigationButtonsState();
        }

        private void CleanupCitySelector()
        {
            if (cityNameTrigger != null) cityNameTrigger.OnButtonActivatedClicked -= HandleCityNameClicked;
            if (previousCityButton != null) previousCityButton.onClick.RemoveListener(OnPreviousCityClicked);
            if (nextCityButton != null) nextCityButton.onClick.RemoveListener(OnNextCityClicked);
            ClearCityRows();
        }

        private void PopulateCitySelectorPopup()
        {
            ClearCityRows();

            if (cityRowTemplate == null || cityListContent == null || _playerCities == null) return;

            foreach (CityDTO city in _playerCities)
            {
                if (city == null) continue;

                CityTopBarCityRowView row = Instantiate(cityRowTemplate, cityListContent);
                row.gameObject.SetActive(true);
                row.Bind(city, HandleCitySelected);
                _cityRows.Add(row);
            }
        }

        private void Update()
        {
            if (citySelectorPopup == null || !citySelectorPopup.activeSelf) return;

            Pointer pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
                CloseCitySelectorWhenPointerIsOutside(pointer.position.ReadValue());
        }

        private void CloseCitySelectorWhenPointerIsOutside(Vector2 screenPosition)
        {
            RectTransform popupRect = citySelectorPopup.transform as RectTransform;
            RectTransform triggerRect = cityNameTrigger != null ? cityNameTrigger.transform as RectTransform : null;
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            if (popupRect != null && RectTransformUtility.RectangleContainsScreenPoint(popupRect, screenPosition, eventCamera)) return;
            if (triggerRect != null && RectTransformUtility.RectangleContainsScreenPoint(triggerRect, screenPosition, eventCamera)) return;

            HideCitySelectorPopup();
        }

        private void ClearCityRows()
        {
            foreach (CityTopBarCityRowView row in _cityRows)
            {
                if (row != null) Destroy(row.gameObject);
            }

            _cityRows.Clear();
        }

        private void UpdateCitySelectorLabel(string cityName)
        {
            if (cityNameLabel != null && !string.IsNullOrWhiteSpace(cityName))
            {
                cityNameLabel.text = cityName;
            }
        }

        private void HandleCityNameClicked(IconTextSidebarButton _)
        {
            if (citySelectorPopup == null || _playerCities == null || _playerCities.Count == 0) return;

            bool showPopup = !citySelectorPopup.activeSelf;
            if (showPopup) PopulateCitySelectorPopup();
            citySelectorPopup.SetActive(showPopup);
        }

        private void HandleCitySelected(CityDTO city)
        {
            if (city == null) return;

            int selectedIndex = _playerCities?.FindIndex(candidate => candidate != null && candidate.Id == city.Id) ?? -1;
            if (selectedIndex < 0) return;

            if (NetworkManager.Instance?.ActiveCityId == city.Id)
            {
                HideCitySelectorPopup();
                return;
            }

            SelectCity(city);
        }

        private void HideCitySelectorPopup()
        {
            if (citySelectorPopup != null) citySelectorPopup.SetActive(false);
        }

        private void SelectCity(CityDTO city)
        {
            if (city == null || city.Id == Guid.Empty || NetworkManager.Instance == null) return;

            NetworkManager.Instance.SelectActiveCity(city.Id);
            CityStateManager.Instance?.StartPollingForCity(city.Id);
            UpdateCitySelectorLabel(city.CityName);
            HideCitySelectorPopup();
            PopulateCitySelectorPopup();
        }

        private void OnPreviousCityClicked()
        {
            CloseAllPopups();
            SelectRelativeCity(-1);
        }

        private void OnNextCityClicked()
        {
            CloseAllPopups();
            SelectRelativeCity(1);
        }

        private void SelectRelativeCity(int offset)
        {
            if (_playerCities == null || _playerCities.Count <= 1) return;

            int currentIndex = FindActiveCityIndex();
            if (currentIndex < 0) currentIndex = 0;

            int nextIndex = (currentIndex + offset + _playerCities.Count) % _playerCities.Count;
            SelectCity(_playerCities[nextIndex]);
        }

        private int FindActiveCityIndex()
        {
            Guid activeCityId = NetworkManager.Instance?.ActiveCityId ?? Guid.Empty;
            return _playerCities?.FindIndex(city => city != null && city.Id == activeCityId) ?? -1;
        }

        private void UpdateNavigationButtonsState()
        {
            bool hasMultipleCities = _playerCities != null && _playerCities.Count > 1;
            if (previousCityButton != null) previousCityButton.interactable = hasMultipleCities;
            if (nextCityButton != null) nextCityButton.interactable = hasMultipleCities;
        }

        private void HandleContextualNavigationRequested(IconTextSidebarButton _)
        {
            CloseAllPopups();
            string activeSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(activeSceneName == WorldMapSceneName ? CityViewSceneName : WorldMapSceneName);
        }
    }
}
