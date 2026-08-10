Shader "UI/HSV_With_TopShadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(HSV Settings)]
        _HsvShift("Hue Shift", Range(0, 360)) = 0
        _HsvSaturation("Saturation", Range(0, 2)) = 1
        _HsvBright("Brightness", Range(0, 2)) = 1

        [Header(Pressed Shadow Settings)]
        _ShadowLength("Shadow Length (%)", Range(0, 1)) = 0.2
        // Increased max intensity to 3 so you can force it to pure black even on sliced sprites
        _ShadowIntensity("Shadow Intensity", Range(0, 3)) = 1.5 
        // Added Power curve. >1 makes the shadow get darker faster towards the top edge.
        _ShadowPower("Shadow Curve (Power)", Range(0.1, 5)) = 1.0 

        // Required for UI Masking
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
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            // HSV
            half _HsvShift;
            half _HsvSaturation;
            half _HsvBright;

            // Shadow
            half _ShadowLength;
            half _ShadowIntensity;
            half _ShadowPower; // New Variable

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 col = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                // --- 1. HSV MATH ---
                half3 resultHsv = half3(col.rgb);
                half cosHsv = _HsvBright * _HsvSaturation * cos(_HsvShift * 3.14159265 / 180.0);
                half sinHsv = _HsvBright * _HsvSaturation * sin(_HsvShift * 3.14159265 / 180.0);
                
                resultHsv.r = (.299 * _HsvBright + .701 * cosHsv + .168 * sinHsv) * col.r
                            + (.587 * _HsvBright - .587 * cosHsv + .330 * sinHsv) * col.g
                            + (.114 * _HsvBright - .114 * cosHsv - .497 * sinHsv) * col.b;
                            
                resultHsv.g = (.299 * _HsvBright - .299 * cosHsv - .328 * sinHsv) * col.r
                            + (.587 * _HsvBright + .413 * cosHsv + .035 * sinHsv) * col.g
                            + (.114 * _HsvBright - .114 * cosHsv + .292 * sinHsv) * col.b;
                            
                resultHsv.b = (.299 * _HsvBright - .300 * cosHsv + 1.250 * sinHsv) * col.r
                            + (.587 * _HsvBright - .588 * cosHsv - 1.050 * sinHsv) * col.g
                            + (.114 * _HsvBright + .886 * cosHsv - .203 * sinHsv) * col.b;
                            
                col.rgb = resultHsv;

                // --- 2. TOP SHADOW MATH ---
                float shadowBottomY = 1.0 - _ShadowLength;
                float shadowFactor = saturate((IN.texcoord.y - shadowBottomY) / max(_ShadowLength, 0.0001));
                
                // NEW: Apply a power curve. This allows the shadow to "ramp up" to black much faster.
                shadowFactor = pow(shadowFactor, _ShadowPower);
                
                // NEW: Added saturate() to the multiplier. Because we allowed _ShadowIntensity 
                // to go above 1.0, we need to ensure the multiplier doesn't drop below 0.0, 
                // which would cause weird inverted color glitches.
                col.rgb *= saturate(1.0 - (shadowFactor * _ShadowIntensity));


                // --- 3. STANDARD UI CLIPPING ---
                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}