using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.PieCharts
{
    public class PieChartCategoryLabel : MonoBehaviour
    {
            public RectTransform myRectTransform;
            public Material myMaterial;
            public Color myVertexColorTint;

            public TextMeshProUGUI labelName;
            public TextMeshProUGUI thingCounter;
            public TextMeshProUGUI thingPercentages;




            public void SetLabelName(string newLabel)
            {
                    labelName.SetText(newLabel);
            }

            public void SetNumberAndPercentagesStrings(string numberString, string percentageString)
            {
                    thingCounter.SetText(numberString);
                    thingPercentages.SetText(percentageString);
            }
    }
}
