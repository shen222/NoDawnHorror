Shader "NoDawn/Classic Laser Sight"
{
    Properties { _Color ("Laser Color", Color) = (1, 0.04, 0.025, 0.9) }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            fixed4 _Color;
            Varyings vert(Input input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }
            fixed4 frag(Varyings input) : SV_Target
            {
                float edge = abs(input.uv.y * 2 - 1);
                return fixed4(_Color.rgb, _Color.a * (1 - smoothstep(0.2, 1, edge)));
            }
            ENDCG
        }
    }
}
