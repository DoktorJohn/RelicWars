Shader "UI/SimpleFillBar"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture Grayscale", 2D) = "gray" {}

        _Color ("UGUI Tint", Color) = (1,1,1,1)

        _Fill ("Fill", Range(0,1)) = 0.65
        [Toggle] _FillFromRight ("Fill From Right", Float) = 0

        _RectSize ("Rect Size Px", Vector) = (256, 24, 0, 0)

        _OuterInsetPx ("Outer Inset Px", Range(0,32)) = 2

        _BgTopColor ("Background Top", Color) = (0.28,0.27,0.24,1)
        _BgBottomColor ("Background Bottom", Color) = (0.08,0.075,0.065,1)
        _BgStrokeColor ("Background Stroke", Color) = (0.72,0.62,0.45,1)
        _BgStrokeWidthPx ("Background Stroke Width Px", Range(0,16)) = 2
        _BgRadiusPx ("Background Radius Px", Range(0,256)) = 256

        _FillTopColor ("Fill Top", Color) = (0.25,0.88,1.0,1)
        _FillBottomColor ("Fill Bottom", Color) = (0.02,0.25,0.38,1)
        _FillStrokeColor ("Fill Stroke", Color) = (0.85,0.95,1.0,1)
        _FillStrokeWidthPx ("Fill Stroke Width Px", Range(0,16)) = 1

        _FillInsetPx ("Fill Inset From Background Px", Range(0,32)) = 3

        _FillLeftRadiusPx ("Fill Left Radius Px", Range(0,256)) = 256
        _FillRightRadiusPx ("Fill Right Radius Px", Range(0,256)) = 256

        _NoiseScalePx ("Noise Scale Px", Vector) = (96, 24, 0, 0)
        _BgNoiseStrength ("Background Noise Strength", Range(0,1)) = 0.08
        _FillNoiseStrength ("Fill Noise Strength", Range(0,1)) = 0.12

        _TopBevelColor ("Fill Bevel Highlight", Color) = (1,1,1,0.55)
        _TopBevelOffsetPx ("Fill Bevel Offset Px", Range(0,32)) = 3
        _TopBevelHeightPx ("Fill Bevel Height Px", Range(0,32)) = 4
        _TopBevelSoftnessPx ("Fill Bevel Softness Px", Range(0,8)) = 1
        _TopBevelStrength ("Fill Bevel Strength", Range(0,1)) = 0.55

        _BottomShadeColor ("Fill Bottom Shade", Color) = (0,0,0,0.35)
        _BottomShadeHeightPx ("Fill Bottom Shade Height Px", Range(0,32)) = 5
        _BottomShadeStrength ("Fill Bottom Shade Strength", Range(0,1)) = 0.25

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
            Name "ProceduralSDFBar"

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
            sampler2D _NoiseTex;

            float4 _Color;

            float _Fill;
            float _FillFromRight;

            float4 _RectSize;
            float _OuterInsetPx;

            float4 _BgTopColor;
            float4 _BgBottomColor;
            float4 _BgStrokeColor;
            float _BgStrokeWidthPx;
            float _BgRadiusPx;

            float4 _FillTopColor;
            float4 _FillBottomColor;
            float4 _FillStrokeColor;
            float _FillStrokeWidthPx;
            float _FillInsetPx;

            float _FillLeftRadiusPx;
            float _FillRightRadiusPx;

            float4 _NoiseScalePx;
            float _BgNoiseStrength;
            float _FillNoiseStrength;

            float4 _TopBevelColor;
            float _TopBevelOffsetPx;
            float _TopBevelHeightPx;
            float _TopBevelSoftnessPx;
            float _TopBevelStrength;

            float4 _BottomShadeColor;
            float _BottomShadeHeightPx;
            float _BottomShadeStrength;

            float _FXBrightness;
            float _FXSaturation;
            float _FXContrast;

            float4 _ClipRect;

            float sdRoundBoxSides(float2 p, float2 halfSize, float leftRadius, float rightRadius)
            {
                halfSize = max(halfSize, float2(0.001, 0.001));

                leftRadius = min(leftRadius, min(halfSize.x, halfSize.y));
                rightRadius = min(rightRadius, min(halfSize.x, halfSize.y));

                // Corner order:
                // x = top-right
                // y = bottom-right
                // z = bottom-left
                // w = top-left
                float4 r = float4(rightRadius, rightRadius, leftRadius, leftRadius);

                r.xy = (p.x > 0.0) ? r.xy : r.zw;

                float radius = (p.y > 0.0) ? r.x : r.y;

                float2 q = abs(p) - halfSize + radius;

                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius;
            }

            float MaskFromDistance(float d)
            {
                float aa = max(fwidth(d), 0.75);
                return 1.0 - smoothstep(0.0, aa, d);
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

            float SampleNoise(float2 uv, float2 rectSize)
            {
                float2 scalePx = max(_NoiseScalePx.xy, float2(1.0, 1.0));
                float2 noiseUV = uv * rectSize / scalePx;

                float n1 = tex2D(_NoiseTex, noiseUV).r;
                float n2 = tex2D(_NoiseTex, noiseUV * 2.17 + float2(13.1, 7.7)).r;

                float n = lerp(n1, n2, 0.35);

                return (n - 0.5) * 2.0;
            }

            float3 GradientWithNoise(
                float y01,
                float noiseValue,
                float4 bottomColor,
                float4 topColor,
                float strength
            )
            {
                float3 col = lerp(bottomColor.rgb, topColor.rgb, y01);
                col *= 1.0 + noiseValue * strength;

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

                // Global inset.
                // This prevents the procedural SDF visual from touching or clipping against the RectTransform edge.
                float outerInset = max(_OuterInsetPx, 0.0);
                float2 bgHalfSize = max(halfSize - outerInset, float2(0.001, 0.001));

                float spriteAlpha = tex2D(_MainTex, uv).a;

                // -------------------------
                // Background SDF
                // -------------------------

                float bgRadius = min(_BgRadiusPx, min(bgHalfSize.x, bgHalfSize.y));

                float dBgOuter = sdRoundBoxSides(
                    p,
                    bgHalfSize,
                    bgRadius,
                    bgRadius
                );

                float bgOuter = MaskFromDistance(dBgOuter);

                float bgStroke = max(_BgStrokeWidthPx, 0.0);

                float2 bgInnerHalf = max(
                    bgHalfSize - bgStroke,
                    float2(0.001, 0.001)
                );

                float bgInnerRadius = max(bgRadius - bgStroke, 0.0);

                float dBgInner = sdRoundBoxSides(
                    p,
                    bgInnerHalf,
                    bgInnerRadius,
                    bgInnerRadius
                );

                float bgInner = MaskFromDistance(dBgInner);

                float bgStrokeMask = saturate(bgOuter - bgInner);

                float noiseValue = SampleNoise(uv, rectSize);

                float3 bgCol = GradientWithNoise(
                    uv.y,
                    noiseValue,
                    _BgBottomColor,
                    _BgTopColor,
                    _BgNoiseStrength
                );

                float3 col = bgCol;

                col = lerp(
                    col,
                    _BgStrokeColor.rgb,
                    bgStrokeMask * _BgStrokeColor.a
                );

                // -------------------------
                // Fill SDF
                // -------------------------

                float fill01 = saturate(_Fill);

                float fillAreaInset = bgStroke + _FillInsetPx;

                float2 fillAreaHalf = max(
                    bgHalfSize - fillAreaInset,
                    float2(0.001, 0.001)
                );

                float fullFillWidth = fillAreaHalf.x * 2.0;
                float fillWidth = max(fullFillWidth * fill01, 0.001);
                float fillHalfX = fillWidth * 0.5;

                float fillCenterX;

                if (_FillFromRight > 0.5)
                {
                    fillCenterX = fillAreaHalf.x - fillHalfX;
                }
                else
                {
                    fillCenterX = -fillAreaHalf.x + fillHalfX;
                }

                float2 fillHalf = float2(fillHalfX, fillAreaHalf.y);
                float2 pFill = float2(p.x - fillCenterX, p.y);

                float fillLeftRadius = min(
                    _FillLeftRadiusPx,
                    min(fillHalf.x, fillHalf.y)
                );

                float fillRightRadius = min(
                    _FillRightRadiusPx,
                    min(fillHalf.x, fillHalf.y)
                );

                float dFillOuter = sdRoundBoxSides(
                    pFill,
                    fillHalf,
                    fillLeftRadius,
                    fillRightRadius
                );

                float fillOuter = MaskFromDistance(dFillOuter);

                fillOuter *= step(0.001, fill01);
                fillOuter *= bgInner;

                float fillStroke = max(_FillStrokeWidthPx, 0.0);

                float2 fillInnerHalf = max(
                    fillHalf - fillStroke,
                    float2(0.001, 0.001)
                );

                float fillInnerLeftRadius = max(fillLeftRadius - fillStroke, 0.0);
                float fillInnerRightRadius = max(fillRightRadius - fillStroke, 0.0);

                float dFillInner = sdRoundBoxSides(
                    pFill,
                    fillInnerHalf,
                    fillInnerLeftRadius,
                    fillInnerRightRadius
                );

                float fillInner = MaskFromDistance(dFillInner);
                fillInner *= fillOuter;

                float fillStrokeMask = saturate(fillOuter - fillInner);

                float3 fillCol = GradientWithNoise(
                    uv.y,
                    noiseValue,
                    _FillBottomColor,
                    _FillTopColor,
                    _FillNoiseStrength
                );

                // -------------------------
                // Fill bevel highlight
                // -------------------------

                float distFromTopPx = (1.0 - uv.y) * rectSize.y;

                float topBevelMask = HorizontalBand(
                    distFromTopPx,
                    _TopBevelOffsetPx,
                    _TopBevelHeightPx,
                    _TopBevelSoftnessPx
                );

                float3 screenTop = 1.0 - (1.0 - fillCol) * (1.0 - _TopBevelColor.rgb);

                fillCol = lerp(
                    fillCol,
                    screenTop,
                    topBevelMask * _TopBevelColor.a * _TopBevelStrength
                );

                // -------------------------
                // Fill bottom shade
                // -------------------------

                float distFromBottomPx = uv.y * rectSize.y;

                float bottomShadeMask = 1.0 - smoothstep(
                    _BottomShadeHeightPx - 0.001,
                    _BottomShadeHeightPx + 1.0,
                    distFromBottomPx
                );

                float3 bottomShade = lerp(
                    fillCol,
                    _BottomShadeColor.rgb,
                    _BottomShadeColor.a
                );

                fillCol = lerp(
                    fillCol,
                    bottomShade,
                    bottomShadeMask * _BottomShadeStrength
                );

                // -------------------------
                // Compose
                // -------------------------

                col = lerp(col, fillCol, fillInner);

                col = lerp(
                    col,
                    _FillStrokeColor.rgb,
                    fillStrokeMask * _FillStrokeColor.a
                );

                col = ApplyBCS(
                    col,
                    _FXBrightness,
                    _FXSaturation,
                    _FXContrast
                );

                float alpha = bgOuter * spriteAlpha;

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