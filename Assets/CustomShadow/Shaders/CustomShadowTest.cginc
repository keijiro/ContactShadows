#include "Common.cginc"

// Light vector
// (reversed light direction in view space) * (ray-trace sample interval)
float3 _LightVector;

// Depth rejection threshold that determines the depth of each pixels.
float _RejectionDepth;

// Edge sharpness parameter
float _Sharpness;

// Total sample count
uint _SampleCount;

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

float4 Fragment(Varyings input) : SV_Target
{
    // Random number seed
    uint seed =
        input.texcoord.x * _CameraDepthTexture_TexelSize.z * 200 +
        input.texcoord.y * _CameraDepthTexture_TexelSize.w * 2000000;

    // View space position of the origin
    float z0 = SampleRawDepth(input.texcoord);
    if (z0 > 0.999999) return 0; // BG early-out
    float3 vp0 = InverseProjectUVZ(input.texcoord, z0);

    // Ray-tracing loop from the origin along the reverse light direction
    float alpha = 1 - 0.25 + Random(seed + 10) * 0.5;
    float offs = Random(seed) * 2;

    UNITY_LOOP for (uint i = 0; i < _SampleCount; i++)
    {
        // View space position of the ray sample
        float3 vp_ray = vp0 + _LightVector * (i + offs);

        // View space position of the depth sample
        float3 vp_depth = InverseProjectUV(ProjectVP(vp_ray));

        // Depth difference between ray/depth sample
        // Negative: Ray sample is closer to the camera (not occluded)
        // Positive: Ray sample is beyond the depth sample (possibly occluded)
        float diff = vp_ray.z - vp_depth.z;

        // Occlusion test
        float rej = _RejectionDepth * (1 + Random(seed + i)) / 2;
        if (diff > 0 && diff < rej) alpha -= 0.5;

        // Completely occluded.
        if (alpha <= 0) return 0;
    }

    return saturate(alpha);
}
