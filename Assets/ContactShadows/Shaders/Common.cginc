// Experimental implementation of contact shadows for Unity
// https://github.com/keijiro/ContactShadows

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

// Interleaved gradient function from Jimenez 2014 http://goo.gl/eomGso
float IGNoise(uint x, uint y)
{
    float f = dot(float2(0.06711056f, 0.00583715f), float2(x, y));
    return frac(52.9829189f * frac(f));
}

// Vertex shader that procedurally draws a full-screen triangle.
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
