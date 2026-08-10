using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.StrategyTopDown
{
    public class DemoTradeRouteRowView : MonoBehaviour
    {
        public GameObject root;
        public Image routeTypeImage;
        public TextMeshProUGUI destinationTMP;
        public TextMeshProUGUI incomeTMP;

        public void SetData(DemoTradeRouteData route, Sprite routeSprite)
        {
            if (route == null)
            {
                SetVisible(false);
                return;
            }

            if (routeTypeImage != null)
            {
                routeTypeImage.sprite = routeSprite;
                routeTypeImage.enabled = routeSprite != null;
            }

            if (destinationTMP != null)
                destinationTMP.SetText(route.destinationProvince);

            if (incomeTMP != null)
                incomeTMP.SetText(route.income.ToString());
        }

        public void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.SetActive(visible);
                return;
            }

            if (routeTypeImage != null)
                routeTypeImage.gameObject.SetActive(visible);

            if (destinationTMP != null)
                destinationTMP.gameObject.SetActive(visible);

            if (incomeTMP != null)
                incomeTMP.gameObject.SetActive(visible);
        }
    }
}
