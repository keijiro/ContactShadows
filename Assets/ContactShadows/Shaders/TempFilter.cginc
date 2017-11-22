// Experimental implementation of contact shadows for Unity
// https://github.com/keijiro/ContactShadows

#include "Common.cginc"

// Check if the gather instructions are available.
#if SHADER_TARGET > 40
    #if defined(TEMP_FILTER_ALT)
        #define TEMP_FILTER_GATHER_ALT
    #else
        #define TEMP_FILTER_GATHER
    #endif
#endif

// Shadow mask from the previous frame
sampler2D _PrevMask;

// Temporary result buffer
UNITY_DECLARE_TEX2D(_TempMask);
float4 _TempMask_TexelSize;

// Temporal filter coefficients
float4x4 _Reprojection;
half _Convergence;

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

// Fragment shader - Temporal filter pass
void FragmentTempFilter(
    float2 uv : TEXCOORD,
    out half4 mask : SV_Target0,
    out half4 history : SV_Target1
)
{
    #if defined(TEMP_FILTER_GATHER)

    // Neighborhood clamping sampling pattern for even frames
    //    +--+--+
    //    |R2|G2|
    // +--+--+--+
    // |R1|G1|B2|
    // +--+--+--+   Y
    // |A1|B1|      |
    // +--+--+      +--X

    float2 offs = _TempMask_TexelSize.xy * 0.5;

    half4 s1 = _TempMask.Gather(sampler_TempMask, uv + offs * float2(-1, -1));
    half4 s2 = _TempMask.Gather(sampler_TempMask, uv + offs * float2(+1, +1));

    float smin = min(min(min(min(min(min(s1.r, s1.g), s1.b), s1.a), s2.r), s2.g), s2.b);
    float smax = max(max(max(max(max(max(s1.r, s1.g), s1.b), s1.a), s2.r), s2.g), s2.b);

    float scenter = s1.g;

    #elif defined(TEMP_FILTER_GATHER_ALT)

    // Neighborhood clamping sampling pattern for odd frames
    // +--+--+
    // |R2|G2|
    // +--+--+--+
    // |A2|R1|G1|
    // +--+--+--+   Y
    //    |A1|B1|   |
    //    +--+--+   +--X

    float2 offs = _TempMask_TexelSize.xy * 0.5;

    half4 s1 = _TempMask.Gather(sampler_TempMask, uv + offs * float2(+1, -1));
    half4 s2 = _TempMask.Gather(sampler_TempMask, uv + offs * float2(-1, +1));

    float smin = min(min(min(min(min(min(s1.r, s1.g), s1.b), s1.a), s2.r), s2.g), s2.b);
    float smax = max(max(max(max(max(max(s1.r, s1.g), s1.b), s1.a), s2.r), s2.g), s2.b);

    float scenter = s1.r;

    #else

    // Neighborhood clamping without texgather

    float4 offs = _TempMask_TexelSize.xyxy * float4(1, 1, -1, 0);

    float s1 = UNITY_SAMPLE_TEX2D(_TempMask, uv - offs.xy).r;
    float s2 = UNITY_SAMPLE_TEX2D(_TempMask, uv - offs.wy).r;
    float s3 = UNITY_SAMPLE_TEX2D(_TempMask, uv - offs.zy).r;

    float s4 = UNITY_SAMPLE_TEX2D(_TempMask, uv - offs.xw).r;
    float s5 = UNITY_SAMPLE_TEX2D(_TempMask, uv          ).r;
    float s6 = UNITY_SAMPLE_TEX2D(_TempMask, uv + offs.xw).r;

    float s7 = UNITY_SAMPLE_TEX2D(_TempMask, uv + offs.xy).r;
    float s8 = UNITY_SAMPLE_TEX2D(_TempMask, uv + offs.wy).r;
    float s9 = UNITY_SAMPLE_TEX2D(_TempMask, uv + offs.zy).r;

    float smin = min(min(min(min(min(min(min(min(s1, s2), s3), s4), s5), s6), s7), s8), s9);
    float smax = max(max(max(max(max(max(max(max(s1, s2), s3), s4), s5), s6), s7), s8), s9);

    float scenter = s5;

    #endif

    // Get the previous frame sample and clamp it with the neighborhood samples.
    float sprev = tex2D(_PrevMask, uv - CalculateMovec(uv)).r;
    sprev = clamp(sprev, smin, smax);

    // Output
    history = mask = lerp(sprev, scenter, _Convergence);
}
