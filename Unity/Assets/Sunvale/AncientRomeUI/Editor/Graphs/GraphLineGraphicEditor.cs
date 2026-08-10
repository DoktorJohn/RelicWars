using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Editor;
using Sunvale.AncientRomeUI.Graphs;


namespace Sunvale.AncientRomeUI.Editor.Graphs
{
    [CustomEditor(typeof(GraphLineGraphic))]
    public class GraphLineGraphicEditor : UnityEditor.Editor
    {
        private Texture2D packIcon;

        private const string Description =
            "Draws one or more data lines as a UI mesh, including thick segments and diamond point markers. Use SetGraphData at runtime or generate dummy data to preview the chart.";

        private void OnEnable()
        {
            packIcon = SunvaleInspectorDescription.LoadPackIcon();
        }

        protected override void OnHeaderGUI()
        {
            base.OnHeaderGUI();
            SunvaleInspectorDescription.DrawHeaderIcon(packIcon);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space(4);
            SunvaleInspectorDescription.DrawBox(packIcon, Description);
            EditorGUILayout.Space(6);

            DrawDefaultInspector();

            GraphLineGraphic script = (GraphLineGraphic)target;

            GUILayout.Space(10);

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.fixedHeight = 35;

            if (GUILayout.Button("Generate Dummy Data", buttonStyle))
            {
                GenerateDummyData(script);
            }
        }

        private void GenerateDummyData(GraphLineGraphic renderer)
        {
            List<GraphLineSeries> dummyLines = new List<GraphLineSeries>();

            int dataPointsCount = 25;
            float startYear = -200f;
            float endYear = 0f;

            GraphLineSeries wealthLine = new GraphLineSeries
            {
                lineName = "Wealth",
                lineColor = new Color(0.6f, 0.1f, 0.1f, 1f),
                lineThickness = 3f,
                pointSize = 6f,
                minX = startYear, maxX = endYear,
                minY = 0f, maxY = 30000f,
                dataPoints = GenerateRandomWalkData(dataPointsCount, startYear, endYear, 5000f, 0f, 30000f, 3000f)
            };
            dummyLines.Add(wealthLine);

            GraphLineSeries happinessLine = new GraphLineSeries
            {
                lineName = "Happiness",
                lineColor = new Color(0.1f, 0.5f, 0.1f, 1f),
                lineThickness = 3f,
                pointSize = 6f,
                minX = startYear, maxX = endYear,
                minY = 0f, maxY = 100f,
                dataPoints = GenerateRandomWalkData(dataPointsCount, startYear, endYear, 50f, 0f, 100f, 15f)
            };
            dummyLines.Add(happinessLine);

            GraphLineSeries popLine = new GraphLineSeries
            {
                lineName = "Population",
                lineColor = new Color(0.2f, 0.3f, 0.6f, 1f),
                lineThickness = 3f,
                pointSize = 6f,
                minX = startYear, maxX = endYear,
                minY = 0f, maxY = 1000000f,
                dataPoints = GenerateRandomWalkData(dataPointsCount, startYear, endYear, 200000f, 0f, 1000000f, 80000f)
            };
            dummyLines.Add(popLine);

            renderer.SetGraphData(dummyLines);
            EditorUtility.SetDirty(renderer);
        }

        private Vector2[] GenerateRandomWalkData(int count, float minX, float maxX, float startY, float minY, float maxY, float maxStep)
        {
            Vector2[] points = new Vector2[count];
            float currentY = startY;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                float currentX = Mathf.Lerp(minX, maxX, t);

                points[i] = new Vector2(currentX, currentY);

                float randomStep = Random.Range(-maxStep, maxStep);
                currentY += randomStep;
                currentY = Mathf.Clamp(currentY, minY, maxY);
            }

            return points;
        }
    }

}
