using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.RPGTopDown
{
    [DisallowMultipleComponent]
    public class SkillTargetingArrowGraphic : MaskableGraphic, IPointerClickHandler
    {
        [Header("Arrow")]
        public Color arrowColor = new Color(1f, 0.85f, 0.25f, 0.9f);
        public Color shadowColor = new Color(0f, 0f, 0f, 0.35f);

        [Min(1f)] public float bodyWidth = 12f;
        [Min(1f)] public float arrowHeadLength = 48f;
        [Min(1f)] public float arrowHeadWidth = 42f;
        [Min(0f)] public float shadowOffset = 4f;

        [Header("Cursor Marker")]
        [Min(1f)] public float markerRadius = 42f;
        [Min(1f)] public float markerArrowLength = 22f;
        [Min(1f)] public float markerArrowWidth = 18f;
        public float markerRotationSpeed = 90f;

        [Header("Click Pulse")]
        public Color clickPulseColor = new Color(1f, 0.9f, 0.35f, 0.85f);
        [Min(0.01f)] public float clickPulseDuration = 0.25f;
        [Min(1f)] public float clickPulseStartRadius = 30f;
        [Min(1f)] public float clickPulseEndRadius = 95f;
        [Min(1f)] public float clickPulseRingWidth = 5f;

        public event Action<PointerEventData> OnTargetClicked;

        private RectTransform cachedRectTransform;

        private bool aimingVisible;
        private Vector2 localStart;
        private Vector2 localEnd;

        private bool clickPulseActive;
        private float clickPulseTime;
        private Vector2 clickPulseLocalPosition;
        
        [Header("Debug")]
        public bool forceFullCanvasRect = true;
        public bool drawDebugArrowWhenNotAiming = false;

        private RectTransform MyRectTransform
        {
            get
            {
                if (cachedRectTransform == null)
                    cachedRectTransform = transform as RectTransform;

                return cachedRectTransform;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            raycastTarget = false;
        }
        
        
        protected override void OnEnable()
        {
            base.OnEnable();

            color = Color.white;

            if (forceFullCanvasRect)
                ForceFullCanvasRect();

            SetAllDirty();
        }

        private void ForceFullCanvasRect()
        {
            RectTransform rt = transform as RectTransform;

            if (rt == null)
                return;

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

    #if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            bodyWidth = Mathf.Max(1f, bodyWidth);
            arrowHeadLength = Mathf.Max(1f, arrowHeadLength);
            arrowHeadWidth = Mathf.Max(1f, arrowHeadWidth);
            markerRadius = Mathf.Max(1f, markerRadius);
            markerArrowLength = Mathf.Max(1f, markerArrowLength);
            markerArrowWidth = Mathf.Max(1f, markerArrowWidth);
            clickPulseDuration = Mathf.Max(0.01f, clickPulseDuration);

            SetVerticesDirty();
        }
    #endif

        private void Update()
        {
            bool shouldDirty = aimingVisible;

            if (clickPulseActive)
            {
                clickPulseTime += Time.unscaledDeltaTime;

                if (clickPulseTime >= clickPulseDuration)
                {
                    clickPulseActive = false;
                    clickPulseTime = clickPulseDuration;
                }

                shouldDirty = true;
            }

            if (shouldDirty)
                SetVerticesDirty();
        }

        public void SetAimingVisible(bool visible)
        {
            aimingVisible = visible;

            // Very important:
            // When aiming, this catches the next click.
            // When not aiming, it does not block the hotbar.
            raycastTarget = visible;

            SetVerticesDirty();
        }

        public void SetScreenPoints(Canvas canvas, Vector2 startScreen, Vector2 endScreen)
        {
            if (forceFullCanvasRect)
                ForceFullCanvasRect();

            color = Color.white;

            Camera eventCamera = GetCanvasCamera(canvas);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                MyRectTransform,
                startScreen,
                eventCamera,
                out localStart
            );

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                MyRectTransform,
                endScreen,
                eventCamera,
                out localEnd
            );

            aimingVisible = true;
            raycastTarget = true;
            
            

            SetAllDirty();
        }

        public void PlayClickPulse(Canvas canvas, Vector2 screenPosition)
        {
            Camera eventCamera = GetCanvasCamera(canvas);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                MyRectTransform,
                screenPosition,
                eventCamera,
                out clickPulseLocalPosition
            );

            clickPulseActive = true;
            clickPulseTime = 0f;

            SetVerticesDirty();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!aimingVisible)
                return;

            OnTargetClicked?.Invoke(eventData);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (drawDebugArrowWhenNotAiming && !aimingVisible)
            {
                Rect rect = MyRectTransform.rect;

                localStart = new Vector2(rect.xMin + rect.width * 0.25f, 0f);
                localEnd = new Vector2(rect.xMax - rect.width * 0.25f, 0f);

                DrawArrow(vh);
                DrawCursorMarker(vh, localEnd, markerRadius, arrowColor);
            }

            if (aimingVisible)
            {
                DrawArrow(vh);
                DrawCursorMarker(vh, localEnd, markerRadius, arrowColor);
            }

            if (clickPulseActive)
            {
                DrawClickPulse(vh);
            }
        }

        private void DrawArrow(VertexHelper vh)
        {
            Vector2 delta = localEnd - localStart;
            float length = delta.magnitude;

            if (length < 5f)
                return;

            Vector2 dir = delta / length;
            Vector2 normal = new Vector2(-dir.y, dir.x);

            float actualHeadLength = Mathf.Min(arrowHeadLength, length * 0.45f);
            Vector2 headBase = localEnd - dir * actualHeadLength;

            Vector2 shadow = new Vector2(shadowOffset, -shadowOffset);

            DrawArrowShape(
                vh,
                localStart + shadow,
                headBase + shadow,
                localEnd + shadow,
                dir,
                normal,
                bodyWidth,
                arrowHeadWidth,
                shadowColor
            );

            DrawArrowShape(
                vh,
                localStart,
                headBase,
                localEnd,
                dir,
                normal,
                bodyWidth,
                arrowHeadWidth,
                arrowColor
            );
        }

        private void DrawArrowShape(
            VertexHelper vh,
            Vector2 start,
            Vector2 headBase,
            Vector2 tip,
            Vector2 dir,
            Vector2 normal,
            float width,
            float headWidth,
            Color color
        )
        {
            float bodyHalf = width * 0.5f;
            float headHalf = headWidth * 0.5f;

            Vector2 bodyStartLeft = start - normal * bodyHalf;
            Vector2 bodyStartRight = start + normal * bodyHalf;
            Vector2 bodyEndRight = headBase + normal * bodyHalf;
            Vector2 bodyEndLeft = headBase - normal * bodyHalf;

            AddQuad(vh, bodyStartLeft, bodyStartRight, bodyEndRight, bodyEndLeft, color);

            Vector2 headLeft = headBase + normal * headHalf;
            Vector2 headRight = headBase - normal * headHalf;

            AddTriangle(vh, tip, headLeft, headRight, color);

            // Small tail cap so the arrow feels less like a hard rectangle.
            Vector2 tail = start - dir * width * 0.5f;
            AddTriangle(vh, tail, bodyStartRight, bodyStartLeft, color);
        }

        private void DrawCursorMarker(VertexHelper vh, Vector2 center, float radius, Color color)
        {
            float timeAngle = Time.unscaledTime * markerRotationSpeed;

            for (int i = 0; i < 4; i++)
            {
                float angle = timeAngle + i * 90f;
                float rad = angle * Mathf.Deg2Rad;

                Vector2 outward = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                Vector2 tangent = new Vector2(-outward.y, outward.x);

                Vector2 tip = center + outward * (radius - markerArrowLength);
                Vector2 baseCenter = center + outward * radius;

                Vector2 left = baseCenter + tangent * (markerArrowWidth * 0.5f);
                Vector2 right = baseCenter - tangent * (markerArrowWidth * 0.5f);

                AddTriangle(vh, tip, left, right, color);
            }
        }

        private void DrawClickPulse(VertexHelper vh)
        {
            float t = Mathf.Clamp01(clickPulseTime / clickPulseDuration);
            float radius = Mathf.Lerp(clickPulseStartRadius, clickPulseEndRadius, t);

            Color pulseColor = clickPulseColor;
            pulseColor.a *= 1f - t;

            DrawRing(vh, clickPulseLocalPosition, radius, clickPulseRingWidth, 32, pulseColor);
            DrawCursorMarker(vh, clickPulseLocalPosition, radius * 0.7f, pulseColor);
        }

        private void DrawRing(VertexHelper vh, Vector2 center, float radius, float width, int segments, Color color)
        {
            float inner = Mathf.Max(0f, radius - width * 0.5f);
            float outer = radius + width * 0.5f;

            for (int i = 0; i < segments; i++)
            {
                float a0 = (float)i / segments * Mathf.PI * 2f;
                float a1 = (float)(i + 1) / segments * Mathf.PI * 2f;

                Vector2 d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
                Vector2 d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));

                Vector2 p0 = center + d0 * inner;
                Vector2 p1 = center + d1 * inner;
                Vector2 p2 = center + d1 * outer;
                Vector2 p3 = center + d0 * outer;

                AddQuad(vh, p0, p1, p2, p3, color);
            }
        }

        private void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int startIndex = vh.currentVertCount;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = a;
            vh.AddVert(vertex);

            vertex.position = b;
            vh.AddVert(vertex);

            vertex.position = c;
            vh.AddVert(vertex);

            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        }

        private void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            int startIndex = vh.currentVertCount;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = a;
            vh.AddVert(vertex);

            vertex.position = b;
            vh.AddVert(vertex);

            vertex.position = c;
            vh.AddVert(vertex);

            vertex.position = d;
            vh.AddVert(vertex);

            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
        }

        private Camera GetCanvasCamera(Canvas canvas)
        {
            if (canvas == null)
                return null;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera;
        }
    }
}
