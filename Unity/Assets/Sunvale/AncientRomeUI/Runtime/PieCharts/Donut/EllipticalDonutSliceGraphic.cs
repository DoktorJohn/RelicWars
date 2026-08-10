using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.PieCharts.Donut
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class EllipticalDonutSliceGraphic : MaskableGraphic
    {
        [Range(0f, 1f)]
        public float startPercent = 0f;
        [Range(0f, 1f)]
        public float fillPercent = 0.5f;

        [Tooltip("Size of the inner cut-out. 0.5 means the hole takes up half the chart.")]
        [Range(0.05f, 0.95f)]
        public float innerHoleSize = 0.5f;
        
        [Tooltip("How many vertices make up a full 180 arc. Higher = smoother edges.")]
        public int arcResolution = 100;

        [Tooltip("Pulls the slice inward by this many pixels so it hides under the border.")]
        public float sliceInset = 1.5f;

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
            if (fillPercent <= 0.001f) return;

            Rect rect = rectTransform.rect;
            
            // Elliptical Radii based on RectTransform Width and Height
            float rxOuter = rect.width / 2f;
            float ryOuter = rect.height; 
            
            float rxInner = rxOuter * innerHoleSize;
            float ryInner = ryOuter * innerHoleSize;
            
            // Approximate how many degrees to shrink the start/end cuts based on the pixel inset
            float avgRadius = (rxOuter + rxInner) / 2f;
            float angularInsetDeg = (avgRadius > 0) ? (sliceInset / avgRadius) * Mathf.Rad2Deg : 0f;

            // Apply angular inset
            float startAngleDeg = -90f + (startPercent * 180f) + angularInsetDeg;
            float fillAngleDeg = (fillPercent * 180f) - (angularInsetDeg * 2f);

            // Safety check: if the inset shrinks the slice out of existence, don't draw it
            if (fillAngleDeg <= 0f) return;

            int segments = Mathf.Max(1, Mathf.CeilToInt(arcResolution * (fillAngleDeg / 180f)));
            float angleStep = fillAngleDeg / segments;

            // 1. Generate Vertices in 3 Rings (Inner, Middle, Outer)
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments; // 0 to 1 along the arc cut

                float currentAngleDeg = startAngleDeg + (angleStep * i);
                float currentAngleRad = currentAngleDeg * Mathf.Deg2Rad;

                float sinA = Mathf.Sin(currentAngleRad); // X axis
                float cosA = Mathf.Cos(currentAngleRad); // Y axis

                // Calculate the outward normal of the ellipse at this point
                Vector2 normal = new Vector2(ryOuter * sinA, rxOuter * cosA).normalized;

                // Calculate Positions
                Vector2 innerPos = new Vector2(sinA * rxInner, cosA * ryInner) + (normal * sliceInset);
                Vector2 outerPos = new Vector2(sinA * rxOuter, cosA * ryOuter) - (normal * sliceInset);
                Vector2 midPos = (innerPos + outerPos) / 2f; // Exactly between the new inset inner/outer

                // --- 1. Add INNER RING (Pushed outward toward middle by inset) ---
                UIVertex innerVert = UIVertex.simpleVert;
                innerVert.color = color;
                innerVert.position = innerPos;
                innerVert.uv0 = new Vector2((innerPos.x / rect.width) + 0.5f, innerPos.y / rect.height);
                innerVert.uv1 = new Vector2(1f, t); // UV1.x = 1 (Dark edge)
                vh.AddVert(innerVert);

                // --- 2. Add MIDDLE RING ---
                UIVertex midVert = UIVertex.simpleVert;
                midVert.color = color;
                midVert.position = midPos;
                midVert.uv0 = new Vector2((midPos.x / rect.width) + 0.5f, midPos.y / rect.height);
                midVert.uv1 = new Vector2(0f, t); // UV1.x = 0 (Clean inner area)
                vh.AddVert(midVert);

                // --- 3. Add OUTER RING (Pushed inward toward middle by inset) ---
                UIVertex outerVert = UIVertex.simpleVert;
                outerVert.color = color;
                outerVert.position = outerPos;
                outerVert.uv0 = new Vector2((outerPos.x / rect.width) + 0.5f, outerPos.y / rect.height);
                outerVert.uv1 = new Vector2(1f, t); // UV1.x = 1 (Dark edge)
                vh.AddVert(outerVert);
            }

            // 2. Connect Vertices into Quads (Triangles)
            for (int i = 0; i < segments; i++)
            {
                // Calculate indices for the 6 vertices that make up the current slice segment
                int in0 = i * 3;       // Inner
                int mid0 = in0 + 1;    // Middle
                int out0 = in0 + 2;    // Outer
                
                int in1 = (i + 1) * 3; // Next Inner
                int mid1 = in1 + 1;    // Next Middle
                int out1 = in1 + 2;    // Next Outer

                // Inner Quad (Inner ring to Middle ring)
                vh.AddTriangle(in0, mid0, mid1);
                vh.AddTriangle(in0, mid1, in1);

                // Outer Quad (Middle ring to Outer ring)
                vh.AddTriangle(mid0, out0, out1);
                vh.AddTriangle(mid0, out1, mid1);
            }
        }
    }
}
