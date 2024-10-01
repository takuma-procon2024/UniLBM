Shader "Unlit/LBM_Particle"
{
    Properties
    {
        size ("Size", Float) = 100
        min_velocity ("Min Velocity", Float) = 0.00001
        max_velocity ("Max Velocity", Float) = 0.25
        hue_speed ("Hue Speed", Float) = 10
        particle_length ("Particle Length", Float) = 0.1
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

        StructuredBuffer<lbm_particle_data> particles;

        float size;
        float min_velocity;
        float max_velocity;
        float hue_speed;
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

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            v2g vert(uint id: SV_VertexID)
            {
                v2g o;
                lbm_particle_data p = particles[id];
                o.vertex = float4(p.pos_lifetime.xyz, 1);
                o.prev_vertex = float4(p.prev_pos_vel.xyz, 1);
                o.uv = float2(0, 0);
                o.color = float4(hsv_2_rgb(float3(saturate(p.prev_pos_vel.w * hue_speed), 1, 1)), 1);
                return o;
            }

            [maxvertexcount(2)]
            void geom(point v2g input[1], inout LineStream<g2f> out_stream)
            {
                // 全ての頂点で共通の値を計算しておく
                float4 pos = input[0].vertex * size;
                float4 prev_pos = input[0].prev_vertex * size;
                float4 col = input[0].color;

                // float dir_length = Length2(dir.xyz);
                // bool is_out_of_velocity = dir_length <= min_velocity || dir_length >= max_velocity;
                // if (is_out_of_velocity) return;

                g2f o;
                o.vertex = TransformObjectToHClip(pos.xyz);
                o.color = col;
                out_stream.Append(o);

                o.vertex = TransformObjectToHClip(prev_pos.xyz);
                o.color = col;
                out_stream.Append(o);

                out_stream.RestartStrip();
            }

            float4 frag(g2f i) : SV_Target
            {
                return i.color;
            }
            ENDHLSL
        }
    }
}