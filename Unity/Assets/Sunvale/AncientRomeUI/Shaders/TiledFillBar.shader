Shader "UI/TiledFillBar"
{
    Properties
    {
        [Header(Textures and Colors)]
        [PerRendererData] _MainTex ("Empty Sprite", 2D) = "white" {}
        _EmptyTint ("Empty Tint Color", Color) = (1,1,1,1)
        _FullTex ("Full Sprite", 2D) = "white" {}
        _FullTint ("Full Tint Color", Color) = (1,1,1,1)
        
        [Header(Bar Settings)]
        _IconCount ("Total Icon Count", Float) = 10
        _FillAmount ("Fill Amount", Range(0, 1)) = 0.55
        
        [Header(Authoring Control (Size and Aspect))]
        _IconScale ("Uniform Master Scale", Range(0.1, 5)) = 1.0
        _IconWidth ("Width Modifier", Range(0.1, 5)) = 0.8
        _IconHeight ("Height Modifier", Range(0.1, 5)) = 0.8
        
        [Header(Directions)]
        [Toggle] _RightToLeft ("Bar Fills Right-To-Left?", Float) = 0
        [Toggle] _VerticalFill ("Partial Fill is Vertical?", Float) = 1

        // Standard UI Properties
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
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
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _FullTex;
            fixed4 _EmptyTint;
            fixed4 _FullTint;
            
            float _IconCount;
            float _FillAmount;
            
            float _IconScale;
            float _IconWidth;
            float _IconHeight;
            
            float _RightToLeft;
            float _VerticalFill;
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
                // 1. Setup Cell and Indexing
                float cellCountU = IN.texcoord.x * _IconCount;
                float forwardIndex = min(floor(cellCountU), _IconCount - 1.0);
                float cellIndex = lerp(forwardIndex, _IconCount - 1.0 - forwardIndex, _RightToLeft);

                // 2. Local UV and Aspect Control
                float localU = frac(cellCountU);
                float localV = IN.texcoord.y;
                
                // Combine the modifiers with the Master Scale
                float actualWidth = _IconWidth * _IconScale;
                float actualHeight = _IconHeight * _IconScale;
                
                // Scale out from the center (0.5, 0.5) using the final Width/Height controls
                float sampleU = (localU - 0.5) / actualWidth + 0.5;
                float sampleV = (localV - 0.5) / actualHeight + 0.5;

                // Create mask to make areas outside the bounds transparent
                float boundsMask = step(0.0, sampleU) * step(sampleU, 1.0) * step(0.0, sampleV) * step(sampleV, 1.0);

                // 3. Fill Logic
                float currentFillPoint = _FillAmount * _IconCount;
                float cellFill = saturate(currentFillPoint - cellIndex);

                float compareU = lerp(sampleU, 1.0 - sampleU, _RightToLeft);
                float compareAxis = lerp(compareU, sampleV, _VerticalFill);
                float isPixelFilled = step(compareAxis, cellFill);

                // 4. Seam / Artifact Fix (Accurately tracks the new master scale)
                float2 dx = ddx(IN.texcoord) * float2(_IconCount / actualWidth, 1.0 / actualHeight);
                float2 dy = ddy(IN.texcoord) * float2(_IconCount / actualWidth, 1.0 / actualHeight);
                float2 finalUV = clamp(float2(sampleU, sampleV), 0.001, 0.999);

                // 5. Sample Textures
                fixed4 emptyColor = tex2Dgrad(_MainTex, finalUV, dx, dy) * _EmptyTint;
                fixed4 fullColor  = tex2Dgrad(_FullTex, finalUV, dx, dy) * _FullTint;

                // 6. Blend and Output
                fixed4 finalColor = lerp(emptyColor, fullColor, isPixelFilled);
                
                // Apply Aspect Bounds and Canvas Image Tint (Vertex Color)
                finalColor.a *= boundsMask; 
                finalColor *= IN.color;

                // 7. RectMask2D Support
                #ifdef UNITY_UI_CLIP_RECT
                    finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
}