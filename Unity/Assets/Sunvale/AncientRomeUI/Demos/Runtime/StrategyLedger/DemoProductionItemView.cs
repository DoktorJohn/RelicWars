using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.StrategyLedger
{
    public class DemoProductionItemView : MonoBehaviour
    {
            public Image icon;
            public TextMeshProUGUI nameLabel;
            public TextMeshProUGUI unitsLabel;
            public TextMeshProUGUI monthlyPercentLabel;
            
            
            
            public void SetName(string value)
            {
                    if (nameLabel != null)
                            nameLabel.SetText(value);
            }

            public void SetUnits(string value)
            {
                    if (unitsLabel != null)
                            unitsLabel.SetText(value);
            }

            public void SetMonthlyPercent(string value)
            {
                    if (monthlyPercentLabel != null)
                            monthlyPercentLabel.SetText(value);
            }

            public void SetIcon(Sprite sprite)
            {
                    if (icon != null)
                            icon.sprite = sprite;
            }
    }
}
