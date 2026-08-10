Shader "UI/SDF/Roman Button Middle Plus"
{
    Properties
    {
        [PerRendererData] _MainTex ("Icon Sprite From Image", 2D) = "white" {}
        _BgTex ("Background Texture", 2D) = "white" {}

        _Color ("UI Tint", Color) = (1,1,1,1)

        // Kept for material compatibility, but disabled by default.
        // This prevents old/red material tint values from corrupting green/blue/orange authored textures.
        _BgTint ("Background Tint", Color) = (1,1,1,1)
        _BgTintStrength ("Background Tint Strength", Range(0,1)) = 0

        _IconTint ("Icon Tint", Color) = (1,1,1,1)
        _IconOpacity ("Icon Opacity", Range(0,1)) = 1

        _CircleRadius ("Circle Radius", Range(0.1,0.707)) = 0.48
        _CircleFeather ("Circle Edge Feather", Range(0.0005,0.05)) = 0.006
        _Aspect ("Aspect Width / Height", Range(0.25,4)) = 1

        _BgScale ("Background Scale", Range(0.25,4)) = 1
        _BgOffsetX ("Background Offset X", Range(-1,1)) = 0
        _BgOffsetY ("Background Offset Y", Range(-1,1)) = 0

        // Brightness is exposure-like instead of additive.
        // 0 = unchanged, +1 = about 2x brighter, -1 = about 0.5x darker.
        _BgBrightness ("Background Brightness / Exposure", Range(-1,1)) = 0

        // Contrast is applied through luminance so it does not push orange toward red or colored textures toward black.
        _BgContrast ("Background Contrast", Range(0,2)) = 1

        _EdgeDarkness ("Edge Darkness", Range(0,1)) = 0.55
        _EdgeWidth ("Edge Dark Width", Range(0.001,0.5)) = 0.18
        _EdgePower ("Edge Dark Power", Range(0.1,8)) = 2.2

        _TopBevelInset ("Top Bevel Inset", Range(0.01,0.35)) = 0.09
        _TopBevelSharpness ("Top Bevel Sharpness", Range(0.001,0.1)) = 0.018
        _TopBevelDarkness ("Top Bevel Darkness", Range(0,1)) = 0.22
        _TopBevelHighlight ("Top Bevel Highlight", Range(0,1)) = 0.12
        _TopBevelBias ("Top Bevel Bias", Range(0.2,8)) = 2.5

        // Optional overlay texture for cracks / scratches / creases.
        [Toggle] _OverlayEnabled ("Use Overlay Texture", Float) = 0
        _OverlayTex ("Overlay Texture", 2D) = "white" {}
        _OverlayTint ("Overlay Tint", Color) = (1,1,1,1)
        _OverlayOpacity ("Overlay Opacity", Range(0,1)) = 0
        _OverlayScale ("Overlay Scale", Range(0.25,8)) = 1
        _OverlayOffsetX ("Overlay Offset X", Range(-2,2)) = 0
        _OverlayOffsetY ("Overlay Offset Y", Range(-2,2)) = 0

        _IconSaturation ("Icon Saturation", Range(0,2)) = 1
        _IconBrightness ("Icon Brightness", Range(0,2)) = 1

        // Icon stroke / outline.
        _IconStrokeColor ("Icon Stroke Color", Color) = (0.18,0.10,0.03,1)
        _IconStrokeSize ("Icon Stroke Size (Texels)", Range(0,8)) = 1.5
        _IconStrokeOpacity ("Icon Stroke Opacity", Range(0,1)) = 1

        // Icon drop shadow.
        _IconShadowColor ("Icon Shadow Color", Color) = (0,0,0,1)
        _IconShadowOpacity ("Icon Shadow Opacity", Range(0,1)) = 0.45
        _IconShadowOffsetX ("Icon Shadow Offset X (Texels)", Range(-16,16)) = 1
        _IconShadowOffsetY ("Icon Shadow Offset Y (Texels)", Range(-16,16)) = -1
        _IconShadowSoftness ("Icon Shadow Softness (Texels)", Range(0,8)) = 1.25

        // Manual icon transform. Animate these from C# if needed.
        _IconScale ("Icon Scale", Range(0.2,2.5)) = 1
        _IconOffsetX ("Icon Offset X", Range(-0.35,0.35)) = 0
        _IconOffsetY ("Icon Offset Y", Range(-0.35,0.35)) = 0

        // Cooldown radial overlay. Progress 0 = fully dark, 1 = fully revealed/ready.
        [Toggle] _CooldownEnabled ("Use Cooldown Overlay", Float) = 0
        _CooldownProgress ("Cooldown Progress", Range(0,1)) = 1
        _CooldownColor ("Cooldown Dark Color", Color) = (0,0,0,1)
        _CooldownOpacity ("Cooldown Darkness", Range(0,1)) = 0.65
        _CooldownStartAngle ("Cooldown Start Angle Degrees", Range(-180,180)) = 90
        [Toggle] _CooldownClockwise ("Cooldown Clockwise", Float) = 1
        _CooldownFeather ("Cooldown Fill Feather", Range(0.0001,0.05)) = 0.006

        // C#-driven rolling shine band. Angle rotates the band; position moves it across the button.
        [Toggle] _SweepHighlightEnabled ("Use Rolling Shine Band", Float) = 0
        _SweepHighlightAngle ("Rolling Shine Band Angle Degrees", Range(-180,180)) = 35
        _SweepHighlightPosition ("Rolling Shine Band Position", Range(-1.5,1.5)) = -1.2
        _SweepHighlightWidth ("Rolling Shine Band Width", Range(0.005,1)) = 0.18
        _SweepHighlightFeather ("Rolling Shine Band Feather", Range(0.001,1)) = 0.12
        _SweepHighlightColor ("Rolling Shine Band Color", Color) = (1,0.92,0.75,1)
        _SweepHighlightOpacity ("Rolling Shine Band Opacity", Range(0,1)) = 0

        // Disabled/unavailable visual state. Applied over the finished button so icon, background, bevels,
        // cooldown, and shine all become grayscale together.
        [Toggle] _GreyscaleDisabled ("Greyscale Disabled", Float) = 0
        _GreyscaleDisabledDarkness ("Greyscale Disabled Darkness", Range(0,1)) = 0.45

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
            "CanUseSpriteAtlas"="False"
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
            Name "RomanButtonMiddle"

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
                float4 vertex : POSITION;
                fixed4 color : COLOR;
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
            sampler2D _BgTex;
            sampler2D _OverlayTex;

            float4 _MainTex_TexelSize;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            fixed4 _Color;
            fixed4 _BgTint;
            float _BgTintStrength;
            fixed4 _IconTint;
            float _IconOpacity;
            float _IconSaturation;
            float _IconBrightness;

            float _CircleRadius;
            float _CircleFeather;
            float _Aspect;

            float _BgScale;
            float _BgOffsetX;
            float _BgOffsetY;
            float _BgBrightness;
            float _BgContrast;

            float _EdgeDarkness;
            float _EdgeWidth;
            float _EdgePower;

            float _TopBevelInset;
            float _TopBevelSharpness;
            float _TopBevelDarkness;
            float _TopBevelHighlight;
            float _TopBevelBias;

            float _OverlayEnabled;
            fixed4 _OverlayTint;
            float _OverlayOpacity;
            float _OverlayScale;
            float _OverlayOffsetX;
            float _OverlayOffsetY;

            fixed4 _IconStrokeColor;
            float _IconStrokeSize;
            float _IconStrokeOpacity;

            fixed4 _IconShadowColor;
            float _IconShadowOpacity;
            float _IconShadowOffsetX;
            float _IconShadowOffsetY;
            float _IconShadowSoftness;

            float _IconScale;
            float _IconOffsetX;
            float _IconOffsetY;

            float _CooldownEnabled;
            float _CooldownProgress;
            fixed4 _CooldownColor;
            float _CooldownOpacity;
            float _CooldownStartAngle;
            float _CooldownClockwise;
            float _CooldownFeather;

            float _SweepHighlightEnabled;
            float _SweepHighlightAngle;
            float _SweepHighlightPosition;
            float _SweepHighlightWidth;
            float _SweepHighlightFeather;
            fixed4 _SweepHighlightColor;
            float _SweepHighlightOpacity;

            float _GreyscaleDisabled;
            float _GreyscaleDisabledDarkness;

            static const float3 LUMA = float3(0.299, 0.587, 0.114);
            static const float PI = 3.14159265;
            static const float TAU = 6.28318530;
            static const float DEG2RAD = 0.0174532925;

            v2f vert(appdata_t v)
            {
                v2f OUT;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.uv = v.texcoord;
                OUT.color = v.color * _Color;

                return OUT;
            }

            float InBounds01(float2 uv)
            {
                return step(0.0, uv.x) *
                    step(0.0, uv.y) *
                    step(uv.x, 1.0) *
                    step(uv.y, 1.0);
            }

            float SampleIconAlphaRaw(float2 uv)
            {
                return tex2D(_MainTex, uv).a * InBounds01(uv);
            }

            float SampleIconAlphaSoft(float2 uv, float2 r)
            {
                float a = 0.0;

                a += SampleIconAlphaRaw(uv);
                a += SampleIconAlphaRaw(uv + float2(r.x, 0.0));
                a += SampleIconAlphaRaw(uv + float2(-r.x, 0.0));
                a += SampleIconAlphaRaw(uv + float2(0.0, r.y));
                a += SampleIconAlphaRaw(uv + float2(0.0, -r.y));
                a += SampleIconAlphaRaw(uv + float2(r.x, r.y));
                a += SampleIconAlphaRaw(uv + float2(-r.x, r.y));
                a += SampleIconAlphaRaw(uv + float2(r.x, -r.y));
                a += SampleIconAlphaRaw(uv + float2(-r.x, -r.y));

                return a / 9.0;
            }

            float ComputeOutlineAlpha(float2 uv, float baseAlpha, float sizeTexels)
            {
                float2 r = _MainTex_TexelSize.xy * max(sizeTexels, 0.0);

                float maxA = 0.0;
                maxA = max(maxA, SampleIconAlphaRaw(uv + float2(r.x, 0.0)));
                maxA = max(maxA, SampleIconAlphaRaw(uv + float2(-r.x, 0.0)));
                maxA = max(maxA, SampleIconAlphaRaw(uv + float2(0.0, r.y)));
                maxA = max(maxA, SampleIconAlphaRaw(uv + float2(0.0, -r.y)));
                maxA = max(maxA, SampleIconAlphaRaw(uv + float2(r.x, r.y)));
                maxA = max(maxA, SampleIconAlphaRaw(uv + float2(-r.x, r.y)));
                maxA = max(maxA, SampleIconAlphaRaw(uv + float2(r.x, -r.y)));
                maxA = max(maxA, SampleIconAlphaRaw(uv + float2(-r.x, -r.y)));

                return saturate(maxA - baseAlpha);
            }

            fixed4 AlphaOver(fixed4 bottom, fixed4 top)
            {
                fixed outA = top.a + bottom.a * (1.0 - top.a);
                fixed3 outRGB = (top.rgb * top.a + bottom.rgb * bottom.a * (1.0 - top.a)) / max(outA, 1e-5);
                return fixed4(outRGB, outA);
            }

            float3 ApplyOptionalTint(float3 color, float3 tint, float tintStrength)
            {
                tintStrength = saturate(tintStrength);
                return lerp(color, color * tint, tintStrength);
            }

            float3 ApplyBackgroundBrightnessContrast(float3 color, float brightness, float contrast)
            {
                color = max(color, 0.0);

                // Exposure-style brightness instead of additive brightness.
                // This keeps color ratios stable, so +0.1 does not suddenly wash everything toward white.
                color *= exp2(brightness);

                // Hue-preserving contrast. We adjust luminance, then scale the original RGB by that luminance ratio.
                // This is much safer for saturated authored textures than per-channel contrast around 0.5.
                float oldLuma = max(dot(color, LUMA), 1e-5);
                float newLuma = (oldLuma - 0.5) * contrast + 0.5;
                newLuma = saturate(newLuma);

                color *= newLuma / oldLuma;
                return saturate(color);
            }

            fixed3 AdjustSaturationBrightness(fixed3 color, float saturation, float brightness)
            {
                float luminance = dot(color, LUMA);
                fixed3 grayscale = luminance.xxx;

                color = lerp(grayscale, color, saturation);
                color *= brightness;

                return saturate(color);
            }

            float PositiveFrac(float x)
            {
                return frac(x + 4.0);
            }

            float Angle01FromStart(float2 p, float startAngleDegrees, float clockwise)
            {
                float angle = atan2(p.y, p.x);
                float start = startAngleDegrees * DEG2RAD;

                float deltaCCW = angle - start;
                float deltaCW = start - angle;
                float delta = lerp(deltaCCW, deltaCW, step(0.5, clockwise));

                return PositiveFrac(delta / TAU);
            }

            float ComputeCooldownMask(float2 aspectP, float circleMask)
            {
                float progress = saturate(_CooldownProgress);
                float angle01 = Angle01FromStart(aspectP, _CooldownStartAngle, _CooldownClockwise);

                // 0 = no reveal, 1 = fully revealed. Feather is normalized around the radial cutoff edge.
                float feather = max(_CooldownFeather, 0.0001);
                float reveal = 1.0 - smoothstep(progress - feather, progress + feather, angle01);

                // Avoid a small lit seam when progress is exactly 0.
                reveal *= step(0.0001, progress);

                float darkMask = 1.0 - reveal;
                return darkMask * circleMask;
            }

            float ComputeSweepHighlight(float2 aspectP, float circleMask)
            {
                // Classic UI shine sweep: a straight, full-width band clipped by the circular button.
                // _SweepHighlightAngle rotates the band itself.
                // _SweepHighlightPosition moves the band along its perpendicular travel axis.
                // Position is normalized to the circle radius: roughly -1 = one edge, 0 = center, +1 = opposite edge.
                float angle = _SweepHighlightAngle * DEG2RAD;
                float2 bandDir = float2(cos(angle), sin(angle));
                float2 travelDir = float2(-bandDir.y, bandDir.x);

                float radius = max(_CircleRadius, 0.0001);
                float signedPos = dot(aspectP, travelDir) / radius;
                float distToBand = abs(signedPos - _SweepHighlightPosition);

                float halfWidth = max(_SweepHighlightWidth * 0.5, 0.0001);
                float feather = max(_SweepHighlightFeather, 0.0001);
                float band = 1.0 - smoothstep(halfWidth, halfWidth + feather, distToBand);

                // Slightly stronger center, softer shoulders. Keeps it looking like a glossy strip rather than a flat rectangle.
                float center = 1.0 - smoothstep(0.0, halfWidth, distToBand);
                band = saturate(band * (0.55 + center * 0.45));

                return band * circleMask;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.uv;

                // Circle SDF.
                float2 p = uv - 0.5;
                p.x *= _Aspect;

                float dist = length(p);
                float sdf = dist - _CircleRadius;
                float circleMask = 1.0 - smoothstep(-_CircleFeather, _CircleFeather, sdf);

                // Background texture.
                float2 bgUV = (uv - 0.5) * _BgScale + 0.5;
                bgUV += float2(_BgOffsetX, _BgOffsetY);

                float4 bgSample = tex2D(_BgTex, bgUV);
                float4 bg = bgSample;

                // Do not multiply authored background texture by _BgTint unless explicitly requested.
                bg.rgb = ApplyOptionalTint(bg.rgb, _BgTint.rgb, _BgTintStrength);
                bg.a *= lerp(1.0, _BgTint.a, saturate(_BgTintStrength));

                bg.rgb = ApplyBackgroundBrightnessContrast(bg.rgb, _BgBrightness, _BgContrast);

                // Optional overlay texture: scratches / creases / grunge.
                if (_OverlayEnabled > 0.5 && _OverlayOpacity > 0.001)
                {
                    float2 ovUV = (uv - 0.5) * _OverlayScale + 0.5;
                    ovUV += float2(_OverlayOffsetX, _OverlayOffsetY);

                    float4 overlay = tex2D(_OverlayTex, ovUV) * _OverlayTint;

                    // Multiply-style dark detail overlay. Best with grayscale scratch texture.
                    float overlayAmount = saturate(_OverlayOpacity * overlay.a);
                    float3 overlayMul = bg.rgb * overlay.rgb;
                    bg.rgb = lerp(bg.rgb, overlayMul, overlayAmount);
                }

                // Edge darkening.
                float inward = saturate((_CircleRadius - dist) / max(_EdgeWidth, 0.0001));
                float edgeRing = pow(1.0 - inward, _EdgePower);
                bg.rgb *= 1.0 - edgeRing * _EdgeDarkness;

                // Top bevel.
                float top01 = saturate((p.y / max(_CircleRadius, 0.0001)) * 0.5 + 0.5);
                float topWeight = pow(top01, _TopBevelBias);

                float bevelCenter = _CircleRadius - _TopBevelInset;

                float outerTop = smoothstep(
                    bevelCenter - _TopBevelSharpness,
                    bevelCenter + _TopBevelSharpness,
                    dist
                );

                float bevelLine = 1.0 - smoothstep(
                    0.0,
                    _TopBevelSharpness,
                    abs(dist - bevelCenter)
                );

                bg.rgb *= 1.0 - outerTop * topWeight * _TopBevelDarkness;
                bg.rgb += bevelLine * topWeight * _TopBevelHighlight;
                bg.rgb = saturate(bg.rgb);

                // Icon UV transform. Auto animation was intentionally removed; drive these from C#.
                float iconScale = max(0.001, _IconScale);
                float2 iconOffset = float2(_IconOffsetX, _IconOffsetY);
                float2 iconUV = (uv - 0.5) / iconScale + 0.5 - iconOffset;

                // Icon main sample.
                fixed4 icon = (tex2D(_MainTex, iconUV) + _TextureSampleAdd) * _IconTint;

                icon.rgb = AdjustSaturationBrightness(
                    icon.rgb,
                    _IconSaturation,
                    _IconBrightness
                );

                float baseIconAlpha = SampleIconAlphaRaw(iconUV);
                icon.a = baseIconAlpha * _IconTint.a * _IconOpacity;

                // Stroke / outline.
                float outlineAlpha = ComputeOutlineAlpha(iconUV, baseIconAlpha, _IconStrokeSize);
                outlineAlpha *= _IconStrokeOpacity;
                outlineAlpha *= (1.0 - baseIconAlpha);

                fixed4 strokeLayer = _IconStrokeColor;
                strokeLayer.a *= outlineAlpha;

                // Drop shadow.
                float2 shadowOffsetUV = float2(_IconShadowOffsetX, _IconShadowOffsetY) * _MainTex_TexelSize.xy;
                float2 shadowSoftUV = max(_IconShadowSoftness, 0.001) * _MainTex_TexelSize.xy;

                float shadowAlpha = SampleIconAlphaSoft(iconUV - shadowOffsetUV, shadowSoftUV);
                shadowAlpha *= _IconShadowOpacity;
                shadowAlpha *= (1.0 - saturate(baseIconAlpha + outlineAlpha));

                fixed4 shadowLayer = _IconShadowColor;
                shadowLayer.a *= shadowAlpha;

                // Compose: bg -> shadow -> stroke -> icon.
                fixed4 col = fixed4(bg.rgb, bg.a);
                col = AlphaOver(col, shadowLayer);
                col = AlphaOver(col, strokeLayer);
                col = AlphaOver(col, icon);

                // Cooldown darkening layer. Drawn over the whole composed button core.
                if (_CooldownEnabled > 0.5 && _CooldownOpacity > 0.001)
                {
                    float cooldownMask = ComputeCooldownMask(p, circleMask);
                    fixed4 cooldownLayer = _CooldownColor;
                    cooldownLayer.a *= cooldownMask * _CooldownOpacity;
                    col = AlphaOver(col, cooldownLayer);
                }

                // Rolling shine band. C# should animate _SweepHighlightPosition and/or _SweepHighlightOpacity.
                if (_SweepHighlightEnabled > 0.5 && _SweepHighlightOpacity > 0.001)
                {
                    float sweepMask = ComputeSweepHighlight(p, circleMask);
                    float sweepAmount = sweepMask * _SweepHighlightOpacity * _SweepHighlightColor.a;
                    col.rgb = saturate(col.rgb + _SweepHighlightColor.rgb * sweepAmount);
                }

                // Disabled/unavailable state. Applied late so the whole button reads as disabled,
                // including icon, background, cooldown overlay, and any active shine.
                if (_GreyscaleDisabled > 0.5)
                {
                    float gray = dot(col.rgb, LUMA);
                    col.rgb = gray.xxx * (1.0 - saturate(_GreyscaleDisabledDarkness));
                }

                col *= IN.color;
                col.a *= circleMask;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
