Shader "Unlit/LBM_Particle"
{
    Properties {}
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
        }
        LOD 100

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal//ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
        #include "Packages/jp.kosen-ac.uni-lbm/RUntime/Effector/Shaders/particle_data.hlsl"

        StructuredBuffer<particle_data> particles;
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert(uint id: SV_VertexID)
            {
                v2f o;
                particle_data p = particles[id];
                o.vertex = float4(p.pos, 1);
                o.uv = float2(0, 0);
                o.color = p.col;
                return o;
            }

            [maxvertexcount(4)]
            void geom(point v2f input[1], inout TriangleStream<v2f> outStream)
            {
                // 全ての頂点で共通の値を計算しておく
                float4 pos = input[0].vertex;
                float4 col = input[0].color;

                // 四角形になるように頂点を生産
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        v2f o;

                        // ビルボード用の行列
                        float4x4 billboard_matrix = UNITY_MATRIX_V;
                        billboard_matrix._m03
                            = billboard_matrix._m13
                            = billboard_matrix._m23
                            = billboard_matrix._m33
                            = 0;

                        // テクスチャ座標
                        float2 uv = float2(x, y);
                        o.uv = uv;

                        // 頂点位置を計算
                        o.vertex = pos + mul(float4((uv * 2 - float2(1, 1)) * 0.1, 0, 1), billboard_matrix);
                        o.vertex = mul(UNITY_MATRIX_VP, o.vertex);

                        // 色
                        o.color = col;

                        // ストリームに頂点を追加
                        outStream.Append(o);
                    }
                }

                // トライアングルストリップを終了
                outStream.RestartStrip();
            }

            float4 frag(v2f i) : SV_Target
            {
                return float4(1.0, 1.0, 1.0

                , 1.0);
            }
            ENDHLSL
        }
    }
}