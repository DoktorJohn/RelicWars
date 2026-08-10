using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.AncientRomeUI.Graphics;


namespace Sunvale.AncientRomeUI.Demos.StrategyTopDown
{
    public class DemoUnitCardView : MonoBehaviour
    {
            public Image unitIcon;
            public SimpleFillBar fillBar;
            public TextMeshProUGUI tmpUnitCounter;



            public void SetIconSprite(Sprite sprite)
            {
                    unitIcon.sprite = sprite;
            }

            public void SetFillbarNormalized(float newValue)
            {
                    fillBar.SetNormalizedValue(newValue);
            }

            public void SetCounterLabel(string s)
            {
                    tmpUnitCounter.SetText(s);
            }
    }
}
