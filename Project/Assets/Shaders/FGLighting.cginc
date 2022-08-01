#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : TEXCOORD1;
    float3 wPos : TEXCOORD2;
    LIGHTING_COORDS(3, 4)
};

sampler2D _MainTex;
float4 _MainTex_ST;
float _Gloss;
float4 _SurfaceColor;
float _GlossPower;
float _FresnelPower;

float Lambert(float3 lightDir, float3 surfNormal)
{
    return saturate(dot(lightDir, surfNormal));
}

v2f vert (appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    o.normal = UnityObjectToWorldNormal(v.normal);
    o.wPos = mul(unity_ObjectToWorld, v.vertex);
    TRANSFER_VERTEX_TO_FRAGMENT(o);

    return o;
}

float4 frag (v2f i) : COLOR
{
    float3 N = normalize(i.normal);

    float attenuation = LIGHT_ATTENUATION(i);
    float3 L = normalize(UnityWorldSpaceLightDir(i.wPos)) * attenuation;

    float diffusion = Lambert(L, N);
    float4 ambientDiffusion = lerp(UNITY_LIGHTMODEL_AMBIENT, _LightColor0.rgba, diffusion);

    float3 dirToCamera = normalize(_WorldSpaceCameraPos - i.wPos);
    float3 halfVector = normalize(L + dirToCamera);
    float specularValue = saturate(dot(halfVector, N));
    float specularExponent = exp2(_Gloss * 11) + 2;
    specularValue = pow(specularValue, specularExponent) * (_Gloss * _GlossPower) * attenuation;

    float fresnel = 1 - saturate(dot(dirToCamera, N));

    float4 col = tex2D(_MainTex, i.uv);
    col *= _SurfaceColor;
    col = col * ambientDiffusion + specularValue;
    col += pow(fresnel, 3) / 2 * _FresnelPower * attenuation;

    return col;
}