Shader "Unlit/LBM_Obstacles"
{
    Properties
    {
        size ("Size", Float) = 100
        line_color ("Line Color", Color) = (1,1,1,1)
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

        StructuredBuffer<uint> field;
        int cell_res;

        float size;
        float4 line_color;
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #pragma shader_feature _ SHOW_OUTFLOW

            struct v2g
            {
                float4 vertex : SV_POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2g vert(uint id: SV_VertexID)
            {
                v2g o;
                uint3 cell = uint3(id % cell_res, id / cell_res % cell_res, id / (cell_res * cell_res));
                o.vertex = float4(cell, 1);
                return o;
            }

            [maxvertexcount(24)]
            void geom(point v2g input[1], uint pid: SV_PrimitiveID, inout LineStream<v2f> out_stream)
            {
                uint field_type = field[pid];

                [flatten]
                if (field_type == 0) return;
                
                #ifndef SHOW_OUTFLOW
                uint3 cell = uint3(pid % cell_res, pid / cell_res % cell_res, pid / (cell_res * cell_res));
                [flatten]
                if (any(cell == 0) || any(cell == cell_res - 1)) return;
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

                [unroll]
                for (uint i = 0; i < 24; i++)
                {
                    v2f o;
                    uint3 vertex = vertices[i];
                    float3 pos = (input[0].vertex.xyz + vertex) * size / cell_res;
                    o.vertex = TransformObjectToHClip(pos);
                    out_stream.Append(o);
                }
            }

            float4 frag(v2f i) : SV_Target
            {
                return line_color;
            }
            ENDHLSL
        }
    }
}