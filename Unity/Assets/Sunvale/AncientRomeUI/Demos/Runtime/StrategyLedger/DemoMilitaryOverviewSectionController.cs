using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.StrategyLedger
{
    public class DemoMilitaryOverviewSectionController : MonoBehaviour
    {
        [Header("Portraits")] public DemoCharacterPortraitTagView firstLegionaryPortrait;
        public DemoCharacterPortraitTagView legatusPortrait;

        [Header("Army")] public TextMeshProUGUI availableManpowerValueText;

        public TextMeshProUGUI legionsValueText;
        public Image legionsFillBarImage;

        public TextMeshProUGUI infantryValueText;
        public Image infantryFillBarImage;

        public TextMeshProUGUI auxiliaryValueText;
        public Image auxiliaryFillBarImage;

        public TextMeshProUGUI skirmishersValueText;
        public Image skirmishersFillBarImage;

        public TextMeshProUGUI equitesValueText;
        public Image equitesFillBarImage;

        public TextMeshProUGUI siegeValueText;
        public Image siegeFillBarImage;

        public TextMeshProUGUI logisticsValueText;
        public Image logisticsFillBarImage;

        [Header("Navy")] public TextMeshProUGUI shipsValueText;
        public Image shipsFillBarImage;

        public TextMeshProUGUI transportsValueText;
        public Image transportsFillBarImage;

        [Header("Fortifications And Provisions")]
        public TextMeshProUGUI heavyFortificationsValueText;

        public TextMeshProUGUI lightFortificationsValueText;

        public TextMeshProUGUI provisionsValueText;
        public Image provisionsFillBarImage;

        private static readonly int FillAmount = Shader.PropertyToID("_FillAmount");

        public void Initialize(RomeMilitaryStatsData militaryStatsData)
        {
            if (militaryStatsData == null)
                return;

            SetPortrait(legatusPortrait, militaryStatsData.legatus, "Legatus");
            SetPortrait(firstLegionaryPortrait, militaryStatsData.primusLegionis, "Primus Legionis");

            SetText(availableManpowerValueText, FormatIntWithSpaces(militaryStatsData.availableManpower));

            SetText(legionsValueText, $"{militaryStatsData.legions} / {militaryStatsData.maxLegions}");
            SetFillBarPercent(legionsFillBarImage, militaryStatsData.legionsPercent);

            SetText(infantryValueText, $"{militaryStatsData.infantryPercent}%");
            SetFillBarPercent(infantryFillBarImage, militaryStatsData.infantryPercent);

            SetText(auxiliaryValueText, $"{militaryStatsData.auxiliaryPercent}%");
            SetFillBarPercent(auxiliaryFillBarImage, militaryStatsData.auxiliaryPercent);

            SetText(skirmishersValueText, $"{militaryStatsData.skirmishersPercent}%");
            SetFillBarPercent(skirmishersFillBarImage, militaryStatsData.skirmishersPercent);

            SetText(equitesValueText, $"{militaryStatsData.equitesPercent}%");
            SetFillBarPercent(equitesFillBarImage, militaryStatsData.equitesPercent);

            SetText(siegeValueText, $"{militaryStatsData.siegePercent}%");
            SetFillBarPercent(siegeFillBarImage, militaryStatsData.siegePercent);

            SetText(logisticsValueText, $"{militaryStatsData.logisticsPercent}%");
            SetFillBarPercent(logisticsFillBarImage, militaryStatsData.logisticsPercent);

            SetText(shipsValueText, $"{militaryStatsData.shipsPercent}%");
            SetFillBarPercent(shipsFillBarImage, militaryStatsData.shipsPercent);

            SetText(transportsValueText, $"{militaryStatsData.transportsPercent}%");
            SetFillBarPercent(transportsFillBarImage, militaryStatsData.transportsPercent);

            SetText(
                heavyFortificationsValueText,
                $"{militaryStatsData.heavyFortifications} / {militaryStatsData.maxHeavyFortifications}"
            );

            SetText(
                lightFortificationsValueText,
                $"{militaryStatsData.lightFortifications} / {militaryStatsData.maxLightFortifications}"
            );

            SetText(provisionsValueText, $"{militaryStatsData.provisionsPercent}%");
            SetFillBarPercent(provisionsFillBarImage, militaryStatsData.provisionsPercent);
        }

        private string FormatIntWithSpaces(int value)
        {
            return value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)
                .Replace(",", " ");
        }
        private void SetPortrait(
            DemoCharacterPortraitTagView portrait,
            RomeCharacterData character,
            string title)
        {
            if (portrait == null || character == null)
                return;

            portrait.SetNameLabelString(GetShortRomanName(character.name));
            portrait.SetBottomExtraLabel(title);

            if (character.portraitSprite != null)
                portrait.SetIconSprite(character.portraitSprite);
        }

        private string GetShortRomanName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return string.Empty;

            string[] nameParts = fullName.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

            if (nameParts.Length == 1)
                return nameParts[0];

            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            for (int i = 0; i < nameParts.Length - 1; i++)
            {
                string part = nameParts[i];

                if (string.IsNullOrWhiteSpace(part))
                    continue;

                char firstLetter = part[0];

                builder.Append(char.ToUpperInvariant(firstLetter));
                builder.Append(". ");
            }

            builder.Append(nameParts[nameParts.Length - 1]);

            return builder.ToString();
        }

        private void SetText(TextMeshProUGUI text, string value)
        {
            if (text == null)
                return;

            text.SetText(value);
        }

        private void SetFillBarPercent(Image image, int percent)
        {
            if (image == null || image.material == null)
                return;

            float fill01 = Mathf.Clamp01(percent / 100f);
            image.material.SetFloat(FillAmount, fill01);
        }
    }
}
