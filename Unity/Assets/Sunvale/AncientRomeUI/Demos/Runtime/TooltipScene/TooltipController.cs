using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.TooltipScene
{
    public enum TooltipWindowId
    {
        nullNothing,
        characterConsul,
        skillTooltip,
        itemTooltip,
        stoneTooltip,
        metalTooltip,
        populationTooltip,
        exoticResourcesTooltip,
        goldCoinsTooltip,
        researchTooltip,
        ideologyTooltip
    }

    [DisallowMultipleComponent]
    public sealed class TooltipController : MonoBehaviour
    {
        public static TooltipController Instance { get; private set; }

        [Serializable]
        public sealed class TooltipWindowMapping
        {
            public TooltipWindowId tooltipPrefab = TooltipWindowId.nullNothing;
            public TooltipWindow window;
        }

        [Header("Canvas")]
        [SerializeField] private Canvas tooltipCanvas;
        [SerializeField] private RectTransform tooltipCanvasRect;
        [SerializeField] private bool forceTopCanvasOrder = true;
        [SerializeField] private int forcedSortingOrder = 30000;

        [Header("Tooltip Windows")]
        [SerializeField] private List<TooltipWindowMapping> tooltipWindows = new List<TooltipWindowMapping>();

        [Header("Placement")]
        [SerializeField] private float screenEdgePadding = 12f;
        [SerializeField] private bool repositionEveryFrame = true;

        [Header("Closing")]
        [SerializeField] private float closeGraceTime = 0.12f;

        private TooltipWindow activeWindow;
        private TooltipTrigger activeSpawner;
        private TooltipWindowId activeTooltipPrefab = TooltipWindowId.nullNothing;

        private float earliestCloseTime;

        private readonly Vector3[] sourceWorldCorners = new Vector3[4];

        private static readonly TooltipTrigger.TooltipPlacement[] AutoOrder =
        {
            TooltipTrigger.TooltipPlacement.Right,
            TooltipTrigger.TooltipPlacement.Left,
            TooltipTrigger.TooltipPlacement.Top,
            TooltipTrigger.TooltipPlacement.Bottom
        };

        private static readonly TooltipTrigger.TooltipPlacement[] RightOrder =
        {
            TooltipTrigger.TooltipPlacement.Right,
            TooltipTrigger.TooltipPlacement.Left,
            TooltipTrigger.TooltipPlacement.Top,
            TooltipTrigger.TooltipPlacement.Bottom
        };

        private static readonly TooltipTrigger.TooltipPlacement[] LeftOrder =
        {
            TooltipTrigger.TooltipPlacement.Left,
            TooltipTrigger.TooltipPlacement.Right,
            TooltipTrigger.TooltipPlacement.Top,
            TooltipTrigger.TooltipPlacement.Bottom
        };

        private static readonly TooltipTrigger.TooltipPlacement[] TopOrder =
        {
            TooltipTrigger.TooltipPlacement.Top,
            TooltipTrigger.TooltipPlacement.Bottom,
            TooltipTrigger.TooltipPlacement.Right,
            TooltipTrigger.TooltipPlacement.Left
        };

        private static readonly TooltipTrigger.TooltipPlacement[] BottomOrder =
        {
            TooltipTrigger.TooltipPlacement.Bottom,
            TooltipTrigger.TooltipPlacement.Top,
            TooltipTrigger.TooltipPlacement.Right,
            TooltipTrigger.TooltipPlacement.Left
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("There is already a TooltipController in the scene. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (tooltipCanvas == null)
                tooltipCanvas = GetComponentInParent<Canvas>();

            if (tooltipCanvasRect == null && tooltipCanvas != null)
                tooltipCanvasRect = tooltipCanvas.transform as RectTransform;

            if (forceTopCanvasOrder && tooltipCanvas != null)
            {
                tooltipCanvas.overrideSorting = true;
                tooltipCanvas.sortingOrder = forcedSortingOrder;
            }

            HideAllWindows();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (activeWindow == null)
                return;

            if (repositionEveryFrame && activeSpawner != null && activeSpawner.IsPointerInside)
                PositionActiveTooltip();

            if (activeWindow.WantsToStayOpen(activeSpawner))
            {
                earliestCloseTime = Time.unscaledTime + closeGraceTime;
                return;
            }

            if (Time.unscaledTime >= earliestCloseTime)
                HideActiveTooltip();
        }

        public bool IsShowingFor(TooltipTrigger spawner)
        {
            return spawner != null &&
                   activeSpawner == spawner &&
                   activeWindow != null &&
                   activeWindow.IsVisible;
        }

        public bool IsShowingFor(TooltipTrigger spawner, TooltipWindowId tooltipPrefab)
        {
            return spawner != null &&
                   activeSpawner == spawner &&
                   activeTooltipPrefab == tooltipPrefab &&
                   activeWindow != null &&
                   activeWindow.IsVisible;
        }

        public void ShowTooltip(TooltipTrigger spawner, TooltipWindowId tooltipPrefab)
        {
            if (spawner == null)
                return;

            if (spawner.TargetRectTransform == null)
                return;

            if (tooltipPrefab == TooltipWindowId.nullNothing)
            {
                Debug.LogWarning(
                    "TooltipTrigger requested TooltipWindowId.nullNothing. No tooltip will be shown. " +
                    "This usually means the enum was not assigned in the Inspector.",
                    spawner
                );

                HideActiveTooltip();
                return;
            }

            TooltipWindow window = GetTooltipWindow(tooltipPrefab);

            if (window == null)
            {
                Debug.LogWarning(
                    $"TooltipController could not find a TooltipWindow paired with TooltipWindowId.{tooltipPrefab}.",
                    this
                );

                HideActiveTooltip();
                return;
            }

            if (activeWindow != null && activeWindow != window)
                activeWindow.Hide();

            activeWindow = window;
            activeSpawner = spawner;
            activeTooltipPrefab = tooltipPrefab;
            earliestCloseTime = Time.unscaledTime + closeGraceTime;

            activeWindow.Show(spawner);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(activeWindow.RectTransform);
            Canvas.ForceUpdateCanvases();

            PositionActiveTooltip();
        }

        public void NotifySpawnerExit(TooltipTrigger spawner)
        {
            if (spawner != activeSpawner)
                return;

            earliestCloseTime = Time.unscaledTime + closeGraceTime;
        }

        public void NotifySpawnerDisabled(TooltipTrigger spawner)
        {
            if (spawner != activeSpawner)
                return;

            activeSpawner = null;
            earliestCloseTime = Time.unscaledTime + closeGraceTime;
        }

        public void HideActiveTooltip()
        {
            if (activeWindow != null)
                activeWindow.Hide();

            activeWindow = null;
            activeSpawner = null;
            activeTooltipPrefab = TooltipWindowId.nullNothing;
        }

        private void HideAllWindows()
        {
            for (int i = 0; i < tooltipWindows.Count; i++)
            {
                TooltipWindowMapping entry = tooltipWindows[i];

                if (entry != null && entry.window != null)
                    entry.window.Hide();
            }

            activeWindow = null;
            activeSpawner = null;
            activeTooltipPrefab = TooltipWindowId.nullNothing;
        }

        private TooltipWindow GetTooltipWindow(TooltipWindowId tooltipPrefab)
        {
            for (int i = 0; i < tooltipWindows.Count; i++)
            {
                TooltipWindowMapping entry = tooltipWindows[i];

                if (entry == null)
                    continue;

                if (entry.tooltipPrefab != tooltipPrefab)
                    continue;

                return entry.window;
            }

            return null;
        }

        private void PositionActiveTooltip()
        {
            if (activeWindow == null)
                return;

            if (activeSpawner == null)
                return;

            if (tooltipCanvasRect == null)
                return;

            RectTransform targetRectTransform = activeSpawner.TargetRectTransform;
            RectTransform tooltipRectTransform = activeWindow.RectTransform;

            if (targetRectTransform == null || tooltipRectTransform == null)
                return;

            tooltipRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            tooltipRectTransform.anchorMax = new Vector2(0.5f, 0.5f);

            Vector2 tooltipSize = GetTooltipSize(tooltipRectTransform);
            Rect sourceRect = GetSourceRectInTooltipCanvas(targetRectTransform);

            Vector2 position = ChooseBestPosition(
                sourceRect,
                tooltipSize,
                tooltipRectTransform.pivot,
                activeSpawner.Spacing,
                activeSpawner.PreferredPlacement
            );

            tooltipRectTransform.anchoredPosition = position;
        }

        private Vector2 GetTooltipSize(RectTransform tooltipRectTransform)
        {
            Vector2 size = tooltipRectTransform.rect.size;

            if (size.x <= 0.01f)
                size.x = LayoutUtility.GetPreferredWidth(tooltipRectTransform);

            if (size.y <= 0.01f)
                size.y = LayoutUtility.GetPreferredHeight(tooltipRectTransform);

            return size;
        }

        private Rect GetSourceRectInTooltipCanvas(RectTransform source)
        {
            source.GetWorldCorners(sourceWorldCorners);

            Canvas sourceCanvas = source.GetComponentInParent<Canvas>();
            Camera sourceCamera = GetCanvasCamera(sourceCanvas);
            Camera tooltipCamera = GetCanvasCamera(tooltipCanvas);

            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;

            for (int i = 0; i < sourceWorldCorners.Length; i++)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                    sourceCamera,
                    sourceWorldCorners[i]
                );

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    tooltipCanvasRect,
                    screenPoint,
                    tooltipCamera,
                    out Vector2 localPoint
                );

                minX = Mathf.Min(minX, localPoint.x);
                minY = Mathf.Min(minY, localPoint.y);
                maxX = Mathf.Max(maxX, localPoint.x);
                maxY = Mathf.Max(maxY, localPoint.y);
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private Vector2 ChooseBestPosition(
            Rect sourceRect,
            Vector2 tooltipSize,
            Vector2 tooltipPivot,
            float spacing,
            TooltipTrigger.TooltipPlacement preferredPlacement
        )
        {
            Rect canvasBounds = tooltipCanvasRect.rect;

            canvasBounds.xMin += screenEdgePadding;
            canvasBounds.xMax -= screenEdgePadding;
            canvasBounds.yMin += screenEdgePadding;
            canvasBounds.yMax -= screenEdgePadding;

            TooltipTrigger.TooltipPlacement[] order = GetPlacementOrder(preferredPlacement);

            Vector2 bestPosition = Vector2.zero;
            float bestOverflow = float.PositiveInfinity;

            for (int i = 0; i < order.Length; i++)
            {
                Vector2 candidatePosition = GetCandidatePosition(
                    sourceRect,
                    tooltipSize,
                    tooltipPivot,
                    spacing,
                    order[i]
                );

                Rect candidateBounds = GetTooltipBounds(candidatePosition, tooltipSize, tooltipPivot);

                if (FitsInside(candidateBounds, canvasBounds))
                    return ClampPivotPosition(candidatePosition, tooltipSize, tooltipPivot, canvasBounds);

                float overflow = GetOverflow(candidateBounds, canvasBounds);

                if (overflow < bestOverflow)
                {
                    bestOverflow = overflow;
                    bestPosition = candidatePosition;
                }
            }

            return ClampPivotPosition(bestPosition, tooltipSize, tooltipPivot, canvasBounds);
        }

        private static TooltipTrigger.TooltipPlacement[] GetPlacementOrder(
            TooltipTrigger.TooltipPlacement preferredPlacement
        )
        {
            switch (preferredPlacement)
            {
                case TooltipTrigger.TooltipPlacement.Right:
                    return RightOrder;

                case TooltipTrigger.TooltipPlacement.Left:
                    return LeftOrder;

                case TooltipTrigger.TooltipPlacement.Top:
                    return TopOrder;

                case TooltipTrigger.TooltipPlacement.Bottom:
                    return BottomOrder;

                default:
                    return AutoOrder;
            }
        }

        private static Vector2 GetCandidatePosition(
            Rect sourceRect,
            Vector2 tooltipSize,
            Vector2 tooltipPivot,
            float spacing,
            TooltipTrigger.TooltipPlacement placement
        )
        {
            Vector2 sourceCenter = sourceRect.center;
            Vector2 position = sourceCenter;

            switch (placement)
            {
                case TooltipTrigger.TooltipPlacement.Right:
                {
                    float leftEdge = sourceRect.xMax + spacing;
                    position.x = leftEdge + tooltipSize.x * tooltipPivot.x;
                    position.y = sourceCenter.y + tooltipSize.y * (tooltipPivot.y - 0.5f);
                    break;
                }

                case TooltipTrigger.TooltipPlacement.Left:
                {
                    float rightEdge = sourceRect.xMin - spacing;
                    position.x = rightEdge - tooltipSize.x * (1f - tooltipPivot.x);
                    position.y = sourceCenter.y + tooltipSize.y * (tooltipPivot.y - 0.5f);
                    break;
                }

                case TooltipTrigger.TooltipPlacement.Top:
                {
                    float bottomEdge = sourceRect.yMax + spacing;
                    position.x = sourceCenter.x + tooltipSize.x * (tooltipPivot.x - 0.5f);
                    position.y = bottomEdge + tooltipSize.y * tooltipPivot.y;
                    break;
                }

                case TooltipTrigger.TooltipPlacement.Bottom:
                {
                    float topEdge = sourceRect.yMin - spacing;
                    position.x = sourceCenter.x + tooltipSize.x * (tooltipPivot.x - 0.5f);
                    position.y = topEdge - tooltipSize.y * (1f - tooltipPivot.y);
                    break;
                }
            }

            return position;
        }

        private static Rect GetTooltipBounds(
            Vector2 pivotPosition,
            Vector2 size,
            Vector2 pivot
        )
        {
            float minX = pivotPosition.x - size.x * pivot.x;
            float minY = pivotPosition.y - size.y * pivot.y;

            return new Rect(minX, minY, size.x, size.y);
        }

        private static bool FitsInside(Rect tooltipBounds, Rect canvasBounds)
        {
            return tooltipBounds.xMin >= canvasBounds.xMin &&
                   tooltipBounds.xMax <= canvasBounds.xMax &&
                   tooltipBounds.yMin >= canvasBounds.yMin &&
                   tooltipBounds.yMax <= canvasBounds.yMax;
        }

        private static float GetOverflow(Rect tooltipBounds, Rect canvasBounds)
        {
            float overflow = 0f;

            overflow += Mathf.Max(0f, canvasBounds.xMin - tooltipBounds.xMin);
            overflow += Mathf.Max(0f, tooltipBounds.xMax - canvasBounds.xMax);
            overflow += Mathf.Max(0f, canvasBounds.yMin - tooltipBounds.yMin);
            overflow += Mathf.Max(0f, tooltipBounds.yMax - canvasBounds.yMax);

            return overflow;
        }

        private static Vector2 ClampPivotPosition(
            Vector2 position,
            Vector2 size,
            Vector2 pivot,
            Rect canvasBounds
        )
        {
            float minPivotX = canvasBounds.xMin + size.x * pivot.x;
            float maxPivotX = canvasBounds.xMax - size.x * (1f - pivot.x);

            float minPivotY = canvasBounds.yMin + size.y * pivot.y;
            float maxPivotY = canvasBounds.yMax - size.y * (1f - pivot.y);

            if (minPivotX > maxPivotX)
                position.x = canvasBounds.center.x + size.x * (pivot.x - 0.5f);
            else
                position.x = Mathf.Clamp(position.x, minPivotX, maxPivotX);

            if (minPivotY > maxPivotY)
                position.y = canvasBounds.center.y + size.y * (pivot.y - 0.5f);
            else
                position.y = Mathf.Clamp(position.y, minPivotY, maxPivotY);

            return position;
        }

        private static Camera GetCanvasCamera(Canvas canvas)
        {
            if (canvas == null)
                return null;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            if (canvas.worldCamera != null)
                return canvas.worldCamera;

            return Camera.main;
        }
    }
}
