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

// Noise texture (used for dithering)
sampler2D _NoiseTex;
float2 _NoiseScale;

// Temporal filter variables
sampler2D _PrevMask;
sampler2D _TempMask;
sampler2D _ShadowMask;
fixed _Convergence;

float4x4 _PreviousVP;
float4x4 _NonJitteredVP;

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

float4 FragmentShadow(Varyings input) : SV_Target
{
    float mask = tex2D(_ShadowMask, input.texcoord).r;
    if (mask < 0.1) return mask;

    // Temporal distributed noise offset
    float offs = tex2D(_NoiseTex, input.texcoord * _NoiseScale).a;

    // View space position of the origin
    float z0 = SampleRawDepth(input.texcoord);
    if (z0 > 0.999999) return mask; // BG early-out
    float3 vp0 = InverseProjectUVZ(input.texcoord, z0);

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

struct CompositeOutput
{
    fixed4 mask : SV_Target0;
    fixed4 history : SV_Target1;
};

float2 CalculateMovec(float2 uv)
{
    float z = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv, 0, 0));

#if defined(UNITY_REVERSED_Z)
    z = 1 - z;
#endif
    float4 cp = float4(float3(uv, z) * 2 - 1, 1);
    float4 vp = mul(unity_CameraInvProjection, cp);
    vp /= vp.w;
    vp.z = -vp.z;
    float4 wp = mul(unity_CameraToWorld, vp);

    float4 prevClipPos = mul(_PreviousVP, wp);
    float4 curClipPos = cp * float4(1, -1, 1, 1);

    float2 prevHPos = prevClipPos.xy / prevClipPos.w;
    float2 curHPos = curClipPos.xy / curClipPos.w;

    float2 vPosPrev = (prevHPos.xy + 1.0f) / 2.0f;
    float2 vPosCur = (curHPos.xy + 1.0f) / 2.0f;
#if UNITY_UV_STARTS_AT_TOP
    vPosPrev.y = 1.0 - vPosPrev.y;
    vPosCur.y = 1.0 - vPosCur.y;
#endif
    return vPosCur - vPosPrev;
}

CompositeOutput FragmentComposite(Varyings input)
{
    float2 uv = input.texcoord;
    float4 duv = _CameraDepthTexture_TexelSize.xyxy * float4(1, 1, -1, 0) * 2;

    float prev = tex2D(_PrevMask, uv - CalculateMovec(uv)).r;

    float p1 = tex2D(_TempMask, uv - duv.xy).r;
    float p2 = tex2D(_TempMask, uv - duv.wy).r;
    float p3 = tex2D(_TempMask, uv - duv.zy).r;

    float p4 = tex2D(_TempMask, uv - duv.xw).r;
    float p5 = tex2D(_TempMask, uv         ).r;
    float p6 = tex2D(_TempMask, uv + duv.xw).r;

    float p7 = tex2D(_TempMask, uv + duv.xy).r;
    float p8 = tex2D(_TempMask, uv + duv.wy).r;
    float p9 = tex2D(_TempMask, uv + duv.zy).r;

    float mp1 = min(min(min(min(min(min(min(min(p1, p2), p3), p4), p5), p6), p7), p8), p9);
    float mp2 = max(max(max(max(max(max(max(max(p1, p2), p3), p4), p5), p6), p7), p8), p9);

    prev = clamp(prev, mp1, mp2);

    CompositeOutput o;
    o.history = o.mask = lerp(prev, p5, _Convergence);
    return o;
}
