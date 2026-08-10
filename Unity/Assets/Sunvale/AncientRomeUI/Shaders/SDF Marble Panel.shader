Shader "Sunvale/UI/SDF Marble Panel Lite Concave"
{
    Properties
    {
        [PerRendererData] _MainTex ("Marble / Panel Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Texture)]
        [Toggle] _ForceRepeat ("Force Repeat With frac()", Float) = 0
        _HsvShift ("Hue Shift", Range(0, 360)) = 0
        _HsvSaturation ("Saturation", Range(0, 2)) = 1
        _HsvBright ("Brightness", Range(0, 4)) = 1
        _TextureContrast ("Texture Contrast", Range(0, 3)) = 1
        _TextureScaleOffset ("Texture Scale Offset XYZW", Vector) = (1,1,0,0)

        [Header(Disabled Greyscale)]
        _GreyscaleAmount ("Greyscale Amount", Range(0, 1)) = 0
        _GreyscaleBrightness ("Greyscale Brightness", Range(-1, 1)) = 0
        _GreyscaleContrast ("Greyscale Contrast", Range(0, 3)) = 1
        _GreyscaleTint ("Greyscale Tint", Color) = (0.55,0.55,0.55,1)
        _GreyscaleTintStrength ("Greyscale Tint Strength", Range(0, 1)) = 0

        [Header(Panel Shape)]
        _PanelInset ("Panel Inset L R T B px", Vector) = (4,4,4,4)
        _RadiusPx ("Normal Corner Radius px", Float) = 2
        _AASoftnessPx ("AA Softness px", Float) = 1.25

        [Header(Concave __ Inverted Corners)]
        _ConcaveRadiusPx ("Concave Radius px", Float) = 8
        [Toggle] _ConcaveTL ("Concave Top Left", Float) = 0
        [Toggle] _ConcaveTR ("Concave Top Right", Float) = 0
        [Toggle] _ConcaveBR ("Concave Bottom Right", Float) = 0
        [Toggle] _ConcaveBL ("Concave Bottom Left", Float) = 0

        [Header(Inner Ambient Occlusion)]
        _InnerShadowWidthPx ("Inner Shadow Width px", Float) = 12
        _InnerShadowStrength ("Inner Shadow Strength", Range(0, 1)) = 0.14

        [Header(Directional Shade)]
        _TopDarkWidthPx ("Top Dark Width px", Float) = 6
        _TopDarkStrength ("Top Dark Strength", Range(0, 1)) = 0.035

        _BottomDarkWidthPx ("Bottom Dark Width px", Float) = 22
        _BottomDarkStrength ("Bottom Dark Strength", Range(0, 1)) = 0.12

        _RightDarkWidthPx ("Right Dark Width px", Float) = 16
        _RightDarkStrength ("Right Dark Strength", Range(0, 1)) = 0.055

        _LeftLightWidthPx ("Left Light Width px", Float) = 10
        _LeftLightStrength ("Left Light Strength", Range(0, 1)) = 0.035

        [Header(Bevel Highlight)]
        _TopHighlightOffsetPx ("Top Highlight Offset px", Float) = 1.5
        _TopHighlightWidthPx ("Top Highlight Width px", Float) = 2.5
        _TopHighlightStrength ("Top Highlight Strength", Range(0, 2)) = 0.28
        _HighlightColor ("Highlight Color", Color) = (1,0.88,0.58,1)

        [Header(Outer Shadow Within Rect)]
        _OuterShadowOffsetPx ("Outer Shadow Offset XY px", Vector) = (0,-3,0,0)
        _OuterShadowSoftnessPx ("Outer Shadow Softness px", Float) = 10
        _OuterShadowStrength ("Outer Shadow Strength", Range(0, 1)) = 0.22
        _OuterShadowColor ("Outer Shadow Color", Color) = (0,0,0,0.45)

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
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex    : POSITION;
                fixed4 color     : COLOR;
                float2 texcoord  : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float2 texcoord2 : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 localUV       : TEXCOORD0;
                float2 tileUV        : TEXCOORD1;
                float2 rectSize      : TEXCOORD2;
                float4 worldPosition : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float _ForceRepeat;
            float _HsvShift;
            float _HsvSaturation;
            float _HsvBright;
            float _TextureContrast;
            float4 _TextureScaleOffset;

            float _GreyscaleAmount;
            float _GreyscaleBrightness;
            float _GreyscaleContrast;
            fixed4 _GreyscaleTint;
            float _GreyscaleTintStrength;

            float4 _PanelInset;
            float _RadiusPx;
            float _AASoftnessPx;

            float _ConcaveRadiusPx;
            float _ConcaveTL;
            float _ConcaveTR;
            float _ConcaveBR;
            float _ConcaveBL;

            float _InnerShadowWidthPx;
            float _InnerShadowStrength;

            float _TopDarkWidthPx;
            float _TopDarkStrength;
            float _BottomDarkWidthPx;
            float _BottomDarkStrength;
            float _RightDarkWidthPx;
            float _RightDarkStrength;
            float _LeftLightWidthPx;
            float _LeftLightStrength;

            float _TopHighlightOffsetPx;
            float _TopHighlightWidthPx;
            float _TopHighlightStrength;
            fixed4 _HighlightColor;

            float4 _OuterShadowOffsetPx;
            float _OuterShadowSoftnessPx;
            float _OuterShadowStrength;
            fixed4 _OuterShadowColor;

            float sdRoundedRect(float2 p, float2 rectMin, float2 rectMax, float radius)
            {
                float2 center = (rectMin + rectMax) * 0.5;
                float2 halfSize = max((rectMax - rectMin) * 0.5, 0.001);

                radius = min(radius, min(halfSize.x, halfSize.y));

                float2 q = abs(p - center) - (halfSize - radius);
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            float sdCircle(float2 p, float2 center, float radius)
            {
                return length(p - center) - radius;
            }

            float subtractCornerCircle(float baseDistance, float2 p, float2 center, float radius, float enabled)
            {
                if (enabled > 0.5 && radius > 0.001)
                {
                    float circleDistance = sdCircle(p, center, radius);
                    baseDistance = max(baseDistance, -circleDistance);
                }

                return baseDistance;
            }

            float panelDistance(float2 p, float2 rectMin, float2 rectMax)
            {
                float2 size = max(rectMax - rectMin, 1.0);
                float normalRadius = min(_RadiusPx, min(size.x, size.y) * 0.5);

                float d = sdRoundedRect(p, rectMin, rectMax, normalRadius);

                float r = min(_ConcaveRadiusPx, min(size.x, size.y) * 0.5);

                float2 cTL = float2(rectMin.x, rectMax.y);
                float2 cTR = float2(rectMax.x, rectMax.y);
                float2 cBR = float2(rectMax.x, rectMin.y);
                float2 cBL = float2(rectMin.x, rectMin.y);

                d = subtractCornerCircle(d, p, cTL, r, _ConcaveTL);
                d = subtractCornerCircle(d, p, cTR, r, _ConcaveTR);
                d = subtractCornerCircle(d, p, cBR, r, _ConcaveBR);
                d = subtractCornerCircle(d, p, cBL, r, _ConcaveBL);

                return d;
            }

            float edgeBand(float distancePx, float offsetPx, float widthPx, float featherPx)
            {
                widthPx = max(widthPx, 0.001);
                featherPx = min(featherPx, widthPx * 0.5);

                float a = smoothstep(offsetPx, offsetPx + featherPx, distancePx);
                float b = 1.0 - smoothstep(offsetPx + widthPx - featherPx, offsetPx + widthPx, distancePx);

                return saturate(a * b);
            }

            float oneSidedFade(float distancePx, float widthPx)
            {
                return saturate(1.0 - distancePx / max(widthPx, 0.001));
            }

            float3 applyHsv(float3 c)
            {
                float shiftRadians = _HsvShift * 3.14159265 / 180.0;

                float cosHsv = _HsvBright * _HsvSaturation * cos(shiftRadians);
                float sinHsv = _HsvBright * _HsvSaturation * sin(shiftRadians);

                float3 resultHsv;

                resultHsv.r = (.299 * _HsvBright + .701 * cosHsv + .168 * sinHsv) * c.r
                            + (.587 * _HsvBright - .587 * cosHsv + .330 * sinHsv) * c.g
                            + (.114 * _HsvBright - .114 * cosHsv - .497 * sinHsv) * c.b;

                resultHsv.g = (.299 * _HsvBright - .299 * cosHsv - .328 * sinHsv) * c.r
                            + (.587 * _HsvBright + .413 * cosHsv + .035 * sinHsv) * c.g
                            + (.114 * _HsvBright - .114 * cosHsv + .292 * sinHsv) * c.b;

                resultHsv.b = (.299 * _HsvBright - .300 * cosHsv + 1.250 * sinHsv) * c.r
                            + (.587 * _HsvBright - .588 * cosHsv - 1.050 * sinHsv) * c.g
                            + (.114 * _HsvBright + .886 * cosHsv - .203 * sinHsv) * c.b;

                return resultHsv;
            }

            float3 adjustTexture(float3 c)
            {
                c = applyHsv(c);
                c = (c - 0.5) * _TextureContrast + 0.5;

                return saturate(c);
            }

            float3 applyGreyscaleLock(float3 c)
            {
                float gray = dot(c, float3(0.299, 0.587, 0.114));

                float3 g = gray.xxx;
                g = (g - 0.5) * _GreyscaleContrast + 0.5;
                g += _GreyscaleBrightness;

                float tintAmount = saturate(_GreyscaleTintStrength * _GreyscaleTint.a);
                g = lerp(g, g * _GreyscaleTint.rgb, tintAmount);

                return lerp(c, saturate(g), saturate(_GreyscaleAmount));
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.localUV = v.texcoord;
                OUT.tileUV = v.texcoord1 * _TextureScaleOffset.xy + _TextureScaleOffset.zw;
                OUT.rectSize = max(v.texcoord2, float2(1.0, 1.0));

                OUT.color = v.color * _Color;

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 rectSize = IN.rectSize;
                float2 p = IN.localUV * rectSize;

                float2 rectMin = float2(_PanelInset.x, _PanelInset.w);
                float2 rectMax = float2(rectSize.x - _PanelInset.y, rectSize.y - _PanelInset.z);

                rectMax = max(rectMax, rectMin + 1.0);

                float aa = max(_AASoftnessPx, 0.5);

                float dist = panelDistance(p, rectMin, rectMax);
                float panelAlpha = 1.0 - smoothstep(-aa, aa, dist);

                float2 shadowP = p - _OuterShadowOffsetPx.xy;
                float shadowDist = panelDistance(shadowP, rectMin, rectMax);

                float outsidePanel = smoothstep(-aa, aa, dist);
                float outerShadow = 1.0 - smoothstep(0.0, max(_OuterShadowSoftnessPx, 0.001), shadowDist);
                outerShadow *= outsidePanel;
                outerShadow *= _OuterShadowStrength;

                float2 sampleUV = IN.tileUV;

                if (_ForceRepeat > 0.5)
                {
                    sampleUV = frac(sampleUV);
                }

                fixed4 tex = tex2D(_MainTex, sampleUV) + _TextureSampleAdd;

                float3 rgb = adjustTexture(tex.rgb);
                rgb *= IN.color.rgb;

                float dLeft = p.x - rectMin.x;
                float dRight = rectMax.x - p.x;
                float dBottom = p.y - rectMin.y;
                float dTop = rectMax.y - p.y;

                float insideDist = max(-dist, 0.0);

                float innerAO = oneSidedFade(insideDist, _InnerShadowWidthPx);
                innerAO *= _InnerShadowStrength;

                float topDark = oneSidedFade(dTop, _TopDarkWidthPx) * _TopDarkStrength;
                float bottomDark = oneSidedFade(dBottom, _BottomDarkWidthPx) * _BottomDarkStrength;
                float rightDark = oneSidedFade(dRight, _RightDarkWidthPx) * _RightDarkStrength;

                float darken = saturate(innerAO + topDark + bottomDark + rightDark);
                rgb *= 1.0 - darken;

                float leftLight = oneSidedFade(dLeft, _LeftLightWidthPx) * _LeftLightStrength;
                rgb += _HighlightColor.rgb * leftLight * _HighlightColor.a;

                float topHighlight = edgeBand(dTop, _TopHighlightOffsetPx, _TopHighlightWidthPx, aa);
                topHighlight *= _TopHighlightStrength;
                rgb += _HighlightColor.rgb * topHighlight * _HighlightColor.a;

                rgb = applyGreyscaleLock(rgb);

                float alpha = panelAlpha * tex.a * IN.color.a;

                float shadowAlpha = outerShadow * _OuterShadowColor.a * IN.color.a;
                float outAlpha = saturate(alpha + shadowAlpha * (1.0 - alpha));

                float3 outRgb = rgb;

                if (outAlpha > 0.0001)
                {
                    float3 shadowRgb = _OuterShadowColor.rgb;
                    outRgb = (rgb * alpha + shadowRgb * shadowAlpha * (1.0 - alpha)) / outAlpha;
                }

                fixed4 result = fixed4(outRgb, outAlpha);

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