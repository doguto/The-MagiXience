// 風向きを示す矢印をグリッド状に敷き詰めるUIシェーダー。
// _MainTex は下向きの矢印1つ分のテクスチャ。セルごとに _Direction 方向へ回転して描き、
// _Offset でスクロールする。縦方向の透明度はスクロールしない矩形のUVで掛ける。
Shader "Project/UI/WindArrows"
{
    Properties
    {
        [PerRendererData] _MainTex ("Arrow Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _RectPixelSize ("Rect Size (px)", Vector) = (400, 32, 0, 0)
        _CellSize ("Cell Size (px)", Vector) = (12, 12, 0, 0)
        _ArrowScale ("Arrow Scale (px per texel)", Float) = 0.5
        _Offset ("Scroll Offset (px)", Vector) = (0, 0, 0, 0)
        _Direction ("Direction", Vector) = (0, 1, 0, 0)
        _AlphaTop ("Alpha Top", Range(0, 1)) = 1
        _AlphaBottom ("Alpha Bottom", Range(0, 1)) = 0.2
        _AlphaSteps ("Alpha Steps (0 = smooth)", Float) = 0
        [Toggle] _PixelSnap ("Pixel Snap", Float) = 1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float4 _RectPixelSize;
            float4 _CellSize;
            float _ArrowScale;
            float4 _Offset;
            float4 _Direction;
            float _AlphaTop;
            float _AlphaBottom;
            float _AlphaSteps;
            float _PixelSnap;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 矩形内のピクセル座標。PixelSnap時は背景のドットに合わせて整数ピクセル単位で動かす
                float2 px = i.texcoord * _RectPixelSize.xy;
                float2 offset = _Offset.xy;
                if (_PixelSnap > 0.5)
                {
                    px = floor(px) + 0.5;
                    offset = round(offset);
                }

                // セル中心からの位置
                float2 cell = max(_CellSize.xy, 1.0);
                float2 q = (frac((px - offset) / cell) - 0.5) * cell;

                // 矢印テクスチャは下向きに描かれているので、テクスチャの上方向を _Direction の逆向きに合わせる
                float2 up = dot(_Direction.xy, _Direction.xy) > 1e-6 ? -normalize(_Direction.xy) : float2(0, 1);
                float2 right = float2(up.y, -up.x);
                float2 local = float2(dot(q, right), dot(q, up));

                float2 texSize = _MainTex_TexelSize.zw;
                float2 uv = local / (texSize * _ArrowScale) + 0.5;
                float inside = step(0, uv.x) * step(uv.x, 1) * step(0, uv.y) * step(uv.y, 1);
                // テクセル中心を読んでドット絵のまま表示する
                uv = (floor(uv * texSize) + 0.5) * _MainTex_TexelSize.xy;

                fixed4 color = tex2D(_MainTex, uv) * i.color;
                color.a *= inside;

                float t = i.texcoord.y;
                if (_AlphaSteps >= 1) t = saturate((floor(t * _AlphaSteps) + 0.5) / _AlphaSteps);
                color.a *= lerp(_AlphaBottom, _AlphaTop, t);
                return color;
            }
            ENDCG
        }
    }
}
