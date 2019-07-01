Shader "Hidden/HDRP/XRMirrorView"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "LightweightPipeline" }

        // 0: TEXTURE2D
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragBilinear

                #define DISABLE_TEXTURE2D_X_ARRAY 1
				#include "Packages/com.unity.render-pipelines.lightweight/Shaders/Utils/TextureXR.hlsl"
				
				TEXTURE2D_X(_BlitTexture);
				float4 FragBilinear(Varyings input) : SV_Target
				{
				#if defined(USE_TEXTURE2D_X_AS_ARRAY)
					return SAMPLE_TEXTURE2D_ARRAY(_BlitTexture, sampler_LinearClamp, input.texcoord.xy, _BlitTexArraySlice);
				#else
					return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord.xy);
				#endif
				}
            ENDHLSL
        }

        // 1: TEXTURE2D_ARRAY
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragBilinear
				
				#define DISABLE_TEXTURE2D_X_ARRAY 0
				#include "Packages/com.unity.render-pipelines.lightweight/Shaders/Utils/TextureXR.hlsl"
				
				TEXTURE2D_X(_BlitTexture);
				float4 FragBilinear(Varyings input) : SV_Target
				{
				#if defined(USE_TEXTURE2D_X_AS_ARRAY)
					return SAMPLE_TEXTURE2D_ARRAY(_BlitTexture, sampler_LinearClamp, input.texcoord.xy, _BlitTexArraySlice);
				#else
					return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord.xy);
				#endif
				}
            ENDHLSL
        }
		
		HLSLINCLUDE

        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

        SamplerState sampler_LinearClamp;
        uniform float4 _BlitScaleBias;
        uniform float4 _BlitScaleBiasRt;
        uniform uint _BlitTexArraySlice;

        struct Attributes
        {
            uint vertexID : SV_VertexID;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID) * _BlitScaleBias.xy + _BlitScaleBias.zw;
            return output;
        }

        Varyings VertQuad(Attributes input)
        {
            Varyings output;
            output.positionCS = GetQuadVertexPosition(input.vertexID) * float4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + float4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);
            output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
            output.texcoord = GetQuadTexCoord(input.vertexID) * _BlitScaleBias.xy + _BlitScaleBias.zw;
            return output;
        }

		ENDHLSL
    }

    Fallback Off
}