Shader "UI/ProceduralGoldBevel"
{
    Properties
    {
        [Header(Gold Colors)]
        _ShadowColor ("Edge Shadow Color", Color) = (0.35, 0.2, 0.05, 1)
        _BaseColor ("Base Gold Color", Color) = (0.8, 0.6, 0.2, 1)
        _HighlightColor ("Highlight Peak Color", Color) = (1.0, 0.95, 0.6, 1)
        
        [Header(Bevel Settings)]
        _HighlightPos ("Highlight Position (0 to 1)", Range(0.1, 0.9)) = 0.55
        _HighlightSharpness ("Highlight Sharpness", Range(1, 15)) = 6.0
        
        [Header(Anti Aliasing)]
        _AASmoothness ("AA Pixel Spread", Range(0.1, 3.0)) = 1.5

        // --- Required UI Properties ---
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Upgraded target to 3.0 to support fwidth()
            #pragma target 3.0 
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0; // y = thickness (0 inside, 1 outside)
                float4 worldPosition : TEXCOORD1;
            };

            half4 _ShadowColor;
            half4 _BaseColor;
            half4 _HighlightColor;
            float _HighlightPos;
            float _HighlightSharpness;
            float _AASmoothness;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float v = IN.texcoord.y;

                // 1. Create a curved profile
                float curve;
                if (v < _HighlightPos) {
                    curve = smoothstep(0.0, _HighlightPos, v);
                } else {
                    curve = smoothstep(1.0, _HighlightPos, v);
                }

                // 2. Base Color Gradient
                half3 finalColor = lerp(_ShadowColor.rgb, _BaseColor.rgb, curve);

                // 3. Add Sharp Specular Highlight
                float highlightIntensity = pow(curve, _HighlightSharpness);
                finalColor = lerp(finalColor, _HighlightColor.rgb, highlightIntensity);

                // ----------------------------------------------------
                // 4. PROCEDURAL ANTI-ALIASING
                // fwidth() gets the change in 'v' across exactly 1 screen pixel.
                // This means fw changes automatically if you scale the UI up or down!
                float fw = fwidth(v);
                
                // Fade the edge to 0 alpha over a distance of [fw * Smoothness]
                // This does both the v=0 (inside) edge and v=1 (outside) edge.
                float aa = smoothstep(0.0, fw * _AASmoothness, v) * 
                           smoothstep(1.0, 1.0 - (fw * _AASmoothness), v);
                // ----------------------------------------------------

                // 5. Output with AA Alpha and UI Clipping
                half4 output = half4(finalColor, IN.color.a * aa);
                output.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                
                return output;
            }
        ENDCG
        }
    }
}