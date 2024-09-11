Shader "Unlit/LBM_3D_Visualize"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Size ("Size", Float) = 10
        _Density ("Density", Float) = 1
        _Velocity ("Velocity", Vector) = (0, 0, 1, 0)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
        }
        Cull Off
        LOD 100

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal//ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

        sampler3D _SolverTex;
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        float _Size, _Density;
        float3 _Velocity;

        CBUFFER_START(UnityPerMaterial)
            float4 _SolverTex_ST;
            float4 _MainTex_ST;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color: COLOR;
                float3 velocity: TEXCOORD1;
            };

            float4x4 rotation_matrix_from_axis_angle(in float3 axis, in float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                float oc = 1.0 - c;

                return float4x4(
                    oc * axis.x * axis.x + c, oc * axis.x * axis.y - axis.z * s, oc * axis.z * axis.x + axis.y * s, 0,
                    oc * axis.x * axis.y + axis.z * s, oc * axis.y * axis.y + c, oc * axis.y * axis.z - axis.x * s, 0,
                    oc * axis.z * axis.x - axis.y * s, oc * axis.y * axis.z + axis.x * s, oc * axis.z * axis.z + c, 0,
                    0, 0, 0, 1
                );
            }

            v2f vert(in appdata v)
            {
                v2f o;
                o.vertex = v.vertex;
                o.uv = float2(0, 0);
                o.color = float4(1, 1, 1, 1);
                o.velocity = tex3Dlod(_SolverTex, float4(v.vertex.xyz / _Size, 0)).xyz;
                return o;
            }

            [maxvertexcount(4)]
            void geom(point v2f input[1], inout TriangleStream<v2f> out_stream)
            {
                v2f o;

                // 全ての頂点で共通の値を計算しておく
                float4 pos = input[0].vertex;
                float4 col = input[0].color;

                // 回転させたい方向
                o.velocity = input[0].velocity;
                float velocity_length = clamp(0.1f, 1, length(o.velocity));
                float3 target_dir = normalize(o.velocity); // 任意の方向ベクトル

                // Z軸方向をtargetDirに向けるための回転行列を作成
                float3 default_normal = float3(0, 0, 1); // 四角形の初期法線（Z軸方向）
                float3 axis = normalize(cross(default_normal, target_dir)); // 回転軸
                float angle = acos(dot(default_normal, target_dir)); // 回転角

                // 回転行列を作成 (axis-angle 回転をクォータニオンで実現)
                float4x4 rot = rotation_matrix_from_axis_angle(axis, angle);

                // 四角形になるように頂点を生産
                [unroll]
                for (int x = 0; x < 2; x++)
                {
                    [unroll]
                    for (int y = 0; y < 2; y++)
                    {
                        // テクスチャ座標
                        float2 uv = float2(x, y);
                        o.uv = uv;

                        // 頂点位置を計算
                        float4 offset = float4((uv * 2 - float2(1, 1)) * velocity_length, 0, 1);
                        o.vertex = pos + mul(rot, offset);
                        o.vertex = mul(UNITY_MATRIX_VP, o.vertex);

                        // 色
                        o.color = col;

                        // ストリームに頂点を追加
                        out_stream.Append(o);
                    }
                }

                // トライアングルストリップを終了
                out_stream.RestartStrip();
            }

            float4 frag(in v2f i) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                clip(col.a - 0.5);
                return col * i.color;
            }
            ENDHLSL
        }
    }
}