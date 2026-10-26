Shader "Hidden/REimagined/RetroScreen"
{
    Properties { _MainTex ("Image", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float4 _MainTex_TexelSize;
            float4 _Atmosphere, _AtmosphereTint, _FogColor;
            float4 _Resolution, _Color;
            float4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float2 pixel = floor(uv * _Resolution.xy);
                float4 col = tex2D(_MainTex, uv);
                // Color and depth are sampled together before the downsample.
                float2 depthUV = uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0) depthUV.y = 1 - depthUV.y;
                #endif
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, depthUV);
                float eyeDepth = LinearEyeDepth(rawDepth);
                if (unity_OrthoParams.w > 0.5)
                {
                    #if defined(UNITY_REVERSED_Z)
                    rawDepth = 1 - rawDepth;
                    #endif
                    eyeDepth = lerp(_ProjectionParams.y, _ProjectionParams.z, rawDepth);
                }
                float luminance = dot(col.rgb, float3(0.299, 0.587, 0.114));
                float3 muted = lerp(luminance.xxx, col.rgb, _Atmosphere.y) * _AtmosphereTint.rgb;
                float fog = saturate((eyeDepth - _Atmosphere.z) / max(0.1, _Atmosphere.w - _Atmosphere.z));
                fog = fog * fog * (3 - 2 * fog);
                col.rgb = lerp(col.rgb, lerp(muted, _FogColor.rgb, fog), _Atmosphere.x);
                return float4(saturate(col.rgb), col.a);
            }
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Resolution, _Color;
            float4 frag(v2f_img i) : SV_Target
            {
                float2 pixel = floor(i.uv * _Resolution.xy);
                float4 col = tex2D(_MainTex, i.uv);
                // Ordered 4x4 Bayer dithering before quantization to 5-bit-like colors.
                float2 p = fmod(pixel, 4);
                float2 lo = fmod(p, 2);
                float2 hi = floor(p / 2);
                float bayerLo = fmod(lo.x + lo.y, 2) * 2 + lo.y;
                float bayerHi = fmod(hi.x + hi.y, 2) * 2 + hi.y;
                float dither = (bayerLo * 4 + bayerHi + 0.5) / 16 - 0.5;
                float levels = max(2, _Color.x - 1);
                col.rgb = floor(saturate(col.rgb + dither * _Color.y / levels) * levels + 0.5) / levels;
                return float4(saturate(col.rgb), col.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
