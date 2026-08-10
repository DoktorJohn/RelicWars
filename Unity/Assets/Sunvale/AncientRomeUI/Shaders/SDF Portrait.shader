Shader "UI/Oval SDF Portrait Frame Overlap"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _TextureSampleAdd ("Texture Sample Add", Vector) = (0,0,0,0)

        _UvRect ("UV Rect xy=min zw=size", Vector) = (0,0,1,1)
        _RectSize ("Rect Size Pixels", Vector) = (128,128,0,0)

        _Inset ("Inset / Safe Padding Pixels", Range(0,128)) = 16
        _StrokeWidth ("Gold Edge Width Pixels", Range(0,64)) = 10
        _Feather ("SDF Feather Pixels", Range(0.25,4)) = 1
        _BevelWidth ("Bevel Width Pixels", Range(0,16)) = 3

        _TextureScale ("Texture Scale / Zoom XY", Vector) = (1,1,0,0)
        _TextureOffset ("Texture Offset XY", Vector) = (0,0,0,0)

        _BackgroundTop ("Background Top", Color) = (0.72, 0.52, 0.25, 1)
        _BackgroundBottom ("Background Bottom", Color) = (0.20, 0.12, 0.05, 1)

        _GoldLight ("Gold Light", Color) = (1.0, 0.82, 0.36, 1)
        _GoldMid ("Gold Mid", Color) = (0.77, 0.50, 0.16, 1)
        _GoldDark ("Gold Dark", Color) = (0.26, 0.14, 0.04, 1)

        _ShadowColor ("Shadow Color", Color) = (0,0,0,0.45)
        _ShadowOffset ("Shadow Offset Pixels", Vector) = (0,-4,0,0)
        _ShadowSoftness ("Shadow Softness Pixels", Range(0,32)) = 6

        _PortraitOverlapStart ("Portrait Overlap Source Y Start", Range(0,1)) = 0.66
        _PortraitOverlapFeather ("Portrait Overlap Source Y Feather", Range(0.001,0.25)) = 0.03
        _PortraitOverlapOutset ("Portrait Overlap Outset Pixels", Range(0,96)) = 14
        _PortraitOverlapRegionY ("Portrait Overlap Screen Y Start", Range(0,1)) = 0.56
        _PortraitOverlapRegionFeather ("Portrait Overlap Screen Y Feather", Range(0.001,0.25)) = 0.05
        _PortraitOverlapOpacity ("Portrait Overlap Opacity", Range(0,1)) = 1
        _PortraitOverlapInnerCover ("Portrait Overlap Inner Cover Pixels", Range(0,24)) = 5

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
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 uv            : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                half4 mask           : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _TextureSampleAdd;

            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            float4 _UvRect;
            float4 _RectSize;

            float _Inset;
            float _StrokeWidth;
            float _Feather;
            float _BevelWidth;

            float4 _TextureScale;
            float4 _TextureOffset;

            fixed4 _BackgroundTop;
            fixed4 _BackgroundBottom;

            fixed4 _GoldLight;
            fixed4 _GoldMid;
            fixed4 _GoldDark;

            fixed4 _ShadowColor;
            float4 _ShadowOffset;
            float _ShadowSoftness;

            float _PortraitOverlapStart;
            float _PortraitOverlapFeather;
            float _PortraitOverlapOutset;
            float _PortraitOverlapRegionY;
            float _PortraitOverlapRegionFeather;
            float _PortraitOverlapOpacity;
            float _PortraitOverlapInnerCover;

            float EllipseSDFApprox(float2 p, float2 r)
            {
                r = max(r, float2(1.0, 1.0));

                float k = length(p / r);

                // Negative = inside ellipse.
                // Positive = outside ellipse.
                return (k - 1.0) * min(r.x, r.y);
            }

            float4 AlphaOver(float4 under, float4 over)
            {
                float a = over.a + under.a * (1.0 - over.a);

                float3 rgb =
                    (over.rgb * over.a + under.rgb * under.a * (1.0 - over.a))
                    / max(a, 1e-5);

                return float4(rgb, a);
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;

                // Standard uGUI RectMask2D support.
                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);

                float2 pixelSize = float2(OUT.vertex.w, OUT.vertex.w);
                pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                OUT.mask = half4(
                    v.vertex.xy * 2.0 - clampedRect.xy - clampedRect.zw,
                    0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy))
                );

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uvSize = max(abs(_UvRect.zw), float2(1e-5, 1e-5));
                float2 frameUV = (IN.uv - _UvRect.xy) / uvSize;

                float2 rectSize = max(_RectSize.xy, float2(1.0, 1.0));
                float2 p = (frameUV - 0.5) * rectSize;

                float feather = max(_Feather, 0.001);
                float inset = max(_Inset, 0.0);

                // Outer oval is pushed inward so the shadow and overlap have room inside the Graphic quad.
                float2 outerR = max(rectSize * 0.5 - inset, float2(1.0, 1.0));

                float stroke = clamp(
                    _StrokeWidth,
                    0.001,
                    min(outerR.x, outerR.y) - 0.001
                );

                float2 innerR = max(outerR - stroke, float2(1.0, 1.0));

                float dOuter = EllipseSDFApprox(p, outerR);
                float dInner = EllipseSDFApprox(p, innerR);

                float outerMask = 1.0 - smoothstep(0.0, feather, dOuter);
                float innerMask = 1.0 - smoothstep(0.0, feather, dInner);

                float ringMask = saturate(outerMask - innerMask);
                float contentMask = innerMask;

                // ------------------------------------------------------------
                // Shadow
                // ------------------------------------------------------------

                float shadowSoft = max(_ShadowSoftness, 0.001);
                float dShadow = EllipseSDFApprox(p - _ShadowOffset.xy, outerR);
                float shadowMask = 1.0 - smoothstep(-shadowSoft, shadowSoft, dShadow);

                float4 shadowCol = float4(
                    _ShadowColor.rgb,
                    shadowMask * _ShadowColor.a * IN.color.a
                );

                // ------------------------------------------------------------
                // Background gradient
                // ------------------------------------------------------------

                // Inner oval local UV, independent from portrait pan/zoom.
                float2 innerUV = p / (innerR * 2.0) + 0.5;

                float bgT = saturate(innerUV.y);
                float4 bgCol = lerp(_BackgroundBottom, _BackgroundTop, bgT);
                bgCol.a *= contentMask * IN.color.a;

                // ------------------------------------------------------------
                // Portrait texture sampling
                // ------------------------------------------------------------

                float2 texScale = max(_TextureScale.xy, float2(0.001, 0.001));
                float2 texOffset = _TextureOffset.xy;

                // Positive TextureOffset moves the visible portrait right/up.
                // TextureScale > 1 zooms in.
                float2 portraitUV = (innerUV - 0.5) / texScale + 0.5 - texOffset;

                // Prevent atlas bleeding without smearing edge pixels visibly.
                float uvInBounds =
                    step(0.0, portraitUV.x) *
                    step(0.0, portraitUV.y) *
                    step(portraitUV.x, 1.0) *
                    step(portraitUV.y, 1.0);

                float2 clampedPortraitUV = clamp(portraitUV, float2(0.001, 0.001), float2(0.999, 0.999));
                float2 sampleUV = _UvRect.xy + clampedPortraitUV * _UvRect.zw;

                float4 texRaw = (tex2D(_MainTex, sampleUV) + _TextureSampleAdd) * IN.color;
                texRaw.a *= uvInBounds;

                // Base portrait is clipped to the inner oval and drawn under the frame.
                float4 portraitInsideCol = texRaw;
                portraitInsideCol.a *= contentMask;

                // ------------------------------------------------------------
                // Gold frame
                // ------------------------------------------------------------

                float2 n = normalize(float2(
                    p.x / max(outerR.x * outerR.x, 1.0),
                    p.y / max(outerR.y * outerR.y, 1.0)
                ) + float2(0.0001, 0.0001));

                float topGradient = saturate((p.y / max(outerR.y, 1.0)) * 0.5 + 0.5);
                float lightFromTop = saturate(dot(n, normalize(float2(-0.2, 1.0))) * 0.5 + 0.5);

                float3 gold = lerp(
                    _GoldDark.rgb,
                    _GoldMid.rgb,
                    saturate(0.22 + topGradient * 0.45 + lightFromTop * 0.25)
                );

                gold = lerp(
                    gold,
                    _GoldLight.rgb,
                    saturate(topGradient * 0.24 + lightFromTop * 0.20)
                );

                float bevelWidth = max(_BevelWidth, 0.001);

                float outerHighlight =
                    (1.0 - smoothstep(0.0, bevelWidth, abs(dOuter))) *
                    saturate(topGradient * 1.35);

                float innerDark =
                    (1.0 - smoothstep(0.0, bevelWidth, abs(dInner))) *
                    saturate((1.0 - topGradient) * 1.25);

                float bottomShade = saturate(1.0 - topGradient);

                float edgeLine =
                    1.0 - smoothstep(
                        0.0,
                        1.25,
                        min(abs(dOuter), abs(dInner))
                    );

                gold = lerp(gold, _GoldLight.rgb, outerHighlight * 0.55);

                gold = lerp(
                    gold,
                    _GoldDark.rgb * 0.7,
                    saturate(innerDark * 0.45 + bottomShade * 0.18 + edgeLine * 0.15)
                );

                float4 frameCol = float4(gold, ringMask * IN.color.a);

                // ------------------------------------------------------------
                // Portrait overlap layer
                // ------------------------------------------------------------

                // Only source pixels above this portrait UV Y are allowed to overlap.
                // Example: 0.66 means top 34% of the portrait texture can break out.
                float overlapFromPortraitY = smoothstep(
                    _PortraitOverlapStart - _PortraitOverlapFeather,
                    _PortraitOverlapStart + _PortraitOverlapFeather,
                    portraitUV.y
                );

                // Only overlap outside the inner oval, so the layer affects the frame/top breakout
                // instead of redrawing the whole portrait on top.
                float overlapFrameCover = smoothstep(
    -_PortraitOverlapInnerCover,
    feather,
    dInner
);

                // Expanded oval controls how far outside the frame the portrait can exist.
                float2 overlapR = outerR + _PortraitOverlapOutset;
                float dOverlap = EllipseSDFApprox(p, overlapR);
                float overlapShapeMask = 1.0 - smoothstep(0.0, feather, dOverlap);

                // Screen-space top-only gate.
                float overlapTopRegion = smoothstep(
                    _PortraitOverlapRegionY - _PortraitOverlapRegionFeather,
                    _PortraitOverlapRegionY + _PortraitOverlapRegionFeather,
                    frameUV.y
                );

                float overlapMask =
    overlapFrameCover *
    overlapShapeMask *
    overlapFromPortraitY *
    overlapTopRegion *
    _PortraitOverlapOpacity;

                float4 portraitOverlapCol = texRaw;
                portraitOverlapCol.a *= overlapMask;

                // ------------------------------------------------------------
                // Compose
                // ------------------------------------------------------------

                float4 col = shadowCol;
                col = AlphaOver(col, bgCol);
                col = AlphaOver(col, portraitInsideCol);
                col = AlphaOver(col, frameCol);
                col = AlphaOver(col, portraitOverlapCol);

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                col.a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}