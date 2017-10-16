#include "UnityCG.cginc"

sampler2D _CameraDepthTexture;
float4 _CameraDepthTexture_TexelSize;

// Hash function from H. Schechter & R. Bridson, goo.gl/RXiKaH
uint Hash(uint s)
{
    s ^= 2747636419u;
    s *= 2654435769u;
    s ^= s >> 16;
    s *= 2654435769u;
    s ^= s >> 16;
    s *= 2654435769u;
    return s;
}

float Random(uint seed)
{
    return float(Hash(seed)) / 4294967295.0; // 2^32-1
}

// Vertex shader that procedurally draws a full-screen triangle.
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
#ifdef UNITY_UV_STARTS_AT_TOP
    o.texcoord.y = 1 - o.texcoord.y;
#endif
    return o;
}
