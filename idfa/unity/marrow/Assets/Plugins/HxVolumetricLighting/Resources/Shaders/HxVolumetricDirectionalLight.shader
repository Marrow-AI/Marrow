// Upgrade NOTE: replaced '_CameraToWorld' with 'unity_CameraToWorld'
// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'unity_World2Shadow' with 'unity_WorldToShadow'


Shader "Hidden/HxVolumetricDirectionalLight"
{
	CGINCLUDE


#include "UnityCG.cginc"
#include "UnityDeferredLibrary.cginc"
#include "UnityPBSLighting.cginc"
#include "HxVolumetricLightCore.cginc"

#pragma shader_feature SHADOWS_OFF

//#pragma multi_compile _ SHADOWS_NATIVE
#pragma shader_feature UNITY_SINGLE_PASS_STEREO
#pragma shader_feature DENSITYPARTICLES_ON
#pragma shader_feature VTRANSPARENCY_ON
#pragma shader_feature FULL_ON
#pragma shader_feature NOISE_ON  
#pragma shader_feature HEIGHTFOG_ON
#pragma shader_feature UNITY_SINGLE_PASS_STEREO
	float hxRayOffset;
	float HxTileSize;
	float4x4 InverseProjectionMatrix;
	float4x4 InverseProjectionMatrix2;
	float4x4 VolumetricMVP;
	float4x4 VolumetricMVP2; //right eye
	float4x4 VolumetricMV;
	float3 hxCameraPosition;
	float3 hxCameraPosition2;
	float4x4 hxCameraToWorld;
	float4x4 hxCameraToWorld2; //second eye
	float FirstLight;
	struct appdata {
		float4 vertex : POSITION;
	};

	struct v2f
	{
		float4 pos : SV_POSITION;
		float4 uv : TEXCOORD0;
#ifdef FULL_ON
		float3 wpos : TEXCOORD1;
#endif
	};

	v2f vert(appdata v)
	{
		v2f o;

		o.pos = mul(VolumetricMVP, v.vertex);
		o.uv = ComputeScreenPos(o.pos);
#ifdef FULL_ON
		o.wpos = mul(unity_ObjectToWorld, v.vertex);
#endif
		return o;
	}



#ifdef FULL_ON

#else
	float4x4 _InvViewProj;
	sampler2D_float  VolumetricDepth;
	float4 VolumetricDepth_TexelSize;
#endif

#if !defined(SHADER_API_GLES)
	// all platforms except GLES2.0 have built-in shadow comparison samplers
#define SHADOWS_NATIVE
#elif defined(SHADER_API_GLES) && defined(UNITY_ENABLE_NATIVE_SHADOW_LOOKUPS)
	// GLES2.0 also has built-in shadow comparison samplers, but only on platforms where we pass UNITY_ENABLE_NATIVE_SHADOW_LOOKUPS from the editor
#define SHADOWS_NATIVE
#endif

#if defined(SHADER_API_D3D11) || defined(SHADER_API_D3D11_9X) || defined(UNITY_COMPILER_HLSLCC)
	// DX11 & hlslcc platforms: built-in PCF
#if defined(SHADER_API_D3D11_9X)
	// FL9.x has some bug where the runtime really wants resource & sampler to be bound to the same slot,
	// otherwise it is skipping draw calls that use shadowmap sampling. Let's bind to #15
	// and hope all works out.
#define CUSTOM_DECLARE_SHADOWMAP(tex) Texture2D tex : register(t15); SamplerComparisonState sampler##tex : register(s15)
#else
#define CUSTOM_DECLARE_SHADOWMAP(tex) Texture2D tex; SamplerComparisonState sampler##tex
#endif
#define CUSTOM_SAMPLE_SHADOW(tex,coord) tex.SampleCmpLevelZero (sampler##tex,(coord).xy,(coord).z)
#define CUSTOM_SAMPLE_SHADOW_PROJ(tex,coord) tex.SampleCmpLevelZero (sampler##tex,(coord).xy/(coord).w,(coord).z/(coord).w)
#elif defined(UNITY_COMPILER_HLSL2GLSL) && defined(SHADOWS_NATIVE)
	// OpenGL-like hlsl2glsl platforms: most of them always have built-in PCF
#define CUSTOM_DECLARE_SHADOWMAP(tex) sampler2DShadow tex
#define CUSTOM_SAMPLE_SHADOW(tex,coord) shadow2D (tex,(coord).xyz)
#define CUSTOM_SAMPLE_SHADOW_PROJ(tex,coord) shadow2Dproj (tex,coord)
#elif defined(SHADER_API_D3D9)
	// D3D9: Native shadow maps FOURCC "driver hack", looks just like a regular
	// texture sample. Have to always do a projected sample
	// so that HLSL compiler doesn't try to be too smart and mess up swizzles
	// (thinking that Z is unused).
#define CUSTOM_DECLARE_SHADOWMAP(tex) sampler2D tex
#define CUSTOM_SAMPLE_SHADOW(tex,coord) tex2Dlod (tex,float4((coord).xyz,1)).r
#define CUSTOM_SAMPLE_SHADOW_PROJ(tex,coord) tex2Dproj (tex,coord).r
#elif defined(SHADER_API_PSSL)
	// PS4: built-in PCF
#define CUSTOM_DECLARE_SHADOWMAP(tex)		Texture2D tex; SamplerComparisonState sampler##tex
#define CUSTOM_SAMPLE_SHADOW(tex,coord)		tex.SampleCmpLOD0(sampler##tex,(coord).xy,(coord).z)
#define CUSTOM_SAMPLE_SHADOW_PROJ(tex,coord)	tex.SampleCmpLOD0(sampler##tex,(coord).xy/(coord).w,(coord).z/(coord).w)
#elif defined(SHADER_API_PSP2)
	// Vita
#define CUSTOM_DECLARE_SHADOWMAP(tex) sampler2D tex
	// tex2d shadow comparison on Vita returns 0 instead of 1 when shadowCoord.z >= 1 causing artefacts in some tests.
	// Clamping Z to the range 0.0 <= Z < 1.0 solves this.
#define CUSTOM_SAMPLE_SHADOW(tex,coord) tex2D<float>(tex, float3((coord).xy, clamp((coord).z, 0.0, 0.999999)))
#define CUSTOM_SAMPLE_SHADOW_PROJ(tex,coord) tex2DprojShadow(tex, coord)
#else
	// Fallback / No built-in shadowmap comparison sampling: regular texture sample and do manual depth comparison
#define CUSTOM_DECLARE_SHADOWMAP(tex) sampler2D_float tex
#define CUSTOM_SAMPLE_SHADOW(tex,coord) ((SAMPLE_DEPTH_TEXTURE(tex,(coord).xy) < (coord).z) ? 0.0 : 1.0)
#define CUSTOM_SAMPLE_SHADOW_PROJ(tex,coord) ((SAMPLE_DEPTH_TEXTURE_PROJ(tex,UNITY_PROJ_COORD(coord)) < ((coord).z/(coord).w)) ? 0.0 : 1.0)
#endif

	CUSTOM_DECLARE_SHADOWMAP(_ShadowMapTexture);


	float3 ShadowBias;

	float2 ShadowDistance;

	float4x4 InverseViewMatrix;
	uniform float4 _SpotLightParams;
	float3 CameraFoward;
	float VolumeScale;
	sampler2D Tile5x5;
	float ExtinctionEffect;
	float3 LightColour;
	float3 LightColour2;
	uniform float4 _LightParams;
	float TintPercent;

	float2 MaxRayDistance;
	float AmbientStrength;
	float3 AmbientSkyColor;
	float3 AmbientEquatorColor;
	float3 AmbientGroundColor;

	inline fixed4 getCascadeWeights(float3 wpos, float z)
	{
		fixed4 zNear = float4(z >= _LightSplitsNear);
		fixed4 zFar = float4(z < _LightSplitsFar);
		fixed4 weights = zNear * zFar;
		return weights;
	}



	inline fixed4 GetCascadeWeights_SplitSpheres(float3 wpos)
	{
		float3 fromCenter0 = wpos.xyz - unity_ShadowSplitSpheres[0].xyz;
		float3 fromCenter1 = wpos.xyz - unity_ShadowSplitSpheres[1].xyz;
		float3 fromCenter2 = wpos.xyz - unity_ShadowSplitSpheres[2].xyz;
		float3 fromCenter3 = wpos.xyz - unity_ShadowSplitSpheres[3].xyz;
		float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

		fixed4 weights = float4(distances2 < unity_ShadowSplitSqRadii);
		weights.yzw = saturate(weights.yzw - weights.xyz);

		return weights;
	}


	inline float4 GetCascadeShadowCoord(float4 wpos, fixed4 cascadeWeights)
	{
			float3 sc0 = mul(unity_WorldToShadow[0], wpos).xyz;
			float3 sc1 = mul(unity_WorldToShadow[1], wpos).xyz;
			float3 sc2 = mul(unity_WorldToShadow[2], wpos).xyz;
			float3 sc3 = mul(unity_WorldToShadow[3], wpos).xyz;
			return float4(sc0 * cascadeWeights[0] + sc1 * cascadeWeights[1] + sc2 * cascadeWeights[2] + sc3 * cascadeWeights[3], 1);

	}

	inline float SampleCascadeShadowMap(float3 wpos,float z)
	{
		//float4 cascadeWeights = GetCascadeWeights_SplitSpheres(wpos);
		float4 cascadeWeights = getCascadeWeights(wpos,z);
		bool inside = cascadeWeights.x + cascadeWeights.y + cascadeWeights.z + cascadeWeights.w > 0;
		float4 samplePos = GetCascadeShadowCoord(float4(wpos, 1), cascadeWeights);

		//samplePos.z = max(min(samplePos.z, 0.99999),0.0001);


		return inside ? CUSTOM_SAMPLE_SHADOW(_ShadowMapTexture, samplePos.xyz) : 1;


		
	
	}


	float2 SunSize;

	
	float DirectionalHenyeyPhase(float cosTheta, float Sky)
	{
		//HenyeyPhase(cosTheta);

		return max(min(Phase2.x * (Phase2.y / max(pow(Phase2.z - Phase2.w * cosTheta, 1.5),0.0001f)) * SunSize.x * max(Sky, SunSize.y), 100), HenyeyPhase(cosTheta));
	}

	vr March(float3 rayDir, float3 worldPos, float3 Ambient, float depth, float zScale,float2 uv)
	{
		vr vrout;
		/*
		vrout.col0 = float4(0, 0, 0, 0);

#ifndef VTRANSPARENCY_OFF
		vrout.col1 = float4(0, 0, 0, 0);
		vrout.col2 = float4(0, 0, 0, 0);
#ifndef S_8
		vrout.col3 = float4(0, 0, 0, 0);
#if defined (S_16)
		vrout.col4 = float4(0, 0, 0, 0);
#endif
#endif

#endif
*/

		//sample skybox cubemap?


		//	ShadeSH9(half4(-rayDir, 1)) * AmbientStrength;// ShadeSH9(half4(-rayDir.xyz, 1));
		//Ambient = half3(length(unity_SHAr), length(unity_SHAg), length(unity_SHAb));


#ifndef SHADOWS_OFF
	//	ShadowUVData shadowData = LoadShadowData(-rayDir);
#endif


		float3 start = _WorldSpaceCameraPos - (rayDir * (((_ProjectionParams.y) / zScale) ));
		
		worldPos += _SpotLightParams.xyz * (ShadowBias.x);
		float3 posDif = start - worldPos;
		float RayDis = length(posDif);
		rayDir = posDif / RayDis;
		float rayDistance = min(RayDis, MaxRayDistance.y);

#ifdef DENSITYPARTICLES_ON
		DensityMap dm = LoadSliceData(uv, rayDistance);
		float LowValue = 0;
		float HighValie = 0;
		float CurrentLow = -1;
#endif

		float shadowMarch = min(MaxRayDistance.x, rayDistance);
		float NonShadowMarch = rayDistance - shadowMarch;
		rayDistance = shadowMarch;

		int NUM_SAMPLES = min(Density.y, 128);
		int numBefore = NUM_SAMPLES;
//#if UNITY_SINGLE_PASS_STEREO
//		float2 spuv = float2(frac(uv.x * 2), uv.y);
//		float centerdis = length(float2(0.5f, 0.5f) - spuv) * 4;
//		NUM_SAMPLES = NUM_SAMPLES * min(1, 2 - centerdis);
//#else
//		//float t = NUM_SAMPLES;
//		//float centerdis = length(float2(0.5f, 0.5f) - uv) * 2.2;
//		//NUM_SAMPLES = floor(t * min(1, 1.1 - centerdis));
//		//clip(NUM_SAMPLES);
//#endif

		float stepSize = (rayDistance)* 1.0f / NUM_SAMPLES;

		float3 currentPos = start;// - (rayDir * _ProjectionParams.y/2);//- (rayDir * rayDistance);


		float index = frac(tex2Dlod(Tile5x5, float4(fmod((_ScreenParams.xy * VolumeScale) * uv, HxTileSize) / HxTileSize, 0, 0)).r + hxRayOffset);

		//float index = tex2D(Tile5x5, interleavedPos.xy).r;
		float rayStartOffset = stepSize * index;
		currentPos -= (rayStartOffset ) * rayDir.xyz;

		float result = 0;
		float AmbientResult = 0;
		float ThisDot = dot(_SpotLightParams.xyz, rayDir);
		float phase = DirectionalHenyeyPhase(ThisDot, depth > 0.99f);

		float3 ThisColor = lerp(LightColour2, LightColour, saturate(TintPercent * (ThisDot + 1) / 2));

		float AmbientPhase = 0.07958f;
		float extinction = 0;

		float VDis = length(currentPos - _WorldSpaceCameraPos);
#if defined (DENSITYPARTICLES_ON) || (VTRANSPARENCY_ON)
		float rayWorldDis = VDis * zScale;
		float zStep = stepSize / zScale;
#endif
		float ext = 0;
		float ThisMarch = 0;
		float ThisMarchResults = 0;
		
	
		[loop]
		for (int i = 0; i < NUM_SAMPLES; i++)
		{

#ifndef SHADOWS_OFF
			float shadowTerm = SampleCascadeShadowMap(currentPos, VDis);
			
		//	float shadowTerm = CUSTOM_SAMPLE_SHADOW(_ShadowMapTexture, GetshadowCo(shadowData, MarchedDistance, currentPos));
		//	MarchedDistance += stepSize * zScale;

#else
			float shadowTerm = 1;
#endif

//#ifdef COOKIE_ON
//			//ummmmmmm
//#endif

			float fogDensity = GetFogDensity(currentPos);
#ifdef DENSITYPARTICLES_ON		
			fogDensity = max(fogDensity + DensityFrom3DTexture(dm, rayWorldDis, LowValue, HighValie, CurrentLow),0);
#endif

			extinction += fogDensity * Density.w * stepSize;
			ext = exp(-extinction);

			ThisMarchResults = fogDensity * stepSize * ext;
			ThisMarch = max(((shadowTerm * phase) + (ShadowBias.z * (1 - shadowTerm))) * ThisMarchResults, 0);
			result += ThisMarch;
			AmbientResult += ThisMarchResults * AmbientPhase;
#ifdef VTRANSPARENCY_ON
			float lum = Luminance(ThisColor * (ThisMarch)+Ambient * AmbientResult);
			AddTransparency(vrout, lum, rayWorldDis);
#endif
			
			currentPos -= rayDir * stepSize;
			VDis += stepSize;
#if defined (DENSITYPARTICLES_ON) || (VTRANSPARENCY_ON)
			rayWorldDis += zStep;
#endif

		}



		if (NonShadowMarch > 0)
		{

			currentPos = _WorldSpaceCameraPos - (rayDir * (stepSize + shadowMarch));


			stepSize = (NonShadowMarch)* 1.0f / NUM_SAMPLES;
			Phase = 0.07958f;
			currentPos -= stepSize * index * rayDir;

#if defined (DENSITYPARTICLES_ON) || (VTRANSPARENCY_ON)
			rayWorldDis = length(currentPos - _WorldSpaceCameraPos) * zScale;
			zStep = stepSize * zScale;
#endif
			for (int i = 0; i < NUM_SAMPLES; i++)
			{
				float fogDensity = GetFogDensity(currentPos);
#ifdef DENSITYPARTICLES_ON		
				fogDensity += (DensityFrom3DTexture(dm, rayWorldDis, LowValue, HighValie, CurrentLow));
#endif
				extinction += fogDensity * Density.w * stepSize;
				ext = exp(-extinction);
				float shadowTerm = 1;
				ThisMarchResults = fogDensity * stepSize * ext;
				result += max(((shadowTerm * phase) + (ShadowBias.z * (1 - shadowTerm))) * ThisMarchResults, 0);
				currentPos -= rayDir * stepSize;
				AmbientResult += ThisMarchResults * AmbientPhase;
#ifdef DENSITYPARTICLES_ON	
				rayWorldDis += zStep;
#endif
			}
		}


		float finalEx = saturate(exp(-extinction));

		vrout.col0 = float4((ThisColor * result) + (Ambient * AmbientResult) * FirstLight, (1 - (lerp(finalEx, 1, 1 - ExtinctionEffect))) * FirstLight);
		
		//vrout.col0 = float4(uv.x, uv.y, 0, 0);
		return vrout;
	}

	vr FragSingle(v2f i)
	{
		float2 uv = i.uv.xy / i.uv.w;


#ifdef FULL_ON
#ifdef UNITY_SINGLE_PASS_STEREO
		float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv.xy, 0, 0)));
		bool Left = uv.x < 0.5f;
		float2 data = uv.xy;
		data.x = frac(data.x * 2.0f);


		float4 clipPos = float4(data * 2.0 - 1.0, 1.0, 1.0);
		float4 cameraRay = mul((!Left ? InverseProjectionMatrix : InverseProjectionMatrix2), clipPos);
		cameraRay = (cameraRay / cameraRay.w);
		float3 wpos = mul(!Left ? hxCameraToWorld2 : hxCameraToWorld, float4(-cameraRay.xyz * (_ProjectionParams.z / cameraRay.z) * depth, 1)).xyz;
		float3 rayDir = normalize(_WorldSpaceCameraPos - wpos);
		float zScale = (dot(CameraFoward, -rayDir));
	
