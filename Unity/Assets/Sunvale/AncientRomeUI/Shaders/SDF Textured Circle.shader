Shader "UI/SDF Textured Circle Bulged"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Texture Controls)]
        _TextureScale ("Texture Tiling XY", Vector) = (1,1,0,0)
        _TextureOffset ("Texture Offset XY", Vector) = (0,0,0,0)
        _TextureRotation ("Texture Rotation Degrees", Range(0,360)) = 0

        [Header(Circle Shape)]
        _CircleRadius ("Circle Radius", Range(0,0.5)) = 0.48
        _EdgeSoftness ("Edge Softness", Range(0,0.05)) = 0.002
        _Aspect ("Rect Aspect Width/Height", Float) = 1

        [Header(Bulge and Lighting)]
        _BulgeAmount ("Bulge Amount", Range(0,1)) = 0.45
        _BulgeUVWarp ("Bulge UV Warp", Range(0,0.2)) = 0.03
        _LightDir ("Light Direction XY", Vector) = (-0.6, 0.8, 0, 0)
        _Ambient ("Ambient", Range(0,2)) = 0.75
        _DiffuseStrength ("Diffuse Strength", Range(0,2)) = 0.45
        _SpecularStrength ("Specular Strength", Range(0,1)) = 0.10
        _SpecularGloss ("Specular Gloss", Range(1,128)) = 32

        [Header(Edge and Rim)]
        _EdgeDarkenStrength ("Edge Darken Strength", Range(0,1)) = 0.18
        _EdgeDarkenPower ("Edge Darken Power", Range(0.5,8)) = 2.0
        _RimShadowWidth ("Rim Shadow Width", Range(0,1)) = 0.18
        _RimShadowStrength ("Rim Shadow Strength", Range(0,1)) = 0.12
        _RimHighlightWidth ("Rim Highlight Width", Range(0,1)) = 0.12
        _RimHighlightStrength ("Rim Highlight Strength", Range(0,1)) = 0.10

        [Header(Drop Shadow)]
        _ShadowColor ("Shadow Color", Color) = (0,0,0,0.28)
        _ShadowOffset ("Shadow Offset XY", Vector) = (0.015,-0.018,0,0)
        _ShadowExpand ("Shadow Expand", Range(0,0.1)) = 0.005
        _ShadowSoftness ("Shadow Softness", Range(0,0.1)) = 0.02

        [Header(Unity UI Masking)]
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
            Name "Default"

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
                float4 vertex   : POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 uv            : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _ClipRect;
            fixed4 _TextureSampleAdd;

            float4 _TextureScale;
            float4 _TextureOffset;
            float _TextureRotation;

            float _CircleRadius;
            float _EdgeSoftness;
            float _Aspect;

            float _BulgeAmount;
            float _BulgeUVWarp;
            float4 _LightDir;
            float _Ambient;
            float _DiffuseStrength;
            float _SpecularStrength;
            float _SpecularGloss;

            float _EdgeDarkenStrength;
            float _EdgeDarkenPower;
            float _RimShadowWidth;
            float _RimShadowStrength;
            float _RimHighlightWidth;
            float _RimHighlightStrength;

            fixed4 _ShadowColor;
            float4 _ShadowOffset;
            float _ShadowExpand;
            float _ShadowSoftness;

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

            float2 RotateUV(float2 uv, float degrees)
            {
                float angle = degrees * 0.01745329252;
                float s = sin(angle);
                float c = cos(angle);

                uv -= 0.5;

                float2 r;
                r.x = uv.x * c - uv.y * s;
                r.y = uv.x * s + uv.y * c;

                return r + 0.5;
            }

            float2 ApplyAspect(float2 p, float aspect)
            {
                if (aspect > 1.0)
                {
                    p.x *= aspect;
                }
                else
                {
                    p.y /= max(aspect, 0.0001);
                }

                return p;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float aspect = max(_Aspect, 0.0001);

                // Local circle coords in -0.5..0.5 space
                float2 p = IN.uv - 0.5;
                float2 pAspect = ApplyAspect(p, aspect);

                float radius = max(_CircleRadius, 0.0001);
                float dist = length(pAspect);
                float sdf = dist - radius;

                float aa = max(fwidth(sdf), _EdgeSoftness);
                float circleMask = 1.0 - smoothstep(-aa, aa, sdf);

                // Normalized radial coordinates inside the circle
                float2 np = pAspect / radius;
                float r01 = saturate(dist / radius);
                float insideSphere = saturate(1.0 - dot(np, np));
                float z = sqrt(insideSphere);

                // Fake convex normal: flat -> bulged
                float3 flatNormal = float3(0, 0, 1);
                float3 bulgedNormal = normalize(float3(np.x, np.y, z));
                float3 normalDir = normalize(lerp(flatNormal, bulgedNormal, _BulgeAmount));

                // Optional subtle UV warp to help the texture feel less flat
                float2 texUV = IN.uv;
                float edgeWarp = (1.0 - z);
                texUV -= np * edgeWarp * (_BulgeUVWarp * 0.06);

                texUV = RotateUV(texUV, _TextureRotation);
                texUV -= 0.5;
                texUV *= _TextureScale.xy;
                texUV += 0.5 + _TextureOffset.xy;

                fixed4 tex = tex2D(_MainTex, texUV) + _TextureSampleAdd;

                float3 baseRgb = tex.rgb * IN.color.rgb;
                float baseAlpha = tex.a * IN.color.a * circleMask;

                // Lighting
                float3 lightDir = normalize(float3(_LightDir.xy, 1.25));
                float3 viewDir = float3(0, 0, 1);
                float3 halfDir = normalize(lightDir + viewDir);

                float ndl = saturate(dot(normalDir, lightDir));
                float spec = pow(saturate(dot(normalDir, halfDir)), max(1.0, _SpecularGloss)) * _SpecularStrength;

                float lightFactor = saturate(_Ambient + ndl * _DiffuseStrength);

                // Edge darkening
                float edgeDark = pow(r01, _EdgeDarkenPower) * _EdgeDarkenStrength;
                lightFactor *= saturate(1.0 - edgeDark);

                // Rim shadow around edge
                float rimShadowMask = smoothstep(1.0 - _RimShadowWidth, 1.0, r01);

                // Rim highlight only on light-facing side
                float2 radialDir = (dist > 0.00001) ? (pAspect / dist) : float2(0, 0);
                float2 lightDir2D = normalize(_LightDir.xy + float2(0.0001, 0.0001));
                float lightSide = saturate(dot(radialDir, lightDir2D));
                float rimHighlightMask = smoothstep(1.0 - _RimHighlightWidth, 1.0, r01);
                float rimHighlight = rimHighlightMask * lightSide * _RimHighlightStrength;

                float3 surfaceRgb = baseRgb;
                surfaceRgb *= lightFactor;
                surfaceRgb *= (1.0 - rimShadowMask * _RimShadowStrength);
                surfaceRgb += spec.xxx;
                surfaceRgb += rimHighlight.xxx;

                // Drop shadow
                float2 shadowOffset = ApplyAspect(_ShadowOffset.xy, aspect);
                float2 shadowP = pAspect - shadowOffset;
                float shadowSdf = length(shadowP) - (radius + _ShadowExpand);
                float shadowAA = max(fwidth(shadowSdf), _ShadowSoftness);
                float shadowMask = 1.0 - smoothstep(-shadowAA, shadowAA, shadowSdf);

                float shadowAlpha = shadowMask * _ShadowColor.a;
                shadowAlpha *= (1.0 - baseAlpha); // hide shadow beneath the disc

                // Composite shadow + disc into one UI output
                float outAlpha = saturate(baseAlpha + shadowAlpha);

                float3 outRgb = 0;
                if (outAlpha > 0.00001)
                {
                    outRgb = (surfaceRgb * baseAlpha + _ShadowColor.rgb * shadowAlpha) / outAlpha;
                }

                #ifdef UNITY_UI_CLIP_RECT
                float clipFactor = UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                outAlpha *= clipFactor;
                #endif

                fixed4 col = fixed4(outRgb, outAlpha);

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}