Shader "Unlit/LBM_Particle"
{
    Properties
    {
        size ("Size", Float) = 100
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
            "PreviewType"="Plane"
        }
        LOD 100

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal//ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
        #include "Packages/jp.kosen-ac.uni-lbm/RUntime/Effector/Shaders/particle_data.hlsl"

        StructuredBuffer<particle_data> particles;

        float size;
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            struct v2g
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float4 prev_vertex : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            v2g vert(uint id: SV_VertexID)
            {
                v2g o;
                particle_data p = particles[id];
                o.vertex = float4(p.pos, 1);
                o.prev_vertex = float4(p.prev_pos, 1);
                o.uv = float2(0, 0);
                o.color = p.col;
                return o;
            }

            [maxvertexcount(2)]
            void geom(point v2g input[1], inout LineStream<v2f> out_stream)
            {
                // 全ての頂点で共通の値を計算しておく
                float4 pos = input[0].vertex * size;
                float4 prev_pos = input[0].prev_vertex * size;
                float4 dir = pos - prev_pos;
                float4 col = input[0].color;

                // if (Length2(dir) <= 0.00001f) return;
                // if (Length2(dir) >= 0.25) return;

                v2f o;
                o.vertex = TransformObjectToHClip(prev_pos);
                o.color = col;
                out_stream.Append(o);

                o.vertex = TransformObjectToHClip(prev_pos + dir);
                // o.vertex = TransformObjectToHClip(prev_pos + float3(0, 0.5, 0));
                o.color = col;
                out_stream.Append(o);

                out_stream.RestartStrip();
            }

            float4 frag(v2f i) : SV_Target
            {
                // return float4(1.0, 1.0, 1.0, 1.0);
                return i.color;
            }
            ENDHLSL
        }
    }
}