#else
		float3 wpos = i.wpos;
		float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv.xy, 0, 0)));
		float3 rayDir = normalize(_WorldSpaceCameraPos - wpos);
		float zScale = (dot(CameraFoward, -rayDir));
		wpos = _WorldSpaceCameraPos - (rayDir *  (depth / zScale));
#endif
#else	

		float4 data = ((tex2Dlod(VolumetricDepth, float4(uv, 0, 0))));
		float depth = (DecodeFloatRG(data.xy));
#ifdef UNITY_SINGLE_PASS_STEREO
		
		bool Left = data.z > 0.5f;
		data.z = frac(data.z * 2.0f);

		float4 clipPos = float4((float2(1,1) -  data.zw) * 2.0 - 1.0, 1.0, 1.0);
		float4 cameraRay = mul((!Left ? InverseProjectionMatrix : InverseProjectionMatrix2), clipPos);
		cameraRay = (cameraRay / cameraRay.w);
		float3 wpos = mul(!Left ? hxCameraToWorld2 : hxCameraToWorld, float4(-cameraRay.xyz * (_ProjectionParams.z / cameraRay.z) * depth, 1)).xyz;
		float3 rayDir = normalize(_WorldSpaceCameraPos - wpos);
		float zScale = (dot(CameraFoward, -rayDir));


