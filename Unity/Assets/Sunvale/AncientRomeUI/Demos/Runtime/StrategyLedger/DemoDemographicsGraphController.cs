using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.AncientRomeUI.Buttons;
using Sunvale.AncientRomeUI.Graphs;


namespace Sunvale.AncientRomeUI.Demos.StrategyLedger
{
    public class DemoDemographicsGraphController : MonoBehaviour
    {
        [Header("UI References")]
        public GraphAxisLabels labels;
        public GraphLineGraphic lineRenderer;
        public GraphGridGraphic gridRenderer;

        [Header("Tabs")]
        public FramedSpriteTabButton fiftyYearsTab;
        public FramedSpriteTabButton hundredYearsTab;
        public FramedSpriteTabButton hundredFiftyYearsTab;
        public FramedSpriteTabButton twoHundredYearsTab;

        [Header("Toggles")]
        public Toggle populationToggle;
        public Toggle moodToggle;
        public Toggle taxToggle;
        public Toggle taxCapitaToggle;

        [Header("Graph Styling")]
        public Color populationColor = new Color(0.2f, 0.3f, 0.6f, 1f); // Blue
        public Color moodColor = new Color(0.1f, 0.5f, 0.1f, 1f);       // Green
        public Color taxColor = new Color(0.6f, 0.1f, 0.1f, 1f);        // Red
        public Color taxCapitaColor = new Color(0.5f, 0.1f, 0.5f, 1f);  // Purple
        public float lineThickness = 3f;
        public float pointSize = 5f;
        [Tooltip("Adds empty margin above/below highest and lowest points")]
        public float yAxisPaddingPercent = 0.1f; 

        private bool wasInitialized;
        private RomeCityData currentData;

        // STATE KEEPING VARIABLES
        private int currentYearSpan = 200; // Default span
        private FramedSpriteTabButton currentTab;
        private bool isPopOn, isMoodOn, isTaxOn, isTaxCapitaOn;

        private void InnerInitialization()
        {
            if (wasInitialized) return;

            // Register Tabs
            fiftyYearsTab.OnButtonActivatedClicked += (tab) => OnTabClicked(50, tab);
            hundredYearsTab.OnButtonActivatedClicked += (tab) => OnTabClicked(100, tab);
            hundredFiftyYearsTab.OnButtonActivatedClicked += (tab) => OnTabClicked(150, tab);
            twoHundredYearsTab.OnButtonActivatedClicked += (tab) => OnTabClicked(200, tab);

            // Register Toggles & Save their states dynamically
            populationToggle.onValueChanged.AddListener((isOn) => { isPopOn = isOn; UpdateGraph(); });
            moodToggle.onValueChanged.AddListener((isOn) => { isMoodOn = isOn; UpdateGraph(); });
            taxToggle.onValueChanged.AddListener((isOn) => { isTaxOn = isOn; UpdateGraph(); });
            taxCapitaToggle.onValueChanged.AddListener((isOn) => { isTaxCapitaOn = isOn; UpdateGraph(); });

            wasInitialized = true;
        }

        public void InitializeForDemographicsData(RomeCityData romeCityDemoData)
        {
            // STATE KEEPING: Set initial default properties ONLY on the first run
            if (!wasInitialized)
            {
                currentYearSpan = 200;
                currentTab = twoHundredYearsTab;

                // Grab default toggle states from inspector setup
                isPopOn = populationToggle.isOn;
                isMoodOn = moodToggle.isOn;
                isTaxOn = taxToggle.isOn;
                isTaxCapitaOn = taxCapitaToggle.isOn;
            }

            InnerInitialization();
            currentData = romeCityDemoData;

            if (!currentData.hasHistoricalData)
            {
                currentData.GenerateHistoricalData();
            }

            // Restore Toggle states gracefully (without triggering the onValueChanged listener event loops)
            populationToggle.SetIsOnWithoutNotify(isPopOn);
            moodToggle.SetIsOnWithoutNotify(isMoodOn);
            taxToggle.SetIsOnWithoutNotify(isTaxOn);
            taxCapitaToggle.SetIsOnWithoutNotify(isTaxCapitaOn);

            // Restore Tab visual state
            SelectTabVisually(currentTab);

            UpdateGraph();
        }

        private void OnTabClicked(int span, FramedSpriteTabButton clickedTab)
        {
            currentYearSpan = span;
            currentTab = clickedTab; // Save state of currently active tab
            SelectTabVisually(clickedTab);
            UpdateGraph();
        }

        private void SelectTabVisually(FramedSpriteTabButton activeTab)
        {
            fiftyYearsTab.SetSelected(fiftyYearsTab == activeTab, true);
            hundredYearsTab.SetSelected(hundredYearsTab == activeTab, true);
            hundredFiftyYearsTab.SetSelected(hundredFiftyYearsTab == activeTab, true);
            twoHundredYearsTab.SetSelected(twoHundredYearsTab == activeTab, true);
        }

