// Experimental implementation of contact shadows for Unity
// https://github.com/keijiro/ContactShadows

#include "UnityCG.cginc"

// Camera depth texture
sampler2D _CameraDepthTexture;
float4 _CameraDepthTexture_TexelSize;

// Noise texture (used for dithering)
sampler2D _NoiseTex;
float2 _NoiseScale;

// Light vector
// (reversed light direction in view space) * (ray-trace sample interval)
float3 _LightVector;

// Depth rejection threshold that determines the depth of each pixels.
float _RejectionDepth;

// Total sample count
uint _SampleCount;

// Temporal filter variables
sampler2D _PrevMask;
#if 1
Texture2D _TempMask;
SamplerState sampler_TempMask;
#else
sampler2D _TempMask;
#endif
sampler2D _ShadowMask;
float4x4 _Reprojection;
half _Convergence;

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

// Calculate the motion vector with using the reprojection matrix
float2 CalculateMovec(float2 uv)
{
    float4 cp = mul(_Reprojection, float4(InverseProjectUV(uv), 1));
    float2 prev = (cp.xy / cp.w + 1) * 0.5;
#if UNITY_UV_STARTS_AT_TOP
    prev.y = 1 - prev.y;
#endif
    return uv - prev;
}

//
// Vertex shader - Full-screen triangle with procedural draw
//
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

//
// Fragment shader - Screen space ray-trancing shadow pass
//
half4 FragmentShadow(float2 uv : TEXCOORD) : SV_Target
{
    float mask = tex2D(_ShadowMask, uv).r;
    if (mask < 0.01) return mask;

    // Temporal distributed noise offset
    float offs = tex2D(_NoiseTex, uv * _NoiseScale).a;

    // View space position of the origin
    float z0 = SampleRawDepth(uv);
    if (z0 > 0.999999) return mask; // BG early-out
    float3 vp0 = InverseProjectUVZ(uv, z0);

    // Ray-tracing loop from the origin along the reverse light direction
    UNITY_LOOP for (uint i = 0; i < _SampleCount; i++)
    {
        // View space position of the ray sample
        float3 vp_ray = vp0 + _LightVector * (i + offs * 2);

        // View space position of the depth sample
        float3 vp_depth = InverseProjectUV(ProjectVP(vp_ray));

        // Depth difference between ray/depth sample
        // Negative: Ray sample is closer to the camera (not occluded)
        // Positive: Ray sample is beyond the depth sample (possibly occluded)
        float diff = vp_ray.z - vp_depth.z;

        // Occlusion test
        if (diff > 0.01 * (1 - offs) && diff < _RejectionDepth) return 0;
    }

    return mask;
}

//
// Neightborhood clamping sampling patterns
//
// Even frames
//    +--+--+
//    |R2|G2|
// +--+--+--+
// |R1|G1|B2|
// +--+--+--+   Y
// |A1|B1|      |
// +--+--+      +--X
//
// Odd frames
// +--+--+
// |R2|G2|
// +--+--+--+
// |A2|R1|G1|
// +--+--+--+
//    |A1|B1|
//    +--+--+
//

//
// Fragment shader - Temporal reprojection filter pass
//
void FragmentTempFilter(
    float2 uv : TEXCOORD,
    out half4 mask : SV_Target0,
    out half4 history : SV_Target1
)
{
#if 1
    float2 ht = _CameraDepthTexture_TexelSize.xy * 0.5;
#ifndef TEMP_FILTER_ALT
    half4 s1 = _TempMask.Gather(sampler_TempMask, uv + ht * float2(-1, -1));
    half4 s2 = _TempMask.Gather(sampler_TempMask, uv + ht * float2(+1, +1));
    float s_min = min(min(min(min(min(min(s1.r, s1.g), s1.b), s1.a), s2.r), s2.g), s2.b);
    float s_max = max(max(max(max(max(max(s1.r, s1.g), s1.b), s1.a), s2.r), s2.g), s2.b);
    float s_center = s1.g;
#else
    half4 s1 = _TempMask.Gather(sampler_TempMask, uv + ht * float2(+1, -1));
    half4 s2 = _TempMask.Gather(sampler_TempMask, uv + ht * float2(-1, +1));
    float s_min = min(min(min(min(min(min(s1.r, s1.g), s1.b), s1.a), s2.r), s2.g), s2.a);
    float s_max = max(max(max(max(max(max(s1.r, s1.g), s1.b), s1.a), s2.r), s2.g), s2.a);
    float s_center = s1.r;
#endif
#else
    float4 duv = _CameraDepthTexture_TexelSize.xyxy * float4(1, 1, -1, 0) * 2;

    // Get the neighborhood min/max samples.
    float s1 = tex2D(_TempMask, uv - duv.xy).r;
    float s2 = tex2D(_TempMask, uv - duv.wy).r;
    float s3 = tex2D(_TempMask, uv - duv.zy).r;

    float s4 = tex2D(_TempMask, uv - duv.xw).r;
    float s5 = tex2D(_TempMask, uv         ).r;
    float s6 = tex2D(_TempMask, uv + duv.xw).r;

    float s7 = tex2D(_TempMask, uv + duv.xy).r;
    float s8 = tex2D(_TempMask, uv + duv.wy).r;
    float s9 = tex2D(_TempMask, uv + duv.zy).r;

    float s_min = min(min(min(min(min(min(min(min(s1, s2), s3), s4), s5), s6), s7), s8), s9);
    float s_max = max(max(max(max(max(max(max(max(s1, s2), s3), s4), s5), s6), s7), s8), s9);

    float s_center = s5;
#endif

    // Get the previous frame sample and clamp it with the neighborhood samples.
    float s_prev = tex2D(_PrevMask, uv - CalculateMovec(uv)).r;
    s_prev = clamp(s_prev, s_min, s_max);

    // Output
    history = mask = lerp(s_prev, s_center, _Convergence);
}
