using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.PieCharts.Donut
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class EllipticalDonutBorderGraphic : MaskableGraphic
    {
        [Range(0f, 1f)] public float startPercent = 0f;
        [Range(0f, 1f)] public float fillPercent = 0.5f;
        [Range(0.05f, 0.95f)] public float innerHoleSize = 0.5f;
        public int arcResolution = 100;
        public float borderThickness = 4f;

        [Header("Alignment")]
        [Range(0f, 1f)]
        [Tooltip("0 = Inside the slice (hides slice edges), 0.5 = Centered, 1 = Outside the slice")]
        public float borderAlignment = 0f;

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
            if (fillPercent <= 0.001f || borderThickness <= 0f) return;

            Rect rect = rectTransform.rect;
            float rxOuter = rect.width / 2f;
            float ryOuter = rect.height; 
            float rxInner = rxOuter * innerHoleSize;
            float ryInner = ryOuter * innerHoleSize;

            bool isFullCircle = fillPercent >= 0.999f;
            int segments = Mathf.Max(1, Mathf.CeilToInt(arcResolution * fillPercent));
            float startAngleDeg = -90f + (startPercent * 180f);
            float angleStep = (fillPercent * 180f) / segments;

            Vector2[] innerPts = new Vector2[segments + 1];
            Vector2[] outerPts = new Vector2[segments + 1];

            // 1. Calculate base mathematical points
            for (int i = 0; i <= segments; i++)
            {
                float angleRad = (startAngleDeg + (angleStep * i)) * Mathf.Deg2Rad;
                float sinA = Mathf.Sin(angleRad);
                float cosA = Mathf.Cos(angleRad);

                innerPts[i] = new Vector2(sinA * rxInner, cosA * ryInner);
                outerPts[i] = new Vector2(sinA * rxOuter, cosA * ryOuter);
            }

            if (isFullCircle)
            {
                DrawRing(vh, outerPts, true);
                DrawRing(vh, innerPts, false);
            }
            else
            {
                DrawContinuousMiteredBorder(vh, outerPts, innerPts);
            }
        }

        private void DrawContinuousMiteredBorder(VertexHelper vh, Vector2[] outerPts, Vector2[] innerPts)
        {
            int pointCount = (outerPts.Length * 2);
            Vector2[] path = new Vector2[pointCount];

            // 2. Build a continuous loop
            for (int i = 0; i < outerPts.Length; i++) path[i] = outerPts[i];
            for (int i = 0; i < innerPts.Length; i++) path[outerPts.Length + i] = innerPts[innerPts.Length - 1 - i];

            Vector2[] innerPerimeter = new Vector2[pointCount];
            Vector2[] outerPerimeter = new Vector2[pointCount];
            float[] distances = new float[pointCount];
            float totalDist = 0f;

            // Alignment calculations
            float innerThick = borderThickness * (1f - borderAlignment);
            float outerThick = borderThickness * borderAlignment;

            // 3. Calculate Miter Joints (Welded corners)
            for (int i = 0; i < pointCount; i++)
            {
                int prev = (i - 1 + pointCount) % pointCount;
                int next = (i + 1) % pointCount;

                Vector2 dirPrev = (path[i] - path[prev]).normalized;
                Vector2 dirNext = (path[next] - path[i]).normalized;

                // Outward normal of the path loop
                Vector2 nPrev = new Vector2(-dirPrev.y, dirPrev.x);
                Vector2 nNext = new Vector2(-dirNext.y, dirNext.x);

                Vector2 miter = (nPrev + nNext).normalized;
                if (miter.sqrMagnitude < 0.001f) miter = nPrev;

                float dot = Vector2.Dot(miter, nPrev);
                float miterScalar = 1f;
                if (dot > 0.05f) miterScalar = 1f / dot;
                
                // Cap miter length to prevent spikes on extreme U-turns
                miterScalar = Mathf.Min(miterScalar, 4f);

                // Calculate inner/outer boundaries based on alignment
                innerPerimeter[i] = path[i] - (miter * (innerThick * miterScalar));
                outerPerimeter[i] = path[i] + (miter * (outerThick * miterScalar));

                distances[i] = Vector2.Distance(path[i], path[next]);
                totalDist += distances[i];
            }

            // 4. Generate the continuous mesh ribbon
            float currentDist = 0f;
            for (int i = 0; i < pointCount; i++)
            {
                int next = (i + 1) % pointCount;

                float u0 = currentDist / totalDist;
                float u1 = (currentDist + distances[i]) / totalDist;

                AddRibbonQuad(vh, innerPerimeter[i], outerPerimeter[i], outerPerimeter[next], innerPerimeter[next], u0, u1);
                
                currentDist += distances[i];
            }
        }

        private void DrawRing(VertexHelper vh, Vector2[] pts, bool isOuterRing)
        {
            int count = pts.Length;
            Vector2[] innerEdge = new Vector2[count];
            Vector2[] outerEdge = new Vector2[count];
            
            float totalDist = 0f;
            float[] distances = new float[count - 1];

            float innerThick = borderThickness * (1f - borderAlignment);
            float outerThick = borderThickness * borderAlignment;

            for (int i = 0; i < count; i++)
            {
                // Wrap indices for a perfectly smooth 360 circle tangent
                int prev = (i == 0) ? count - 2 : i - 1;
                int next = (i == count - 1) ? 1 : i + 1;
                
                Vector2 tangent = (pts[next] - pts[prev]).normalized;
                Vector2 normal = new Vector2(-tangent.y, tangent.x); // Normal pointing outward
                
                // If it's the inner hole ring, the shape's body is the opposite direction
                if (!isOuterRing) normal = -normal;

                innerEdge[i] = pts[i] - normal * innerThick;
                outerEdge[i] = pts[i] + normal * outerThick;

                if (i < count - 1)
                {
                    distances[i] = Vector2.Distance(pts[i], pts[i + 1]);
                    totalDist += distances[i];
                }
            }

            float currentDist = 0f;
            for (int i = 0; i < count - 1; i++)
            {
                float u0 = currentDist / totalDist;
                float u1 = (currentDist + distances[i]) / totalDist;

                AddRibbonQuad(vh, innerEdge[i], outerEdge[i], outerEdge[i + 1], innerEdge[i + 1], u0, u1);

                currentDist += distances[i];
            }
        }

        private void AddRibbonQuad(VertexHelper vh, Vector2 innerLeft, Vector2 outerLeft, Vector2 outerRight, Vector2 innerRight, float uLeft, float uRight)
        {
            int i = vh.currentVertCount;
            UIVertex v = UIVertex.simpleVert;
            v.color = color;

            v.position = innerLeft;  v.uv0 = new Vector2(uLeft, 0f); vh.AddVert(v); 
            v.position = outerLeft;  v.uv0 = new Vector2(uLeft, 1f); vh.AddVert(v); 
            v.position = outerRight; v.uv0 = new Vector2(uRight, 1f); vh.AddVert(v); 
            v.position = innerRight; v.uv0 = new Vector2(uRight, 0f); vh.AddVert(v); 

            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
