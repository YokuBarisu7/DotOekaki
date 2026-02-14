Shader "UI/CircleColorSpectrum"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord * 2 - 1; // [-1,1] に変換
                return o;
            }

            fixed4 HsvToRgb(float h, float s, float v)
            {
                float3 rgb = clamp(abs(fmod(h * 6 + float3(0, 4, 2), 6) - 3) - 1, 0, 1);
                return fixed4((rgb - 0.5) * s + 0.5, 1) * v;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                float dist = length(uv);

                // 円の外側を透明に
                if (dist > 1.0) discard;

                // 角度を計算 (0〜1 に正規化)
                float angle = atan2(uv.y, uv.x) / (2 * 3.1415926) + 0.5;

                // 中心を白くするために、距離で色を補間
                float t = smoothstep(0.0, 0.0, dist);  // 0 〜 0.5 で白 → スペクトラムに変化
                fixed4 color = lerp(fixed4(1,1,1,1), HsvToRgb(angle, 1, 1), t);

                return color;
            }
            ENDCG
        }
    }
}