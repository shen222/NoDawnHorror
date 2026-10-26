Shader "Custom/CameraBlur"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 1.0
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
            float4 _MainTex_TexelSize; // (1/width, 1/height, width, height)
            float _BlurSize;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float2 offset = _MainTex_TexelSize.xy * _BlurSize;

                fixed4 col = fixed4(0,0,0,0);
                col += tex2D(_MainTex, uv + offset * float2(-1, -1)) * 0.0625;
                col += tex2D(_MainTex, uv + offset * float2( 0, -1)) * 0.125;
                col += tex2D(_MainTex, uv + offset * float2( 1, -1)) * 0.0625;

                col += tex2D(_MainTex, uv + offset * float2(-1,  0)) * 0.125;
                col += tex2D(_MainTex, uv + offset * float2( 0,  0)) * 0.25;
                col += tex2D(_MainTex, uv + offset * float2( 1,  0)) * 0.125;

                col += tex2D(_MainTex, uv + offset * float2(-1,  1)) * 0.0625;
                col += tex2D(_MainTex, uv + offset * float2( 0,  1)) * 0.125;
                col += tex2D(_MainTex, uv + offset * float2( 1,  1)) * 0.0625;

                return col;
            }
            ENDCG
        }
    }
}
