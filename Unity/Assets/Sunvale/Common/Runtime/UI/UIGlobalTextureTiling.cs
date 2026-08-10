using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.Common.UI
{
    [RequireComponent(typeof(Graphic))]
    [ExecuteAlways]
    [AddComponentMenu("Sunvale/Common/UIGlobalTextureTiling")]
    public class UIGlobalTextureTiling : BaseMeshEffect
    {
        public enum TilingShaderTarget 
        { 
            CustomBevelShader, // Uses UV1 for tiling, UV2 for pixel size, leaves UV0 alone
            StandardUIShader   // Overwrites UV0 for tiling (Default Unity UI)
        }

        [Header("Target Setup")]
        [Tooltip("Choose CustomBevelShader for Tabs that are used in demo options scene (they need extra data), or StandardUIShader for normal UI elements.")]
        public TilingShaderTarget shaderTarget = TilingShaderTarget.StandardUIShader;

        [Header("Tiling Settings")]
        public bool doGlobalTiling = true;
        public Vector2 textureScale = new Vector2(1f, 1f);
        public Vector2 textureOffset = Vector2.zero;
        public float globalScaleMultiplier = 1000f; 

        
        
        protected override void OnEnable()
        {
            base.OnEnable();
            if (graphic.canvas != null)
            {
                var canvas = graphic.canvas;
                var channels = canvas.additionalShaderChannels;
                
                // We only strictly need extra channels for the Custom Shader
                if (shaderTarget == TilingShaderTarget.CustomBevelShader)
                {
                    channels |= AdditionalCanvasShaderChannels.TexCoord1;
                    channels |= AdditionalCanvasShaderChannels.TexCoord2;
                }
                
                canvas.additionalShaderChannels = channels;
            }
            graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!isActiveAndEnabled || graphic.canvas == null)
                return;

            UIVertex vert = new UIVertex();
            Matrix4x4 localToCanvas = graphic.canvas.transform.worldToLocalMatrix * transform.localToWorldMatrix;

            Rect rect = graphic.rectTransform.rect;
            Vector2 rectSize = new Vector2(rect.width, rect.height);

            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vert, i);

                // Calculate Global Tiling Coordinates
                Vector3 canvasPos = localToCanvas.MultiplyPoint3x4(vert.position);
                float u = (canvasPos.x / globalScaleMultiplier) * textureScale.x + textureOffset.x;
                float v = (canvasPos.y / globalScaleMultiplier) * textureScale.y + textureOffset.y;

                if (shaderTarget == TilingShaderTarget.CustomBevelShader)
                {
                    // ---- CUSTOM SHADER BEHAVIOR ----
                    if (doGlobalTiling)
                        vert.uv1 = new Vector4(u, v, 0, 0); // Put tiling in UV1
                    else
                        vert.uv1 = vert.uv0; 

                    vert.uv2 = new Vector4(rectSize.x, rectSize.y, 0, 0); // Put pixel size in UV2
                    // (UV0 is left completely alone so the bevel edge math works)
                }
                else if (shaderTarget == TilingShaderTarget.StandardUIShader)
                {
                    // ---- STANDARD UI BEHAVIOR ----
                    if (doGlobalTiling)
                    {
                        // Overwrite UV0 directly! This makes standard UI materials see the tiling.
                        vert.uv0 = new Vector4(u, v, 0, 0); 
                    }
                }

                vh.SetUIVertex(vert, i);
            }
        }

    #if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (graphic != null)
                graphic.SetVerticesDirty();
        }
    #endif
    }

}
