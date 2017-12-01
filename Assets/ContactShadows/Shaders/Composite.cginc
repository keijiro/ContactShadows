// Experimental implementation of contact shadows for Unity
// https://github.com/keijiro/ContactShadows

#include "Common.cginc"

sampler2D _TempMask;
float4 _TempMask_TexelSize;

half4 FragmentComposite(float2 uv : TEXCOORD) : SV_Target
{
    float4 duv = _TempMask_TexelSize.xyxy * float4(1, 1, -1, 0) * 1.25;

    half s0 = tex2D(_TempMask, uv - duv.xy).r;
    half s1 = tex2D(_TempMask, uv - duv.wy).r * 2;
    half s2 = tex2D(_TempMask, uv - duv.zy).r;

    half s3 = tex2D(_TempMask, uv + duv.zw).r * 2;
    half s4 = tex2D(_TempMask, uv + duv.ww).r * 4;
    half s5 = tex2D(_TempMask, uv + duv.xw).r * 2;

    half s6 = tex2D(_TempMask, uv + duv.zy).r;
    half s7 = tex2D(_TempMask, uv + duv.wy).r * 2;
    half s8 = tex2D(_TempMask, uv + duv.xy).r;

    //return half4(0, 0, 0, tex2D(_TempMask, uv).r);

    return (s0 + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8) / 16;
}
