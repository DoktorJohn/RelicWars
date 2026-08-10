using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Graphs
{
    [System.Serializable]
    public class GraphLineSeries
    {
        public string lineName = "New Data Line"; // Helpful for Editor debugging
        public Color lineColor = Color.white;
        public float lineThickness = 3f;
        public float pointSize = 6f; // Draws a diamond at each point to hide joints

        [Tooltip("The actual raw data. X = Year/Time, Y = Value")]
        public Vector2[] dataPoints;

        [Header("Scale Bounds")]
        [Tooltip("The lowest and highest X values (e.g., Year -200 to Year 0)")]
        public float minX;
        public float maxX;

        [Tooltip("The lowest and highest Y values (e.g., Happiness 0 to 100)")]
        public float minY;
        public float maxY;
    }
}