#else

		float4 clipPos = float4((float2(1, 1) - data.zw) * 2.0 - 1.0, 1.0, 1.0);
		float4 cameraRay = mul((InverseProjectionMatrix), clipPos);
		cameraRay = (cameraRay / cameraRay.w);
		float3 wpos = mul(hxCameraToWorld, float4(-cameraRay.xyz * (_ProjectionParams.z / cameraRay.z) * depth, 1)).xyz;
		float3 rayDir = normalize(_WorldSpaceCameraPos - wpos);
		float zScale = (dot(CameraFoward, -rayDir));
#endif
#endif

		float up = dot(float3(0, -1, 0), rayDir);
		float forwardA = (dot(normalize(float3(rayDir.x, 0, rayDir.z)), rayDir) - 0.7) * 3.3333;

		float3 Ambient = AmbientSkyColor * AmbientStrength;


		return March(rayDir, wpos, Ambient, depth, zScale, uv);
	}

	vr FragTri(v2f i)
	{
		float2 uv = i.uv.xy / i.uv.w;


#ifdef FULL_ON
#ifdef UNITY_SINGLE_PASS_STEREO
		float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv.xy, 0, 0)));
		bool Left = uv.x < 0.5f;
		float2 data = uv.xy;
		data.x = frac(data.x * 2.0f);


		float4 clipPos = float4(data * 2.0 - 1.0, 1.0, 1.0);
		float4 cameraRay = mul((!Left ? InverseProjectionMatrix : InverseProjectionMatrix2), clipPos);
		cameraRay = (cameraRay / cameraRay.w);
		float3 wpos = mul(!Left ? hxCameraToWorld2 : hxCameraToWorld, float4(-cameraRay.xyz * (_ProjectionParams.z / cameraRay.z) * depth, 1)).xyz;
		float3 rayDir = normalize(_WorldSpaceCameraPos - wpos);
		float zScale = (dot(CameraFoward, -rayDir));

