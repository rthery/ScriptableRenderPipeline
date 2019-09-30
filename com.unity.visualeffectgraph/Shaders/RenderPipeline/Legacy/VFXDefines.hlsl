#include "HLSLSupport.cginc"

#define UNITY_VERTEX_OUTPUT_STEREO // So that templates compile

//Additionnal empty wrapper (equivalent to expected function in com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl)
float3 GetAbsolutePositionWS(float3 positionRWS)
{
    return positionRWS;
}
float3 GetCameraRelativePositionWS(float3 positionWS)
{
    return positionWS;
}
