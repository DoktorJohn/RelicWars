using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.StrategyLedger
{
    public class DemoDemographicsOverviewSectionController : MonoBehaviour
    {
        private const string RedColor = "89211D";
        private const string GreenColor = "236313";

        public TextMeshProUGUI populationCounterTMP;
        public TextMeshProUGUI grwothCounterTMP;

        public DemoPopulationStrataStatView plebsDisplayer;
        public DemoPopulationStrataStatView patricianDisplayer;
        public DemoPopulationStrataStatView slavesDisplayer;
        public DemoPopulationStrataStatView merchantsDisplayer;
        public DemoPopulationStrataStatView dependatsDisplayer;

        public void Initialize(RomeEmpireStatsData statsData)
        {
            if (statsData == null)
                return;

            SetText(populationCounterTMP, $"Population: {FormatNumber(statsData.population)}");

            string growthColor = statsData.growth < 0f ? RedColor : GreenColor;
            string coloredGrowthValue = $"<color=#{growthColor}>{FormatPercent(statsData.growth)}</color>";
            SetText(grwothCounterTMP, $"Growth: {coloredGrowthValue}");

            SetStrataDisplayer(plebsDisplayer, "Plebs", statsData.plebs, statsData.population);
            SetStrataDisplayer(patricianDisplayer, "Patricians", statsData.patricians, statsData.population);
            SetStrataDisplayer(slavesDisplayer, "Slaves", statsData.slaves, statsData.population);
            SetStrataDisplayer(merchantsDisplayer, "Merchants", statsData.merchants, statsData.population);
            SetStrataDisplayer(dependatsDisplayer, "Dependent", statsData.dependant, statsData.population);
        }

        private void SetStrataDisplayer(
            DemoPopulationStrataStatView displayer,
            string label,
            int count,
            int totalPopulation)
        {
            if (displayer == null)
                return;

            displayer.SetLabelText(label);
            displayer.SetCountText(FormatNumber(count));
            displayer.SetPercentageText(GetPopulationPercent(count, totalPopulation));
        }

        private string GetPopulationPercent(int count, int totalPopulation)
        {
            if (totalPopulation <= 0)
                return "0%";

            float percentage = (float)count / totalPopulation * 100f;
            return $"{percentage:0.#}%";
        }

        private string FormatPercent(float value)
        {
            return $"{value:0.#}%";
        }

        private string FormatNumber(int value)
        {
            return value.ToString("N0").Replace(",", " ");
        }

        private void SetText(TextMeshProUGUI tmp, string value)
        {
            if (tmp != null)
                tmp.SetText(value);
        }
    }
}
