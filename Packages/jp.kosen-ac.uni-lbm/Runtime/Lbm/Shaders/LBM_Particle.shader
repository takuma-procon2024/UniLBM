Shader "Unlit/LBM_Particle"
{
    Properties
    {
        size ("Size", Float) = 100
        min_velocity ("Min Velocity", Float) = 0.00001
        max_velocity ("Max Velocity", Float) = 0.25
        hue_speed ("Hue Speed", Float) = 10
        particle_width ("Particle Width", Float) = 0.1
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
        float particle_length;
        float particle_width;
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
                float4 prev_and_vel : TEXCOORD1;
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
                o.prev_and_vel = p.prev_pos_vel;
                o.uv = float2(0, 0);
                o.color = float4(hsv_2_rgb(float3(saturate(p.prev_pos_vel.w * hue_speed), 1, 1)), 1);
                return o;
            }

            [maxvertexcount(8)]
            void geom(point v2g input[1], inout TriangleStream<g2f> out_stream)
            {
                // 全ての頂点で共通の値を計算しておく
                float4 pos = input[0].vertex;
                float4 prev_pos = input[0].prev_and_vel;
                float4 col = input[0].color;

                // パーティクルの長さ調整
                float3 dir = pos.xyz - prev_pos.xyz;
                prev_pos.xyz -= dir * particle_length;

                // 速度が範囲外の場合は描画しない
                bool is_out_of_velocity = prev_pos.w <= min_velocity || prev_pos.w >= max_velocity;
                [flatten]
                if (is_out_of_velocity)
                    return;

                // 四角形になるように頂点を生成
                float3 cam_dir = pos.xyz - _WorldSpaceCameraPos;
                float3 right = normalize(cross(cam_dir, dir));

                float width = lerp(particle_width * 0.5f, particle_width, saturate(prev_pos.w / max_velocity));
                float3 p00 = pos.xyz + right * width;
                float3 p01 = pos.xyz - right * width;
                float3 p10 = prev_pos.xyz + right * width;
                float3 p11 = prev_pos.xyz - right * width;

                // カメラとの角度によってビルボードを切り替える
                // bool cam_dot = abs(dot(normalize(cam_dir), normalize(dir))) > 0.5f;
                // float4x4 bill_board = UNITY_MATRIX_V;
                // bill_board._m03 = bill_board._m13 = bill_board._m23 = bill_board._m33 = 0;
                // float qw = width * 1.3f;
                // p00 = cam_dot ? pos + mul(bill_board, float4(qw, qw, 0, 0)).xyz : p00;
                // p01 = cam_dot ? pos + mul(bill_board, float4(qw, -qw, 0, 0)).xyz : p01;
                // p10 = cam_dot ? pos + mul(bill_board, float4(-qw, qw, 0, 0)).xyz : p10;
                // p11 = cam_dot ? pos + mul(bill_board, float4(-qw, -qw, 0, 0)).xyz : p11;

                g2f o;
                o.color = col;

                // 表面
                o.vertex = TransformObjectToHClip(p00 * size);
                out_stream.Append(o);

                o.vertex = TransformObjectToHClip(p01 * size);
                out_stream.Append(o);

                o.vertex = TransformObjectToHClip(p10 * size);
                out_stream.Append(o);

                o.vertex = TransformObjectToHClip(p11 * size);
                out_stream.Append(o);

                // 裏面
                o.vertex = TransformObjectToHClip(p11 * size);
                out_stream.Append(o);

                o.vertex = TransformObjectToHClip(p01 * size);
                out_stream.Append(o);

                o.vertex = TransformObjectToHClip(p00 * size);
                out_stream.Append(o);

                o.vertex = TransformObjectToHClip(p10 * size);
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