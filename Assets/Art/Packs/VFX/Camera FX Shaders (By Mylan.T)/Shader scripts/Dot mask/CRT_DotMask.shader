Shader "Custom/CRT_DotMask"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _DotSize ("Dot Size", Range(100, 1000)) = 300
        _DotIntensity ("Dot Intensity", Range(0, 1)) = 0.3
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
            float _DotSize;
            float _DotIntensity;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float2 pixelPos = uv * _DotSize;

                float2 cell = frac(pixelPos);

                float rMask = smoothstep(0.4, 0.2, distance(cell, float2(0.2, 0.5)));
                float gMask = smoothstep(0.4, 0.2, distance(cell, float2(0.5, 0.5)));
                float bMask = smoothstep(0.4, 0.2, distance(cell, float2(0.8, 0.5)));

                float3 dotMask = float3(rMask, gMask, bMask);

                float3 original = tex2D(_MainTex, uv).rgb;

                float3 finalColor = original * (1 - _DotIntensity) + (original * dotMask) * _DotIntensity;

                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}

