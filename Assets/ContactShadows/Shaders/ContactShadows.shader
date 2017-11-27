// Experimental implementation of contact shadows for Unity
// https://github.com/keijiro/ContactShadows

Shader "Hidden/PostEffects/ContactShadows"
{
    // Subshader with texture gather operations (DX11, PS4, Xbone, etc.)
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // #0 - Shadow mask construction pass
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentShadow
            #pragma target 4.5
            #include "Raytrace.cginc"
            ENDCG
        }

        // #2 - Temporal filter pass for even frames
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentTempFilter
            #pragma target 4.5
            #include "TempFilter.cginc"
            ENDCG
        }

        // #3 - Temporal filter pass for odd frames
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentTempFilter
            #pragma target 4.5
            #define TEMP_FILTER_ALT
            #include "TempFilter.cginc"
            ENDCG
        }
    }

    // Subshader without texture gather operations
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // #0 - Shadow mask construction pass
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentShadow
            #pragma target 3.0
            #include "Raytrace.cginc"
            ENDCG
        }

        // #2 - Temporal filter pass for even frames
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentTempFilter
            #pragma target 3.0
            #include "TempFilter.cginc"
            ENDCG
        }

        // #3 - Temporal filter pass for odd frames
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentTempFilter
            #pragma target 3.0
            #define TEMP_FILTER_ALT
            #include "TempFilter.cginc"
            ENDCG
        }
    }
}
