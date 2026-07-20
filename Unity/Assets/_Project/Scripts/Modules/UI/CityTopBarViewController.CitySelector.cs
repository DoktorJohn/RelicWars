using System;
using Project.Modules.City;
using Project.Network.Manager;
using Project.Network.Models;
using Project.Scripts.Domain.DTOs;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Project.Modules.UI
{
    public partial class CityTopBarViewController
    {
        private void InitializeCitySelector()
        {
            _citySelectorSection = _rootVisualElement.Q<VisualElement>("City-Selector-Section");
            _citySelectorCurrentCityLabel = _rootVisualElement.Q<Label>("City-Selector-CurrentCity-Label");
            _citySelectorRenameInput = _rootVisualElement.Q<TextField>("City-Selector-Rename-Input");
            _citySelectorDropdownContainer = _rootVisualElement.Q<VisualElement>("City-Selector-Dropdown-Container");
            _citySelectorDropdownScroll = _rootVisualElement.Q<ScrollView>("City-Selector-Dropdown-Scroll");

            _previousCityButton = _rootVisualElement.Q<Button>("City-Selector-Arrow-Left");
            _nextCityButton = _rootVisualElement.Q<Button>("City-Selector-Arrow-Right");

            if (_citySelectorCurrentCityLabel != null)
            {
                _citySelectorCurrentCityLabel.RegisterCallback<ClickEvent>(OnCityLabelClicked);
            }

            if (_citySelectorRenameInput != null)
            {
                _citySelectorRenameInput.RegisterCallback<FocusOutEvent>(OnRenameInputFocusOut);
                _citySelectorRenameInput.RegisterCallback<KeyDownEvent>(OnRenameInputKeyDown);
            }

            if (_previousCityButton != null) _previousCityButton.clicked += OnPreviousCityClicked;
            if (_nextCityButton != null) _nextCityButton.clicked += OnNextCityClicked;
        }

        private void InitializeNavigationButtons()
        {
            _navigationButton = _rootVisualElement.Q<Button>("City-TopBar-MapButton");
            if (_navigationButton != null)
            {
                _navigationButton.clicked -= HandleContextualNavigationRequested;
                _navigationButton.clicked += HandleContextualNavigationRequested;
            }
        }

        private void PopulateCityDropdown()
        {
            if (_citySelectorDropdownScroll == null) return;

            _citySelectorDropdownScroll.Clear();

            foreach (var city in _playerCities)
            {
                var cityLabel = new Label(city.CityName);
                cityLabel.AddToClassList("city-selector-item");
                cityLabel.RegisterCallback<ClickEvent>(evt => OnCityDropdownItemClicked(city));
                _citySelectorDropdownScroll.Add(cityLabel);
            }
        }

        private void UpdateCitySelectorLabel(string cityName)
        {
            if (_citySelectorCurrentCityLabel == null) return;

            if (!string.IsNullOrEmpty(cityName))
            {
                _citySelectorCurrentCityLabel.text = cityName;
            }
        }

        private void OnCityLabelClicked(ClickEvent evt)
        {
            if (evt.clickCount == 2)
            {
                EnableRenameMode();
            }
            else
            {
                ToggleDropdown();
            }
        }

        private void ToggleDropdown()
        {
            if (_citySelectorDropdownContainer == null) return;

            bool isVisible = _citySelectorDropdownContainer.style.display == DisplayStyle.Flex;
            _citySelectorDropdownContainer.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void OnCityDropdownItemClicked(CityDTO city)
        {
            if (_citySelectorDropdownContainer != null)
                _citySelectorDropdownContainer.style.display = DisplayStyle.None;

            if (city == null || city.Id == Guid.Empty || NetworkManager.Instance == null)
            {
                return;
            }

            NetworkManager.Instance.SelectActiveCity(city.Id);
            CityStateManager.Instance?.StartPollingForCity(city.Id);
            UpdateCitySelectorLabel(city.CityName);
        }

        private void EnableRenameMode()
        {
            if (_citySelectorCurrentCityLabel == null || _citySelectorRenameInput == null) return;

            _citySelectorCurrentCityLabel.style.display = DisplayStyle.None;
            _citySelectorDropdownContainer.style.display = DisplayStyle.None;

            _citySelectorRenameInput.style.display = DisplayStyle.Flex;
            _citySelectorRenameInput.value = _citySelectorCurrentCityLabel.text;
            _citySelectorRenameInput.Focus();
        }

        private void OnRenameInputFocusOut(FocusOutEvent evt)
        {
            CommitRename();
        }

        private void OnRenameInputKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                CommitRename();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                CancelRename();
            }
        }

        private void CancelRename()
        {
            if (_citySelectorCurrentCityLabel == null || _citySelectorRenameInput == null) return;

            _citySelectorRenameInput.style.display = DisplayStyle.None;
            _citySelectorCurrentCityLabel.style.display = DisplayStyle.Flex;
        }

        private void CommitRename()
        {
            if (_citySelectorRenameInput == null || _citySelectorCurrentCityLabel == null) return;

            if (_citySelectorRenameInput.style.display == DisplayStyle.None) return;

            string newName = _citySelectorRenameInput.value;

            if (string.IsNullOrWhiteSpace(newName) || newName.Length < 3)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[CityTopBar] Name too short.");
#endif
                CancelRename();
                return;
            }

            if (CityStateManager.Instance == null || NetworkManager.Instance == null)
            {
                CancelRename();
                return;
            }

            Guid currentCityId = CityStateManager.Instance.CityId;
            if (currentCityId == Guid.Empty)
            {
                CancelRename();
                return;
            }

            StartCoroutine(NetworkManager.Instance.City.ChangeCityName(currentCityId, newName, NetworkManager.Instance.JwtToken, (response) =>
            {
                if (!isActiveAndEnabled)
                {
                    return;
                }

                if (response == null)
                {
                    CancelRename();
                    return;
                }

                if (response.Success)
                {
                    _citySelectorCurrentCityLabel.text = response.CityName;

                    var cityInList = _playerCities.Find(c => c.Id == response.CityId);
                    if (cityInList != null) cityInList.CityName = response.CityName;
                    PopulateCityDropdown();

                    if (WorldMapStateManager.Instance != null)
                    {
                        WorldMapStateManager.Instance.InvalidateAllCachedChunks();
                    }
                }
                else
                {
                    Debug.LogError($"[CityTopBar] Rename failed: {response.Message}");
                }

                CancelRename();
            }));
        }

        private void OnPreviousCityClicked()
        {
            if (_playerCities == null || _playerCities.Count <= 1) return;

            var currentCityId = NetworkManager.Instance?.ActiveCityId ?? Guid.Empty;
            var currentIndex = _playerCities.FindIndex(c => c.Id == currentCityId);

            if (currentIndex == -1) currentIndex = 0;

            int newIndex = currentIndex - 1;
            if (newIndex < 0) newIndex = _playerCities.Count - 1;

            var newCity = _playerCities[newIndex];
            OnCityDropdownItemClicked(newCity);
        }

        private void OnNextCityClicked()
        {
            if (_playerCities == null || _playerCities.Count <= 1) return;

            var currentCityId = NetworkManager.Instance?.ActiveCityId ?? Guid.Empty;
            var currentIndex = _playerCities.FindIndex(c => c.Id == currentCityId);

            if (currentIndex == -1) currentIndex = 0;

            int newIndex = currentIndex + 1;
            if (newIndex >= _playerCities.Count) newIndex = 0;

            var newCity = _playerCities[newIndex];
            OnCityDropdownItemClicked(newCity);
        }

        private void UpdateNavigationButtonsState()
        {
            bool hasMultipleCities = _playerCities != null && _playerCities.Count > 1;

            if (_previousCityButton != null) _previousCityButton.SetEnabled(hasMultipleCities);
            if (_nextCityButton != null) _nextCityButton.SetEnabled(hasMultipleCities);
        }

        private void HandleContextualNavigationRequested()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;

            if (currentSceneName == WorldMapSceneName)
            {
                SceneManager.LoadScene(CityViewSceneName);
                return;
            }

            SceneManager.LoadScene(WorldMapSceneName);
        }
    }
}
