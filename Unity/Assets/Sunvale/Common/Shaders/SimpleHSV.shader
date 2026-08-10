Shader "UI/Simple_HSV_Lighten"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        
        _HsvShift("Hue Shift", Range(0, 360)) = 0
        _HsvSaturation("Saturation", Range(0, 2)) = 1
        _HsvBright("Brightness", Range(0, 4)) = 1

       
        _LightenAmount("Lighten Amount", Range(0, 1)) = 0
        _LightenStrength("Lighten Strength", Range(0, 2)) = 1

        [Header(Disabled Greyscale)]
        _GreyscaleAmount ("Greyscale Amount", Range(0, 1)) = 0
        _GreyscaleBrightness ("Greyscale Brightness", Range(-1, 1)) = 0
        _GreyscaleContrast ("Greyscale Contrast", Range(0, 3)) = 1
        _GreyscaleTint ("Greyscale Tint", Color) = (0.55,0.55,0.55,1)
        _GreyscaleTintStrength ("Greyscale Tint Strength", Range(0, 1)) = 0

        // Required for UI Masking
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            half _HsvShift;
            half _HsvSaturation;
            half _HsvBright;

            half _LightenAmount;
            half _LightenStrength;

            float _GreyscaleAmount;
            float _GreyscaleBrightness;
            float _GreyscaleContrast;
            fixed4 _GreyscaleTint;
            float _GreyscaleTintStrength;

            float3 applyGreyscaleLock(float3 c)
            {
                float gray = dot(c, float3(0.299, 0.587, 0.114));

                float3 g = gray.xxx;
                g = (g - 0.5) * _GreyscaleContrast + 0.5;
                g += _GreyscaleBrightness;

                // Optional dull grey tint. Useful for disabled / locked UI.
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
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 col = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                // HSV adjustment
                half shiftRadians = _HsvShift * 3.14159265 / 180.0;

                half cosHsv = _HsvBright * _HsvSaturation * cos(shiftRadians);
                half sinHsv = _HsvBright * _HsvSaturation * sin(shiftRadians);

                half3 resultHsv;

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

                
                half lighten = saturate(_LightenAmount * _LightenStrength);
                col.rgb = 1.0 - (1.0 - col.rgb) * (1.0 - lighten);

                
                col.rgb = applyGreyscaleLock(col.rgb);

                col.rgb = saturate(col.rgb);

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