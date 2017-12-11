// Experimental implementation of contact shadows for Unity
// https://github.com/keijiro/ContactShadows

#include "Common.cginc"

sampler2D _TempMask;
float4 _TempMask_TexelSize;

// Cross bilateral denoise filter

float GWeight(float zbase, float z)
{
    return 1 / (1 + 10000 * abs(z - zbase));
}

half4 FragmentComposite(float2 uv : TEXCOORD) : SV_Target
{
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

    float w0 = GWeight(z4, z0);
    float w1 = GWeight(z4, z1);
    float w2 = GWeight(z4, z2);
    float w3 = GWeight(z4, z3);
    float w4 = 1;
    float w5 = GWeight(z4, z5);
    float w6 = GWeight(z4, z6);
    float w7 = GWeight(z4, z7);
    float w8 = GWeight(z4, z8);

    half s0 = tex2D(_TempMask, uv0).r * w0;
    half s1 = tex2D(_TempMask, uv1).r * w1;
    half s2 = tex2D(_TempMask, uv2).r * w2;
    half s3 = tex2D(_TempMask, uv3).r * w3;
    half s4 = tex2D(_TempMask, uv4).r * w4;
    half s5 = tex2D(_TempMask, uv5).r * w5;
    half s6 = tex2D(_TempMask, uv6).r * w6;
    half s7 = tex2D(_TempMask, uv7).r * w7;
    half s8 = tex2D(_TempMask, uv8).r * w8;

    return (s0 + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8) /
        (w0 + w1 + w2 + w3 + w4 + w5 + w6 + w7 + w8);
}
