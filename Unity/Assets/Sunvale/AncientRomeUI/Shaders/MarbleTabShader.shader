Shader "UI/Custom/TabSDF_PhotoshopBevel"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Photoshop Inner Bevel Settings)]
        _BevelSize ("Size (Pixels)", Float) = 4.0
        _BevelDepth ("Depth (Multiplier %)", Float) = 2.72
        
        [Header(Shading  Top Edge Highlight)]
        _HighlightColor ("Highlight Color (Red-ish)", Color) = (1.0, 0.6, 0.6, 0.53) // RGB + Alpha(Opacity)
        
        [Header(Shading  Bottom _ Side Shadows)]
        _ShadowColor ("Shadow Color", Color) = (0.0, 0.0, 0.0, 0.50) // RGB + Alpha(Opacity)
        _SideShadowStrength ("Side Shadow Multiplier", Range(0, 1)) = 0.5 // 90deg light hits top hard, but sides get partial shadow
        
        [Header(Solid Edge Border)]
        _BorderColor("Border Color (Hover Inject)", Color) = (1, 1, 1, 0)
        _BorderWidth("Border Width (Pixels)", Float) = 0.0
        
        [Header(HSV Settings)]
        _HsvShift("Hue Shift", Range(-180, 180)) = 0
        _HsvSaturation("Saturation", Range(0, 2)) = 1
        _HsvBright("Brightness", Range(0, 2)) = 1

        // Standard UGUI Stencil properties
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 uv0      : TEXCOORD0; 
                float2 uv1      : TEXCOORD1; 
                float2 uv2      : TEXCOORD2; // Pixel Dimensions from C#
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv0      : TEXCOORD0;
                float2 uv1      : TEXCOORD1;
                float2 uv2      : TEXCOORD2;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            
            float _BevelSize;
            float _BevelDepth;
            float4 _HighlightColor;
            float4 _ShadowColor;
            float _SideShadowStrength;
            
            float4 _BorderColor;
            float _BorderWidth;
            
            float _HsvShift;
            float _HsvSaturation;
            float _HsvBright;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv0 = v.uv0;
                o.uv1 = v.uv1;
                o.uv2 = v.uv2; 
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 1. Sample Background
                half4 col = tex2D(_MainTex, IN.uv1) * IN.color;

                // 2. HSV Math
                half3 resultHsv = half3(col.rgb);
                half cosHsv = _HsvBright * _HsvSaturation * cos(_HsvShift * 3.14159265 / 180.0);
                half sinHsv = _HsvBright * _HsvSaturation * sin(_HsvShift * 3.14159265 / 180.0);
                
                resultHsv.r = (.299 * _HsvBright + .701 * cosHsv + .168 * sinHsv) * col.r + (.587 * _HsvBright - .587 * cosHsv + .330 * sinHsv) * col.g + (.114 * _HsvBright - .114 * cosHsv - .497 * sinHsv) * col.b;
                resultHsv.g = (.299 * _HsvBright - .299 * cosHsv - .328 * sinHsv) * col.r + (.587 * _HsvBright + .413 * cosHsv + .035 * sinHsv) * col.g + (.114 * _HsvBright - .114 * cosHsv + .292 * sinHsv) * col.b;
                resultHsv.b = (.299 * _HsvBright - .300 * cosHsv + 1.250 * sinHsv) * col.r + (.587 * _HsvBright - .588 * cosHsv - 1.050 * sinHsv) * col.g + (.114 * _HsvBright + .886 * cosHsv - .203 * sinHsv) * col.b;
                col.rgb = resultHsv;

                // 3. PIXEL PERFECT DISTANCES TO ALL 4 EDGES
                float2 pixelPos = IN.uv0 * IN.uv2; 
                
                float dL = pixelPos.x;                       // Dist to Left
                float dR = IN.uv2.x - pixelPos.x;            // Dist to Right
                float dB = pixelPos.y;                       // Dist to Bottom
                float dT = IN.uv2.y - pixelPos.y;            // Dist to Top

                float minDist = min(min(dL, dR), min(dB, dT));

                // 4. CHISEL BEVEL MATH
                if (_BevelSize > 0.0 && minDist <= _BevelSize)
                {
                    // Linear falloff from edge (1.0) to inner boundary (0.0)
                    float bevelT = 1.0 - (minDist / _BevelSize);
                    
                    // Apply Photoshop "Depth" (272% pushes the contrast to make it 'hard')
                    float intensity = saturate(bevelT * _BevelDepth);

                    // Identify which side we are closest to (Creates perfect 45-degree miters at corners)
                    // The +0.01 handles ties perfectly to blend the corners
                    float wT = step(dT, minDist + 0.01);
                    float wB = step(dB, minDist + 0.01);
                    float wL = step(dL, minDist + 0.01);
                    float wR = step(dR, minDist + 0.01);
                    
                    // Normalize weights so corners don't get double bright/dark
                    float totalW = wT + wB + wL + wR;
                    wT /= totalW; wB /= totalW; wL /= totalW; wR /= totalW;

                    // Calculate Photoshop Blend Modes
                    // Screen: 1 - (1 - Base)(1 - BlendColor)
                    float3 screenColor = 1.0 - (1.0 - col.rgb) * (1.0 - _HighlightColor.rgb);
                    // Multiply: Base * BlendColor
                    float3 multColor = col.rgb * _ShadowColor.rgb;

                    // Accumulate directional lighting
                    float3 bevelEffect = col.rgb;
                    
                    // Top gets Screen Highlight
                    bevelEffect = lerp(bevelEffect, screenColor, wT * _HighlightColor.a);
                    
                    // Bottom gets Multiply Shadow
                    bevelEffect = lerp(bevelEffect, multColor, wB * _ShadowColor.a);
                    
                    // Sides get partial Multiply Shadow (to simulate 90-degree grazing light)
                    bevelEffect = lerp(bevelEffect, multColor, (wL + wR) * _ShadowColor.a * _SideShadowStrength);

                    // Apply final assembled bevel to the pixel
                    col.rgb = lerp(col.rgb, bevelEffect, intensity);
                }

                // 5. Solid Crisp Border (Hover Inject, draws over the very edge)
                if (_BorderWidth > 0.0 && _BorderColor.a > 0.0)
                {
                    float borderMask = 1.0 - smoothstep(_BorderWidth - 1.5, _BorderWidth, minDist);
                    col.rgb = lerp(col.rgb, _BorderColor.rgb, borderMask * _BorderColor.a);
                }

                return col;
            }
            ENDCG
        }
    }
}