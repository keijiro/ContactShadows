// Experimental implementation of contact shadows for Unity
// https://github.com/keijiro/ContactShadows

Shader "Hidden/PostEffects/ContactShadows"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // #0 - Shadow mask construction pass
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentShadow
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
            #define TEMP_FILTER_GATHER
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
            #define TEMP_FILTER_GATHER_ALT
            #include "TempFilter.cginc"
            ENDCG
        }
    }
}
