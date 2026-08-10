using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.PieCharts
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class PieChartSliceGraphic : MaskableGraphic
    {
        [Range(0f, 1f)]
        public float startPercent = 0f;
        [Range(0f, 1f)]
        public float fillPercent = 0.25f; // 0.25 is a 90-degree slice
        
        [Tooltip("How many vertices make up a full 360 circle. Higher = smoother edges.")]
        public int circleResolution = 100;

        
        #if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty();
        }
        #endif

       protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            // If the slice is basically empty, don't draw anything
            if (fillPercent <= 0.001f) return;

            // Get the dimensions of the RectTransform
            Rect rect = rectTransform.rect;
            float outerRadius = Mathf.Min(rect.width, rect.height) / 2f;

            // 1. Add the Center Vertex (Vertex 0)
            UIVertex centerVert = UIVertex.simpleVert;
            centerVert.color = color;
            centerVert.position = Vector2.zero; // Local center
            centerVert.uv0 = new Vector2(0.5f, 0.5f); // Center of texture
            
            // ---> NEW: EDGE DETECTION DATA <---
            // x = 0 (Center of circle), y = 0.5 (Middle of the straight cuts, prevents artifacts)
            centerVert.uv1 = new Vector2(0f, 0.5f); 

            vh.AddVert(centerVert);

            // Calculate how many segments we need for this specific slice
            int segments = Mathf.Max(1, Mathf.CeilToInt(circleResolution * fillPercent));
            
            // We start at 12 o'clock and go clockwise
            float startAngleDeg = startPercent * 360f;
            float fillAngleDeg = fillPercent * 360f;
            float angleStep = fillAngleDeg / segments;

            // 2. Add the outer arc vertices
            for (int i = 0; i <= segments; i++)
            {
                // ---> NEW: EDGE DETECTION PROGRESS <---
                // Calculate a 0.0 to 1.0 progress value along the outer arc
                float t = (float)i / segments;

                // Calculate current angle (Start at top: 12 o'clock)
                // standard math 0 is Right, so we use Sin for X and Cos for Y to start at Top
                float currentAngleDeg = startAngleDeg + (angleStep * i);
                float currentAngleRad = currentAngleDeg * Mathf.Deg2Rad;

                float x = Mathf.Sin(currentAngleRad) * outerRadius;
                float y = Mathf.Cos(currentAngleRad) * outerRadius;

                UIVertex vert = UIVertex.simpleVert;
                vert.color = color;
                vert.position = new Vector2(x, y);
                
                // Map UVs so the texture sits perfectly across the bounding box
                // This prevents the texture from squishing or rotating with the slice
                vert.uv0 = new Vector2(
                    (x / rect.width) + 0.5f, 
                    (y / rect.height) + 0.5f
                );
                
                // ---> NEW: EDGE DETECTION DATA <---
                // x = 1 (Outer edge radius), y = t (Distance from StartCut to EndCut)
                vert.uv1 = new Vector2(1f, t);

                vh.AddVert(vert);
            }

            // 3. Connect vertices to form triangles
            // The center is 0. The arc vertices are 1, 2, 3...
            for (int i = 1; i <= segments; i++)
            {
                vh.AddTriangle(0, i, i + 1);
            }
        }
    }
}
