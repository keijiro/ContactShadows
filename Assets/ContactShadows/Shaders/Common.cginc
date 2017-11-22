// Experimental implementation of contact shadows for Unity
// https://github.com/keijiro/ContactShadows

#include "UnityCG.cginc"

// Camera depth texture
sampler2D _CameraDepthTexture;
float4 _CameraDepthTexture_TexelSize;

// Get a raw depth from the depth buffer.
float SampleRawDepth(float2 uv)
{
    float z = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv, 0, 0));
#if defined(UNITY_REVERSED_Z)
    z = 1 - z;
#endif
    return z;
}

// Inverse project UV + raw depth into the view space.
float3 InverseProjectUVZ(float2 uv, float z)
{
    float4 cp = float4(float3(uv, z) * 2 - 1, 1);
    float4 vp = mul(unity_CameraInvProjection, cp);
    return float3(vp.xy, -vp.z) / vp.w;
}

// Inverse project UV into the view space with sampling the depth buffer.
float3 InverseProjectUV(float2 uv)
{
    return InverseProjectUVZ(uv, SampleRawDepth(uv));
}

// Project a view space position into the clip space.
float2 ProjectVP(float3 vp)
{
    float4 cp = mul(unity_CameraProjection, float4(vp.xy, -vp.z, 1));
    return (cp.xy / cp.w + 1) * 0.5;
}

// Vertex shader - Full-screen triangle with procedural draw
float2 Vertex(
    uint vertexID : SV_VertexID,
    out float4 position : SV_POSITION
) : TEXCOORD
{
    float x = (vertexID != 1) ? -1 : 3;
    float y = (vertexID == 2) ? -3 : 1;
    position = float4(x, y, 1, 1);

    float u = (x + 1) / 2;
#ifdef UNITY_UV_STARTS_AT_TOP
    float v = (1 - y) / 2;
#else
    float v = (y + 1) / 2;
#endif
    return float2(u, v);
}
