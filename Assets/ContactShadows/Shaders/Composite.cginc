// Experimental implementation of contact shadows for Unity
// https://github.com/keijiro/ContactShadows

#include "Common.cginc"

sampler2D _TempMask;

half4 FragmentComposite(float2 uv : TEXCOORD) : SV_Target
{
    return half4(0, 0, 0, tex2D(_TempMask, uv).r);
}
