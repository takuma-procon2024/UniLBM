Shader "Unlit/LBM_Obstacles"
{
    Properties
    {
        size ("Size", Float) = 100
        line_color ("Line Color", Color) = (1,1,1,1)
        outflow_color ("Outflow Color", Color) = (1,0,0,1)
        [Toggle(SHOW_OUTFLOW)] show_outflow ("Show Outflow", Float) = 0
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
        int cell_res;

        float size;
        float4 line_color;
        float4 outflow_color;
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #pragma shader_feature _ SHOW_OUTFLOW
            #pragma multi_compile _ PACK_COLLISION_CELL

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

                #ifndef SHOW_OUTFLOW
                [flatten]
                if (field_type == OUTFLOW_BOUNDARY_TYPE)
                    return;
                #endif

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

                bool is_cloth = field_type != OUTFLOW_BOUNDARY_TYPE;

                #ifdef PACK_COLLISION_CELL
                uint cloth_x = (field_type & 0x0fff0000) >> 16;
                #else
                uint cloth_x = (field_type & 0xffff0000) >> 16;
                #endif
                uint cloth_y = field_type & 0x0000ffff;
                float4 c_color = float4(cloth_x, cloth_y, 0, 0) / 64;
                #ifdef PACK_COLLISION_CELL
                c_color = field_type & 0x10000000 ? 1 : c_color;
                #endif

                [unroll]
                for (uint i = 0; i < 24; i++)
                {
                    g2f o;
                    uint3 vertex = vertices[i];
                    float3 pos = (input[0].vertex.xyz + vertex) * size / cell_res;
                    o.vertex = TransformObjectToHClip(pos);
                    #ifdef SHOW_OUTFLOW
                    o.color = field_type == OUTFLOW_BOUNDARY_TYPE ? outflow_color : is_cloth ? c_color : line_color;
                    #else
                    o.color = is_cloth ? c_color : line_color;
                    #endif
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