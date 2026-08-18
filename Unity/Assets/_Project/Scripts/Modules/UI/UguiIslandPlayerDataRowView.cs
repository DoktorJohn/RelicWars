using Project.Network.Models;
using TMPro;
using UnityEngine;

namespace Project.Modules.UI
{
    public sealed class UguiIslandPlayerDataRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerText;
        [SerializeField] private TMP_Text cityText;
        [SerializeField] private TMP_Text allianceText;
        [SerializeField] private TMP_Text coordinatesText;
        [SerializeField] private TMP_Text pointsText;

        public void Bind(WorldIslandCityDTO city)
        {
            ResolveReferences();

            if (playerText != null)
                playerText.text = city.IsNPC ? "NPC Village" : ValueOrDash(city.WorldPlayerName);
            if (cityText != null)
                cityText.text = ValueOrDash(city.CityName);
            if (allianceText != null)
                allianceText.text = city.AllianceId.HasValue ? ValueOrDash(city.AllianceName) : "-";
            if (coordinatesText != null)
                coordinatesText.text = $"{city.X}, {city.Y}";
            if (pointsText != null)
                pointsText.text = city.Points.ToString("N0");
        }

        private void ResolveReferences()
        {
            TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text label in labels)
            {
                switch (label.name)
                {
                    case "Player LabelTMP": playerText ??= label; break;
                    case "City LabelTMP": cityText ??= label; break;
                    case "Alliance LabelTMP": allianceText ??= label; break;
                    case "Coordinates LabelTMP": coordinatesText ??= label; break;
                    case "Points LabelTMP": pointsText ??= label; break;
                }
            }
        }

        private static string ValueOrDash(string value) =>
            string.IsNullOrWhiteSpace(value) ? "-" : value;
    }
}
