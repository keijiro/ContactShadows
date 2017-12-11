// Experimental implementation of contact shadows for Unity
// https://github.com/keijiro/ContactShadows

#include "UnityCG.cginc"

// Check if the gather instructions are available.
#if SHADER_TARGET > 40
    #define TEX_GATHER_AVAILABLE
#endif

// Vertex shader - Full-screen triangle with procedural draw
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
