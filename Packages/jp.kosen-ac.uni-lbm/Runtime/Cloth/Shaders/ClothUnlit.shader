Shader "Unlit/ClothUnlit"
{
    Properties
    {
        _main_tex("Texture", 2D) = "white" {}
        _pos_scale("Position Scale", Vector) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        Cull Off
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
        
        sampler2D _position_tex;
        float4 _pos_scale;
        sampler2D _main_tex;
        float4 _main_tex_ST;
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(in appdata v)
            {
                float4 pos = tex2Dlod(_position_tex, float4(v.uv, 0, 0));
                
                v2f o;
                o.vertex = TransformObjectToHClip(pos.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _main_tex);
                return o;
            }

            float4 frag(in v2f i) : SV_Target
            {
                float4 col = tex2D(_main_tex, i.uv);
                return col;
            }
            ENDHLSL
        }
    }
}