#else
		float3 wpos = i.wpos;
		float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv.xy, 0, 0)));
		float3 rayDir = normalize(_WorldSpaceCameraPos - wpos);
		float zScale = (dot(CameraFoward, -rayDir));
		wpos = _WorldSpaceCameraPos - (rayDir *  (depth / zScale));
#endif
#else	

		float4 data = ((tex2Dlod(VolumetricDepth, float4(uv, 0, 0))));
		float depth = (DecodeFloatRG(data.xy));
#ifdef UNITY_SINGLE_PASS_STEREO

		bool Left = data.z > 0.5f;
		data.z = frac(data.z * 2.0f);

		float4 clipPos = float4((float2(1, 1) - data.zw) * 2.0 - 1.0, 1.0, 1.0);
		float4 cameraRay = mul((!Left ? InverseProjectionMatrix : InverseProjectionMatrix2), clipPos);
		cameraRay = (cameraRay / cameraRay.w);
		float3 wpos = mul(!Left ? hxCameraToWorld2 : hxCameraToWorld, float4(-cameraRay.xyz * (_ProjectionParams.z / cameraRay.z) * depth, 1)).xyz;
		float3 rayDir = normalize(_WorldSpaceCameraPos - wpos);
		float zScale = (dot(CameraFoward, -rayDir));


