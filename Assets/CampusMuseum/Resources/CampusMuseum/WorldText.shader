Shader "CampusMuseum/WorldText"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct VertexData
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;

            Varyings Vert(VertexData vertex)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(vertex);
                UNITY_INITIALIZE_OUTPUT(Varyings, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.position = UnityObjectToClipPos(vertex.position);
                output.uv = vertex.uv;
                output.color = vertex.color;
                return output;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                return fixed4(input.color.rgb, input.color.a * tex2D(_MainTex, input.uv).a);
            }
            ENDCG
        }
    }
}
