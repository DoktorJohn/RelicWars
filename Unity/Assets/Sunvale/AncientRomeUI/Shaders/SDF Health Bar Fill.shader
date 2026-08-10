Shader "UI/HealthBarFillOnly"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _FillTex ("Main Fill Texture (Grayscale Tile)", 2D) = "white" {}
        _GhostTex ("Ghost Fill Texture (Grayscale Tile)", 2D) = "white" {}

        _Color ("UGUI Tint", Color) = (1,1,1,1)

        _MainFill ("Main Fill", Range(0,1)) = 0.75
        _GhostFill ("Ghost Fill", Range(0,1)) = 0.75

        _MainTopColor ("Main Top Color", Color) = (0.92, 0.20, 0.18, 1)
        _MainBottomColor ("Main Bottom Color", Color) = (0.38, 0.03, 0.03, 1)

        _GhostTopColor ("Ghost Top Color", Color) = (1.00, 0.72, 0.34, 1)
        _GhostBottomColor ("Ghost Bottom Color", Color) = (0.58, 0.22, 0.06, 1)

        _PatternStrengthMain ("Main Pattern Strength", Range(0,1)) = 0.18
        _PatternStrengthGhost ("Ghost Pattern Strength", Range(0,1)) = 0.18

        _MainEdgeColor ("Main Edge Color", Color) = (1.0, 0.86, 0.60, 0.95)
        _GhostEdgeColor ("Ghost Edge Color", Color) = (1.0, 0.76, 0.36, 0.90)
        _EdgeWidthPx ("Edge Width Px", Range(0,16)) = 3

        _RectSize ("Rect Size Px", Vector) = (512, 64, 0, 0)
        _PixelsPerTile ("Pixels Per Tile", Vector) = (128, 64, 0, 0)

        _RadiusPx ("Corner Radius Px", Range(0,32)) = 0

        _TopHighlightStrength ("Top Highlight Strength", Range(0,1)) = 0.06
        _TopHighlightPower ("Top Highlight Power", Range(0.1,16)) = 3.0

        _TopBevelColor ("Top Bevel Color", Color) = (1.00, 0.86, 0.62, 0.45)
        _TopBevelOffsetPx ("Top Bevel Offset Px", Range(0,32)) = 3
        _TopBevelHeightPx ("Top Bevel Height Px", Range(0,32)) = 8
        _TopBevelSoftnessPx ("Top Bevel Softness Px", Range(0,8)) = 1
        _TopBevelStrength ("Top Bevel Strength", Range(0,1)) = 0.55

        _BottomBevelColor ("Bottom Bevel Color", Color) = (0.00, 0.00, 0.00, 0.45)
        _BottomBevelOffsetPx ("Bottom Bevel Offset Px", Range(0,32)) = 4
        _BottomBevelHeightPx ("Bottom Bevel Height Px", Range(0,32)) = 9
        _BottomBevelSoftnessPx ("Bottom Bevel Softness Px", Range(0,8)) = 1
        _BottomBevelStrength ("Bottom Bevel Strength", Range(0,1)) = 0.50

        _FXBrightness ("FX Brightness", Range(0,2)) = 1
        _FXSaturation ("FX Saturation", Range(0,2)) = 1
        _FXContrast ("FX Contrast", Range(0,2)) = 1

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
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
            Name "HealthBarFill"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _FillTex;
            sampler2D _GhostTex;

            float4 _Color;

            float _MainFill;
            float _GhostFill;

            float4 _MainTopColor;
            float4 _MainBottomColor;
            float4 _GhostTopColor;
            float4 _GhostBottomColor;

            float _PatternStrengthMain;
            float _PatternStrengthGhost;

            float4 _MainEdgeColor;
            float4 _GhostEdgeColor;
            float _EdgeWidthPx;

            float4 _RectSize;
            float4 _PixelsPerTile;
            float _RadiusPx;

            float _TopHighlightStrength;
            float _TopHighlightPower;

            float4 _TopBevelColor;
            float _TopBevelOffsetPx;
            float _TopBevelHeightPx;
            float _TopBevelSoftnessPx;
            float _TopBevelStrength;

            float4 _BottomBevelColor;
            float _BottomBevelOffsetPx;
            float _BottomBevelHeightPx;
            float _BottomBevelSoftnessPx;
            float _BottomBevelStrength;

            float _FXBrightness;
            float _FXSaturation;
            float _FXContrast;

            float4 _ClipRect;

            float sdRoundBox(float2 p, float2 halfSize, float radius)
            {
                radius = min(radius, min(halfSize.x, halfSize.y));
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            float BoundaryMask(float xPx, float boundaryPx, float widthPx, float aa)
            {
                return 1.0 - smoothstep(max(0.0, widthPx - aa), widthPx + aa, abs(xPx - boundaryPx));
            }

            float HorizontalBand(float distFromEdgePx, float offsetPx, float heightPx, float softnessPx)
            {
                softnessPx = max(softnessPx, 0.001);

                float startPx = offsetPx;
                float endPx = offsetPx + heightPx;

                float enter = smoothstep(startPx - softnessPx, startPx + softnessPx, distFromEdgePx);
                float exit = 1.0 - smoothstep(endPx - softnessPx, endPx + softnessPx, distFromEdgePx);

                return saturate(enter * exit);
            }

            float3 ApplyBCS(float3 c, float brightness, float saturation, float contrast)
            {
                float luma = dot(c, float3(0.299, 0.587, 0.114));
                c = lerp(luma.xxx, c, saturation);
                c = (c - 0.5) * contrast + 0.5;
                c *= brightness;
                return saturate(c);
            }

            float3 SamplePatternedGradient(
                sampler2D texSampler,
                float2 tileUV,
                float y01,
                float4 bottomColor,
                float4 topColor,
                float patternStrength)
            {
                float texV = tex2D(texSampler, tileUV).r;

                // Remap this texture so its average gray becomes neutral.
                texV = saturate((texV - 0.40) * 2.25 + 0.5);

                float3 baseCol = lerp(bottomColor.rgb, topColor.rgb, y01);

                // Broad marble value variation.
                float detail = (texV - 0.5) * 2.0;
                float3 col = baseCol * (1.0 + detail * patternStrength);

                // Dark cracks.
                float crack = 1.0 - smoothstep(0.18, 0.36, texV);

                // Bright veins.
                float vein = smoothstep(0.62, 0.88, texV);

                float3 crackCol = baseCol * 0.28;
                float3 veinCol = saturate(baseCol + float3(0.32, 0.16, 0.06));

                col = lerp(col, crackCol, crack * 0.65);
                col = lerp(col, veinCol, vein * 0.45);

                return saturate(col);
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = saturate(i.uv);

                float2 rectSize = max(_RectSize.xy, float2(1.0, 1.0));
                float2 halfSize = rectSize * 0.5;
                float2 p = (uv - 0.5) * rectSize;

                // shape mask
                float d = sdRoundBox(p, halfSize, _RadiusPx);
                float aa = max(fwidth(d), 0.75);
                float shapeMask = 1.0 - smoothstep(0.0, aa, d);

                // x position in pixels
                float xPx = uv.x * rectSize.x;

                // normalized fill values
                float mainFill = saturate(_MainFill);
                float ghostFill = saturate(_GhostFill);

                // boundaries in pixels
                float mainBoundaryPx = mainFill * rectSize.x;
                float ghostBoundaryPx = ghostFill * rectSize.x;

                // masks
                float mainMask = 1.0 - smoothstep(mainBoundaryPx - aa, mainBoundaryPx + aa, xPx);
                float ghostMask = 1.0 - smoothstep(ghostBoundaryPx - aa, ghostBoundaryPx + aa, xPx);

                // common region = main region up to min(main, ghost)
                float commonMask = min(mainMask, ghostMask);

                // occupied region = region up to max(main, ghost)
                float occupiedMask = max(mainMask, ghostMask);

                // ghost-only band = space between main and ghost boundaries
                float ghostOnlyMask = saturate(occupiedMask - commonMask);

                // tile UVs
                float2 pixelsPerTile = max(_PixelsPerTile.xy, float2(1.0, 1.0));
                float2 tileUV = uv * rectSize / pixelsPerTile;

                // sampled colors
                float3 mainCol = SamplePatternedGradient(
                    _FillTex,
                    tileUV,
                    uv.y,
                    _MainBottomColor,
                    _MainTopColor,
                    _PatternStrengthMain
                );

                float3 ghostCol = SamplePatternedGradient(
                    _GhostTex,
                    tileUV,
                    uv.y,
                    _GhostBottomColor,
                    _GhostTopColor,
                    _PatternStrengthGhost
                );

                // compose occupied fill
                float3 col = 0.0;
                col += mainCol * commonMask;
                col += ghostCol * ghostOnlyMask;

                float occupied = saturate(commonMask + ghostOnlyMask);

                // soft top highlight
                float softTopHighlight =
                    pow(saturate(uv.y), _TopHighlightPower) *
                    _TopHighlightStrength;

                col += softTopHighlight * occupied;

                // bevel bands
                float distFromTopPx = (1.0 - uv.y) * rectSize.y;
                float distFromBottomPx = uv.y * rectSize.y;

                float topBevelMask = HorizontalBand(
                    distFromTopPx,
                    _TopBevelOffsetPx,
                    _TopBevelHeightPx,
                    _TopBevelSoftnessPx
                );

                float bottomBevelMask = HorizontalBand(
                    distFromBottomPx,
                    _BottomBevelOffsetPx,
                    _BottomBevelHeightPx,
                    _BottomBevelSoftnessPx
                );

                // top bevel: screen-like lighten
                float3 screenTop = 1.0 - (1.0 - col) * (1.0 - _TopBevelColor.rgb);
                col = lerp(
                    col,
                    screenTop,
                    topBevelMask * _TopBevelColor.a * _TopBevelStrength * occupied
                );

                // bottom bevel: dark overlay
                float3 darkBottom = col * (1.0 - _BottomBevelColor.a) + _BottomBevelColor.rgb * _BottomBevelColor.a;
                col = lerp(
                    col,
                    darkBottom,
                    bottomBevelMask * _BottomBevelStrength * occupied
                );

                // fill edges
                float mainEdge = BoundaryMask(xPx, mainBoundaryPx, _EdgeWidthPx, aa);
                float ghostEdge = BoundaryMask(xPx, ghostBoundaryPx, _EdgeWidthPx, aa);

                mainEdge *= step(0.001, mainFill) * (1.0 - step(0.999, mainFill));
                ghostEdge *= step(0.001, ghostFill) * (1.0 - step(0.999, ghostFill));

                mainEdge *= shapeMask;
                ghostEdge *= shapeMask;

                col = lerp(col, _MainEdgeColor.rgb, mainEdge * _MainEdgeColor.a);
                col = lerp(col, _GhostEdgeColor.rgb, ghostEdge * _GhostEdgeColor.a);

                // final brightness/saturation/contrast
                col = ApplyBCS(col, _FXBrightness, _FXSaturation, _FXContrast);

                float alpha = occupied * shapeMask;

                fixed4 outCol = fixed4(col, alpha) * i.color;

                #ifdef UNITY_UI_CLIP_RECT
                outCol.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(outCol.a - 0.001);
                #endif

                return outCol;
            }
            ENDCG
        }
    }
}