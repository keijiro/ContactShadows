Shader "Hidden/Reprojector"
{
    Properties
    {
        _MainTex("", 2D) = "white" {}
    }

    CGINCLUDE

    #include "../CustomShadow/Shaders/Common.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;

    float4x4 _PreviousVP;
    float4x4 _NonJitteredVP;

    half4 Fragment(Varyings input) : SV_Target
    {
        float2 uv = input.texcoord.xy;

        float z = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv, 0, 0));

    #if defined(UNITY_REVERSED_Z)
        z = 1 - z;
    #endif
        float4 cp = float4(float3(uv, z) * 2 - 1, 1);
        float4 vp = mul(unity_CameraInvProjection, cp);
        vp /= vp.w;
        vp.z = -vp.z;
        float4 wp = mul(unity_CameraToWorld, vp);

        float4 prevClipPos = mul(_PreviousVP, wp);
        float4 curClipPos = cp * float4(1, -1, 1, 1);

        float2 prevHPos = prevClipPos.xy / prevClipPos.w;
        float2 curHPos = curClipPos.xy / curClipPos.w;

        float2 vPosPrev = (prevHPos.xy + 1.0f) / 2.0f;
        float2 vPosCur = (curHPos.xy + 1.0f) / 2.0f;
#if UNITY_UV_STARTS_AT_TOP
        vPosPrev.y = 1.0 - vPosPrev.y;
        vPosCur.y = 1.0 - vPosCur.y;
#endif
        float2 mv = vPosCur - vPosPrev;

        return tex2D(_MainTex, input.texcoord - mv);
    }

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            ENDCG
        }
    }
}
