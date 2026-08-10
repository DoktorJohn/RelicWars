using System;
using Project.Network.Models;
using Sunvale.AncientRomeUI.Buttons;
using TMPro;
using UnityEngine;

namespace Project.Modules.UI
{
    public sealed class CityTopBarCityRowView : MonoBehaviour
    {
        [SerializeField] private IconTextSidebarButton button;
        [SerializeField] private TMP_Text cityNameLabel;

        private CityDTO _city;
        private Action<CityDTO> _selected;

        public void Bind(CityDTO city, Action<CityDTO> selected)
        {
            _city = city;
            _selected = selected;
            if (cityNameLabel != null) cityNameLabel.text = city?.CityName ?? string.Empty;
            if (button == null) return;

            button.OnButtonActivatedClicked -= HandleSelected;
            button.OnButtonActivatedClicked += HandleSelected;
        }

        private void OnDestroy()
        {
            if (button != null) button.OnButtonActivatedClicked -= HandleSelected;
        }

        private void HandleSelected(IconTextSidebarButton _)
        {
            _selected?.Invoke(_city);
        }
    }
}
