using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Graphs
{
    [RequireComponent(typeof(GraphGridGraphic))]
    public class GraphAxisLabels : MonoBehaviour
    {
        [Header("Dependencies")]
        public TMP_Text textPrefab;
        public RectTransform labelsContainer;

        [Header("X-Axis (Bottom)")]
        public float xMinVal = -200f;
        public float xMaxVal = 0f;
        public float xLabelPadding = 20f;

        [Header("Y-Axis (Left)")]
        public float yLeftMinVal = 0f;
        public float yLeftMaxVal = 30f;
        public float yLeftPadding = 25f;

        [Header("Y-Axis (Right)")]
        public float yRightMinVal = 0f;
        public float yRightMaxVal = 100f;
        public float yRightPadding = 25f;

        // The Pool. Hidden from Inspector to avoid clutter, but serialized so it survives Assembly Reloads
        [SerializeField, HideInInspector] 
        private List<TMP_Text> labelPool = new List<TMP_Text>();

        public void GenerateLabels()
        {
            GraphGridGraphic gridRenderer = GetComponent<GraphGridGraphic>();
            if (gridRenderer == null || textPrefab == null || labelsContainer == null)
            {
                Debug.LogWarning("Missing references on UGraphLabels. Please assign Prefab and Container.");
                return;
            }

            Rect rect = gridRenderer.rectTransform.rect;
            int activeLabelCount = 0; // Tracks how many labels we actually use this generation

            // 1. Generate Horizontal Labels (X-Axis / Bottom)
            if (gridRenderer.showBottomNotches)
            {
                for (int i = 0; i < gridRenderer.verticalLines; i++)
                {
                    float t = (float)i / (gridRenderer.verticalLines - 1);
                    float xPos = rect.xMin + (t * rect.width);
                    float val = Mathf.Lerp(xMinVal, xMaxVal, t);

                    Vector2 pos = new Vector2(xPos, rect.yMin - gridRenderer.notchLength - xLabelPadding);
                    
                    // Bottom Labels: Center aligned
                    SetupLabel(activeLabelCount, pos, Mathf.RoundToInt(val).ToString(), HorizontalAlignmentOptions.Center);
                    activeLabelCount++;
                }
            }

            // 2. Generate Vertical Labels (Y-Axis / Left & Right)
            for (int i = 0; i < gridRenderer.horizontalLines; i++)
            {
                float t = (float)i / (gridRenderer.horizontalLines - 1);
                float yPos = rect.yMin + (t * rect.height);

                // Left Labels
                if (gridRenderer.showLeftNotches)
                {
                    float leftVal = Mathf.Lerp(yLeftMinVal, yLeftMaxVal, t);
                    Vector2 leftPos = new Vector2(rect.xMin - gridRenderer.notchLength - yLeftPadding, yPos);
                    
                    // Left Labels: Right aligned
                    SetupLabel(activeLabelCount, leftPos, Mathf.RoundToInt(leftVal).ToString(), HorizontalAlignmentOptions.Right);
                    activeLabelCount++;
                }

                // Right Labels
                if (gridRenderer.showRightNotches)
                {
                    float rightVal = Mathf.Lerp(yRightMinVal, yRightMaxVal, t);
                    Vector2 rightPos = new Vector2(rect.xMax + gridRenderer.notchLength + yRightPadding, yPos);
                    
                    // Right Labels: Left aligned
                    SetupLabel(activeLabelCount, rightPos, Mathf.RoundToInt(rightVal).ToString(), HorizontalAlignmentOptions.Left);
                    activeLabelCount++;
                }
            }

            // 3. Deactivate any leftover labels in the pool that we didn't use
            for (int i = activeLabelCount; i < labelPool.Count; i++)
            {
                if (labelPool[i] != null)
                {
                    labelPool[i].gameObject.SetActive(false);
                }
            }
        }

        private void SetupLabel(int poolIndex, Vector2 localPosition, string textValue, HorizontalAlignmentOptions align)
        {
            TMP_Text lbl = GetOrCreateLabel(poolIndex);
            
            lbl.text = textValue;
            lbl.horizontalAlignment = align;
            lbl.verticalAlignment = VerticalAlignmentOptions.Middle;

            // Reset anchors and pivot to exact center for precise positioning
            lbl.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            lbl.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            lbl.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            lbl.rectTransform.localPosition = localPosition;
        }

        private TMP_Text GetOrCreateLabel(int index)
        {
            // If we need a label that exists in the pool list
            if (index < labelPool.Count)
            {
                // Null check in case the user manually deleted a child GameObject in the Editor
                if (labelPool[index] == null) 
                {
                    labelPool[index] = Instantiate(textPrefab, labelsContainer);
                }
                
                labelPool[index].gameObject.SetActive(true);
                return labelPool[index];
            }
            else
            {
                // We need a new label, pool is too small
                TMP_Text newLabel = Instantiate(textPrefab, labelsContainer);
                labelPool.Add(newLabel);
                return newLabel;
            }
        }
    }

}