#else

		float4 clipPos = float4(data.zw * 2.0 - 1.0, 1.0, 1.0);
		float4 cameraRay = mul(InverseProjectionMatrix, clipPos);
		cameraRay = (cameraRay / cameraRay.w);
		float3 wpos = mul(unity_CameraToWorld, float4(cameraRay.xyz * (_ProjectionParams.z / cameraRay.z) * depth, 1)).xyz;
		float3 rayDir = normalize(_WorldSpaceCameraPos - wpos);
		float zScale = (dot(CameraFoward, -rayDir));
#endif
#endif

		float up = dot(float3(0, -1, 0), rayDir);
		float forwardA = (dot(normalize(float3(rayDir.x, 0, rayDir.z)), rayDir) - 0.7) * 3.3333;

		float3 Ambient = ((max(up, 0) * AmbientSkyColor) + (max(-up, 0) * AmbientGroundColor) + (max(forwardA * forwardA * forwardA * forwardA * forwardA, 0) * AmbientEquatorColor)) * AmbientStrength;



		return March(rayDir, wpos, Ambient, depth, zScale, uv);
	}

	vr FragSky(v2f i)
	{
		float2 uv = i.uv.xy / i.uv.w;


#ifdef FULL_ON
#ifdef UNITY_SINGLE_PASS_STEREO
		float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv.xy, 0, 0)));
		bool Left = uv.x < 0.5f;
		float2 data = uv.xy;
		data.x = frac(data.x * 2.0f);


		float4 clipPos = float4(data * 2.0 - 1.0, 1.0, 1.0);
		float4 cameraRay = mul((!Left ? InverseProjectionMatrix : InverseProjectionMatrix2), clipPos);
		cameraRay = (cameraRay / cameraRay.w);
		float3 wpos = mul(!Left ? hxCameraToWorld2 : hxCameraToWorld, float4(-cameraRay.xyz * (_ProjectionParams.z / cameraRay.z) * depth, 1)).xyz;
		float3 rayDir = normalize(_WorldSpaceCameraPos - wpos);
		float zScale = (dot(CameraFoward, -rayDir));

