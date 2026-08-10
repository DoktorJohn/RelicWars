Shader "UI/InventorySlotProceduralNoise"
{
    Properties
    {
        [PerRendererData] _MainTex ("Item Atlas", 2D) = "white" {}
        _Color ("UI Tint", Color) = (1,1,1,1)
        _ItemAspectScale ("Item Aspect Scale XY", Vector) = (1,1,0,0)

        // Shape
        _CornerRadius ("Corner Radius", Range(0, 0.2)) = 0.018

        // Layer widths: outside -> inside
        _InsetShadowWidth ("Inset Shadow Width", Range(0, 0.08)) = 0.010
        _OuterRimWidth ("Outer Rim Width", Range(0, 0.05)) = 0.010
        _FrameBodyWidth ("Frame Body Width", Range(0, 0.08)) = 0.020
        _InnerLineWidth ("Inner Bright Line Width", Range(0, 0.03)) = 0.005
        _CreaseWidth ("Inner Crease Width", Range(0, 0.03)) = 0.004
        _BevelWidth ("Background Bevel Width", Range(0, 0.05)) = 0.012

        // Procedural noise
        _NoiseSeed ("Noise Seed", Range(0, 1000)) = 0
        _FrameNoiseScale ("Frame Noise Scale", Range(4, 220)) = 90
        _FrameNoiseStrength ("Frame Noise Strength", Range(0, 0.6)) = 0.16
        _BgNoiseScale ("Background Noise Scale", Range(2, 120)) = 24
        _BgNoiseStrength ("Background Noise Strength", Range(0, 0.5)) = 0.10

        // Frame colors
        _InsetShadowColor ("Inset Shadow Color", Color) = (0.025, 0.017, 0.012, 1)

        _GoldLight ("Gold Light", Color) = (1.00, 0.86, 0.46, 1)
        _GoldMid ("Gold Mid", Color) = (0.68, 0.43, 0.12, 1)
        _GoldDark ("Gold Dark", Color) = (0.24, 0.13, 0.035, 1)

        _CreaseColor ("Inner Crease", Color) = (0.055, 0.030, 0.014, 1)

        // Inner bevel + background
        _BevelHighlight ("Bevel Highlight", Color) = (0.95, 0.72, 0.24, 1)
        _BevelShadow ("Bevel Shadow", Color) = (0.16, 0.065, 0.020, 1)

        _BgEdgeColor ("Background Edge", Color) = (0.045, 0.010, 0.008, 1)
        _BgCenterColor ("Background Center", Color) = (0.30, 0.040, 0.028, 1)

        _BgBrightness ("BG Brightness", Range(0, 3)) = 1
        _BgContrast ("BG Contrast", Range(0, 3)) = 1
        _BgSaturation ("BG Saturation", Range(0, 3)) = 1

        _CenterGlow ("Center Glow", Range(0, 2)) = 1.0
        _DirectionalStrength ("Directional Strength", Range(0, 2)) = 1.0

        // Item
        _ItemAtlasRect ("Item Atlas Rect XYWH", Vector) = (0,0,1,1)
        _ItemOffset ("Item Offset XY", Vector) = (0,0,0,0)
        _ItemScale ("Item Scale", Range(0.1, 3)) = 1.0
        _ItemOpacity ("Item Opacity", Range(0, 1)) = 1.0

        _ItemBrightness ("Item Brightness", Range(0, 3)) = 1
        _ItemContrast ("Item Contrast", Range(0, 3)) = 1
        _ItemSaturation ("Item Saturation", Range(0, 3)) = 1

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
            Name "InventorySlotProceduralNoise"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

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
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _ItemAspectScale;
            fixed4 _Color;
            float4 _ClipRect;

            float _CornerRadius;

            float _InsetShadowWidth;
            float _OuterRimWidth;
            float _FrameBodyWidth;
            float _InnerLineWidth;
            float _CreaseWidth;
            float _BevelWidth;

            float _NoiseSeed;
            float _FrameNoiseScale;
            float _FrameNoiseStrength;
            float _BgNoiseScale;
            float _BgNoiseStrength;

            fixed4 _InsetShadowColor;

            fixed4 _GoldLight;
            fixed4 _GoldMid;
            fixed4 _GoldDark;

            fixed4 _CreaseColor;

            fixed4 _BevelHighlight;
            fixed4 _BevelShadow;

            fixed4 _BgEdgeColor;
            fixed4 _BgCenterColor;

            float _BgBrightness;
            float _BgContrast;
            float _BgSaturation;

            float _CenterGlow;
            float _DirectionalStrength;

            float4 _ItemAtlasRect;
            float4 _ItemOffset;
            float _ItemScale;
            float _ItemOpacity;

            float _ItemBrightness;
            float _ItemContrast;
            float _ItemSaturation;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.uv = v.texcoord;
                OUT.color = v.color * _Color;

                return OUT;
            }

            float sdRoundRect(float2 uv, float inset, float radius)
            {
                radius = max(radius, 0.0001);

                float2 halfSize = 0.5 - inset - radius;
                float2 q = abs(uv - 0.5) - halfSize;

                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            float maskFromSDF(float d)
            {
                float aa = max(fwidth(d) * 1.2, 0.0001);
                return smoothstep(aa, -aa, d);
            }

            float shapeMask(float2 uv, float inset)
            {
                float r = max(_CornerRadius - inset * 0.15, 0.0001);
                return maskFromSDF(sdRoundRect(uv, inset, r));
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float2 u = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i + float2(0.0, 0.0));
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                float x1 = lerp(a, b, u.x);
                float x2 = lerp(c, d, u.x);

                return lerp(x1, x2, u.y);
            }

            float ProceduralNoise(float2 uv, float scale, float seed)
            {
                float2 p = uv * scale + float2(seed * 13.17, seed * 47.31);

                float softA = ValueNoise(p);
                float softB = ValueNoise(p * 2.37 + 19.19);
                float grain = Hash21(floor(p * 6.0 + seed * 3.11));

                float n = softA * 0.60 + softB * 0.27 + grain * 0.13;

                // Built-in contrast so it does not become marble-smooth.
                n = saturate((n - 0.5) * 2.75 + 0.5);

                return n * 2.0 - 1.0;
            }

            float3 ApplyBCS(float3 c, float brightness, float contrast, float saturation)
            {
                c *= brightness;

                float l = dot(c, float3(0.299, 0.587, 0.114));
                c = lerp(l.xxx, c, saturation);

                c = (c - 0.5) * contrast + 0.5;
                return saturate(c);
            }

            float3 ApplyValueNoise(float3 c, float noise, float strength)
            {
                float m = 1.0 + noise * strength;
                float additive = noise * strength * 0.045;

                return saturate(c * m + additive);
            }

            float DirectionalFactor(float2 uv)
            {
                // Top-left brighter, bottom-right darker.
                float f = uv.y - uv.x;
                return saturate(0.5 + f * 0.5 * _DirectionalStrength);
            }

            float EdgeProfile(float2 uv)
            {
                float d = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
                return saturate(1.0 - d * 8.0);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.uv;

                float p0 = 0.0;
                float p1 = p0 + _InsetShadowWidth;
                float p2 = p1 + _OuterRimWidth;
                float p3 = p2 + _FrameBodyWidth;
                float p4 = p3 + _InnerLineWidth;
                float p5 = p4 + _CreaseWidth;
                float p6 = p5 + _BevelWidth;

                float m0 = shapeMask(uv, p0);
                float m1 = shapeMask(uv, p1);
                float m2 = shapeMask(uv, p2);
                float m3 = shapeMask(uv, p3);
                float m4 = shapeMask(uv, p4);
                float m5 = shapeMask(uv, p5);
                float m6 = shapeMask(uv, p6);

                float bandInsetShadow = saturate(m0 - m1);
                float bandOuterRim = saturate(m1 - m2);
                float bandFrameBody = saturate(m2 - m3);
                float bandInnerLine = saturate(m3 - m4);
                float bandCrease = saturate(m4 - m5);
                float bandBevel = saturate(m5 - m6);
                float bandContent = m6;

                float dir = DirectionalFactor(uv);
                float edge = EdgeProfile(uv);

                float bottomDark = pow(saturate(1.0 - uv.y), 1.5);
                float rightDark = pow(saturate(uv.x), 1.5);
                float occlusion = saturate((bottomDark * 0.65 + rightDark * 0.35) * 0.8);

                float frameNoise = ProceduralNoise(uv, _FrameNoiseScale, _NoiseSeed);
                float bgNoise = ProceduralNoise(uv + float2(4.73, 8.19), _BgNoiseScale, _NoiseSeed + 37.0);

                float3 col = 0;

                // 1. Inset shadow.
                col = lerp(col, _InsetShadowColor.rgb, bandInsetShadow);

                // 2. Outer rim.
                float3 outerRimCol = lerp(_GoldDark.rgb, _GoldLight.rgb, dir);
                outerRimCol = lerp(outerRimCol, outerRimCol * 0.76, occlusion * 0.65);
                outerRimCol = ApplyValueNoise(outerRimCol, frameNoise, _FrameNoiseStrength * 0.75);

                col = lerp(col, outerRimCol, bandOuterRim);

                // 3. Main gold frame body.
                float vertical = saturate(uv.y * 0.9 + 0.1);

                float3 frameCol = lerp(_GoldDark.rgb, _GoldMid.rgb, vertical);
                frameCol = lerp(frameCol, _GoldLight.rgb, dir * 0.65);
                frameCol = lerp(frameCol, _GoldDark.rgb, occlusion * 0.72);

                frameCol *= lerp(0.96, 1.06, edge);
                frameCol = ApplyValueNoise(frameCol, frameNoise, _FrameNoiseStrength);

                col = lerp(col, frameCol, bandFrameBody);

                // 4. Thin inner highlight line.
                float3 innerLineCol = lerp(_GoldMid.rgb, _GoldLight.rgb, dir);
                innerLineCol = lerp(innerLineCol, innerLineCol * 0.84, occlusion * 0.5);
                innerLineCol = ApplyValueNoise(innerLineCol, frameNoise, _FrameNoiseStrength * 0.38);

                col = lerp(col, innerLineCol, bandInnerLine);

                // 5. Inner crease.
                col = lerp(col, _CreaseColor.rgb, bandCrease);

                // 6. Main background with procedural variation.
                float2 center = (uv - 0.5) * 2.0;
                float radial = saturate(1.0 - length(center));
                radial = pow(radial, 1.55) * _CenterGlow;

                radial = saturate(radial + bgNoise * _BgNoiseStrength * 0.45);

                float3 bgCol = lerp(_BgEdgeColor.rgb, _BgCenterColor.rgb, radial);

                // Darker edge/vignette feeling.
                float vignette = saturate(length(center));
                bgCol = lerp(bgCol, _BgEdgeColor.rgb, pow(vignette, 2.2) * 0.35);

                bgCol = ApplyValueNoise(bgCol, bgNoise, _BgNoiseStrength * 0.65);
                bgCol = ApplyBCS(bgCol, _BgBrightness, _BgContrast, _BgSaturation);

                col = lerp(col, bgCol, bandContent);

                // 7. Hard chiseled inner bevel.
                float leftD = uv.x;
                float rightD = 1.0 - uv.x;
                float bottomD = uv.y;
                float topD = 1.0 - uv.y;

                float topLeftDist = min(leftD, topD);
                float bottomRightDist = min(rightD, bottomD);

                float bevelHighlightSide = step(topLeftDist, bottomRightDist);

                float3 bevelCol = lerp(_BevelShadow.rgb, _BevelHighlight.rgb, bevelHighlightSide);
                bevelCol = lerp(bevelCol, bevelCol * 0.78, occlusion * 0.45);
                bevelCol = ApplyValueNoise(bevelCol, frameNoise, _FrameNoiseStrength * 0.85);

                col = lerp(col, bevelCol, bandBevel);

                // 8. Item atlas sampling.
                float scale = max(_ItemScale, 0.001);

                // Subtract the offset before scale/aspect is applied to match screen-pixel dragging
                float2 itemLocalUV = (uv - 0.5 - _ItemOffset.xy) * _ItemAspectScale.xy / scale + 0.5;

                float inItemUV =
                    step(0.0, itemLocalUV.x) * step(itemLocalUV.x, 1.0) *
                    step(0.0, itemLocalUV.y) * step(itemLocalUV.y, 1.0);

                float2 itemAtlasUV = _ItemAtlasRect.xy + itemLocalUV * _ItemAtlasRect.zw;

                fixed4 item = tex2D(_MainTex, itemAtlasUV) * inItemUV;
                item.rgb = ApplyBCS(item.rgb, _ItemBrightness, _ItemContrast, _ItemSaturation);

                float itemA = item.a * _ItemOpacity * bandContent;
                col = lerp(col, item.rgb, itemA);

                fixed4 result;
                result.rgb = col * IN.color.rgb;
                result.a = m0 * IN.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                    result.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(result.a - 0.001);
                #endif

                return result;
            }
            ENDCG
        }
    }
}