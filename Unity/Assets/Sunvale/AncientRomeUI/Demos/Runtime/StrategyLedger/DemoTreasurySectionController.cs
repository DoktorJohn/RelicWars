using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.StrategyLedger
{
    public class DemoTreasurySectionController : MonoBehaviour
    {
        [Header("Treasury")]
        public TextMeshProUGUI treasuryAmountText;

        [Header("Monthly Income Numbers")]
        public TextMeshProUGUI taxesIncomeText;
        public TextMeshProUGUI tradeIncomeText;
        public TextMeshProUGUI mintingIncomeText;
        public TextMeshProUGUI tariffsIncomeText;
        public TextMeshProUGUI plunderIncomeText;
        public TextMeshProUGUI monthlyIncomeTotalText;

        [Header("Monthly Expense Numbers")]
        public TextMeshProUGUI armyExpenseText;
        public TextMeshProUGUI navyExpenseText;
        public TextMeshProUGUI administrationExpenseText;
        public TextMeshProUGUI constructionExpenseText;
        public TextMeshProUGUI inflationExpenseText;
        public TextMeshProUGUI monthlyExpenseTotalText;

        public void Initialize(RomeEmpireStatsData data)
        {
            if (data == null)
                return;

            SetText(treasuryAmountText, FormatNumber(data.treasuryBalance));

            SetText(taxesIncomeText, FormatSignedNumber(data.taxes));
            SetText(tradeIncomeText, FormatSignedNumber(data.tradeIncome));
            SetText(mintingIncomeText, FormatSignedNumber(data.mintingIncome));
            SetText(tariffsIncomeText, FormatSignedNumber(data.tariffsIncome));
            SetText(plunderIncomeText, FormatSignedNumber(data.plunderIncome));
            SetText(monthlyIncomeTotalText, FormatSignedNumber(data.totalIncome));

            SetText(armyExpenseText, FormatSignedNumber(data.armyExpenditure));
            SetText(navyExpenseText, FormatSignedNumber(data.navyExpenditure));
            SetText(administrationExpenseText, FormatSignedNumber(data.administrationExpenditure));
            SetText(constructionExpenseText, FormatSignedNumber(data.constructionExpenditure));
            SetText(inflationExpenseText, FormatSignedNumber(data.inflationExpenditure));
            SetText(monthlyExpenseTotalText, FormatSignedNumber(data.totalExpenditure));
        }

        private string FormatSignedNumber(float value)
        {
            string sign = value > 0f ? "+" : "";
            return $"{sign}{FormatNumber(value)}";
        }

        private string FormatNumber(float value)
        {
            long roundedValue = Convert.ToInt64(Math.Round(value));
            return roundedValue.ToString("N0", CultureInfo.InvariantCulture).Replace(",", " ");
        }

        private void SetText(TextMeshProUGUI tmp, string value)
        {
            if (tmp != null)
                tmp.SetText(value);
        }
    }
}
