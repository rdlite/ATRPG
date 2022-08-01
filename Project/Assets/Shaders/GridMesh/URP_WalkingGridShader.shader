Shader "GrdiViews/WalkGrid"
{
    Properties { 
        [NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
        [NoScaleOffset] _DelinkingNodesMap("Delinking nodes vap", 2D) = "white" {}
        _FillTransparent("Fill transparent", Range(0, 1)) = 1
        _FillColor("Fill color", Color) = (1,1,1,1)
        _OutlineColor("Outline color", Color) = (1,1,1,1)
        _TextureOffset("Texture offset", Float) = 1
        _Smooth0("Smooth0", Float) = 0
        _Smooth1("Smooth1", Float) = 1
        _Smooth2("Smooth2", Float) = 0
        _Smooth3("Smooth3", Float) = 1
        _OutlineThickness("Outline thickness", Range(0, 1)) = .1
        _InlineAlphaThickness("Inline alpha thickness", Range(0, 4)) = 2
        _InlineAlphaColor("Inline alpha color", Color) = (1,1,1,1)
    }
    SubShader{
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+1" "RenderPipeline" = "UniversalRenderPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On

        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"            

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_DelinkingNodesMap);
            SAMPLER(sampler_DelinkingNodesMap);

            CBUFFER_START(UnityPerMaterial)
                float _InlineAlphaThickness;
                float _Smooth0;
                float _Smooth1;
                float _Smooth2;
                float _Smooth3;
                float _OutlineThickness;
                float _TextureOffset;
                float _FillTransparent;
                float4 _FillColor;
                float4 _InlineAlphaColor;
                float4 _OutlineColor;
                float4 _DelinkingNodesMap_ST;
                float4 _MainTex_ST;
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
                float4 delinkingMapCol = SAMPLE_TEXTURE2D(_DelinkingNodesMap, sampler_DelinkingNodesMap, i.uv + float2(_TextureOffset, _TextureOffset));
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(_TextureOffset, _TextureOffset)) * delinkingMapCol;
                float4 startCol = col;

                float inlineAlpha = 1 - pow(smoothstep(col.r, _Smooth0, _Smooth1), _InlineAlphaThickness);

                col = round(col);

                float4 outline = startCol;
                outline = saturate(float4(round(outline).r * _OutlineThickness - startCol.r, 0, 0, 1) * 4 * _OutlineThickness) > .99;
                outline = outline.xxxx;

                float4 inlineColored = float4(_InlineAlphaColor.rgb, smoothstep(inlineAlpha, _Smooth2, _Smooth3));

                if (inlineColored.a < .1) {
                    inlineColored.a = 0;
                }

                float4 outColor = outline * _OutlineColor + (col - outline) * (_FillColor - ((1 - inlineColored) * inlineColored.a)) * (_FillTransparent + inlineColored.a);

                return outColor * col.r;
            }
            ENDHLSL
        }
    }
}