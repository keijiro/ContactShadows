// Experimental implementation of contact shadows for Unity
// https://github.com/keijiro/ContactShadows

#include "Common.cginc"

#if defined(TEX_GATHER_AVAILABLE)
Texture2D _CameraDepthTexture;
Texture2D _TempMask;
SamplerState sampler_CameraDepthTexture;
SamplerState sampler_TempMask;
#else
sampler2D _CameraDepthTexture;
sampler2D _TempMask;
#endif
float4 _CameraDepthTexture_TexelSize;
float4 _TempMask_TexelSize;

float GWeight(float zbase, float z)
{
    return 1 / (1 + 10000 * abs(z - zbase));
}

half4 FragmentComposite(float2 uv : TEXCOORD) : SV_Target
{
    #if defined(TEX_GATHER_AVAILABLE)

    float2 offs = _TempMask_TexelSize.xy * 0.5;

    half4 sg1 = _TempMask.Gather(sampler_TempMask, uv + offs * float2(-1, -1));
    half4 sg2 = _TempMask.Gather(sampler_TempMask, uv + offs * float2(+1, -1));
    half4 sg3 = _TempMask.Gather(sampler_TempMask, uv + offs * float2(-1, +1));
    half4 sg4 = _TempMask.Gather(sampler_TempMask, uv + offs * float2(+1, +1));

    half4 zg1 = _CameraDepthTexture.Gather(sampler_CameraDepthTexture, uv + offs * float2(-1, -1));
    half4 zg2 = _CameraDepthTexture.Gather(sampler_CameraDepthTexture, uv + offs * float2(+1, -1));
    half4 zg3 = _CameraDepthTexture.Gather(sampler_CameraDepthTexture, uv + offs * float2(-1, +1));
    half4 zg4 = _CameraDepthTexture.Gather(sampler_CameraDepthTexture, uv + offs * float2(+1, +1));

    half s0 = sg1.a;
    half s1 = sg1.b;
    half s2 = sg2.b;
    half s3 = sg1.r;
    half s4 = sg1.g;
    half s5 = sg2.g;
    half s6 = sg3.r;
    half s7 = sg3.g;
    half s8 = sg4.g;

    half z0 = zg1.a;
    half z1 = zg1.b;
    half z2 = zg2.b;
    half z3 = zg1.r;
    half z4 = zg1.g;
    half z5 = zg2.g;
    half z6 = zg3.r;
    half z7 = zg3.g;
    half z8 = zg4.g;

    #else // TEX_GATHER_AVAILABLE

    float4 duv = _TempMask_TexelSize.xyxy * float4(1, 1, -1, 0) * 1.25;

    float2 uv0 = uv - duv.xy;
    float2 uv1 = uv - duv.wy;
    float2 uv2 = uv - duv.zy;
    float2 uv3 = uv + duv.zw;
    float2 uv4 = uv;
    float2 uv5 = uv + duv.xw;
    float2 uv6 = uv + duv.zy;
    float2 uv7 = uv + duv.wy;
    float2 uv8 = uv + duv.xy;

    float z0 = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv0, 0, 0));
    float z1 = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv1, 0, 0));
    float z2 = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv2, 0, 0));
    float z3 = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv3, 0, 0));
    float z4 = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv4, 0, 0));
    float z5 = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv5, 0, 0));
    float z6 = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv6, 0, 0));
    float z7 = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv7, 0, 0));
    float z8 = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv8, 0, 0));

    half s0 = tex2D(_TempMask, uv0).r;
    half s1 = tex2D(_TempMask, uv1).r;
    half s2 = tex2D(_TempMask, uv2).r;
    half s3 = tex2D(_TempMask, uv3).r;
    half s4 = tex2D(_TempMask, uv4).r;
    half s5 = tex2D(_TempMask, uv5).r;
    half s6 = tex2D(_TempMask, uv6).r;
    half s7 = tex2D(_TempMask, uv7).r;
    half s8 = tex2D(_TempMask, uv8).r;

    #endif // TEX_GATHER_AVAILABLE

    float w0 = GWeight(z4, z0);
    float w1 = GWeight(z4, z1);
    float w2 = GWeight(z4, z2);
    float w3 = GWeight(z4, z3);
    float w4 = 1;
    float w5 = GWeight(z4, z5);
    float w6 = GWeight(z4, z6);
    float w7 = GWeight(z4, z7);
    float w8 = GWeight(z4, z8);

    return (s0 * w0 + s1 * w1 + s2 * w2 + s3 * w3 + s4 * w4 + s5 * w5 + s6 * w6 + s7 * w7 + s8 * w8) /
        (w0 + w1 + w2 + w3 + w4 + w5 + w6 + w7 + w8);
}
