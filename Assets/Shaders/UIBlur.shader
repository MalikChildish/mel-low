Shader "Custom/UIBlur"
{
    Properties
    {
        _Size  ("Blur Radius", Range(0, 6)) = 2
        _Color ("Tint Color",  Color)       = (0.08, 0.08, 0.12, 0.6)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        GrabPass { "_BGTex" }

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _BGTex;
            float4    _BGTex_TexelSize;
            float     _Size;
            fixed4    _Color;

            struct appdata { float4 vertex : POSITION; };
            struct v2f     { float4 pos : SV_POSITION; float4 grab : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos  = UnityObjectToClipPos(v.vertex);
                o.grab = ComputeGrabScreenPos(o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.grab.xy / i.grab.w;

                // 3x3 gaussian-weighted blur
                fixed4 col = fixed4(0,0,0,0);
                float  w[3] = { 0.25, 0.5, 0.25 };

                for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                {
                    float2 offset = float2(x, y) * _BGTex_TexelSize.xy * _Size;
                    col += tex2D(_BGTex, uv + offset) * (w[x+1] * w[y+1]);
                }

                // Blend tint over blurred background
                col.rgb = lerp(col.rgb, _Color.rgb, _Color.a);
                col.a   = 1;
                return col;
            }
            ENDCG
        }
    }
}
