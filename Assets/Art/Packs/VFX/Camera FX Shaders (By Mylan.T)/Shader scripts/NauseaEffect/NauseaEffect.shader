Shader "Custom/NauseaEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WaveStrength ("Wave Strength", Range(0, 0.1)) = 0.03
        _WaveSpeed ("Wave Speed", Range(0, 20)) = 5.0
        _ChromaticAmount ("Chromatic Aberration", Range(0, 5)) = 1.5
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
            float _WaveStrength;
            float _WaveSpeed;
            float _ChromaticAmount;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                uv.x += sin(uv.y * 30.0 + _Time.y * _WaveSpeed) * _WaveStrength;
                uv.y += cos(uv.x * 30.0 + _Time.y * _WaveSpeed * 0.6) * _WaveStrength;

                float2 offset = (_ChromaticAmount / 1000.0);

                float r = tex2D(_MainTex, uv + offset).r;
                float g = tex2D(_MainTex, uv).g;
                float b = tex2D(_MainTex, uv - offset).b;

                return fixed4(r, g, b, 1.0);
            }
            ENDCG
        }
    }
}
