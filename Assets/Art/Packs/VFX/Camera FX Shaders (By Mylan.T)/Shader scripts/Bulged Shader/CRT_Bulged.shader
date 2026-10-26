Shader "Custom/CRT_Bulged"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _DistortionAmount ("Distortion Amount", Range(0, 0.5)) = 0.1
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
            float _DistortionAmount;

            float2 BarrelDistortion(float2 uv, float amt)
            {
                float2 cc = uv - 0.5;
                float dist = dot(cc, cc);
                uv = uv + cc * dist * amt;
                return uv;
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                uv = BarrelDistortion(uv, _DistortionAmount);

                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                    return fixed4(0,0,0,1);

                fixed4 col = tex2D(_MainTex, uv);

                return col;
            }
            ENDCG
        }
    }
}
