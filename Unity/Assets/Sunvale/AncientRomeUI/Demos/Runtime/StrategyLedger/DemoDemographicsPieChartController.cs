using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.AncientRomeUI.Buttons;
using Sunvale.AncientRomeUI.PieCharts;


namespace Sunvale.AncientRomeUI.Demos.StrategyLedger
{
    public class DemoDemographicsPieChartController : MonoBehaviour
    {
        public enum DemographicsPieChartMode
        {
            Strata,
            Age
        }

        [Header("References")] public List<PieChartCategoryLabel> labelsList;
        public PieChartGenerator pieChartGenerator;
        public FramedSpriteTabButton strataTab;
        public FramedSpriteTabButton ageTab;

        [Header("Settings")] public DemographicsPieChartMode defaultMode = DemographicsPieChartMode.Strata;
        public bool animateTabs = true;

        [Tooltip("When false, categories with 0 value are hidden from labels and pie chart.")]
        public bool showZeroValueCategories = false;

        [Tooltip("Use 0 for whole percentages, 1 for 12.3%, 2 for 12.34%, etc.")]
        public int percentageDecimalPlaces = 0;

        private bool wasInitialized;
        private RomeCityData currentDemoData;
        private DemographicsPieChartMode currentMode;

        private static readonly string[] StrataNames =
        {
            "Dependants",
            "Slaves",
            "Plebs",
            "Freemen",
            "Merchants",
            "Patricians"
        };

        private static readonly string[] AgeNames =
        {
            "Children",
            "Youth",
            "Adults",
            "Seniors",
            "Elders"
        };


        private void OnDestroy()
        {
            if (strataTab != null)
                strataTab.OnButtonActivatedClicked -= HandleTabClicked;

            if (ageTab != null)
                ageTab.OnButtonActivatedClicked -= HandleTabClicked;
        }

        private void InnerInitialization()
        {
            if (wasInitialized)
                return;

            wasInitialized = true;
            strataTab.OnButtonActivatedClicked += HandleTabClicked;
            ageTab.OnButtonActivatedClicked += HandleTabClicked;
        }

        public void InitializeForDemographics(RomeCityData demoData)
        {
            // STATE KEEPING: Only set the mode to default on the very first initialization
            if (!wasInitialized)
            {
                currentMode = defaultMode;
            }

            InnerInitialization();
            currentDemoData = demoData;

            // Mode is no longer overwritten here, so it naturally keeps its state

            RefreshTabs(false);
            RefreshPieChart();
        }

        private void HandleTabClicked(FramedSpriteTabButton clickedTab)
        {
            if (clickedTab == strataTab)
                SetMode(DemographicsPieChartMode.Strata);
            else if (clickedTab == ageTab)
                SetMode(DemographicsPieChartMode.Age);
        }

        public void SetMode(DemographicsPieChartMode newMode)
        {
            if (currentMode == newMode && currentDemoData != null)
                return;

            currentMode = newMode;

            RefreshTabs(animateTabs);
            RefreshPieChart();
        }

        private void RefreshTabs(bool withAnimation)
        {
            if (strataTab != null)
                strataTab.SetSelected(currentMode == DemographicsPieChartMode.Strata, withAnimation);

            if (ageTab != null)
                ageTab.SetSelected(currentMode == DemographicsPieChartMode.Age, withAnimation);
        }

        private void RefreshPieChart()
        {
            if (currentDemoData == null)
                return;

            if (labelsList == null || labelsList.Count == 0)
            {
                Debug.LogWarning($"{nameof(DemoDemographicsPieChartController)} has no labels assigned.");
                return;
            }

            if (pieChartGenerator == null)
            {
                Debug.LogWarning($"{nameof(DemoDemographicsPieChartController)} has no pie chart generator assigned.");
                return;
            }

            switch (currentMode)
            {
                case DemographicsPieChartMode.Strata:
                    ApplyCategories(
                        StrataNames,
                        new[]
                        {
                            currentDemoData.dependant,
                            currentDemoData.slaves,
                            currentDemoData.plebs,
                            currentDemoData.freemen,
                            currentDemoData.merchants,
                            currentDemoData.patricians
                        }
                    );
                    break;

                case DemographicsPieChartMode.Age:
                    ApplyCategories(
                        AgeNames,
                        new[]
                        {
                            currentDemoData.children,
                            currentDemoData.youth,
                            currentDemoData.adults,
                            currentDemoData.seniors,
                            currentDemoData.elders
                        }
                    );
                    break;
            }
        }

        private void ApplyCategories(string[] categoryNames, int[] values)
        {
            int total = 0;

            for (int i = 0; i < values.Length; i++)
            {
                if (showZeroValueCategories || values[i] > 0)
                    total += Mathf.Max(0, values[i]);
            }

            List<PieChartGenerator.PieChartCategory> pieCategories =
                new List<PieChartGenerator.PieChartCategory>();

            int activeLabelIndex = 0;

            for (int i = 0; i < categoryNames.Length; i++)
            {
                int value = Mathf.Max(0, values[i]);

                if (!showZeroValueCategories && value <= 0)
                    continue;

                if (activeLabelIndex >= labelsList.Count)
                {
                    Debug.LogWarning(
                        $"{nameof(DemoDemographicsPieChartController)} needs more labels. " +
                        $"Missing label for category '{categoryNames[i]}'."
                    );
                    break;
                }

                PieChartCategoryLabel label = labelsList[activeLabelIndex];

                if (label != null)
                {
                    label.gameObject.SetActive(true);
                    label.SetLabelName(categoryNames[i]);

                    float percent = total > 0 ? value / (float) total * 100f : 0f;

                    label.SetNumberAndPercentagesStrings(
                        FormatNumber(value),
                        $"({FormatPercent(percent)}%)"
                    );

                    pieCategories.Add(new PieChartGenerator.PieChartCategory
                    {
                        name = categoryNames[i],
                        value = value,
                        color = label.myVertexColorTint,
                        material = label.myMaterial
                    });
                }

                activeLabelIndex++;
            }

            for (int i = activeLabelIndex; i < labelsList.Count; i++)
            {
                if (labelsList[i] != null)
                    labelsList[i].gameObject.SetActive(false);
            }

            pieChartGenerator.SetNewCategoriesMakesACopyOfList(pieCategories);
            pieChartGenerator.GenerateChart();
        }

        private string FormatNumber(int value)
        {
            return value.ToString("N0");
        }

        private string FormatPercent(float percent)
        {
            int safeDecimalPlaces = Mathf.Max(0, percentageDecimalPlaces);

            if (safeDecimalPlaces == 0)
                return Mathf.RoundToInt(percent).ToString();

            return percent.ToString("F" + safeDecimalPlaces);
        }
    }
}
