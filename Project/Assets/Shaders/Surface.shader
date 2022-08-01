Shader "Unlit/Surface"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SurfaceColor("Surface Color", Color) = (1,1,1,1)
        _Gloss("Smoothness", Range(0, 1)) = 0
        _GlossPower("Gloss power", Range(1, 10)) = 4
        _FresnelPower("Fresnel power", Range(0, 4)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 100
        
        // base surface pass
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "FGLighting.cginc"

            ENDCG
        }

        // add surface pass
        Pass
        {
            Tags { "LightMode" = "ForwardAdd" }
            Blend One One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd

            #include "FGLighting.cginc"

            ENDCG
        }
    }
}