Shader "UI/Custom/BannerFlutter"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // --- Flutter Properties ---
        _FlutterSpeed ("Flutter Speed", Float) = 20.0
        _FlutterFrequency ("Flutter Frequency (Ripples)", Float) = 15.0
        _FlutterAmplitude ("Flutter Amplitude (Pixels)", Float) = 5.0
        _MaskFalloff ("Bottom Mask Falloff (Higher = closer to edge)", Float) = 6.0
        
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
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            
            float4 _ClipRect;
            half4 _TextureSampleAdd;

            float _FlutterSpeed;
            float _FlutterFrequency;
            float _FlutterAmplitude;
            float _MaskFalloff;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                OUT.worldPosition = v.vertex;

                // --- FLUTTER LOGIC ---
                
                // 1. The Mask: Use pow() to aggressively push the mask to the very bottom
                float bottomMask = 1.0 - v.texcoord.y; 
                bottomMask = pow(bottomMask, _MaskFalloff); 

                // 2. The Wave: Use normalized UVs (v.texcoord) instead of giant UI pixel coordinates
                float time = _Time.y * _FlutterSpeed;
                // Subtracting Y from X makes the ripples travel diagonally down the fabric
                float wave = sin(time + (v.texcoord.x * _FlutterFrequency) - (v.texcoord.y * _FlutterFrequency));
                
                // Apply flutter purely to the X (horizontal wiggle)
                v.vertex.x += wave * _FlutterAmplitude * bottomMask;
                
                // ---------------------

                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}