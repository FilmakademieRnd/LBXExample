#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile _ _ADDITIONAL_LIGHTS
#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
#pragma multi_compile_fragment _ _SHADOWS_SOFT
#pragma multi_compile _ SHADOWS_SHADOWMASK
//#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
//#pragma multi_compile _ LIGHTMAP_ON
//#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
#pragma multi_compile_fragment _LIGHT_COOKIES

#ifndef SHADERGRAPH_PREVIEW
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#endif


void MainLight_half(half3 WorldPosition, half3 WorldNormal, out half3 Diffuse, out half3 Color)
{
    Color = 0;
    Diffuse = 0;
    
#ifndef SHADERGRAPH_PREVIEW
#if SHADOWS_SCREEN
   half3 clipPos = TransformWorldToHClip(WorldPosition);
   half4 shadowCoord = ComputeScreenPos(clipPos);
#else
    half4 shadowCoord = TransformWorldToShadowCoord(WorldPosition);
#endif
    Light mainLight = GetMainLight(shadowCoord);
    
    half attenuationCombined = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
    half3 attenuatedLightColor = mainLight.color * attenuationCombined;
    
    Diffuse = LightingLambert(attenuatedLightColor, mainLight.direction, WorldNormal);
    Color += attenuatedLightColor;
#endif
   
}

void AdditionalLight_half(half3 WorldPosition, half3 WorldNormal, out half3 Diffuse, out half3 Color)
{
   Diffuse = 0;
   Color = 0;

#ifndef SHADERGRAPH_PREVIEW
   WorldNormal = normalize(WorldNormal);
   int pixelLightCount = GetAdditionalLightsCount();
   for (int i = 0; i < pixelLightCount; ++i)
   {
       Light light = GetAdditionalLight(i, WorldPosition, half4(1,1,1,1));
       half attenuationPerLight = (light.distanceAttenuation * light.shadowAttenuation);
       half3 attenuatedLightColor = light.color * attenuationPerLight;
      
      //float3 attenuatedLightColor = light.color * step(0.5,(light.distanceAttenuation * light.shadowAttenuation));

      Diffuse += LightingLambert(attenuatedLightColor, light.direction, WorldNormal);
      Color += attenuatedLightColor;
   }
#endif
}

void LightChar_half(half3 WorldPosition, half3 WorldNormal, half3 ViewDirection, half DiffOffset, half DiffSmoothness, half RimOffset, half RimSmoothness, half RimPower, out half3 Diffuse, out half3 Rim)
{
    Diffuse = 0;
    Rim = 0;
    
#ifdef SHADERGRAPH_PREVIEW
    Diffuse = half4(1,1,1,1);
#else
#if SHADOWS_SCREEN
   half3 clipPos = TransformWorldToHClip(WorldPosition);
   half4 shadowCoord = ComputeScreenPos(clipPos);
#else
    half4 shadowCoord = TransformWorldToShadowCoord(WorldPosition);
#endif
    Light mainLight = GetMainLight(shadowCoord);
    
    half d = dot(WorldNormal, mainLight.direction);
    half f = pow((1.0 - dot(WorldNormal, ViewDirection)), RimPower);
    half dn = d * 0.5 + 0.5;
    half rim = f * dn;
    
    half attenuationCombined = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
    Diffuse = smoothstep(DiffOffset, DiffOffset + DiffSmoothness, dn) * attenuationCombined * mainLight.color;
    Rim = smoothstep(RimOffset, RimOffset + RimSmoothness, rim) * attenuationCombined * mainLight.color;
    
    int pixelLightCount = GetAdditionalLightsCount();
    for (int i = 0; i < pixelLightCount; ++i)
    {
        mainLight = GetAdditionalLight(i, WorldPosition, half4(1, 1, 1, 1));
        attenuationCombined = (mainLight.distanceAttenuation * mainLight.shadowAttenuation);
        
        d = dot(WorldNormal, mainLight.direction);
        dn = d * 0.5 + 0.5;
        rim = f * dn;
      
        Diffuse += smoothstep(DiffOffset, DiffOffset + DiffSmoothness, dn) * attenuationCombined * mainLight.color;
        Rim += smoothstep(RimOffset, RimOffset + RimSmoothness, rim) * attenuationCombined * mainLight.color;
    }
    
#endif
}


void Shadowmask_half(float2 lightmapUV, out half4 shadowMask)
{
   #ifdef SHADERGRAPH_PREVIEW 
   shadowMask = half4(1,1,1,1);
   #else
   OUTPUT_LIGHTMAP_UV(lightmapUV, unity_LightmapST, lightmapUV);
   shadowMask = SAMPLE_SHADOWMASK(lightmapUV) ;
   #endif
}
#endif