Shader "Unlit/LBM_Obstacles"
{
    Properties
    {
        size ("Size", Float) = 100
        velocity_scale ("Velocity Scale", Float) = 1
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
        #include "Packages/jp.kosen-ac.uni-lbm/ShaderLibraly/lbm_utility.hlsl"

        StructuredBuffer<uint> field;
        StructuredBuffer<float3> field_velocity;
        int cell_res;

        float size;
        float velocity_scale;
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            struct v2g
            {
                float4 vertex : SV_POSITION;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float4 color: COLOR;
            };

            v2g vert(uint id: SV_VertexID)
            {
                v2g o;
                uint3 cell = uint3(id % cell_res, id / cell_res % cell_res, id / (cell_res * cell_res));
                o.vertex = float4(cell, 1);
                return o;
            }

            [maxvertexcount(24)]
            void geom(point v2g input[1], uint pid: SV_PrimitiveID, inout LineStream<g2f> out_stream)
            {
                uint field_type = field[pid];

                [flatten]
                if (field_type == FLUID_TYPE)
                    return;

                static const uint3 vertices[24] = {
                    uint3(0, 0, 0), uint3(1, 0, 0),
                    uint3(0, 0, 0), uint3(0, 1, 0),
                    uint3(0, 0, 0), uint3(0, 0, 1),

                    uint3(1, 0, 0), uint3(1, 1, 0),
                    uint3(1, 0, 0), uint3(1, 0, 1),

                    uint3(0, 1, 0), uint3(1, 1, 0),
                    uint3(0, 1, 0), uint3(0, 1, 1),

                    uint3(0, 0, 1), uint3(1, 0, 1),
                    uint3(0, 0, 1), uint3(0, 1, 1),

                    uint3(1, 1, 1), uint3(1, 0, 1),
                    uint3(1, 1, 1), uint3(1, 1, 0),
                    uint3(1, 1, 1), uint3(0, 1, 1),
                };

                [unroll]
                for (uint i = 0; i < 24; i++)
                {
                    g2f o;
                    uint3 vertex = vertices[i];
                    float3 pos = (input[0].vertex.xyz + vertex) * (size / cell_res);
                    float3 vel = field_velocity[pid] * velocity_scale;
                    vel = saturate(vel);
                    float3 vel_length = saturate(length_sq(vel));

                    o.vertex = TransformObjectToHClip(pos);
                    o.color = float4(vel_length, 1);
                    out_stream.Append(o);
                }
            }

            float4 frag(g2f i) : SV_Target
            {
                return i.color;
            }
            ENDHLSL
        }
    }
}