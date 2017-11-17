// Experimental implementation of contact shadows for Unity
// https://github.com/keijiro/ContactShadows

Shader "Hidden/PostEffects/ContactShadows"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentShadow
            #pragma target 3.5
            #include "ContactShadows.cginc"
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentComposite
            #pragma target 3.5
            #include "ContactShadows.cginc"
            ENDCG
        }
    }
}
