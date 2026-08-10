using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.StrategyLedger
{
    public class DemoCharacterPortraitTagView : MonoBehaviour
    {

        public Image iconImage;

        public TextMeshProUGUI nameLabelTMP;
        public TextMeshProUGUI bottomExtraLabel;



        public void SetIconSprite(Sprite sprite)
        {
            iconImage.sprite = sprite;
        }


        public void SetNameLabelString(string s)
        {
            nameLabelTMP.SetText(s);
        }

        public void SetBottomExtraLabel(string s)
        {
            bottomExtraLabel.SetText(s);
        }
    }
}
