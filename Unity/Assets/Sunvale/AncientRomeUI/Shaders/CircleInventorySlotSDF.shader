Shader "UI/CircleInventorySlotSDF_FrameUnderItemPopout"
{
    Properties
    {
        [PerRendererData] _MainTex ("Item Atlas", 2D) = "white" {}
        _Color ("UI Tint", Color) = (1,1,1,1)

        // Item sprite sampling.
        _ItemAtlasRect ("Item Atlas Rect XYWH", Vector) = (0,0,1,1)
        _ItemAspectScale ("Item Aspect Scale XY", Vector) = (1,1,0,0)
        _ItemOffset ("Item Offset XY", Vector) = (0,0,0,0)
        _ItemScale ("Item Scale", Range(0.1, 3)) = 1.0
        _ItemOpacity ("Item Opacity", Range(0, 1)) = 1.0

        _ItemBrightness ("Item Brightness", Range(0, 3)) = 1
        _ItemContrast ("Item Contrast", Range(0, 3)) = 1
        _ItemSaturation ("Item Saturation", Range(0, 3)) = 1

        // Item is circle-masked only below this Y.
        // 0 = bottom of the Image rect, 1 = top.
        // Example:
        // 0.50 masks only bottom half.
        // 0.65 masks more of the item.
        // 0.35 allows more item popout.
        _ItemLowerCircleMaskY ("Item Lower Circle Mask Y", Range(0, 1)) = 0.58
        _ItemLowerCircleMaskSoftness ("Item Lower Mask Softness", Range(0.001, 0.25)) = 0.045

        // Increase if the frame covers more of the circle edge.
        _ItemMaskInset ("Item Mask Inset", Range(0, 0.2)) = 0.020

        // Frame texture drawn under the item.
        _FrameTex ("Frame Texture", 2D) = "white" {}
        _FrameOpacity ("Frame Opacity", Range(0, 1)) = 1
        _FrameScale ("Frame Scale", Range(0.25, 2)) = 1
        _FrameOffset ("Frame Offset XY", Vector) = (0,0,0,0)
        _FrameBrightness ("Frame Brightness", Range(0, 3)) = 1
        _FrameContrast ("Frame Contrast", Range(0, 3)) = 1
        _FrameSaturation ("Frame Saturation", Range(0, 3)) = 1

        // Circular slot shape.
        _SlotRadius ("Slot Radius", Range(0.1, 0.5)) = 0.405
        _EdgeSoftness ("Edge Softness", Range(0.0001, 0.03)) = 0.004

        // Background.
        _BgEdgeColor ("Background Edge Color", Color) = (0.030, 0.018, 0.012, 1)
        _BgCenterColor ("Background Center Color", Color) = (0.145, 0.080, 0.042, 1)

        _CenterGlow ("Center Glow", Range(0, 2)) = 0.75
        _VignetteStrength ("Vignette Strength", Range(0, 2)) = 0.85

        // Procedural background noise.
        _NoiseSeed ("Noise Seed", Range(0, 1000)) = 0
        _BgNoiseScale ("Background Noise Scale", Range(2, 140)) = 34
        _BgNoiseStrength ("Background Noise Strength", Range(0, 0.5)) = 0.12

        _BgBrightness ("BG Brightness", Range(0, 3)) = 1
        _BgContrast ("BG Contrast", Range(0, 3)) = 1
        _BgSaturation ("BG Saturation", Range(0, 3)) = 1

        // Outer ambient shadow.
        _AmbientShadowColor ("Ambient Shadow Color", Color) = (0, 0, 0, 1)
        _AmbientShadowStrength ("Ambient Shadow Strength", Range(0, 1)) = 0.38
        _AmbientShadowRadius ("Ambient Shadow Radius", Range(0.05, 0.7)) = 0.43
        _AmbientShadowSoftness ("Ambient Shadow Softness", Range(0.01, 0.5)) = 0.12
        _AmbientShadowOffset ("Ambient Shadow Offset XY", Vector) = (0, -0.025, 0, 0)

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
            Name "CircleSlotFrameUnderItemPopout"

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
            sampler2D _FrameTex;

            fixed4 _Color;
            float4 _ClipRect;

            float4 _ItemAtlasRect;
            float4 _ItemAspectScale;
            float4 _ItemOffset;
            float _ItemScale;
            float _ItemOpacity;

            float _ItemBrightness;
            float _ItemContrast;
            float _ItemSaturation;

            float _ItemLowerCircleMaskY;
            float _ItemLowerCircleMaskSoftness;
            float _ItemMaskInset;

            float _FrameOpacity;
            float _FrameScale;
            float4 _FrameOffset;
            float _FrameBrightness;
            float _FrameContrast;
            float _FrameSaturation;

            float _SlotRadius;
            float _EdgeSoftness;

            fixed4 _BgEdgeColor;
            fixed4 _BgCenterColor;

            float _CenterGlow;
            float _VignetteStrength;

            float _NoiseSeed;
            float _BgNoiseScale;
            float _BgNoiseStrength;

            float _BgBrightness;
            float _BgContrast;
            float _BgSaturation;

            fixed4 _AmbientShadowColor;
            float _AmbientShadowStrength;
            float _AmbientShadowRadius;
            float _AmbientShadowSoftness;
            float4 _AmbientShadowOffset;

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

            float CircleSDF(float2 uv, float radius)
            {
                return length(uv - 0.5) - radius;
            }

            float CircleMask(float2 uv, float radius)
            {
                float d = CircleSDF(uv, radius);
                float aa = max(fwidth(d) * 1.2 + _EdgeSoftness, 0.0001);
                return smoothstep(aa, -aa, d);
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

            float RectMask(float2 uv)
            {
                return
                    step(0.0, uv.x) * step(uv.x, 1.0) *
                    step(0.0, uv.y) * step(uv.y, 1.0);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.uv;

                float slotMask = CircleMask(uv, _SlotRadius);
                float itemCircleMask = CircleMask(uv, max(_SlotRadius - _ItemMaskInset, 0.001));

                float2 centered = uv - 0.5;
                float dist = length(centered);

                float bgNoise = ProceduralNoise(
                    uv + float2(4.73, 8.19),
                    _BgNoiseScale,
                    _NoiseSeed + 37.0
                );

                // Circular background.
                float radial = saturate(1.0 - dist / max(_SlotRadius, 0.0001));
                radial = pow(radial, 1.55) * _CenterGlow;
                radial = saturate(radial + bgNoise * _BgNoiseStrength * 0.45);

                float3 bgCol = lerp(_BgEdgeColor.rgb, _BgCenterColor.rgb, radial);

                float vignette = saturate(dist / max(_SlotRadius, 0.0001));
                bgCol = lerp(
                    bgCol,
                    _BgEdgeColor.rgb,
                    pow(vignette, 2.2) * _VignetteStrength * 0.35
                );

                bgCol = ApplyValueNoise(bgCol, bgNoise, _BgNoiseStrength);
                bgCol = ApplyBCS(bgCol, _BgBrightness, _BgContrast, _BgSaturation);

                // Outer shadow.
                float2 shadowCenter = 0.5 + _AmbientShadowOffset.xy;
                float shadowDist = length(uv - shadowCenter);

                float shadowBlob =
                    1.0 - smoothstep(
                        _AmbientShadowRadius,
                        _AmbientShadowRadius + _AmbientShadowSoftness,
                        shadowDist
                    );

                float outerShadow = shadowBlob * (1.0 - slotMask) * _AmbientShadowStrength;

                // Frame texture.
                // This is not circle-clipped. Use transparent pixels in the frame texture.
                float safeFrameScale = max(_FrameScale, 0.001);
                float2 frameUV = (uv - 0.5 - _FrameOffset.xy) / safeFrameScale + 0.5;
                float inFrameUV = RectMask(frameUV);

                fixed4 frame = tex2D(_FrameTex, frameUV) * inFrameUV;
                frame.rgb = ApplyBCS(frame.rgb, _FrameBrightness, _FrameContrast, _FrameSaturation);
                float frameA = frame.a * _FrameOpacity;

                // Item atlas sampling.
                float safeItemScale = max(_ItemScale, 0.001);
                float2 itemLocalUV = (uv - 0.5 - _ItemOffset.xy) * _ItemAspectScale.xy / safeItemScale + 0.5;

                float inItemUV = RectMask(itemLocalUV);
                float2 itemAtlasUV = _ItemAtlasRect.xy + itemLocalUV * _ItemAtlasRect.zw;

                fixed4 item = tex2D(_MainTex, itemAtlasUV) * inItemUV;
                item.rgb = ApplyBCS(item.rgb, _ItemBrightness, _ItemContrast, _ItemSaturation);

                // Lower circle mask.
                // Below _ItemLowerCircleMaskY: item uses the circular mask.
                // Above _ItemLowerCircleMaskY: item is not circle-masked and can overlap the frame.
                float lowerMaskBlend =
                    1.0 - smoothstep(
                        _ItemLowerCircleMaskY - _ItemLowerCircleMaskSoftness,
                        _ItemLowerCircleMaskY + _ItemLowerCircleMaskSoftness,
                        uv.y
                    );

                float itemPopoutMask = lerp(1.0, itemCircleMask, lowerMaskBlend);
                float itemA = item.a * _ItemOpacity * itemPopoutMask;

                // Compose.
                float3 col = _AmbientShadowColor.rgb;

                // Background only inside the circle.
                col = lerp(col, bgCol, slotMask);

                // Frame under item.
                col = lerp(col, frame.rgb, frameA);

                // Item on top of frame.
                col = lerp(col, item.rgb, itemA);

                fixed4 result;
                result.rgb = col * IN.color.rgb;

                // Include frame and item alpha so popout can render outside the circle.
                result.a = saturate(slotMask + outerShadow + frameA + itemA) * IN.color.a;

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