        private void UpdateGraph()
        {
            if (currentData == null) return;

            List<GraphLineSeries> activeLines = new List<GraphLineSeries>();
            int activeTogglesCount = 0;

            // X bounds for the current viewed span
            float globalMinX = -currentYearSpan;
            float globalMaxX = 0f;

            // Process toggles to build graph lines
            if (moodToggle.isOn)
            {
                activeTogglesCount++;
                activeLines.Add(ExtractLineData("Mood", currentData.historicalMood, moodColor, globalMinX, globalMaxX));
            }
            if (populationToggle.isOn)
            {
                activeTogglesCount++;
                activeLines.Add(ExtractLineData("Population", currentData.historicalPopulation, populationColor, globalMinX, globalMaxX));
            }
            if (taxToggle.isOn)
            {
                activeTogglesCount++;
                activeLines.Add(ExtractLineData("Taxes", currentData.historicalTax, taxColor, globalMinX, globalMaxX));
            }
            if (taxCapitaToggle.isOn)
            {
                activeTogglesCount++;
                activeLines.Add(ExtractLineData("Tax/Capita", currentData.historicalTaxCapita, taxCapitaColor, globalMinX, globalMaxX));
            }

            // Apply generated lines to the graph renderer
            lineRenderer.SetGraphData(activeLines);

            // Update Labels & Y-Axis visibility
            if (activeTogglesCount == 1)
            {
                // Show labels for exactly one toggle
                gridRenderer.showLeftNotches = true;
                gridRenderer.showRightNotches = true;

                GraphLineSeries activeLine = activeLines[0];
                labels.yLeftMinVal = activeLine.minY;
                labels.yLeftMaxVal = activeLine.maxY;
                labels.yRightMinVal = activeLine.minY;
                labels.yRightMaxVal = activeLine.maxY;
            }
            else
            {
                // 0 or >= 2 toggles: hide Y-axis labels. Lines will still respect their own mapped Min/Max
                gridRenderer.showLeftNotches = false;
                gridRenderer.showRightNotches = false;
            }

            // Update X-Axis labels range
            labels.xMinVal = globalMinX;
            labels.xMaxVal = globalMaxX;

            // Force visual update on the modified UI components
            gridRenderer.SetVerticesDirty();
            labels.GenerateLabels();
        }

        private GraphLineSeries ExtractLineData(string name, float[] rawData, Color color, float minX, float maxX)
        {
            // Safe guard
            if (rawData == null || rawData.Length == 0) return new GraphLineSeries();

            int totalArraySize = rawData.Length; // Expected to be 200 (index 0 to 199)

            // Ensure we don't try to read outside array bounds if a shorter array gets passed
            int actualSpan = Mathf.Min(currentYearSpan, totalArraySize);
            int startIndex = totalArraySize - actualSpan;

            Vector2[] points = new Vector2[actualSpan];
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            // Extract slice, map X values and find Y extents
            for (int i = 0; i < actualSpan; i++)
            {
                int dataIndex = startIndex + i;

                // Dynamically spread the points across the requested span, no matter the array size
                float xVal;
                if (actualSpan > 1)
                {
                    float t = (float) i / (actualSpan - 1);
                    xVal = Mathf.Lerp(minX, maxX, t);
                }
                else
                {
                    xVal = maxX;
                }

                float yVal = rawData[dataIndex];

                points[i] = new Vector2(xVal, yVal);

                // Calculate min/max natively to scale line perfectly
                if (yVal < minY) minY = yVal;
                if (yVal > maxY) maxY = yVal;
            }

            // Add optional padding so the peaks don't stick to the very top/bottom of the graph border
            if (Mathf.Approximately(minY, maxY))
            {
                // Flatline
                minY -= 10f;
                maxY += 10f;
            }
            else if (yAxisPaddingPercent > 0)
            {
                float padding = (maxY - minY) * yAxisPaddingPercent;
                minY -= padding;
                maxY += padding;
            }

            return new GraphLineSeries
            {
                lineName = name,
                lineColor = color,
                lineThickness = lineThickness,
                pointSize = pointSize,
                minX = minX,
                maxX = maxX,
                minY = minY,
                maxY = maxY,
                dataPoints = points
            };
        }
#if UNITY_EDITOR
            
            
        [Header("Editor Preview")]
        public bool autoUpdatePreview = true;
        
        [Header("Mock Data")]
        public float[] mockPopulation = { 1000f, 1200f, 1500f, 1400f, 1800f };
        public float[] mockMood = { 50f, 45f, 60f, 55f, 70f };
        public float[] mockTax = { 200f, 250f, 230f, 300f, 350f };
        public float[] mockTaxCapita = { 2f, 2.5f, 2.2f, 3f, 3.5f };

        public void PreviewMockData()
        {
            wasInitialized = true;
            
            if (currentData == null)
            {
                currentData = new RomeCityData();
            }

            // Pass the custom mock arrays directly instead of generating mathematically
            currentData.historicalPopulation = mockPopulation;
            currentData.historicalMood = mockMood;
            currentData.historicalTax = mockTax;
            currentData.historicalTaxCapita = mockTaxCapita;
            currentData.hasHistoricalData = true;

            isPopOn = populationToggle != null ? populationToggle.isOn : true;
            isMoodOn = moodToggle != null ? moodToggle.isOn : true;
            isTaxOn = taxToggle != null ? taxToggle.isOn : true;
            isTaxCapitaOn = taxCapitaToggle != null ? taxCapitaToggle.isOn : true;

            UpdateGraph();
        }

        public void ClearPreview()
        {
            if (lineRenderer != null)
            {
                lineRenderer.SetGraphData(new System.Collections.Generic.List<GraphLineSeries>());
            }
        }
#endif
    }
}
