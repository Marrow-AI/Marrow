// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'




Shader "Hidden/HxVolumetricProjector"
{
	CGINCLUDE


#include "UnityCG.cginc"
#include "UnityDeferredLibrary.cginc"
#include "UnityPBSLighting.cginc"
#include "HxVolumetricLightCore.cginc"



#pragma shader_feature DENSITYPARTICLES_ON
#pragma shader_feature VTRANSPARENCY_ON
#pragma shader_feature FULL_ON
#pragma shader_feature NOISE_ON
#pragma shader_feature HEIGHTFOG_ON
#pragma shader_feature UNITY_SINGLE_PASS_STEREO

		float OrthoLight;
	float3 hxCameraPosition;
	float3 hxCameraPosition2;
	float4x4 hxCameraToWorld;
	float4x4 hxCameraToWorld2; //second eye
	float hxNearPlane;
	uniform float4 _SpotLightParams;
	float hxRayOffset;
	uniform float3 LightColour;
	uniform float3 LightColour2;
	float TintPercent;
	float3 CameraFoward;
	uniform float4 _LightParams;
	uniform float4 _CustomLightPosition;
	float HxTileSize;
	uniform sampler2D Tile5x5;
	float4x4 InverseProjectionMatrix;
	float4x4 InverseProjectionMatrix2;
	float4x4 InverseViewMatrix;

	float4x4 VolumetricMVP;
	float4x4 VolumetricMV;
	float4x4 SpotLightMatrix;
	
	uniform float VolumeScale;

#ifdef FULL_ON

#else
	float4x4 _InvViewProj;
	sampler2D_float  VolumetricDepth;
#endif

	sampler2D _ShadowTex;
	sampler2D _FalloffTex;
	


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

	v2f vert(appdata v)
	{
		v2f o;
#if UNITY_SINGLE_PASS_STEREO
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = ComputeScreenPos(o.pos);
#ifdef FULL_ON
		o.ray = mul(UNITY_MATRIX_MV, v.vertex).xyz * float3(1, 1, 1);
#endif
#else
		o.pos = mul(VolumetricMVP, v.vertex);
		o.uv = ComputeScreenPos(o.pos);
#ifdef FULL_ON
		o.ray = mul(VolumetricMV, v.vertex).xyz * float3(1, 1, 1);
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
	//v2f vert(appdata v)
	//{
	//	v2f o;
	//	o.pos = mul(VolumetricMVP, v.vertex);
	//	o.uv = ComputeScreenPos(o.pos);
	//
	//	#ifdef FULL_ON
	//	o.ray = mul(VolumetricMV, v.vertex).xyz * float3(-1, -1, 1);
	//	#endif
	//	return o;
	//}



	static const float PI4 = 12.5663706f;

	struct Intersection {
		bool hit;
		float t1, t2;
	};

		float4 ShadowBias;

	float3 TopFrustumNormal;
	float3 BottomFrustumNormal;
	float3 LeftFrustumNormal;
	float3 RightFrustumNormal;
	float3 UpFrustumOffset;
	float3 RightFrustumOffset;

	float2 IntersectionPoints(float3 rayDir)
	{
		float Near = 0;
		float Far = 1000000;
		
		float3 topDenom;
		float3 top;
		float3 bottomDenom;
		float3 bottom;

		bottomDenom.x = dot(_SpotLightParams.xyz, rayDir);
		bottom.x = dot((_CustomLightPosition + _SpotLightParams.xyz * _LightParams.z) - _WorldSpaceCameraPos, _SpotLightParams.xyz);

		bottomDenom.y = dot(BottomFrustumNormal, rayDir);
		bottom.y = dot(_CustomLightPosition - _WorldSpaceCameraPos - UpFrustumOffset, BottomFrustumNormal);

		bottomDenom.z = dot(LeftFrustumNormal, rayDir);
		bottom.z = dot(_CustomLightPosition - _WorldSpaceCameraPos - RightFrustumOffset, LeftFrustumNormal);

		topDenom.x = dot(-_SpotLightParams.xyz, rayDir);
		top.x = dot((_CustomLightPosition + _SpotLightParams * hxNearPlane) - _WorldSpaceCameraPos, -_SpotLightParams.xyz);

		topDenom.y = dot(TopFrustumNormal, rayDir);
		top.y = dot(_CustomLightPosition - _WorldSpaceCameraPos + UpFrustumOffset, TopFrustumNormal);

		topDenom.z = dot(RightFrustumNormal, rayDir);
		top.z = dot(_CustomLightPosition - _WorldSpaceCameraPos + RightFrustumOffset, RightFrustumNormal);

		float near = 0;
		float far = 100000;
		if (topDenom.x > 0)
		{
			far = min(far, top.x / topDenom.x);
		}
		else if (topDenom.x < 0)
		{
			near = max(near, top.x / topDenom.x);
		}

		if (topDenom.y > 0)
		{
			far = min(far, top.y / topDenom.y);
		}
		else if (topDenom.y < 0)
		{
			near = max(near, top.y / topDenom.y);
		}

		if (topDenom.z > 0)
		{
			far = min(far, top.z / topDenom.z);
		}
		else if (topDenom.z < 0)
		{
			near = max(near, top.z / topDenom.z);
		}

		if (bottomDenom.x > 0)
		{
			far = min(far, bottom.x / bottomDenom.x);
		}
		else if (bottomDenom.x < 0)
		{
			near = max(near, bottom.x / bottomDenom.x);
		}

		if (bottomDenom.y > 0)
		{
			far = min(far, bottom.y / bottomDenom.y);
		}
		else if (bottomDenom.y < 0)
		{
			near = max(near, bottom.y / bottomDenom.y);
		}

		if (bottomDenom.z > 0)
		{
			far = min(far, bottom.z / bottomDenom.z);
		}
		else if (bottomDenom.z < 0)
		{
			near = max(near, bottom.z / bottomDenom.z);
		}



		
		return float2(near, far);

	}

	Intersection lineVsSphere(float3 linePos, float3 lineDir, float3 spherePos, float sphereRadius)
	{

		float3 sphereToLine = linePos - spherePos;
			float b = 2 * dot(lineDir, sphereToLine);
		float c = dot(sphereToLine, sphereToLine) - sphereRadius * sphereRadius;

		float sqrtInner = b * b - 4 * c;
		if (sqrtInner < 0) {
			Intersection i;
			i.hit = false;
			i.t1 = 0;
			i.t2 = 0;
			return i;
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


	Intersection lineVsInfiniteMirroredCone(float3 linePos, float3 lineDir, float3 conePos, float3 coneDir, float coneCosAngle)
	{

		float cos2 = coneCosAngle * coneCosAngle;
		float dirsDot = dot(coneDir, lineDir);
		float3 lineToCone = linePos - conePos;
			float coneDirDotLineToCone = dot(coneDir, lineToCone);


		float a = (dirsDot * dirsDot) - cos2;
		float b = 2.0 * (coneDirDotLineToCone * dirsDot - cos2 * dot(lineToCone, lineDir));
		float c = coneDirDotLineToCone * coneDirDotLineToCone - cos2 * dot(lineToCone, lineToCone);


		float sqrtInner = b * b - 4 * a * c;
		if (sqrtInner < 0) {
			Intersection i;
			i.hit = false;
			i.t1 = 0;
			i.t2 = 0;
			return i;
		}


		float sqrtProd = sqrt(sqrtInner);
		float a2 = 2 * a;
		float t1 = (-b - sqrtProd) / a2;
		float t2 = (-b + sqrtProd) / a2;

		Intersection i;
		i.hit = true;
		i.t1 = t1;
		i.t2 = t2;
		return i;
	}



	Intersection rayVsFiniteCone(float3 rayPos, float3 rayDir, float3 conePos, float3 coneDir, float coneCosAngle, float coneRange)
	{
		Intersection coneIsect = lineVsInfiniteMirroredCone(rayPos, rayDir, conePos, coneDir, coneCosAngle);
	
		if (!coneIsect.hit) {
			Intersection i;
			i.hit = false;
			i.t1 = 0;
			i.t2 = 0;
			return i;
		}

		Intersection sphereIsect = lineVsSphere(rayPos, rayDir, conePos, coneRange);
		float3 c1 = rayPos + rayDir * coneIsect.t1;
			float3 c2 = rayPos + rayDir * coneIsect.t2;
			float3 s1 = rayPos + rayDir * sphereIsect.t1;
			float3 s2 = rayPos + rayDir * sphereIsect.t2;

			float3 coneToC1 = c1 - conePos;
			float3 coneToC2 = c2 - conePos;
			float3 coneToS1 = s1 - conePos;
			float3 coneToS2 = s2 - conePos;

			float hits[4] = { 0, 0, 0, 0 }; 
		int index = 0;
		float mins[4] = { 100000, 100000, 100000, 100000 };
		float maxs[4] = { -100000, -100000, -100000, -100000 };


		if (dot(coneToC1, coneDir) > 0 && length(coneToC1) < coneRange) {
			mins[0] = min(coneIsect.t1, mins[0]);
			maxs[0] = max(coneIsect.t1, maxs[0]);
			index++;
		}
		if (dot(coneToC2, coneDir) > 0 && length(coneToC2) < coneRange) {
			mins[1] = min(coneIsect.t2, mins[1]);
			maxs[1] = max(coneIsect.t2, maxs[1]);
			index++;
		}
		if (dot(normalize(coneToS1), coneDir) > coneCosAngle) {
			mins[2] = min(sphereIsect.t1, mins[2]);
			maxs[2] = max(sphereIsect.t1, maxs[2]);
			index++;
		}
		if (dot(normalize(coneToS2), coneDir) > coneCosAngle) {
			mins[3] = min(sphereIsect.t2, mins[3]);
			maxs[3] = max(sphereIsect.t2, maxs[3]);
			index++;
		}

		float closest = min(mins[0], min(mins[1], min(mins[2], mins[3])));
		float farthest = max(maxs[0], max(maxs[1], max(maxs[2], maxs[3])));

		Intersection i;
		i.hit = index == 2 && farthest >= 0;
		i.t1 = max(0, closest);
		i.t2 = max(0, farthest);
		return i;
	}






vr MarchColor(float3 dir, float dis, float3 wpos, float2 uv, float2 InterleavePos, float3 EnterPoint, float3 ExitPoint)
{
	vr vrout;


	//convert into local space
	float3 WorldEnter = EnterPoint;
	dis = length(WorldEnter - _WorldSpaceCameraPos);
	float3 difWorld = ExitPoint - EnterPoint;
	int NUM_SAMPLES = min(Density.y, 128);
//#if UNITY_SINGLE_PASS_STEREO
//	float2 spuv = float2(frac(uv.x * 2), uv.y);
//	float centerdis = length(float2(0.5f, 0.5f) - spuv) * 4;
//	NUM_SAMPLES = NUM_SAMPLES * min(1, 2 - centerdis);
//#else
//	//float t = NUM_SAMPLES;
//	//float centerdis = length(float2(0.5f, 0.5f) - uv) * 2.2;
//	//NUM_SAMPLES = floor(t * min(1, 1.1 - centerdis));
//	//clip(NUM_SAMPLES);
//#endif

	
	float3 RayDirection = normalize(difWorld);
	float MarchLength = length(difWorld);
	float WolrdstepSize = MarchLength / (NUM_SAMPLES);
	float3 dirScaledWorld = RayDirection  * (WolrdstepSize);



	EnterPoint = mul(unity_WorldToObject, float4(EnterPoint - _CustomLightPosition, 0)).xyz;
	ExitPoint = mul(unity_WorldToObject, float4(ExitPoint - _CustomLightPosition, 0)).xyz;


	float3 Difff = ExitPoint - EnterPoint;

	float stepSize = (length(Difff) / NUM_SAMPLES);
	float3 dirScaled = normalize(Difff) * stepSize;

	InterleavePos = InterleavePos.xy / HxTileSize;
	float index = frac(tex2Dlod(Tile5x5, float4(InterleavePos.xy, 0, 0)).r + hxRayOffset);
	float3 Offset = dirScaled * (index);
	float3 OffsetWorld = dirScaledWorld * index;

	EnterPoint += Offset;

	WorldEnter += OffsetWorld;


	float extinction = (dis)* Density.x * Density.w;


	float extinctionAdd = WolrdstepSize * Density.w;
	float3 AccumulatedColor = float3(0,0,0);

#if defined (VTRANSPARENCY_ON) ||  (DENSITYPARTICLES_ON)
	float zScale =  (dot(CameraFoward, RayDirection));
	float rayWorldDis = dis * zScale;
	float zStep = WolrdstepSize / zScale;
#endif

#ifdef DENSITYPARTICLES_ON
	DensityMap dm = LoadSliceData(uv, dis + MarchLength);
#endif

#ifdef DENSITYPARTICLES_ON
	extinction += DensityToPoint(dm, rayWorldDis) * Density.w;
	float LowValue = 0;
	float HighValie = 0;
	float CurrentLow = -1;
#endif


	float3 ThisMarch;

	for (int i = 0; i < NUM_SAMPLES; i++)
	{
		
		float radiusAtz = EnterPoint.z * 0.5;
		float2 suv = float2(1 / radiusAtz * EnterPoint.x, 1 / radiusAtz * EnterPoint.y);
		suv /= 2;


		suv.x += 0.5f;
		suv.y += 0.5f;

		float3 tolight = WorldEnter - _CustomLightPosition;

		float fogDensity = GetFogDensity(WorldEnter);

#ifdef DENSITYPARTICLES_ON
		fogDensity = max(fogDensity + DensityFrom3DTexture(dm, rayWorldDis, LowValue, HighValie, CurrentLow),0);
#endif

		float phase = HenyeyPhase(dot(normalize(tolight), -RayDirection));

		float att = dot(tolight, tolight) * (1.0 / (_LightParams.z * _LightParams.z));
		float atten = tex2Dlod(_FalloffTex, float4(att, att, 0, 0)).a;
		float4 ProjectorColor = tex2Dlod(_ShadowTex, float4(lerp(suv.xy, EnterPoint.xy + float2(0.5f,0.5f), OrthoLight), 0, 0));
		atten *= ProjectorColor.a;
	


		//add to slices....
		ThisMarch = (ProjectorColor * lerp(LightColour2.rgb, LightColour.rgb, saturate(TintPercent * (min(1 - att, 1 - length(suv.xy - float2(0.5,0.5)) * 2)) / 2))) * max((phase) * atten * exp(-extinction)* fogDensity * WolrdstepSize, 0);
		AccumulatedColor += ThisMarch;

#ifdef VTRANSPARENCY_ON
		float lum = Luminance(ThisMarch);
		AddTransparency(vrout, lum, rayWorldDis);
#endif

		extinction += extinctionAdd * fogDensity;
		EnterPoint += dirScaled;
	
		WorldEnter += dirScaledWorld;
#if defined (VTRANSPARENCY_ON) || (DENSITYPARTICLES_ON)
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



		float3 lightPos = _CustomLightPosition;
		float3 dif = wpos - lightPos;
		float dis = length(dif);
		float3 rayDir = normalize(wpos - _WorldSpaceCameraPos);
		float3 rayDis = length(wpos - _WorldSpaceCameraPos) - 0.05;
		//Intersection isect = rayVsFiniteCone(_WorldSpaceCameraPos, rayDir, _CustomLightPosition.xyz, _SpotLightParams.xyz, cos(_SpotLightParams.w), _LightParams.z);
		//
		//if (!isect.hit) {
		//	discard;
		//	//clip(-1);
		//	//return float4(0, 0, 0, 1);
		//}
		//
		//float endT = min(max(isect.t2, isect.t1), rayDis);
		//float startT = max(min(isect.t2, isect.t1), 0);

		float2 intersectTest = IntersectionPoints(rayDir);
		
		float endT = min(intersectTest.y, rayDis);
		float startT = max(intersectTest.x, 0);

		if (endT <= startT) {
			discard;
			//clip(-1);
			//return float4(0, 0, 0, 1);
		}

		float3 near = _WorldSpaceCameraPos + rayDir * startT;
		float3 far = _WorldSpaceCameraPos + rayDir * endT;

		float3 p0 = _CustomLightPosition + _SpotLightParams.xyz * hxNearPlane;
		float tn = dot(near - p0, _SpotLightParams.xyz);
		float tf = dot(far - p0, _SpotLightParams.xyz);

		if (tn <= 0 && tf <= 0)
		{
			discard;
		}

		if (tn > 0 && tf <= 0)
		{
			float denom = dot(-_SpotLightParams.xyz, rayDir);
			if (denom > 1e-6) {
			
				float3 p0l0 = p0 - _WorldSpaceCameraPos;
				float t = dot(p0l0, -_SpotLightParams.xyz) / denom;
				far = _WorldSpaceCameraPos + rayDir * t;
			}
		}

		if (tn <= 0 && tf > 0)
		{
			float denom = dot(_SpotLightParams.xyz, rayDir);
			if (denom > 1e-6) {
		
				float3 p0l0 = p0 - _WorldSpaceCameraPos;
				float t = dot(p0l0, _SpotLightParams.xyz) / denom;
				near = _WorldSpaceCameraPos + rayDir * t;
			}
		}


		return MarchColor(rayDis, startT, wpos, uv.xy, fmod((_ScreenParams.xy * VolumeScale) * uv, HxTileSize), near, far);
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


