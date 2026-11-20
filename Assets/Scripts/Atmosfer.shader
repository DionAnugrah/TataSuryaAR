Shader "TataSurya/Atmosfer"
{
    Properties
    {
        _Warna ("Warna", Color) = (0.4, 0.6, 1, 1)
        _Kekuatan ("Kekuatan", Float) = 1
        _Pangkat ("Pangkat Fresnel (0 = polos)", Float) = 2.6
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Warna;
                half _Kekuatan;
                half _Pangkat;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings  { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.viewWS = GetWorldSpaceViewDir(p.positionWS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 v = normalize(i.viewWS);
                half f = pow(1.0 - saturate(dot(n, v)), _Pangkat) * _Kekuatan;
                return half4(_Warna.rgb * f, f);
            }
            ENDHLSL
        }
    }
}