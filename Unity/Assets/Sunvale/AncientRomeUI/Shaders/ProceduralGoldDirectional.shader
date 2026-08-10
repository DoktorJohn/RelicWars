Shader "UI/ProceduralGoldPalette"
{
    Properties
    {
        [Header(Gold Palette (Top to Bottom))]
        _Color5 ("1. Top Edge Highlight", Color) = (1.0, 0.95, 0.7, 1)
        _Color4 ("2. Main Gold Body", Color) = (0.85, 0.65, 0.25, 1)
        _Color3 ("3. Mid Highlight Stripe", Color) = (0.95, 0.8, 0.4, 1)
        _Color2 ("4. Core Shadow", Color) = (0.35, 0.2, 0.05, 1)
        _Color1 ("5. Bottom Edge Rim", Color) = (0.55, 0.35, 0.1, 1)
        
        [Header(Band Positions)]
        _Pos4 ("Top Edge Boundary", Range(0, 1)) = 0.85
        _Pos3 ("Mid Highlight Start", Range(0, 1)) = 0.55
        _Pos2 ("Shadow Start", Range(0, 1)) = 0.35
        _Pos1 ("Bottom Rim Start", Range(0, 1)) = 0.10
        _BandBlend ("Band Softness", Range(0.001, 0.3)) = 0.08
        
        [Header(Organic Noise)]
        _NoiseScale ("Noise Scale", Float) = 0.75
        _NoiseStrength ("Noise Wobble Strength", Range(0, 0.3)) = 0.05

        [Header(Lighting Setup)]
        _LightDir ("Light Direction (X, Y, Z)", Vector) = (-0.5, 0.8, 1.0)
        _BevelCurve ("Bevel Roundness", Range(0.1, 2.0)) = 0.7
        
        [Header(Anti Aliasing)]
        _AASmoothness ("AA Pixel Spread", Range(0.1, 3.0)) = 1.5

        // --- Required UI Properties ---
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
            #pragma target 3.0 
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0; 
                float2 texcoord1 : TEXCOORD1; // 2D Normal from C#
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0; // y = line thickness width (0 to 1)
                float2 normal2D : TEXCOORD1; 
                float4 worldPosition : TEXCOORD2;
            };

            // Palette
            half4 _Color1, _Color2, _Color3, _Color4, _Color5;
            float _Pos1, _Pos2, _Pos3, _Pos4, _BandBlend;

            // Noise & Light
            float _NoiseScale, _NoiseStrength;
            float4 _LightDir;
            float _BevelCurve;
            float _AASmoothness;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.normal2D = v.texcoord1;
                OUT.color = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float v = IN.texcoord.y;
                float2 n2d = normalize(IN.normal2D);

                // 1. Reconstruct 3D Shape
                float nx = (0.5 - v) * 2.0; 
                float profileX = sign(nx) * pow(abs(nx), _BevelCurve);
                float profileZ = sqrt(max(0.0, 1.0 - profileX * profileX));

                // 2. World Space Normal & Lighting
                float3 worldNormal = normalize(float3(n2d.x * profileX, n2d.y * profileX, profileZ));
                float3 L = normalize(_LightDir.xyz);
                
                // NdotL goes from -1 (darkest angle) to +1 (brightest angle)
                float NdotL = dot(worldNormal, L);

                // Map NdotL to a 0.0 to 1.0 Gradient Ramp
                float ramp = NdotL * 0.5 + 0.5;

                // 3. Apply Organic Noise Wobble
                // Based on world position so it doesn't stretch based on line length
                float noise = sin(IN.worldPosition.x * _NoiseScale) * cos(IN.worldPosition.y * _NoiseScale);
                ramp += noise * _NoiseStrength;
                ramp = saturate(ramp);

                // 4. Map the 5-Color Palette!
                // Start with bottom rim, and layer colors on top based on ramp thresholds
                half3 finalColor = _Color1.rgb;
                finalColor = lerp(finalColor, _Color2.rgb, smoothstep(_Pos1 - _BandBlend, _Pos1 + _BandBlend, ramp));
                finalColor = lerp(finalColor, _Color3.rgb, smoothstep(_Pos2 - _BandBlend, _Pos2 + _BandBlend, ramp));
                finalColor = lerp(finalColor, _Color4.rgb, smoothstep(_Pos3 - _BandBlend, _Pos3 + _BandBlend, ramp));
                finalColor = lerp(finalColor, _Color5.rgb, smoothstep(_Pos4 - _BandBlend, _Pos4 + _BandBlend, ramp));

                // 5. Apply UI Drop Shadow natively!
                // IN.color is pure white for the wire, but Black/Transparent for the Shadow component.
                finalColor *= IN.color.rgb;

                // 6. Anti-Aliasing
                float fw = fwidth(v);
                float aa = smoothstep(0.0, fw * _AASmoothness, v) * 
                           smoothstep(1.0, 1.0 - (fw * _AASmoothness), v);

                half4 output = half4(finalColor, IN.color.a * aa);
                output.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                
                return output;
            }
        ENDCG
        }
    }
}