#else
		float3 wpos = i.wpos;
		float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv.xy, 0, 0)));
		float3 rayDir = normalize(_WorldSpaceCameraPos - wpos);
		float zScale = (dot(CameraFoward, -rayDir));
		wpos = _WorldSpaceCameraPos - (rayDir *  (depth / zScale));
#endif
#else	

		float4 data = ((tex2Dlod(VolumetricDepth, float4(uv, 0, 0))));
		float depth = (DecodeFloatRG(data.xy));
#ifdef UNITY_SINGLE_PASS_STEREO

		bool Left = data.z > 0.5f;
		data.z = frac(data.z * 2.0f);

		float4 clipPos = float4((float2(1, 1) - data.zw) * 2.0 - 1.0, 1.0, 1.0);
		float4 cameraRay = mul((!Left ? InverseProjectionMatrix : InverseProjectionMatrix2), clipPos);
		cameraRay = (cameraRay / cameraRay.w);
		float3 wpos = mul(!Left ? hxCameraToWorld2 : hxCameraToWorld, float4(-cameraRay.xyz * (_ProjectionParams.z / cameraRay.z) * depth, 1)).xyz;
		float3 rayDir = normalize(_WorldSpaceCameraPos - wpos);
		float zScale = (dot(CameraFoward, -rayDir));


