Shader "Custom/RetroEffect"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.5
        _ColorFade ("Color Fade", Range(0, 1)) = 0.2
        _NoiseStrength ("Noise Strength", Range(0, 0.2)) = 0.05
        _Distortion ("Distortion Amount", Range(0, 0.05)) = 0.02
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
            float _ScanlineIntensity;
            float _ColorFade;
            float _NoiseStrength;
            float _Distortion;
            float4 _MainTex_TexelSize;

            float rand(float2 uv)
            {
                return frac(sin(dot(uv.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                uv.x += sin(uv.y * 120.0 + _Time.y * 5.0) * _Distortion;

                float r = tex2D(_MainTex, uv + float2(0.002, 0)).r;
                float g = tex2D(_MainTex, uv).g;
                float b = tex2D(_MainTex, uv - float2(0.002, 0)).b;

                fixed4 col = fixed4(r, g, b, 1.0);

                float scan = sin(uv.y * 800.0) * _ScanlineIntensity;
                col.rgb -= scan;

                col.rgb = floor(col.rgb * (6 - _ColorFade * 5)) / (6 - _ColorFade * 5);

                float noise = (rand(uv * _Time.y) - 0.5) * _NoiseStrength;
                col.rgb += noise;

                return col;
            }
            ENDCG
        }
    }
}
