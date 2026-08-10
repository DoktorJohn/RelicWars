Shader "UI/SDF_InvertedRectFill_Tiled"
{
    // works with UIGlobalTilingModifier script in custom bevel mode
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Texture)]
        [Toggle] _ForceRepeat ("Force Repeat With frac()", Float) = 1

        [Header(Shape)]
        _Radius ("Cutout Radius", Float) = 20
        _Inset ("Inset", Float) = 0
        _EdgeSoftness ("Edge Softness", Float) = 1

        [Header(UI Masking)]
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
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex    : POSITION;
                float4 color     : COLOR;
                float2 texcoord  : TEXCOORD0; // original UI UV
                float2 texcoord1 : TEXCOORD1; // global tiling UV from UIGlobalTilingModifier
                float2 texcoord2 : TEXCOORD2; // rect size from UIGlobalTilingModifier
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 uv            : TEXCOORD0;
                float2 tileUV        : TEXCOORD1;
                float2 rectSize      : TEXCOORD2;
                float2 localPos      : TEXCOORD3;
                float4 worldPosition : TEXCOORD4;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            float _ForceRepeat;

            float _Radius;
            float _Inset;
            float _EdgeSoftness;
            float4 _ClipRect;

            float sdInvertedBox(float2 p, float2 halfSize, float r)
            {
                p = abs(p);
                float2 b = halfSize - r;

                float dTop = (p.x < b.x)
                    ? abs(p.y - halfSize.y)
                    : length(p - float2(b.x, halfSize.y));

                float dRight = (p.y < b.y)
                    ? abs(p.x - halfSize.x)
                    : length(p - float2(halfSize.x, b.y));

                float dArc = min(
                    length(p - float2(b.x, halfSize.y)),
                    length(p - float2(halfSize.x, b.y))
                );

                if (halfSize.x - p.x > 0.0 && halfSize.y - p.y > 0.0)
                {
                    dArc = min(dArc, abs(length(halfSize - p) - r));
                }

                float unsignedDist = min(min(dTop, dRight), dArc);

                bool inBox = p.x < halfSize.x && p.y < halfSize.y;
                bool inCutout = p.x > b.x && p.y > b.y && length(halfSize - p) < r;

                float distSign = (inBox && !inCutout) ? -1.0 : 1.0;
                return unsignedDist * distSign;
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;

                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.color = IN.color * _Color;

                OUT.uv = IN.texcoord;
                OUT.tileUV = IN.texcoord1;

                // Rect size is supplied automatically by UIGlobalTilingModifier via UV2.
                OUT.rectSize = max(IN.texcoord2.xy, float2(0.001, 0.001));

                // Keep SDF shape based on original UV0, not tiled UV.
                OUT.localPos = (IN.texcoord - 0.5) * OUT.rectSize;

                OUT.worldPosition = IN.vertex;

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 halfSize = IN.rectSize * 0.5;
                float radius = clamp(_Radius, 0.0, min(halfSize.x, halfSize.y));

                float d = sdInvertedBox(IN.localPos, halfSize, radius);
                d += _Inset;

                float aa = max(fwidth(d), _EdgeSoftness);
                float shapeAlpha = smoothstep(aa, -aa, d);

                // Always use UV1. Your modifier writes either global tiling UVs,
                // or the original UV0 into UV1 when doGlobalTiling is false.
                float2 sampleUV = IN.tileUV;

                if (_ForceRepeat > 0.5)
                {
                    sampleUV = frac(sampleUV);
                }

                fixed4 col = tex2D(_MainTex, sampleUV) * IN.color;
                col.a *= shapeAlpha;

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