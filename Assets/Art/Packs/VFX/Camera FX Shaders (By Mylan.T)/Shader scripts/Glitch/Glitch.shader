Shader "Custom/Glitch"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Intensity", Range(0, 1)) = 0.5
        _TimeSpeed ("Time Speed", Float) = 1.0
        _BlockSize ("Block Size", Float) = 10.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Intensity;
            float _TimeSpeed;
            float _BlockSize;
            float4 _MainTex_TexelSize;

            float rand(float2 co)
            {
                return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                float time = _Time.y * _TimeSpeed;

                // Random horizontal offset
                float block = floor(uv.y * _BlockSize);
                float glitch = rand(float2(block, time)) * _Intensity;

                // Glitch shift (horizontal RGB split)
                float rShift = glitch * 0.05;
                float gShift = glitch * 0.02;
                float bShift = glitch * -0.03;

                fixed r = tex2D(_MainTex, uv + float2(rShift, 0)).r;
                fixed g = tex2D(_MainTex, uv + float2(gShift, 0)).g;
                fixed b = tex2D(_MainTex, uv + float2(bShift, 0)).b;

                fixed4 col = fixed4(r, g, b, 1);

                if (rand(float2(time * 5.0, uv.y)) < _Intensity * 0.2)
                {
                    col.rgb += rand(uv * time) * 0.2;
                }

                return col;
            }
            ENDCG
        }
    }
}
