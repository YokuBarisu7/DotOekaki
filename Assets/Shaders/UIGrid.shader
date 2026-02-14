Shader "UI/GridOverlay_Improved"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _GridColor     ("Grid Color", Color) = (0.2,0.2,0.2,1)
        _GridThickness ("Grid Thickness (px)", Range(0.0, 5.0)) = 1.0

        // C#から自動セットする前提（初期値は仮）
        _RectSize      ("Rect Size (pixels)", Vector) = (900,900,0,0)

        // 追加：グリッドの分割数（cols, rows）
        _GridCount     ("Grid Count (cols,rows)", Vector) = (32,32,0,0)

        // 追加：ON/OFF（0=off, 1=on）
        _GridEnabled   ("Grid Enabled", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha


        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
            };

            float4 _GridColor;
            float  _GridThickness;
            float4 _RectSize;
            float4 _GridCount;
            float  _GridEnabled;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // OFFなら即透明（開始時OFFもこれでOK）
                if (_GridEnabled < 0.5)
                    return fixed4(0,0,0,0);

                float2 rect = max(_RectSize.xy, 1e-5);

                // cols/rows（最低1）
                float2 count = max(_GridCount.xy, 1.0);

                // セルのピクセルサイズを「分割数」から決定
                float2 cell = rect / count;

                // UV -> pixel position
                float2 p = i.uv * rect;

                // セル内座標 [0..cell)
                float2 f = frac(p / cell) * cell;

                // 境界までの距離（ピクセル）
                float2 d2 = min(f, cell - f);
                float  d  = min(d2.x, d2.y);

                // AA: 画面微分で境界幅を安定化（線がボケ過ぎたり欠けにくい）
                float aa = max(fwidth(d), 1e-4);

                // thickness(px) 以内を線として残す（外側へなだらかに）
                float lineMask = 1.0 - smoothstep(_GridThickness, _GridThickness + aa, d);

                return fixed4(_GridColor.rgb, _GridColor.a * lineMask);
            }
            ENDCG
        }
    }
}
