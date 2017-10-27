Shader "Hidden/CustomShadowTest"
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
            #include "CustomShadowTest.cginc"
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentFilter
            #pragma target 3.5
            #include "CustomShadowTest.cginc"
            ENDCG
        }
        Pass
        {
            Blend Zero SrcAlpha
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentComposite
            #pragma target 3.5
            #include "CustomShadowTest.cginc"
            ENDCG
        }
    }
}
