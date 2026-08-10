using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Graphs
{
    [RequireComponent(typeof(CanvasRenderer))]
    [ExecuteAlways]
    public class GraphGridGraphic : MaskableGraphic
    {
        [Header("Grid Lines")]
        public int horizontalLines = 6;
        public int verticalLines = 5;
        public float innerThickness = 1.5f;
        
        [Header("Outer Edges")]
        public float edgeThickness = 4f;
        public bool drawTopEdge = true;
        public bool drawBottomEdge = true;
        public bool drawLeftEdge = true;
        public bool drawRightEdge = true;

        [Header("Notches")]
        public float notchLength = 8f;
        public float notchThickness = 2f;
        public bool showLeftNotches = true;
        public bool showRightNotches = true;
        public bool showBottomNotches = true;

        #if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            horizontalLines = Mathf.Max(2, horizontalLines);
            verticalLines = Mathf.Max(2, verticalLines);
            innerThickness = Mathf.Max(0.1f, innerThickness);
            SetVerticesDirty();
        }
        #endif

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform.rect;
            float width = rect.width;
            float height = rect.height;

            // 1. Draw Horizontal Lines & Y-Axis Notches
            for (int i = 0; i < horizontalLines; i++)
            {
                float t = (float)i / (horizontalLines - 1);
                float yPos = rect.yMin + (t * height);
                
                // CHANGED HERE: Only the bottom line (i == 0) gets the thick edge. 
                // The top line (i == horizontalLines - 1) gets the inner thickness.
                bool isBottomEdge = (i == 0);
                float currentThick = isBottomEdge ? edgeThickness : innerThickness;

                // Skip drawing edge lines if toggled off
                bool shouldDraw = true;
                if (i == 0 && !drawBottomEdge) shouldDraw = false;
                if (i == horizontalLines - 1 && !drawTopEdge) shouldDraw = false;

                if (shouldDraw)
                {
                    DrawAxisAlignedLine(vh, new Vector2(rect.xMin, yPos), new Vector2(rect.xMax, yPos), true, currentThick);
                }

                // Draw Notches (Only on the exact grid lines, including edges)
                if (showLeftNotches)
                    DrawAxisAlignedLine(vh, new Vector2(rect.xMin - notchLength, yPos), new Vector2(rect.xMin, yPos), true, notchThickness);
                
                if (showRightNotches)
                    DrawAxisAlignedLine(vh, new Vector2(rect.xMax, yPos), new Vector2(rect.xMax + notchLength, yPos), true, notchThickness);
            }

            // 2. Draw Vertical Lines & X-Axis Notches
            for (int i = 0; i < verticalLines; i++)
            {
                float t = (float)i / (verticalLines - 1);
                float xPos = rect.xMin + (t * width);

                // Vertical lines keep the thick edges on both left (i==0) and right (i==verticalLines-1)
                bool isEdge = (i == 0 || i == verticalLines - 1);
                float currentThick = isEdge ? edgeThickness : innerThickness;

                bool shouldDraw = true;
                if (i == 0 && !drawLeftEdge) shouldDraw = false;
                if (i == verticalLines - 1 && !drawRightEdge) shouldDraw = false;

                if (shouldDraw)
                {
                    DrawAxisAlignedLine(vh, new Vector2(xPos, rect.yMin), new Vector2(xPos, rect.yMax), false, currentThick);
                }

                if (showBottomNotches)
                    DrawAxisAlignedLine(vh, new Vector2(xPos, rect.yMin - notchLength), new Vector2(xPos, rect.yMin), false, notchThickness);
            }
        }

        private void DrawAxisAlignedLine(VertexHelper vh, Vector2 start, Vector2 end, bool isHorizontal, float thickness)
        {
            UIVertex[] verts = new UIVertex[4];
            for (int i = 0; i < 4; i++)
            {
                verts[i] = UIVertex.simpleVert;
                verts[i].color = color; 
            }

            float halfThick = thickness / 2f;

            if (isHorizontal)
            {
                verts[0].position = new Vector3(start.x, start.y - halfThick);
                verts[1].position = new Vector3(start.x, start.y + halfThick);
                verts[2].position = new Vector3(end.x, end.y + halfThick);
                verts[3].position = new Vector3(end.x, end.y - halfThick);
            }
            else 
            {
                verts[0].position = new Vector3(start.x - halfThick, start.y);
                verts[1].position = new Vector3(start.x - halfThick, end.y);
                verts[2].position = new Vector3(start.x + halfThick, end.y);
                verts[3].position = new Vector3(start.x + halfThick, start.y);
            }

            vh.AddUIVertexQuad(verts);
        }
    }
}
