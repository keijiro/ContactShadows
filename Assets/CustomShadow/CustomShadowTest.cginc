#include "UnityCG.cginc"

sampler2D _MainTex;
sampler2D _CameraDepthTexture;

float3 _LightDirection;

struct Varyings
{
    float4 position : SV_POSITION;
    float2 texcoord : TEXCOORD0;
};

Varyings Vertex(uint vertexID : SV_VertexID)
{
    float x = (vertexID != 1) ? -1 : 3;
    float y = (vertexID == 2) ? -3 : 1;
    float4 vpos = float4(x, y, 1, 1);

    Varyings o;
    o.position = vpos;
    o.texcoord = (vpos.xy + 1) / 2;
    return o;
}

float SampleDepth(float2 uv)
{
    float z = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
#if defined(UNITY_REVERSED_Z)
    z = 1 - z;
#endif
    return z;
}

float3 InverseProjectUVZ(float2 uv, float z)
{
    float4 cp = float4(float3(uv, z) * 2 - 1, 1);
    float4 vp = mul(unity_CameraInvProjection, cp);
    vp.xyz /= vp.w;
    vp.z = -vp.z;
    return vp;
}

float3 InverseProjectUV(float2 uv)
{
    float z = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
#if defined(UNITY_REVERSED_Z)
    z = 1 - z;
#endif
    return InverseProjectUVZ(uv, z);
}

float2 ProjectVP(float3 vp)
{
    vp.z = -vp.z;
    float4 cp = mul(unity_CameraProjection, float4(vp, 1));
    cp.xyz /= cp.w;
    return cp.xy / 2 + 0.5;
}

float4 Fragment(Varyings input) : SV_Target
{
    float4 src = tex2D(_MainTex, input.texcoord);

    // Depth sample at the origin
    float2 uv0 = input.texcoord;
    float z0 = SampleDepth(uv0);
    if (z0 > 0.999999) return 0; // BG early-out

    // View space position of the origin
    float3 vp = InverseProjectUVZ(uv0, z0);

    float alpha = 0;

    // Move along the light vector and get a depth sample.
    [loop]
    for (int i = 1; i < 128; i++)
    {
        float3 vp2 = vp + _LightDirection * 0.005 * i;
        float2 uv2 = ProjectVP(vp2);

        // Resample the depth at the displaced point.
        float3 vp3 = InverseProjectUV(uv2);

        if (vp3.z < vp2.z - 0.01 && vp2.z - vp3.z < 0.2) return 0;//src * i / 256;
    }

    return src;
}
