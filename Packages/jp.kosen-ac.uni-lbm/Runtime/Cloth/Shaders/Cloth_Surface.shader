Shader "Unlit/Cloth_Surface"
{
    Properties
    {
        _main_tex ("Texture", 2D) = "white" {}
        main_color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
        }
        LOD 100
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

        TEXTURE2D(_main_tex);
        SAMPLER(sampler_main_tex);

        SAMPLER(_position_tex);
        SAMPLER(_normal_tex);

        float4 main_color;

        CBUFFER_START(UnityPerMaterial)
            float4 _main_tex_ST;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            struct appdata
            {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal: NORMAL;
                float2 uv : TEXCOORD0;
                float fog_factor: TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                float3 pos = tex2Dlod(_position_tex, float4(v.uv, 0, 0)).xyz;

                v2f o;
                o.vertex = TransformObjectToHClip(float4(pos, v.vertex.w));
                o.normal = tex2Dlod(_normal_tex, float4(v.uv, 0, 0)).xyz;
                o.uv = TRANSFORM_TEX(v.uv, _main_tex);
                o.fog_factor = ComputeFogFactor(o.vertex.z);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_main_tex, sampler_main_tex, i.uv);
                col *= main_color;

                // apply fog
                col.rgb = MixFog(col.rgb, i.fog_factor);
                return col;
            }
            ENDHLSL
        }
    }
}