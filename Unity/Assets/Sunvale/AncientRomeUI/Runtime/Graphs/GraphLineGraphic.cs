using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Graphs
{
    [RequireComponent(typeof(CanvasRenderer))]
    [ExecuteAlways]
    public class GraphLineGraphic : MaskableGraphic
    {
        [Header("Graph Data")]
        public List<GraphLineSeries> graphLines = new List<GraphLineSeries>();

        // This allows you to update the graph from an external script
        public void SetGraphData(List<GraphLineSeries> newLines)
        {
            graphLines = newLines;
            SetVerticesDirty(); // Forces the mesh to redraw
        }

        #if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty(); // Updates immediately if you tweak colors/values in Editor
        }
        
        #endif

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); // Erase old mesh

            if (graphLines == null || graphLines.Count == 0) return;

            Rect rect = rectTransform.rect;

            // Loop through every line data set provided
            foreach (GraphLineSeries line in graphLines)
            {
                if (line.dataPoints == null || line.dataPoints.Length < 2) continue;

                // 1. Calculate Screen Positions for this specific line
                Vector2[] screenPoints = new Vector2[line.dataPoints.Length];
                
                for (int i = 0; i < line.dataPoints.Length; i++)
                {
                    // InverseLerp finds the percentage (0.0 to 1.0) of the value within the bounds
                    float normX = Mathf.InverseLerp(line.minX, line.maxX, line.dataPoints[i].x);
                    float normY = Mathf.InverseLerp(line.minY, line.maxY, line.dataPoints[i].y);

                    // Lerp maps that percentage to the physical RectTransform pixel coordinates
                    screenPoints[i] = new Vector2(
                        Mathf.Lerp(rect.xMin, rect.xMax, normX),
                        Mathf.Lerp(rect.yMin, rect.yMax, normY)
                    );
                }

                // 2. Draw the Thick Lines connecting the points
                for (int i = 0; i < screenPoints.Length - 1; i++)
                {
                    DrawThickLine(vh, screenPoints[i], screenPoints[i + 1], line.lineThickness, line.lineColor);
                }

                // 3. Draw the Point Diamonds (Hides overlapping miter joints nicely)
                if (line.pointSize > 0)
                {
                    for (int i = 0; i < screenPoints.Length; i++)
                    {
                        DrawDiamondDot(vh, screenPoints[i], line.pointSize, line.lineColor);
                    }
                }
            }
        }

        // --- MESH GENERATION HELPERS ---

        private void DrawThickLine(VertexHelper vh, Vector2 start, Vector2 end, float thickness, Color color)
        {
            Vector2 direction = (end - start).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * (thickness / 2f);

            UIVertex[] verts = new UIVertex[4];
            for (int i = 0; i < 4; i++)
            {
                verts[i] = UIVertex.simpleVert;
                verts[i].color = color;
            }

            // Apply Positions and UVs
            // UVs are mapped so X goes along the line, and Y goes across the thickness
            
            verts[0].position = start + normal; // Top Left
            verts[0].uv0 = new Vector2(0, 1);

            verts[1].position = start - normal; // Bottom Left
            verts[1].uv0 = new Vector2(0, 0);

            verts[2].position = end - normal;   // Bottom Right
            verts[2].uv0 = new Vector2(1, 0);

            verts[3].position = end + normal;   // Top Right
            verts[3].uv0 = new Vector2(1, 1);

            vh.AddUIVertexQuad(verts);
        }

        private void DrawDiamondDot(VertexHelper vh, Vector2 center, float size, Color color)
        {
            float halfSize = size / 2f;
            UIVertex[] verts = new UIVertex[4];
            for (int i = 0; i < 4; i++)
            {
                verts[i] = UIVertex.simpleVert;
                verts[i].color = color;
            }

            // Positions form a diamond, UVs map a standard texture square onto that diamond
            verts[0].position = new Vector3(center.x, center.y + halfSize); // Top
            verts[0].uv0 = new Vector2(0.5f, 1f);

            verts[1].position = new Vector3(center.x + halfSize, center.y); // Right
            verts[1].uv0 = new Vector2(1f, 0.5f);

            verts[2].position = new Vector3(center.x, center.y - halfSize); // Bottom
            verts[2].uv0 = new Vector2(0.5f, 0f);

            verts[3].position = new Vector3(center.x - halfSize, center.y); // Left
            verts[3].uv0 = new Vector2(0f, 0.5f);

            vh.AddUIVertexQuad(verts);
        }

       
    }

}
