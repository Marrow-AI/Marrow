// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'



Shader "Hidden/HxVolumetricPointLight"
{
	CGINCLUDE
#define SHADOWS_NATIVE
#define POINT
#define SHADOWS_CUBE

#include "UnityCG.cginc"
#include "UnityDeferredLibrary.cginc"
#include "UnityPBSLighting.cginc"
#include "HxVolumetricLightCore.cginc"


#pragma shader_feature DENSITYPARTICLES_ON 
#pragma shader_feature VTRANSPARENCY_ON
#pragma shader_feature FULL_ON
#pragma shader_feature SHADOWS_OFF
#pragma shader_feature NOISE_ON 
#pragma shader_feature POINT_COOKIE
#pragma shader_feature HEIGHTFOG_ON
#pragma shader_feature UNITY_SINGLE_PASS_STEREO


		sampler2D _FalloffTex;
	float hxRayOffset;
	float4x4 InverseProjectionMatrix2;
	float4x4 InverseProjectionMatrix;
	uniform float3 LightColour;
	uniform float3 LightColour2;
	float3 CameraFoward;
	uniform float4 _LightParams;
	uniform float4 _CustomLightPosition;

	float HxTileSize;
	uniform sampler2D Tile5x5;

	float3 hxCameraPosition;
	float3 hxCameraPosition2;
	float4x4 hxCameraToWorld;
	float4x4 hxCameraToWorld2; //second eye
	float4x4 InverseViewMatrix;

	float4x4 VolumetricMVP;
	float4x4 VolumetricMV;
	float TintPercent;

#ifdef FULL_ON

#else
float4x4 _InvViewProj;
sampler2D_float  VolumetricDepth;
#endif

#ifdef POINT_COOKIE
samplerCUBE PointCookieTexture;
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
#define CUSTOM_DECLARE_TEXCUBE_SHADOWMAP(tex) TextureCube tex : register(t15); SamplerComparisonState sampler##tex : register(s15)
#else
#define CUSTOM_DECLARE_SHADOWMAP(tex) Texture2D tex; SamplerComparisonState sampler##tex
#define CUSTOM_DECLARE_TEXCUBE_SHADOWMAP(tex) TextureCube tex; SamplerComparisonState sampler##tex
#endif
#define CUSTOM_SAMPLE_SHADOW(tex,coord) tex.SampleCmpLevelZero (sampler##tex,(coord).xy,(coord).z)
#define CUSTOM_SAMPLE_SHADOW_PROJ(tex,coord) tex.SampleCmpLevelZero (sampler##tex,(coord).xy/(coord).w,(coord).z/(coord).w)
#define CUSTOM_SAMPLE_TEXCUBE_SHADOW(tex,coord) tex.SampleCmpLevelZero (sampler##tex,(coord).xyz,(coord).w)
#elif defined(UNITY_COMPILER_HLSL2GLSL) && defined(SHADOWS_NATIVE)
// OpenGL-like hlsl2glsl platforms: most of them always have built-in PCF
#define CUSTOM_DECLARE_SHADOWMAP(tex) sampler2DShadow tex
#define CUSTOM_DECLARE_TEXCUBE_SHADOWMAP(tex) samplerCUBEShadow tex
#define CUSTOM_SAMPLE_SHADOW(tex,coord) shadow2D (tex,(coord).xyz)
#define CUSTOM_SAMPLE_SHADOW_PROJ(tex,coord) shadow2Dproj (tex,coord)
#define CUSTOM_SAMPLE_TEXCUBE_SHADOW(tex,coord) ((texCUBE(tex,(coord).xyz) < (coord).w) ? 0.0 : 1.0)
#elif defined(SHADER_API_D3D9)
// D3D9: Native shadow maps FOURCC "driver hack", looks just like a regular
// texture sample. Have to always do a projected sample
// so that HLSL compiler doesn't try to be too smart and mess up swizzles
// (thinking that Z is unused).
#define CUSTOM_DECLARE_SHADOWMAP(tex) sampler2D tex
#define CUSTOM_DECLARE_TEXCUBE_SHADOWMAP(tex) samplerCUBE tex
#define CUSTOM_SAMPLE_SHADOW(tex,coord) tex2Dproj (tex,float4((coord).xyz,1)).r
#define CUSTOM_SAMPLE_SHADOW_PROJ(tex,coord) tex2Dproj (tex,coord).r
#define CUSTOM_SAMPLE_TEXCUBE_SHADOW(tex,coord) (texCUBEproj(tex,coord).r)
#elif defined(SHADER_API_PSSL)
// PS4: built-in PCF
#define CUSTOM_DECLARE_SHADOWMAP(tex)        Texture2D tex; SamplerComparisonState sampler##tex
#define CUSTOM_DECLARE_TEXCUBE_SHADOWMAP(tex) samplerCUBE tex; SamplerComparisonState sampler##tex
#define CUSTOM_SAMPLE_SHADOW(tex,coord)      tex.SampleCmpLOD0(sampler##tex,(coord).xy,(coord).z)
#define CUSTOM_SAMPLE_SHADOW_PROJ(tex,coord) tex.SampleCmpLOD0(sampler##tex,(coord).xy/(coord).w,(coord).z/(coord).w)
#define CUSTOM_SAMPLE_TEXCUBE_SHADOW(tex,coord) tex.SampleCmpLOD0(sampler##tex,(coord).xyz,(coord).w)
#elif defined(SHADER_API_PSP2)
// Vita
#define CUSTOM_DECLARE_SHADOWMAP(tex) sampler2D tex
#define CUSTOM_DECLARE_TEXCUBE_SHADOWMAP(tex) samplerCUBE tex
// tex2d shadow comparison on Vita returns 0 instead of 1 when shadowCoord.z >= 1 causing artefacts in some tests.
// Clamping Z to the range 0.0 <= Z < 1.0 solves this.
#define CUSTOM_SAMPLE_SHADOW(tex,coord) tex2D<float>(tex, float3((coord).xy, clamp((coord).z, 0.0, 0.999999)))
#define CUSTOM_SAMPLE_SHADOW_PROJ(tex,coord) tex2DprojShadow(tex, coord)
#define CUSTOM_SAMPLE_TEXCUBE_SHADOW(tex,coord) texCUBE<float>(tex, float3((coord).xyz, clamp((coord).w, 0.0, 0.999999)))
#else
// Fallback / No built-in shadowmap comparison sampling: regular texture sample and do manual depth comparison
#define CUSTOM_DECLARE_SHADOWMAP(tex) sampler2D_float tex
#define CUSTOM_DECLARE_TEXCUBE_SHADOWMAP(tex) samplerCUBE_float tex
#define CUSTOM_SAMPLE_SHADOW(tex,coord) ((SAMPLE_DEPTH_TEXTURE(tex,(coord).xy) < (coord).z) ? 0.0 : 1.0)
#define CUSTOM_SAMPLE_SHADOW_PROJ(tex,coord) ((SAMPLE_DEPTH_TEXTURE_PROJ(tex,UNITY_PROJ_COORD(coord)) < ((coord).z/(coord).w)) ? 0.0 : 1.0)
#define CUSTOM_SAMPLE_TEXCUBE_SHADOW(tex,coord) ((SAMPLE_DEPTH_CUBE_TEXTURE(tex,(coord).xyz) < (coord).w) ? 0.0 : 1.0)
#endif



	float VolumeScale;
	uniform float VolumeRes;
	float4 _CameraDepthTexture_TexelSize;


	
	struct appdata {
		float4 vertex : POSITION;

	};

	struct v2f {
		float4 pos : SV_POSITION;
		float4 uv : TEXCOORD0;
#ifdef FULL_ON
		float3 ray : TEXCOORD1;
#endif
	};

	inline float CustomSampleCubeDistance(float3 vec)
	{
#if defined (SHADOWS_CUBE_IN_DEPTH_TEX)
		return 1;
#else
#if UNITY_VERSION > 565 && (UNITY_VERSION < 20171 || UNITY_VERSION > 201722) //unity switched shadow maps for 2 version for some reason.
		return UnityDecodeCubeShadowDepth(UNITY_SAMPLE_TEXCUBE_LOD(_ShadowMapTexture, vec, 0));

#else
#if defined(NGSS_USE_BIAS_FADE) 
		return UnityDecodeCubeShadowDepth(UNITY_SAMPLE_TEXCUBE_LOD(_ShadowMapTexture, vec, 0)); //NGSS changes format
#else
		//might need something for metal as well. 
		return UnityDecodeCubeShadowDepth(texCUBElod(_ShadowMapTexture, float4(vec, 0)));
#endif
#endif
#endif
	}


	float3 ShadowBias;
#ifndef SHADOWS_OFF


	float ShadowDistance;
	//uniform samplerCUBE_float _ShadowMapTexture;

	inline half CustomSampleShadowmap(float3 vec)
	{
#if defined (SHADOWS_CUBE_IN_DEPTH_TEX)
		float3 absVec = abs(vec);
		float dominantAxis = max(max(absVec.x, absVec.y), absVec.z); // TODO use max3() instead
		dominantAxis = max(0.00001, dominantAxis - _LightProjectionParams.z); // shadow bias from point light is apllied here.
		dominantAxis *= _LightProjectionParams.w; // bias
		float mydist = -_LightProjectionParams.x + _LightProjectionParams.y / dominantAxis; // project to shadow map clip space [0; 1]

#if defined(UNITY_REVERSED_Z)
		mydist = 1.0 - mydist; // depth buffers are reversed! Additionally we can move this to CPP code!
#endif

		half shadow = UNITY_SAMPLE_TEXCUBE_SHADOW(_ShadowMapTexture, float4(vec, mydist));
		return lerp(_LightShadowData.r, 1.0, shadow);


#else 

		float mydist = ((length(vec) + ShadowBias.x) * _LightParams.y);

		float dist = CustomSampleCubeDistance(vec); //UnityDecodeCubeShadowDepth(texCUBElod(_ShadowMapTexture, float4(vec, 0)));

		return dist < mydist ? 0 : 1.0;
#endif
	}
#endif

	v2f vert(appdata v)
	{
		v2f o;
#if UNITY_SINGLE_PASS_STEREO
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = ComputeScreenPos(o.pos);
#ifdef FULL_ON
		o.ray = mul(UNITY_MATRIX_MV, v.vertex).xyz;
#endif
#else
		o.pos = mul(VolumetricMVP, v.vertex);
		o.uv = ComputeScreenPos(o.pos);
#ifdef FULL_ON
		
		o.ray = mul(VolumetricMV, v.vertex).xyz;
#endif
#endif
		return o;
	}

	v2f vert2(appdata v)
	{
		v2f o;
#if UNITY_SINGLE_PASS_STEREO

		o.pos = UnityObjectToClipPos(v.vertex);
		o.pos.z = min(_ProjectionParams.z, o.pos.z);
		o.uv = ComputeScreenPos(o.pos);
#ifdef FULL_ON
		o.ray = mul(UNITY_MATRIX_MV, v.vertex).xyz * float3(1, 1, 1);
#endif
#else
		o.pos = mul(VolumetricMVP, v.vertex);
		o.pos.z = min(_ProjectionParams.z, o.pos.z);
		o.uv = ComputeScreenPos(o.pos);
#ifdef FULL_ON
		o.ray = mul(VolumetricMV, v.vertex).xyz * float3(1, 1, 1);
#endif
#endif
		return o;
	}


	static const float PI = 3.14159265f;
	static const float PI4 = 12.5663706f;
	static const float G_SCATTERING = 0.8;

	float ComputeScattering(float lightDotView)
	{
		float result = 1.0f - G_SCATTERING;
		result *= result;
		result /= (4.0f * PI * pow(1.0f + G_SCATTERING * G_SCATTERING - (2.0f * G_SCATTERING) *      lightDotView, 1.5f));
		return result;
	}


	struct Intersection {
		bool hit;
		float t1, t2;
	};

	Intersection lineVsSphere(float3 linePos, float3 lineDir, float3 spherePos, float sphereRadius)
	{
		float3 sphereToLine = linePos - spherePos;
		float b = 2 * dot(lineDir, sphereToLine);
		float c = dot(sphereToLine, sphereToLine) - sphereRadius * sphereRadius;

		float sqrtInner = b * b - 4 * c;
		if (sqrtInner < 0) {
			clip(-1);
		}

		float sqrtProd = sqrt(sqrtInner);
		float t1 = (-b - sqrtProd) / 2;
		float t2 = (-b + sqrtProd) / 2;

		Intersection i;
		i.hit = true;
		i.t1 = t1;
		i.t2 = t2;

		return i;
	}



	vr MarchColor(float3 wpos, float index,float2 uv)
	{
		vr vrout;
		float3 dif = wpos - _CustomLightPosition;
		float3 dir = normalize(wpos - _WorldSpaceCameraPos);
		float rayDis = length(wpos - _WorldSpaceCameraPos);
		int NUM_SAMPLES = min(Density.y, 128);


		

//#if UNITY_SINGLE_PASS_STEREO
//		float2 spuv = float2(frac(uv.x * 2), uv.y);
//		float centerdis = length(float2(0.5f, 0.5f) -  spuv) * 4;
//		NUM_SAMPLES = NUM_SAMPLES * min(1, 2 - centerdis);
//#else
//		//float t = NUM_SAMPLES;
//		//float centerdis = length(float2(0.5f, 0.5f) - uv) * 2.2;
//		//NUM_SAMPLES = floor(t * min(1, 1.1 - centerdis));
//		//clip(NUM_SAMPLES);
//#endif





		Intersection intersect = lineVsSphere(_WorldSpaceCameraPos, dir, _CustomLightPosition, _LightParams.z);

		float endT = min(max(intersect.t2, intersect.t1), rayDis);
		float startT = max(min(intersect.t2, intersect.t1), 0);

		clip(endT - startT - 0.0001f); //clip if near is behind depth //this shouldnt ever be called now

		float3 EnterPoint = _WorldSpaceCameraPos + dir * (startT);
		float3 ExitPoint = _WorldSpaceCameraPos + dir * (endT);

		float MarchLength = endT - startT;
		float stepSize = MarchLength / (NUM_SAMPLES);
		float3 dirScaled = dir * stepSize;
		
		float3 Offset = dirScaled * index;
	
		EnterPoint += Offset;
		#ifdef NOISE_ON		
		//todo heightfog...
		float extinction = (startT) * Density.x * Density.w;
		#else
		float extinction = (startT) * Density.x * Density.w;
		#endif
		float extinctionAdd = stepSize * Density.w;
		float3 AccumulatedColor = float3(0,0,0);
		

#if defined (VTRANSPARENCY_ON) ||  (DENSITYPARTICLES_ON)
		float zScale = (dot(CameraFoward, dir));
		float rayWorldDis = length(EnterPoint - _WorldSpaceCameraPos) * zScale;
		float zStep = stepSize / zScale;
#endif

#ifdef DENSITYPARTICLES_ON
		DensityMap dm = LoadSliceData(uv, endT);
#endif

#ifdef DENSITYPARTICLES_ON
		extinction += DensityToPoint(dm, rayWorldDis) * Density.w;
		float LowValue = 0;
		float HighValue = 0;
		float CurrentLow = -1;
#endif
		float colorBlend = _LightParams.y * length(cross(dir, EnterPoint - _CustomLightPosition));
		float3 rayColor = lerp(LightColour.rgb, LightColour2.rgb, colorBlend * colorBlend);
		float3 tolight;
		float3 ThisColor;
		[loop]
		for (int i = 0; i < NUM_SAMPLES; i++)
		{
			
			tolight = EnterPoint - _CustomLightPosition;

			float cd = 1;
			#ifndef SHADOWS_OFF
			cd = CustomSampleShadowmap(tolight);
			#endif
			float3 ld = normalize(tolight);
			float fogDensity = GetFogDensity(EnterPoint);
#ifdef DENSITYPARTICLES_ON
			fogDensity = max(fogDensity + DensityFrom3DTexture(dm, rayWorldDis, LowValue, HighValue, CurrentLow),0);
#endif
			float phase = HenyeyPhase(dot(ld, -dir));


			float att = dot(tolight, tolight) * (1.0 / (_LightParams.z * _LightParams.z));
			float atten = tex2Dlod(_FalloffTex, float4(att, 0, 0, 0)).a;//tex2Dlod(_LightTextureB0, float4(att.r, 0, 0, 0)).UNITY_ATTEN_CHANNEL;//  tex2Dlod(_FalloffTex, float4(att, 0, 0, 0)).a;

#ifdef POINT_COOKIE

			atten *= texCUBElod(PointCookieTexture, float4(mul(unity_WorldToObject, float4(EnterPoint, 1)).xyz, 0)).a;
#endif
			
				ThisColor = lerp(LightColour2.rgb, LightColour.rgb, saturate(TintPercent * (1-att) /2)) * max(0,(phase * cd + (ShadowBias.z * (1 - cd))) * atten * exp(-extinction)* fogDensity * stepSize);
				AccumulatedColor += ThisColor;


#ifdef VTRANSPARENCY_ON
				float lum = Luminance(ThisColor);
				AddTransparency(vrout, lum, rayWorldDis);
#endif


				EnterPoint += dirScaled;
				
				extinction += extinctionAdd * fogDensity; 

#ifdef DENSITYPARTICLES_ON	
				rayWorldDis += zStep;
#endif
		}
	
		vrout.col0 = float4((AccumulatedColor), 0);


		return vrout;
	}


	vr frag(v2f i)
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
#else

		float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv.xy, 0, 0)));
		float3 wpos = mul(hxCameraToWorld, float4(-i.ray * (_ProjectionParams.z / i.ray.z) * depth, 1)).xyz;
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
	
