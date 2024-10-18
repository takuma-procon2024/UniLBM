Shader "Projection/Homography"
{
    Properties
    {
        _ui_texture ("UI Texture", 2D) = "white"
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float4x4 _HomographyMatrix, _HomographyInvMatrix;
        sampler2D _ui_texture;
        float4 _ui_texture_ST;

        float2 apply_homography(in float2 u, in float4x4 mat)
        {
            float s = mat[2][0] * u.x + mat[2][1] * u.y + mat[2][2];
            float x = (mat[0][0] * u.x + mat[0][1] * u.y + mat[0][2]) / s;
            float y = (mat[1][0] * u.x + mat[1][1] * u.y + mat[1][2]) / s;
            return float2(x, y);
        }
        ENDHLSL

        Pass
        {
            // ホモグラフィ変換パス

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            Varyings vert(in Attributes IN)
            {
                Varyings OUT;

                #if SHADER_API_GLES
                float4 pos = input.positionOS;
                float2 uv  = input.uv;
                #else
                float4 pos = GetFullScreenTriangleVertexPosition(IN.vertexID);
                float2 uv = GetFullScreenTriangleTexCoord(IN.vertexID);
                #endif

                OUT.positionCS = pos;
                OUT.texcoord = uv;
                return OUT;
            }

            float4 frag(in Varyings IN) : SV_Target
            {
                float2 p = IN.texcoord;
                float2 uv = apply_homography(p, _HomographyInvMatrix);
                bool is_in_range_uv = any(uv < 0.f) || any(uv > 1.f);
                float4 col = is_in_range_uv
                                 ? float4(0, 0, 0, 1)
                                 : SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearRepeat, uv);
                float4 ui_color = is_in_range_uv
                                      ? float4(0, 0, 0, 0)
                                      : tex2D(_ui_texture, (uv - _ui_texture_ST.zw) * _ui_texture_ST.xy);
                col = all(ui_color.xyz < 0.001f) || ui_color.a < 0.001f ? col : ui_color;
                return col;
                // return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearRepeat, IN.texcoord);
            }
            ENDHLSL
        }
    }
}