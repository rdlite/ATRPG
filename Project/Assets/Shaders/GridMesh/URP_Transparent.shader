Shader "Custom/Transparent"
{
    Properties{
        _MainTex("Main texture", 2D) = "white" {}
        _Transparency("Transparency", Range(0, 1)) = 1
    }
    SubShader{
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalRenderPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On

        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"            

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMateria)
            float4 _MainTex_ST;
            float _Transparency;
            CBUFFER_END

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v) {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag(v2f i) : SV_Target {
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                col.a = _Transparency;
                return col;
            }
            ENDHLSL
        }
    }
}