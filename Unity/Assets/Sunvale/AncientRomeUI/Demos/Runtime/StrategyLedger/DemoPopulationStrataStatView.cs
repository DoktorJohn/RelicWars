using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.StrategyLedger
{
    public class DemoPopulationStrataStatView : MonoBehaviour
    {
            public Image icon;
            public TextMeshProUGUI labelName;
            public TextMeshProUGUI countTMP;
            public TextMeshProUGUI percentageTMP;




            public void SetIcon(Sprite sprite)
            {
                icon.sprite = sprite;
            }


            public void SetLabelText(string s)
            {
                labelName.SetText(s);
            }
            
            public void SetCountText(string s)
            {
                countTMP.SetText(s);
            }
            
            public void SetPercentageText(string s)
            {
                percentageTMP.SetText(s);
            }
            
    }
}
