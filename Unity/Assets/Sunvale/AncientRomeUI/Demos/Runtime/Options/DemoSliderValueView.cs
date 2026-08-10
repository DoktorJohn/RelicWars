using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.Options
{
    public class DemoSliderValueView : MonoBehaviour
    {
        public Slider slider;
        public TextMeshProUGUI numberLabel;

        private void Awake()
        {
            // Slider works in 0–1 range
            slider.minValue = 0f;
            slider.maxValue = 1f;

            // Initialize randomly between 0.2 and 0.8
            slider.value = Random.Range(0.2f, 0.8f);

            // Update label immediately
            UpdateNumberLabel(slider.value);

            // Listen for changes
            slider.onValueChanged.AddListener(UpdateNumberLabel);
        }

        private void UpdateNumberLabel(float value)
        {
            int displayValue = Mathf.RoundToInt(value * 100f);
            numberLabel.text = displayValue.ToString();
        }

        private void OnDestroy()
        {
            slider.onValueChanged.RemoveListener(UpdateNumberLabel);
        }
    }
}