#else

		float4 clipPos = float4((float2(1, 1) - data.zw) * 2.0 - 1.0, 1.0, 1.0);
		float4 cameraRay = mul((InverseProjectionMatrix), clipPos);
		cameraRay = (cameraRay / cameraRay.w);
		float3 wpos = mul(hxCameraToWorld, float4(-cameraRay.xyz * (_ProjectionParams.z / cameraRay.z) * depth, 1)).xyz;
#endif

#endif
		
		float index = frac(tex2Dlod(Tile5x5, float4(fmod((_ScreenParams.xy * VolumeScale) * uv, HxTileSize) / HxTileSize, 0, 0)).r + hxRayOffset);
	
		return MarchColor(wpos,index, uv);// , near, far);
	
	}



		ENDCG
		SubShader
	{
		
		Fog{ Mode Off }
		Lighting Off
		ZWrite Off
			
		Blend One One



		Pass
		{
	
		Blend One One
			ZWrite Off
			ZTest Always
			Cull Front
			CGPROGRAM
#pragma target 3.0
#pragma vertex vert2
#pragma fragment frag 
#pragma exclude_renderers nomrt
			ENDCG
		}

		Pass
			{
		
			Blend One One
				ZWrite Off
				ZTest LEqual
				Cull Back
				CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag  
#pragma exclude_renderers nomrt
				ENDCG
			}
	}

}

