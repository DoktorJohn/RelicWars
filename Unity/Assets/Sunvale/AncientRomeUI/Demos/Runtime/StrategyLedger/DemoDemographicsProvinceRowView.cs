using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.AncientRomeUI.Buttons;


namespace Sunvale.AncientRomeUI.Demos.StrategyLedger
{
    public class DemoDemographicsProvinceRowView : MonoBehaviour
    {
        public RowHighlightButton myHighlightButton;

        public TextMeshProUGUI cityNameTMP;
        public TextMeshProUGUI provinceNameTMP;
        public TextMeshProUGUI populationTMP;
        public TextMeshProUGUI growthTMP;
        public TextMeshProUGUI taxTMP;
        public TextMeshProUGUI taxCapitaTMP;
        public TextMeshProUGUI moodTMP;

        public Image cityPictureImage;
        public Image moodSmileyImage;

        public Color defaultTextColor;
        public Color greenPositiveTextColor;
        public Color redNegativeColor;

        public float monoSpaceSpacingForNumbers = 0.5f;

        public void Initialize(RomeCityData romeCityData)
        {
            cityNameTMP.text = romeCityData.cityName;
            provinceNameTMP.text = romeCityData.provinceName;

            populationTMP.text = FormatNumber(romeCityData.population);
            taxTMP.text = FormatNumber(romeCityData.taxes);
            taxCapitaTMP.text = FormatNumber(romeCityData.taxPerCapita);

            growthTMP.text = FormatSignedPercent(romeCityData.growth);
            growthTMP.color = GetSignedColor(romeCityData.growth);

            moodTMP.text = FormatMood(romeCityData.mood);
            moodTMP.color = GetMoodColor(romeCityData.mood);

            cityPictureImage.sprite = romeCityData.cityIcon;
            cityPictureImage.enabled = romeCityData.cityIcon != null;

            moodSmileyImage.sprite = romeCityData.cityMoodIcon;
            moodSmileyImage.enabled = romeCityData.cityMoodIcon != null;
        }

        private string FormatNumber(int value)
        {
            string formattedValue = value.ToString("#,0", CultureInfo.InvariantCulture);
            return WrapInMonoSpace(formattedValue);
        }

        private string FormatSignedPercent(int value)
        {
            string formattedValue;

            if (value > 0)
                formattedValue = "+" + value.ToString(CultureInfo.InvariantCulture) + "%";
            else
                formattedValue = value.ToString(CultureInfo.InvariantCulture) + "%";

            return WrapInMonoSpace(formattedValue);
        }

        private string FormatMood(int mood)
        {
            string formattedValue = mood.ToString(CultureInfo.InvariantCulture);
            return WrapInMonoSpace(formattedValue);
        }

        private string WrapInMonoSpace(string text)
        {
            string spacing = monoSpaceSpacingForNumbers.ToString("0.###", CultureInfo.InvariantCulture);
            return $"<mspace={spacing}em>{text}</mspace>";
        }

        private Color GetSignedColor(int value)
        {
            if (value > 0)
                return greenPositiveTextColor;

            if (value < 0)
                return redNegativeColor;

            return defaultTextColor;
        }

        private Color GetMoodColor(int mood)
        {
            if (mood <= 40)
                return redNegativeColor;

            if (mood >= 61)
                return greenPositiveTextColor;

            return defaultTextColor;
        }
    }
}
