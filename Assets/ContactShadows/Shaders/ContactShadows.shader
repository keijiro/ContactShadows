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
            #pragma target 4.5
            #include "ContactShadows.cginc"
            ENDCG
        }

        // #2 - Temporal filter pass
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentTempFilter
            #pragma target 4.5
            #include "ContactShadows.cginc"
            ENDCG
        }
    }
}
