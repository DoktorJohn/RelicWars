using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sunvale.AncientRomeUI.SkillTree
{
    [ExecuteAlways]
    public class SkillTreeConnectionBuilder : MonoBehaviour
    {
        [Header("Editor Tools")]
        [Tooltip("When ON, lines will instantly redraw as you move nodes, change settings, or add connections.")]
        public bool liveUpdateInEditor = false;

        [Header("Containers")]
        public RectTransform nodeContainer;
        public RectTransform lineContainer;

        [Header("Line Settings")]
        public SkillTreeConnectionStyle lineStyle = SkillTreeConnectionStyle.Orthogonal;
        
        [Header("Orthogonal Settings")]
        [Range(0.05f, 0.95f)] public float orthogonalShoulderPosition = 0.5f;
        public float cornerRadius = 25f;
        [Range(2, 24)] public int cornerSegments = 8;
        
        [Header("General Look")]
        public float defaultThickness = 4f;
        public Color defaultLineColor = Color.white;
        public Material lineMaterial;
        
        [Header("Drop Shadow")]
        public bool addDropShadow = true;
        public Color shadowColor = new Color(0f, 0f, 0f, 0.5f);
        public Vector2 shadowOffset = new Vector2(2f, -2f);
        
        [Header("Bezier Specific Settings")]
        public int defaultSegments = 36;
        public float defaultHandleScale = 0.45f;
        
        [Header("Arrow")]
        public bool drawArrow = true;
        public float arrowWidth = 16f;
        public float arrowLength = 20f;

        [Header("Current Nodes")]
        public List<SkillTreeNode> nodes = new List<SkillTreeNode>();
        
       

    #if UNITY_EDITOR
        private int _lastTopologyHash = -1;
        private readonly Dictionary<SkillTreeNode, int> _topologyNodeIndexes = new Dictionary<SkillTreeNode, int>();
    #endif

        private void Awake()
        {
            if (Application.isPlaying && liveUpdateInEditor)
                Debug.LogWarning("<b>[SkillTreeConnectionBuilder]</b> 'Live Update In Editor' is ON. Turn this off during gameplay for better performance!");
        }

    #if UNITY_EDITOR
        void Update()
        {
            if (Application.isPlaying || !liveUpdateInEditor) return;

            // Scans the hierarchy to see if you added/deleted a node or changed a connection
            int currentHash = GetTopologyHash();
            if (currentHash != _lastTopologyHash)
            {
                _lastTopologyHash = currentHash;
                CollectNodes();
                BuildLines();
            }
        }

        int GetTopologyHash()
        {
            unchecked
            {
                int hash = 17;

                if (nodeContainer == null)
                    return hash;

                _topologyNodeIndexes.Clear();

                hash = hash * 31 + nodeContainer.childCount;

                for (int i = 0; i < nodeContainer.childCount; i++)
                {
                    var child = nodeContainer.GetChild(i);
                    var node = child.GetComponent<SkillTreeNode>();

                    if (node != null && !_topologyNodeIndexes.ContainsKey(node))
                        _topologyNodeIndexes.Add(node, i);
                }

                for (int i = 0; i < nodeContainer.childCount; i++)
                {
                    var child = nodeContainer.GetChild(i);
                    var node = child.GetComponent<SkillTreeNode>();

                    if (node == null)
                        continue;

                    hash = hash * 31 + i;
                    hash = hash * 31 + node.predecessors.Count;

                    for (int j = 0; j < node.predecessors.Count; j++)
                    {
                        var predecessor = node.predecessors[j];

                        int predecessorIndex = -1;

                        if (predecessor != null && _topologyNodeIndexes.TryGetValue(predecessor, out int foundIndex))
                            predecessorIndex = foundIndex;

                        hash = hash * 31 + predecessorIndex;
                    }
                }

                return hash;
            }
        }

        public void CollectNodes()
        {
            if (nodeContainer == null) return;
            nodes = new List<SkillTreeNode>(nodeContainer.GetComponentsInChildren<SkillTreeNode>());
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void ClearLines()
        {
            if (lineContainer == null) return;
            for (int i = lineContainer.childCount - 1; i >= 0; i--)
            {
                var child = lineContainer.GetChild(i);
                UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        public void BuildLines()
        {
            if (lineContainer == null || nodeContainer == null) return;
            ClearLines();

            foreach (var node in nodes)
            {
                if (node == null) continue;
                node.manager = this; 

                if (node.predecessors.Count == 0) continue;

                foreach (var pred in node.predecessors)
                {
                    if (pred == null) continue;

                    GameObject go = new GameObject($"Line {pred.name} -> {node.name}");
                    UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Skill Tree Line");
                    
                    RectTransform rt = go.AddComponent<RectTransform>();
                    rt.SetParent(lineContainer, false);
                    StretchToParent(rt);

                    SkillTreeConnectionGraphic line = go.AddComponent<SkillTreeConnectionGraphic>();
                    
                    line.manager = this; 
                    line.fromNode = pred;
                    line.toNode = node;
                    
                    line.lineStyle = lineStyle;
                    line.orthogonalShoulderPosition = orthogonalShoulderPosition;
                    line.cornerRadius = cornerRadius;
                    line.cornerSegments = cornerSegments;
                    line.thickness = defaultThickness;
                    line.color = defaultLineColor;
                    
                    if (lineMaterial != null) line.material = lineMaterial;
                    
                    if (addDropShadow)
                    {
                        Shadow shadow = go.AddComponent<Shadow>();
                        shadow.effectColor = shadowColor;
                        shadow.effectDistance = shadowOffset;
                    }
                    
                    line.segments = defaultSegments;
                    line.handleScale = defaultHandleScale;
                    line.drawArrow = drawArrow;
                    line.arrowWidth = arrowWidth;
                    line.arrowLength = arrowLength;
                }
            }
        }

        private void StretchToParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    #endif
    }
}