using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.RPGTopDown
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
    public class InventoryPointerLineGraphic : Graphic
    {
        [Header("Path Nodes")]
        [Tooltip("Drag the RectTransforms here in order.")]
        public List<RectTransform> pathNodes = new List<RectTransform>();

        [Header("Look")]
        [Min(0.5f)] public float thickness = 3f;
        [Tooltip("Extra geometry width added to allow the shader to feather the edges. 1.5 to 2 is ideal.")]
        [Min(0f)] public float aaPadding = 1.5f; 
        public bool generateUVs = true;

        [NonSerialized] public Canvas targetCanvas;
        private Camera _cam;

        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = false;
            CacheCanvas();
            if (targetCanvas != null) 
                targetCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            SetAllDirty();
        }

        void CacheCanvas()
        {
            targetCanvas = targetCanvas ? targetCanvas : GetComponentInParent<Canvas>();
            _cam = targetCanvas && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? targetCanvas.worldCamera : null;
        }

    #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (pathNodes == null || pathNodes.Count < 2) return;

            bool shouldDraw = false;
            if (Selection.activeGameObject == gameObject) shouldDraw = true;
            else
            {
                foreach (var node in pathNodes)
                {
                    if (node != null && Selection.activeGameObject == node.gameObject)
                    {
                        shouldDraw = true;
                        break;
                    }
                }
            }

            if (!shouldDraw) return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < pathNodes.Count; i++)
            {
                if (pathNodes[i] == null) continue;
                Gizmos.DrawWireCube(pathNodes[i].position, Vector3.one * (thickness * 2f));
                if (i < pathNodes.Count - 1 && pathNodes[i + 1] != null)
                    Gizmos.DrawLine(pathNodes[i].position, pathNodes[i + 1].position);
            }
        }
    #endif

        public void CollectNodesFromChildren()
        {
            pathNodes.Clear();
            int nodeIndex = 0;

            foreach (Transform child in transform)
            {
                RectTransform rt = child.GetComponent<RectTransform>();
                if (rt != null)
                {
                    #if UNITY_EDITOR
                    Undo.RecordObject(child.gameObject, "Rename Node");
                    child.name = "Node_" + nodeIndex;
                    #endif
                    pathNodes.Add(rt);
                    nodeIndex++;
                }
            }
            BakeMesh();
        }

        public void BakeMesh() { SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (pathNodes == null || pathNodes.Count < 2 || thickness <= 0f) return;

            CacheCanvas();
            List<Vector2> localPoints = new List<Vector2>();
            foreach (var node in pathNodes)
            {
                if (node != null) localPoints.Add(WorldToLocal(node.position));
            }

            if (localPoints.Count < 2) return;
            GeneratePolyLine(vh, localPoints);
        }

        void GeneratePolyLine(VertexHelper vh, List<Vector2> pts)
        {
            float totalLen = 0f;
            float[] lens = new float[pts.Count];
            for (int i = 1; i < pts.Count; i++) 
            { 
                totalLen += Vector2.Distance(pts[i - 1], pts[i]); 
                lens[i] = totalLen; 
            }

            // 1. We physically expand the mesh by aaPadding
            float geoHalfThick = (thickness * 0.5f) + aaPadding;
            
            // 2. We calculate the UV ratio so 0 and 1 represent the true visual thickness
            float vPaddingRatio = thickness > 0 ? (aaPadding / thickness) : 0f;
            float vTopEdge = 0.0f - vPaddingRatio;
            float vBottomEdge = 1.0f + vPaddingRatio;

            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 forward = Vector2.zero;

                if (i == 0) forward = (pts[1] - pts[0]).normalized;
                else if (i == pts.Count - 1) forward = (pts[i] - pts[i - 1]).normalized;
                else 
                {
                    Vector2 dirIn = (pts[i] - pts[i - 1]).normalized;
                    Vector2 dirOut = (pts[i + 1] - pts[i]).normalized;
                    forward = (dirIn + dirOut).normalized;
                }

                Vector2 normal = new Vector2(-forward.y, forward.x);
                float miterCorrection = 1f;

                if (i > 0 && i < pts.Count - 1)
                {
                    Vector2 dirIn = (pts[i] - pts[i - 1]).normalized;
                    Vector2 trueNormal = new Vector2(-dirIn.y, dirIn.x);
                    float miterDot = Vector2.Dot(normal, trueNormal);
                    if (Mathf.Abs(miterDot) > 0.2f) miterCorrection = 1f / miterDot; 
                }

                // Expanded physical vertices
                Vector2 pUp = pts[i] + normal * (geoHalfThick * miterCorrection);
                Vector2 pDown = pts[i] - normal * (geoHalfThick * miterCorrection);

                float u = (generateUVs && totalLen > 1e-4f) ? lens[i] / totalLen : 0f;

                // Notice UVs now use the expanded ratios
                AddSimpleVert(vh, pUp, new Vector2(u, vTopEdge), normal); 
                AddSimpleVert(vh, pDown, new Vector2(u, vBottomEdge), normal);

                if (i > 0)
                {
                    int idx = vh.currentVertCount;
                    vh.AddTriangle(idx - 4, idx - 3, idx - 2);
                    vh.AddTriangle(idx - 2, idx - 3, idx - 1);
                }
            }
        }

        void AddSimpleVert(VertexHelper vh, Vector2 pos, Vector2 uv, Vector2 normal)
        {
            UIVertex vert = UIVertex.simpleVert; 
            vert.color = color; 
            vert.position = pos; 
            vert.uv0 = uv; 
            vert.uv1 = normal; 
            vh.AddVert(vert);
        }

        Vector2 WorldToLocal(Vector2 world)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, RectTransformUtility.WorldToScreenPoint(_cam, world), _cam, out Vector2 local);
            return local;
        }
    }

}