#else

		float4 clipPos = float4(data.zw * 2.0 - 1.0, 1.0, 1.0);
		float4 cameraRay = mul(InverseProjectionMatrix, clipPos);
		cameraRay = (cameraRay / cameraRay.w);
		float3 wpos = mul(unity_CameraToWorld, float4(cameraRay.xyz * (_ProjectionParams.z / cameraRay.z) * depth, 1)).xyz;
		float3 rayDir = normalize(_WorldSpaceCameraPos - wpos);
		float zScale = (dot(CameraFoward, -rayDir));
#endif
#endif

		float3 Ambient = float3(0, 0, 0);


		return March(rayDir, wpos, Ambient, depth, zScale, uv);
	}
	

		ENDCG
		SubShader
	{

		Pass
		{
	
			Tags{ "IgnoreProjector" = "True" }
			ZTest Always Cull Back ZWrite Off
			Blend One One
			
			CGPROGRAM
#pragma vertex vert
#pragma fragment FragSingle
#pragma target 3.0
			ENDCG
		}

			Pass
		{

			Tags{ "IgnoreProjector" = "True" }
			ZTest Always Cull Back ZWrite Off
			Blend One One

			CGPROGRAM
#pragma vertex vert
#pragma fragment FragTri
#pragma target 3.0
			ENDCG
		}

			Pass
		{
			ZTest Always Cull Back ZWrite Off
			Blend One One

			CGPROGRAM
#pragma vertex vert
#pragma fragment FragSky
#pragma target 3.0
			ENDCG
		}
	}
	Fallback off
}
