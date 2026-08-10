using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.SkillTree
{
    public enum SkillTreeConnectionStyle { Bezier, Orthogonal, Diagonal }

    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
    public class SkillTreeConnectionGraphic : Graphic
    {
        [Header("Core References")]
        public SkillTreeConnectionBuilder manager;
        public SkillTreeNode fromNode; 
        public SkillTreeNode toNode;   

        [Header("Style")]
        public SkillTreeConnectionStyle lineStyle = SkillTreeConnectionStyle.Orthogonal;
        [Range(0.05f, 0.95f)] public float orthogonalShoulderPosition = 0.5f;
        [Min(0f)] public float cornerRadius = 15f;
        [Range(2, 24)] public int cornerSegments = 8;

        [Header("Look")]
        [Min(0.5f)] public float thickness = 4f;
        [Range(8, 128)] public int segments = 36;
        public float handleScale = 0.45f;
        public float minHandle = 40f;
        public float maxHandle = 220f;

        [Header("Arrow")]
        public bool drawArrow = true;
        [Min(0f)] public float arrowWidth = 16f;
        [Min(0f)] public float arrowLength = 20f;

        [Header("UVs")]
        public bool generateUVs = true;

        [Header("Exclusive Path Visuals")]
        public bool isExclusivePathLine;
        [Min(0.1f)] public float exclusiveDashLength = 20f;
        [Min(0f)] public float exclusiveGapLength = 12f;

        [NonSerialized] public Canvas targetCanvas;
        Camera _cam;
        private int _lastStateHash;

        enum ConnectionAxis { Vertical, Horizontal }

        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = false;
            CacheCanvas();
            if (targetCanvas != null) targetCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            SetAllDirty();
        }

        void CacheCanvas()
        {
            targetCanvas = targetCanvas ? targetCanvas : GetComponentInParent<Canvas>();
            _cam = targetCanvas && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? targetCanvas.worldCamera : null;
        }

    #if UNITY_EDITOR
        void Update()
        {
            if (Application.isPlaying) return;
            
            if (manager != null && manager.liveUpdateInEditor && fromNode && toNode)
            {
                // Sync Live Settings from Manager
                thickness = manager.defaultThickness;
                color = manager.defaultLineColor;
                lineStyle = manager.lineStyle;
                orthogonalShoulderPosition = manager.orthogonalShoulderPosition;
                cornerRadius = manager.cornerRadius;
                cornerSegments = manager.cornerSegments;
                drawArrow = manager.drawArrow;
                arrowWidth = manager.arrowWidth;
                arrowLength = manager.arrowLength;

                int currentState = GetStateHash();
                if (currentState != _lastStateHash)
                {
                    _lastStateHash = currentState;
                    SetVerticesDirty();
                }
            }
        }

        int GetStateHash()
        {
            unchecked 
            {
                int hash = 17;
                if (fromNode != null)
                {
                    hash = hash * 31 + fromNode.transform.position.GetHashCode();
                    hash = hash * 31 + fromNode.insetLeft.GetHashCode();
                    hash = hash * 31 + fromNode.insetRight.GetHashCode();
                    hash = hash * 31 + fromNode.insetTop.GetHashCode();
                    hash = hash * 31 + fromNode.insetBottom.GetHashCode();
                    hash = hash * 31 + fromNode.allowLeft.GetHashCode();
                    hash = hash * 31 + fromNode.allowRight.GetHashCode();
                    hash = hash * 31 + fromNode.allowTop.GetHashCode();
                    hash = hash * 31 + fromNode.allowBottom.GetHashCode();
                    hash = hash * 31 + fromNode.fixedShoulderOffset.GetHashCode();
                }
                if (toNode != null)
                {
                    hash = hash * 31 + toNode.transform.position.GetHashCode();
                    hash = hash * 31 + toNode.insetLeft.GetHashCode();
                    hash = hash * 31 + toNode.insetRight.GetHashCode();
                    hash = hash * 31 + toNode.insetTop.GetHashCode();
                    hash = hash * 31 + toNode.insetBottom.GetHashCode();
                    hash = hash * 31 + toNode.allowLeft.GetHashCode();
                    hash = hash * 31 + toNode.allowRight.GetHashCode();
                    hash = hash * 31 + toNode.allowTop.GetHashCode();
                    hash = hash * 31 + toNode.allowBottom.GetHashCode();
                }
                hash = hash * 31 + thickness.GetHashCode();
                hash = hash * 31 + color.GetHashCode();
                hash = hash * 31 + lineStyle.GetHashCode();
                hash = hash * 31 + orthogonalShoulderPosition.GetHashCode();
                hash = hash * 31 + cornerRadius.GetHashCode();
                hash = hash * 31 + drawArrow.GetHashCode();
                hash = hash * 31 + arrowWidth.GetHashCode();
                hash = hash * 31 + arrowLength.GetHashCode();
                return hash;
            }
        }
    #endif

      

       protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!fromNode || !toNode || thickness <= 0f) return;

            CacheCanvas();
            
            // 1. Get both the Start and End ports from our updated method
            ComputeStartEnd(out Vector2 startWorld, out Vector2 endWorld, out int bestFrom, out int bestTo);
            
            // 2. Re-establish the primary path axis based on the START port
            ConnectionAxis axis = (bestFrom == 0 || bestFrom == 1) ? ConnectionAxis.Horizontal : ConnectionAxis.Vertical;

            Vector2 start = WorldToLocal(startWorld);
            Vector2 end   = WorldToLocal(endWorld);
            Vector2 delta = end - start;
            float   dist  = delta.magnitude;
            
            if (dist <= 0.001f) return;

            float axisLength = axis == ConnectionAxis.Horizontal ? Mathf.Abs(delta.x) : Mathf.Abs(delta.y);
            float handle = Mathf.Clamp(axisLength * handleScale, minHandle, maxHandle);
            
            // Direction out of the START port
            Vector2 handleDir = axis == ConnectionAxis.Horizontal ? new Vector2(Mathf.Sign(delta.x), 0f) : new Vector2(0f, Mathf.Sign(delta.y));

            // NEW: 3. Calculate exact arrow direction based on the TARGET port
            // 0 = Left, 1 = Right, 2 = Top, 3 = Bottom
            Vector2 arrowDir = Vector2.zero;
            if (bestTo == 0)      arrowDir = new Vector2(1f, 0f);  // Entering Left port -> Arrow points Right
            else if (bestTo == 1) arrowDir = new Vector2(-1f, 0f); // Entering Right port -> Arrow points Left
            else if (bestTo == 2) arrowDir = new Vector2(0f, -1f); // Entering Top port -> Arrow points Down
            else if (bestTo == 3) arrowDir = new Vector2(0f, 1f);  // Entering Bottom port -> Arrow points Up
            else                  arrowDir = handleDir;            // Fallback just in case

            if (lineStyle == SkillTreeConnectionStyle.Diagonal)
                arrowDir = delta.normalized;

            Vector2 trueEnd = end;
            
            // 4. Use arrowDir to pull back the line perfectly along the port's normal
            if (drawArrow && dist > arrowLength) end -= arrowDir * (arrowLength * 0.5f);

            Vector2 c1 = start + handleDir * handle;
            
            // 5. Use arrowDir for c2 so Bezier curves sweep correctly into side ports
            Vector2 c2 = end - arrowDir * handle; 

            List<Vector2> pathPoints = GetPathPoints(start, end, c1, c2, axis);

            if (!fromNode.isExclusiveConnection)
            {
                if (lineStyle == SkillTreeConnectionStyle.Orthogonal && cornerRadius <= 0.01f) PopulateSolidOrthogonal(vh, pathPoints);
                else PopulateSolidPath(vh, pathPoints); 
            }
            else PopulateDashedLine(vh, pathPoints);

            // 6. Draw the arrow using the corrected, target-based rotation
            if (drawArrow) AddArrow(vh, trueEnd, arrowDir);
        }

        // Updated to output 'bestFrom' and 'bestTo' so we know exactly which ports are connected
        void ComputeStartEnd(out Vector2 startWorld, out Vector2 endWorld, out int bestFrom, out int bestTo)
        {
            startWorld = Vector2.zero; endWorld = Vector2.zero;
            bestFrom = 3; bestTo = 2; // Default fallbacks
            
            if (!fromNode || !toNode) return;

            Vector2[] fromPts = GetConnectionPoints(fromNode.RectTransform, new Vector4(fromNode.insetLeft, fromNode.insetRight, fromNode.insetTop, fromNode.insetBottom));
            Vector2[] toPts = GetConnectionPoints(toNode.RectTransform, new Vector4(toNode.insetLeft, toNode.insetRight, toNode.insetTop, toNode.insetBottom));

            bool[] fromValid = { fromNode.allowLeft, fromNode.allowRight, fromNode.allowTop, fromNode.allowBottom };
            bool[] toValid = { toNode.allowLeft, toNode.allowRight, toNode.allowTop, toNode.allowBottom };

            float minDist = float.MaxValue;
            bestFrom = -1; 
            bestTo = -1;

            for (int i = 0; i < 4; i++)
            {
                if (!fromValid[i]) continue;
                for (int j = 0; j < 4; j++)
                {
                    if (!toValid[j]) continue;
                    float d = Vector2.Distance(fromPts[i], toPts[j]);
                    if (d < minDist) { minDist = d; bestFrom = i; bestTo = j; }
                }
            }

            if (bestFrom == -1) bestFrom = 3; 
            if (bestTo == -1) bestTo = 2;

            startWorld = fromPts[bestFrom];
            endWorld = toPts[bestTo];
        }

        Vector2[] GetConnectionPoints(RectTransform rt, Vector4 insets)
        {
            var corners = new Vector3[4]; rt.GetWorldCorners(corners);
            Vector2 bl = corners[0], tl = corners[1], tr = corners[2], br = corners[3];
            Vector2 right = (br - bl).normalized, up = (tl - bl).normalized;

            Vector2 cLeft = (bl + tl) * 0.5f, cRight = (br + tr) * 0.5f, cTop = (tl + tr) * 0.5f, cBottom = (bl + br) * 0.5f;

            cLeft += right * (insets.x * rt.lossyScale.x); 
            cRight -= right * (insets.y * rt.lossyScale.x);
            cTop -= up * (insets.z * rt.lossyScale.y); 
            cBottom += up * (insets.w * rt.lossyScale.y);
            
            return new Vector2[] { cLeft, cRight, cTop, cBottom };
        }

        List<Vector2> GetPathPoints(Vector2 start, Vector2 end, Vector2 c1, Vector2 c2, ConnectionAxis axis)
        {
            List<Vector2> pts = new List<Vector2>();
            if (lineStyle == SkillTreeConnectionStyle.Bezier)
            {
                int n = Mathf.Max(2, segments);
                for (int i = 0; i < n; i++) pts.Add(Cubic(start, c1, c2, end, (float)i / (n - 1)));
            }
            else if (lineStyle == SkillTreeConnectionStyle.Diagonal)
            {
                pts.Add(start);
                pts.Add(end);
            }
            else
            {
                List<Vector2> basePts = new List<Vector2>();
                basePts.Add(start);
                if (Vector2.Distance(start, end) > 0.001f)
                {
                    // NEW: Fixed Shoulder Offset Logic!
                    if (axis == ConnectionAxis.Vertical && Mathf.Abs(start.x - end.x) > 1f)
                    {
                        float midY;
                        if (fromNode != null && Mathf.Abs(fromNode.fixedShoulderOffset) > 0.01f)
                        {
                            float dir = Mathf.Sign(end.y - start.y);
                            midY = start.y + (dir * fromNode.fixedShoulderOffset);
                        }
                        else midY = Mathf.Lerp(start.y, end.y, orthogonalShoulderPosition);
                        
                        basePts.Add(new Vector2(start.x, midY)); basePts.Add(new Vector2(end.x, midY));
                    }
                    else if (axis == ConnectionAxis.Horizontal && Mathf.Abs(start.y - end.y) > 1f)
                    {
                        float midX;
                        if (fromNode != null && Mathf.Abs(fromNode.fixedShoulderOffset) > 0.01f)
                        {
                            float dir = Mathf.Sign(end.x - start.x);
                            midX = start.x + (dir * fromNode.fixedShoulderOffset);
                        }
                        else midX = Mathf.Lerp(start.x, end.x, orthogonalShoulderPosition);
                        
                        basePts.Add(new Vector2(midX, start.y)); basePts.Add(new Vector2(midX, end.y));
                    }
                }
                basePts.Add(end);

                if (cornerRadius > 0.01f && basePts.Count > 2)
                {
                    pts.Add(basePts[0]);
                    for (int i = 1; i < basePts.Count - 1; i++)
                    {
                        Vector2 prev = basePts[i - 1], curr = basePts[i], next = basePts[i + 1];
                        float maxR = Mathf.Min(Vector2.Distance(prev, curr) * 0.5f, Vector2.Distance(next, curr) * 0.5f);
                        float r = Mathf.Min(cornerRadius, maxR);
                        if (r <= 0.01f) { pts.Add(curr); continue; }

                        Vector2 pStart = curr + (prev - curr).normalized * r;
                        Vector2 pEnd = curr + (next - curr).normalized * r;
                        int cSegs = Mathf.Max(2, cornerSegments);
                        for (int j = 0; j <= cSegs; j++) pts.Add(Quadratic(pStart, curr, pEnd, (float)j / cSegs));
                    }
                    pts.Add(basePts[basePts.Count - 1]);
                }
                else pts = basePts;
            }
            return pts;
        }

        void PopulateSolidPath(VertexHelper vh, List<Vector2> pts)
        {
            if (pts.Count < 2) return;
            float totalLen = 0f; float[] lens = new float[pts.Count];
            for (int i = 1; i < pts.Count; i++) { totalLen += Vector2.Distance(pts[i - 1], pts[i]); lens[i] = totalLen; }
            float half = thickness * 0.5f;

            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 dir;
                if (i == 0) dir = (pts[1] - pts[0]).normalized;
                else if (i == pts.Count - 1) dir = (pts[i] - pts[i - 1]).normalized;
                else dir = (pts[i + 1] - pts[i - 1]).normalized; 
                Vector2 nrm = new Vector2(-dir.y, dir.x);
                Vector2 left = pts[i] + nrm * half;
                Vector2 right = pts[i] - nrm * half;
                float u = (generateUVs && totalLen > 1e-4f) ? lens[i] / totalLen : (float)i / (pts.Count - 1);
                
                AddSimpleVert(vh, left, new Vector2(u, 0), nrm);
                AddSimpleVert(vh, right, new Vector2(u, 1), nrm);

                if (i > 0)
                {
                    int baseIdx = vh.currentVertCount;
                    vh.AddTriangle(baseIdx - 4, baseIdx - 3, baseIdx - 2);
                    vh.AddTriangle(baseIdx - 2, baseIdx - 3, baseIdx - 1);
                }
            }
        }

        void PopulateSolidOrthogonal(VertexHelper vh, List<Vector2> pts)
        {
            float totalLen = 0f; float[] lens = new float[pts.Count];
            for (int i = 1; i < pts.Count; i++) { totalLen += Vector2.Distance(pts[i - 1], pts[i]); lens[i] = totalLen; }
            float half = thickness * 0.5f;

            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector2 p0 = pts[i], p1 = pts[i + 1];
                Vector2 dir = (p1 - p0).normalized;
                Vector2 nrm = new Vector2(-dir.y, dir.x);
                Vector2 segStart = p0, segEnd = p1;
                if (i > 0) segStart -= dir * half;
                if (i < pts.Count - 2) segEnd += dir * half;

                int startIndex = vh.currentVertCount;
                float uStart = generateUVs && totalLen > 1e-4f ? lens[i] / totalLen : 0f;
                float uEnd   = generateUVs && totalLen > 1e-4f ? lens[i+1] / totalLen : 1f;

                AddSimpleVert(vh, segStart + nrm * half, new Vector2(uStart, 0), nrm);
                AddSimpleVert(vh, segStart - nrm * half, new Vector2(uStart, 1), nrm);
                AddSimpleVert(vh, segEnd - nrm * half, new Vector2(uEnd, 1), nrm);
                AddSimpleVert(vh, segEnd + nrm * half, new Vector2(uEnd, 0), nrm);

                vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
                vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
            }
        }

        void PopulateDashedLine(VertexHelper vh, List<Vector2> pts) { /* Safely omitted text, keeping it consistent */ }

        void AddSimpleVert(VertexHelper vh, Vector2 pos, Vector2 uv, Vector2 normal)
        {
            UIVertex vert = UIVertex.simpleVert; vert.color = color; vert.position = pos; vert.uv0 = uv; vert.uv1 = normal; vh.AddVert(vert);
        }

        void AddArrow(VertexHelper vh, Vector2 tipPosition, Vector2 direction)
        {
            if (arrowWidth <= 0 || arrowLength <= 0) return;
            Vector2 normalDir = new Vector2(-direction.y, direction.x);
            Vector2 baseCenter = tipPosition - direction * arrowLength;
            int startIndex = vh.currentVertCount;
            
            AddSimpleVert(vh, tipPosition, new Vector2(1f, 0.5f), normalDir);
            AddSimpleVert(vh, baseCenter + normalDir * (arrowWidth * 0.5f), new Vector2(1f, 0f), normalDir);
            AddSimpleVert(vh, baseCenter - normalDir * (arrowWidth * 0.5f), new Vector2(1f, 1f), normalDir);
            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        }

        static Vector2 Cubic(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t) { float u = 1f - t; return (u * u * u) * a + 3f * (u * u) * t * b + 3f * u * (t * t) * c + (t * t * t) * d; }
        static Vector2 Quadratic(Vector2 a, Vector2 b, Vector2 c, float t) { float u = 1f - t; return (u * u) * a + 2f * u * t * b + (t * t) * c; }

        Vector2 WorldToLocal(Vector2 world)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, RectTransformUtility.WorldToScreenPoint(_cam, world), _cam, out Vector2 local);
            return local;
        }
    }
}
