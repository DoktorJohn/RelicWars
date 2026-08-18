using System;
using Project.Network.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.Modules.UI
{
    public sealed class UguiCityPlayerProfileDataRowView : MonoBehaviour, IPointerClickHandler
    {
        private TMP_Text _cityName;
        private TMP_Text _coordinates;
        private TMP_Text _points;
        private CityDTO _city;
        private Action<CityDTO> _onSelected;

        private void Awake()
        {
            _cityName = FindText("City LabelTMP");
            _coordinates = FindText("Coordinates LabelTMP");
            _points = FindText("Points LabelTMP");
        }

        public void Bind(CityDTO city, Action<CityDTO> onSelected)
        {
            _city = city;
            _onSelected = onSelected;
            if (_cityName != null) _cityName.text = city?.CityName ?? string.Empty;
            if (_coordinates != null) _coordinates.text = city == null ? string.Empty : $"{city.X}, {city.Y}";
            if (_points != null) _points.text = city?.Points.ToString("N0") ?? string.Empty;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && _city != null)
                _onSelected?.Invoke(_city);
        }

        private TMP_Text FindText(string objectName)
        {
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
                if (text.name.Equals(objectName, StringComparison.Ordinal)) return text;
            return null;
        }
    }
}
