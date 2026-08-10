Shader "UI/PieChartMarble"
{
    Properties
    {
        [Header(Texture Settings)]
        _PatternTex ("Greyscale Texture", 2D) = "white" {}
        _TextureIntensity ("Texture Intensity", Range(0, 2)) = 1.0
        
        [Header(Edge Darkening Settings)]
        _EdgeDarkenColor ("Edge Color (Alpha = Intensity)", Color) = (0, 0, 0, 0.5)
        _EdgeThickness ("Edge Thickness", Range(0, 0.5)) = 0.05
        _EdgeSmoothness ("Edge Smoothness", Range(0.001, 0.5)) = 0.05
        
        // --- Required Properties for Unity UI Masking ---
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)
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

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 uv1      : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 uv1      : TEXCOORD1;
                float4 worldPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _PatternTex;
            float _TextureIntensity;
            
            // Edge Properties
            half4 _EdgeDarkenColor;
            float _EdgeThickness;
            float _EdgeSmoothness;
            
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = v.texcoord; 
                OUT.uv1 = v.uv1;
                OUT.color = v.color;
                
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 1. Calculate Marble Texture Logic
                half gray = tex2D(_PatternTex, IN.texcoord).r;
                half offset = (gray - 0.5) * 2.0 * _TextureIntensity;
                
                half3 finalRGB;
                if (offset > 0) {
                    finalRGB = lerp(IN.color.rgb, half3(1, 1, 1), saturate(offset)); 
                } else {
                    finalRGB = lerp(IN.color.rgb, half3(0, 0, 0), saturate(-offset));
                }

                // 2. EDGE DETECTION CALCULATION
                float distToOuterCurve = 1.0 - IN.uv1.x;
                float distToStartAngle = IN.uv1.y;
                float distToEndAngle   = 1.0 - IN.uv1.y;
                
                float minDist = min(distToOuterCurve, min(distToStartAngle, distToEndAngle));

                // 3. BLEND WITH ALPHA INTENSITY
                // smoothstep returns 0 at the extreme edge, and 1 inside the slice
                float edgeGradient = smoothstep(_EdgeThickness, _EdgeThickness + _EdgeSmoothness, minDist);

                // We invert it so 1 is the edge, and 0 is the inside.
                float edgeFactor = 1.0 - edgeGradient;

                // Multiply by the color's Alpha channel!
                // If alpha is 0.5, the max blend amount will be 0.5.
                float blendAmount = edgeFactor * _EdgeDarkenColor.a;

                // Blend the final RGB based on the calculated amount
                finalRGB = lerp(finalRGB, _EdgeDarkenColor.rgb, blendAmount);

                // 4. Output
                half4 finalColor = half4(finalRGB, IN.color.a);
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

                return finalColor;
            }
        ENDCG
        }
    }
}