Shader "UI/SDF_Frame_Dynamic_Slice_Cooldown"
{
    Properties
    {
        [Header(Cooldown _ Carved Track Range)]
        _TrackAngleStart ("Track Start Angle (0-1)", Range(0, 1)) = 0.0
        _TrackAngleEnd ("Track End Angle (0-1)", Range(0, 1)) = 1.0
        _FillAmount ("Fill Progress (0-1)", Range(0, 1)) = 0.35
        
        [Header(Track Dimensions and Colors)]
        _TrackStart ("Track Inner Edge", Range(0.01, 0.99)) = 0.25
        _TrackEnd ("Track Outer Edge", Range(0.01, 0.99)) = 0.75
        _FillColor ("Active Fill Color (Blue)", Color) = (0.2, 0.4, 0.8, 1.0)
        _TrackColor ("Empty Cavity Color (Dark)", Color) = (0.15, 0.1, 0.05, 1.0)
        _CavityShadow ("Cavity Shadow Depth", Range(0.001, 0.05)) = 0.015

        [Header(Frame Dimensions)]
        _Thickness ("Frame Thickness", Range(0.01, 0.5)) = 0.15
        _OuterBevel ("Outer Bevel Width", Range(0.01, 0.4)) = 0.15
        _InnerBevel ("Inner Bevel Width", Range(0.01, 0.4)) = 0.20
        _Blur ("Band Blend Softness", Range(0.001, 0.1)) = 0.02
        _MaxBandBlur ("Max Blur (Prevents Zoom-Out Mush)", Range(0.01, 0.2)) = 0.05
        
        [Header(Color Palette (Outer to Inner))]
        _Color1_OuterEdge ("1. Outer Edge (Dark)", Color) = (0.45, 0.30, 0.05, 1.0)
        _Color2_OuterHigh ("2. Outer Highlight", Color) = (1.00, 0.90, 0.50, 1.0)
        _Color3_MidBase   ("3. Mid Body Base", Color) = (0.75, 0.55, 0.15, 1.0)
        _Color4_InnerHigh ("4. Inner Highlight (White)", Color) = (0.95, 0.90, 0.70, 1.0)
        _Color5_InnerShad ("5. Inner Groove (Shadow)", Color) = (0.35, 0.20, 0.02, 1.0)
        _Color6_InnerRim  ("6. Inner Rim Highlight", Color) = (0.90, 0.75, 0.30, 1.0)

        [Header(Lighting _ Volume)]
        _LightAngle ("Light Angle", Range(0, 360)) = 135
        _LightFocus ("Light Band Narrowness", Range(0.0, 1.0)) = 0.8
        _LightIntensity ("Highlight Intensity", Range(0, 2)) = 1.2
        _ShadowIntensity ("Shadow Intensity", Range(0, 1)) = 0.6
        _BumpStrength ("Texture Bump Strength", Range(0, 0.5)) = 0.1

        [Header(Textures (Use Grayscale))]
        _GrungeTex ("Grunge / Tarnish (Grayscale)", 2D) = "white" {}
        _GrungeOpacity ("Grunge Darkness", Range(0, 1)) = 0.5
        _Distortion ("Hammered Metal Distortion", Range(0, 0.1)) = 0.02
        
        _ScratchTex ("Cracks/Scratches (White on Black)", 2D) = "black" {}
        _ScratchColor ("Crack Dark Color", Color) = (0.2, 0.1, 0.0, 1.0)
        _ScratchOpacity ("Crack Visibility", Range(0, 1)) = 0.8

        // UGUI Requirements
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]

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
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            // Properties
            float _TrackAngleStart, _TrackAngleEnd, _FillAmount;
            float _TrackStart, _TrackEnd, _CavityShadow;
            fixed4 _FillColor, _TrackColor;

            float _Thickness, _OuterBevel, _InnerBevel, _Blur, _MaxBandBlur;
            fixed4 _Color1_OuterEdge, _Color2_OuterHigh, _Color3_MidBase, _Color4_InnerHigh, _Color5_InnerShad, _Color6_InnerRim;
            float _LightAngle, _LightFocus, _LightIntensity, _ShadowIntensity, _BumpStrength;
            
            sampler2D _GrungeTex; float4 _GrungeTex_ST; 
            sampler2D _ScratchTex; float4 _ScratchTex_ST;
            float _GrungeOpacity, _Distortion, _ScratchOpacity;
            fixed4 _ScratchColor;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color;
                return OUT;
            }

           fixed4 frag(v2f IN) : SV_Target
            {
                float2 centerUV = IN.texcoord * 2.0 - 1.0;
                float dist = length(centerUV);
                float pixelFw = fwidth(dist) * 1.5; 

                // 1. GLOBAL BOUNDS
                float outerRadius = 1.0;
                float innerRadius = 1.0 - (_Thickness * 2.0);
                float frameBoundsMask = smoothstep(outerRadius + pixelFw, outerRadius - pixelFw, dist) 
                                      * smoothstep(innerRadius - pixelFw, innerRadius + pixelFw, dist);

                // 2. BASE GOLDEN FRAME
                float2 grungeUV = IN.texcoord * _GrungeTex_ST.xy + _GrungeTex_ST.zw;
                float2 scratchUV = IN.texcoord * _ScratchTex_ST.xy + _ScratchTex_ST.zw;
                float grungeMap = tex2D(_GrungeTex, grungeUV).r;
                float scratchMap = tex2D(_ScratchTex, scratchUV).r;

                float framePos = (outerRadius - dist) / (_Thickness * 2.0);
                framePos += (grungeMap - 0.5) * _Distortion;

                float safeBlur = min(fwidth(framePos), _MaxBandBlur) + _Blur; 
                float stop1 = _OuterBevel * 0.3; float stop2 = _OuterBevel; float stop3 = 1.0 - _InnerBevel - 0.05; float stop4 = 1.0 - _InnerBevel; float stop5 = 1.0 - (_InnerBevel * 0.2);

                fixed4 baseGold = _Color1_OuterEdge;
                baseGold = lerp(baseGold, _Color2_OuterHigh, smoothstep(stop1 - safeBlur, stop1 + safeBlur, framePos));
                baseGold = lerp(baseGold, _Color3_MidBase,   smoothstep(stop2 - safeBlur, stop2 + safeBlur, framePos));
                baseGold = lerp(baseGold, _Color4_InnerHigh, smoothstep(stop3 - safeBlur, stop3 + safeBlur, framePos)); 
                baseGold = lerp(baseGold, _Color5_InnerShad, smoothstep(stop4 - safeBlur, stop4 + safeBlur, framePos));
                baseGold = lerp(baseGold, _Color6_InnerRim,  smoothstep(stop5 - safeBlur, stop5 + safeBlur, framePos));

                float radAngle = _LightAngle * (3.14159 / 180.0);
                float2 lightDir = float2(cos(radAngle), sin(radAngle)); 
                float2 perturbedNormal = normalize((centerUV / (dist + 0.0001)) + (grungeMap - 0.5) * _BumpStrength);
                float normalInvert = lerp(1.0, -1.0, smoothstep(1.0 - _InnerBevel - safeBlur*3.0, 1.0 - _InnerBevel + safeBlur*3.0, framePos));
                
                float NdotL = dot(perturbedNormal * normalInvert, lightDir);  
                baseGold.rgb += (_Color2_OuterHigh.rgb * smoothstep(lerp(-0.5, 0.95, _LightFocus), 1.0, NdotL) * _LightIntensity); 
                baseGold.rgb = lerp(baseGold.rgb, float3(0,0,0), smoothstep(lerp(-0.5, 0.95, _LightFocus), 1.0, -NdotL) * _ShadowIntensity);
                baseGold.rgb = lerp(baseGold.rgb, baseGold.rgb * grungeMap, _GrungeOpacity);
                baseGold.rgb = lerp(baseGold.rgb, _ScratchColor.rgb, scratchMap * _ScratchOpacity);
                baseGold.a = 1.0;

                // 3. RADIAL & ANGULAR TRACK LOGIC
                
                // Track Radial Mask
                float trackFw = fwidth(framePos) * 1.0; 
                float trackRadialMask = smoothstep(_TrackStart - trackFw, _TrackStart + trackFw, framePos) 
                                      * smoothstep(_TrackEnd + trackFw, _TrackEnd - trackFw, framePos);

                // Calculate Angle Logic
                float angle = frac(atan2(centerUV.x, centerUV.y) / 6.2831853 + 1.0); 
                
                // Track Length and Relative Angle (Handles wrapping if Start > End)
                float trackLen = _TrackAngleEnd - _TrackAngleStart;
                if (trackLen < 0.0) trackLen += 1.0; // Automatically wraps around 12 o'clock
                
                // Edge case: Prevent 0-length from breaking math. If sliders match exactly at 0/1, it's a full circle.
                if (trackLen == 0.0) {
                    if (_TrackAngleStart == 0.0 && _TrackAngleEnd == 1.0) trackLen = 1.0;
                    else trackLen = 0.0001; 
                }

                float relAngle = frac(angle - _TrackAngleStart + 1.0);
                float angAA = (pixelFw * 1.5) / (dist * 6.2831853 + 0.001); // Angular anti-aliasing
                
                // Mask that restricts the track to the defined angular slice
                float trackAngleMask = smoothstep(-angAA, angAA, relAngle) * smoothstep(trackLen + angAA, trackLen - angAA, relAngle);
                
                float trackMask = trackRadialMask * trackAngleMask;

                // 4. THE FILL PROGRESS (Mapped to the track length)
                float fillProgress = relAngle / trackLen; 
                float fillMask = smoothstep(_FillAmount + angAA/trackLen, _FillAmount - angAA/trackLen, fillProgress);
                
                fixed4 trackContent = lerp(_TrackColor, _FillColor, fillMask);

                // 5. FOUR-WALL 3D CAVITY SHADOW
                // Distance to Radial Walls (Inner/Outer Edge)
                float radialDistPhysical = min(abs(framePos - _TrackStart), abs(framePos - _TrackEnd)) * (_Thickness * 2.0);
                
                // Distance to Angular Walls (Start/End Slice) converted to physical arc length
                float arcDistStart = relAngle * 6.2831853 * dist;
                float arcDistEnd = (trackLen - relAngle) * 6.2831853 * dist;
                float angularDistPhysical = min(arcDistStart, arcDistEnd);
                
                // Combine distances to find the closest wall
                float distToClosestWall = min(radialDistPhysical, angularDistPhysical);
                
                // Darken the edges of the track to simulate depth
                float shadowDepth = smoothstep(0.0, _CavityShadow, distToClosestWall); 
                trackContent.rgb *= lerp(0.2, 1.0, shadowDepth); 

                // 6. FINAL COMPOSITING
                fixed4 finalColor = baseGold;
                finalColor.rgb = lerp(baseGold.rgb, trackContent.rgb, trackMask);

                finalColor.a *= frameBoundsMask;
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                finalColor *= IN.color;

                return finalColor;
            }
            ENDCG
        }
    }
}