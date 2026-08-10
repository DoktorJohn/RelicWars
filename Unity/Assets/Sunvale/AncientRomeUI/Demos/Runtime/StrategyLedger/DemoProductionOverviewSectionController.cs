using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.StrategyLedger
{
    public class DemoProductionOverviewSectionController : MonoBehaviour
    {
        private const string RedColor = "89211D";
        private const string GreenColor = "236313";

        public DemoProductionItemView wheatItem;
        public DemoProductionItemView meatItem;
        public DemoProductionItemView woodItem;
        public DemoProductionItemView linenItem;

        public DemoProductionItemView olivesItem;
        public DemoProductionItemView horsesItem;
        public DemoProductionItemView bronzeItem;
        public DemoProductionItemView produceItem;

        public void Initialize(RomeEmpireStatsData data)
        {
            if (data == null)
                return;

            SetProductionItem(wheatItem, "Wheat", data.wheatProduction, data.wheatPercent);
            SetProductionItem(meatItem, "Meat", data.meatProduction, data.meatPercent);
            SetProductionItem(woodItem, "Wood", data.woodProduction, data.woodPercent);
            SetProductionItem(linenItem, "Linen", data.linenProduction, data.linenPercent);

            SetProductionItem(olivesItem, "Olives", data.olivesProduction, data.olivesPercent);
            SetProductionItem(horsesItem, "Horses", data.horsesProduction, data.horsesPercent);
            SetProductionItem(bronzeItem, "Bronze", data.bronzeProduction, data.bronzePercent);
            SetProductionItem(produceItem, "Produce", data.produceProduction, data.producePercent);
        }

        private void SetProductionItem(DemoProductionItemView item, string itemName, float units, float monthlyPercent)
        {
            if (item == null)
                return;

            item.SetName(itemName);
            item.SetUnits(FormatNumber(units));
            item.SetMonthlyPercent(FormatColoredPercent(monthlyPercent));
        }

        private string FormatColoredPercent(float value)
        {
            string color = value < 0f ? RedColor : GreenColor;
            string sign = value > 0f ? "+" : "";

            return $"<color=#{color}>{sign}{value:0.#}%</color>";
        }

        private string FormatNumber(float value)
        {
            long roundedValue = Convert.ToInt64(Math.Round(value));
            return roundedValue.ToString("N0", CultureInfo.InvariantCulture).Replace(",", " ");
        }
    }
}
