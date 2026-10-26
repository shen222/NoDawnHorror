Shader "Custom/ColorPulse"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 1
        _Intensity ("Color Intensity", Range(0, 1)) = 0.3
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
            float _PulseSpeed;
            float _Intensity;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                float time = _Time.y * _PulseSpeed;

                float r = 0.5 + 0.5 * sin(time);
                float g = 0.5 + 0.5 * sin(time + 2.0);
                float b = 0.5 + 0.5 * sin(time + 4.0);
                float3 pulseColor = float3(r, g, b);

                float3 original = tex2D(_MainTex, uv).rgb;
                float3 finalColor = lerp(original, pulseColor, _Intensity);

                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}
