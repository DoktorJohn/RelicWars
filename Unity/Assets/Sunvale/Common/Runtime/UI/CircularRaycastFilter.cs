using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.Common.UI
{
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("Sunvale/Common/CircularRaycastFilter")]
    public class CircularRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        [Header("Circle Hit Area")]
        [Range(0.1f, 1.5f)]
        public float radiusMultiplier = 0.5f;

        [Range(-1f, 1f)]
        public float centerOffsetX = 0f;

        [Range(-1f, 1f)]
        public float centerOffsetY = 0f;

        public bool invert = false;

        public RectTransform rectTransform;

    #if UNITY_EDITOR
        [Header("Scene Gizmo")]
        public bool showCircleGizmo = true;

        [Range(12, 128)]
        public int gizmoSegments = 64;

        public Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.95f);
    #endif

        private void Reset()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void OnValidate()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform,
                    screenPoint,
                    eventCamera,
                    out Vector2 localPoint))
            {
                return false;
            }

            GetCircleData(rectTransform, out Vector2 center, out float radius);

            bool insideCircle = Vector2.Distance(localPoint, center) <= radius;

            return invert ? !insideCircle : insideCircle;
        }

        private void GetCircleData(RectTransform target, out Vector2 center, out float radius)
        {
            Rect rect = target.rect;

            center = rect.center;
            center.x += rect.width * 0.5f * centerOffsetX;
            center.y += rect.height * 0.5f * centerOffsetY;

            radius = Mathf.Min(rect.width, rect.height) * radiusMultiplier;
        }

    #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!showCircleGizmo)
                return;

            RectTransform target = rectTransform != null
                ? rectTransform
                : GetComponent<RectTransform>();

            if (target == null)
                return;

            GetCircleData(target, out Vector2 center, out float radius);

            UnityEditor.Handles.color = gizmoColor;

            Vector3[] circlePoints = new Vector3[gizmoSegments + 1];

            for (int i = 0; i <= gizmoSegments; i++)
            {
                float t = i / (float)gizmoSegments;
                float angle = t * Mathf.PI * 2f;

                Vector2 localPoint = center + new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                ) * radius;

                circlePoints[i] = target.TransformPoint(localPoint);
            }

            UnityEditor.Handles.DrawAAPolyLine(3f, circlePoints);

            // Small center cross.
            float crossSize = radius * 0.08f;

            Vector3 left = target.TransformPoint(center + Vector2.left * crossSize);
            Vector3 right = target.TransformPoint(center + Vector2.right * crossSize);
            Vector3 down = target.TransformPoint(center + Vector2.down * crossSize);
            Vector3 up = target.TransformPoint(center + Vector2.up * crossSize);

            UnityEditor.Handles.DrawAAPolyLine(2f, left, right);
            UnityEditor.Handles.DrawAAPolyLine(2f, down, up);
        }
    #endif
    }

}
