#include "UnityCG.cginc"
// Upgrade NOTE: excluded shader from DX11, Xbox360, OpenGL ES 2.0 because it uses unsized arrays
#pragma exclude_renderers d3d11 xbox360 gles
#include "Assets/Plugins/HxVolumetricLighting/Resources/Shaders/HxQualitySettings.cginc"

sampler2D VolumetricTexture;



#if defined (TS_16)
#if defined(SHADER_API_D3D11) || defined(SHADER_API_PSSL)
#define S_16
#else 
#define S_12
#endif
#endif

#ifdef TS_12
#define S_12
#endif

#ifdef TS_8
#define S_8
#endif

#ifdef TS_4
#define S_4
#endif
float hxNoiseContrast;
float4 Phase;
float4 Phase2;
float HenyeyPhase(float cosTheta)
{

	return Phase.x * (Phase.y / (pow(Phase.z - Phase.w * cosTheta, 1.5)));
}

struct vr {
#if defined(SHADER_API_PSSL)
	fixed4 col0 : S_TARGET_OUTPUT;
#else
	fixed4 col0 : COLOR0;
#endif
#ifdef VTRANSPARENCY_ON
#if defined(SHADER_API_PSSL)
	fixed4 col1 : S_TARGET_OUTPUT1;
#else
	fixed4 col1 : COLOR1;
#endif
#ifndef S_4
#if defined(SHADER_API_PSSL)
	fixed4 col2 : S_TARGET_OUTPUT2;
#else
	fixed4 col2 : COLOR2;
#endif
#ifndef S_8
#if defined(SHADER_API_PSSL)
	fixed4 col3 : S_TARGET_OUTPUT3;
#else
	fixed4 col3 : COLOR3;
#endif
#if defined (S_16)
#if defined(SHADER_API_PSSL)
	fixed4 col4 : S_TARGET_OUTPUT4;
#else
	fixed4 col4 : COLOR4;
#endif
#endif
#endif
#endif
//	fixed4 col5 : COLOR5;
//	fixed4 col6 : COLOR6;
//	fixed4 col7 : COLOR7;
//#endif
#endif
};


#ifdef NOISE_ON
uniform sampler3D NoiseTexture3D;
float3 NoiseOffset;
float3 NoiseScale;
#endif

float4 Density;
float3 FogHeights;

//#if UNITY_VERSION >= 540
float4x4 hxVolumeMatrixOld[10];
float2 hxVolumeSettingsOld[10];
float4x4 hxVolumeMatrix[50];
float2 hxVolumeSettings[50];

void CalcualteDensityShape(inout float volumeDensity,float3 wpos, float4x4 toLocal,float2 settings)
{
	float3 lpos = mul(toLocal, float4(wpos,1)).xyz;

	if (lpos.x > -0.5 && lpos.x < 0.5 && lpos.y > -0.5 && lpos.y < 0.5 && lpos.z > -0.5 && lpos.z < 0.5)
	{	
		if (settings.y <= 3)
		{
			if (settings.y == 0)
			{
				volumeDensity = max(settings.x, volumeDensity);
			}
			else if (settings.y == 1)
			{
				volumeDensity += settings.x;
			}
			else if (settings.y == 2)
			{
				volumeDensity = min(settings.x, volumeDensity);
			}
			else if (settings.y == 3)
			{
				volumeDensity -= settings.x;
			}
		}
		else if (settings.y <= 7 && length(lpos) < 0.5f)
		{
			if (settings.y == 4)
			{
				volumeDensity = max(settings.x, volumeDensity);
			}
			else if (settings.y == 5)
			{
				volumeDensity += settings.x;
			}
			else if (settings.y == 6)
			{
				volumeDensity = min(settings.x, volumeDensity);
			}
			else if (settings.y == 7)
			{
				volumeDensity -= settings.x;
			}
		}
		else if (settings.y <= 11 && length(lpos.xz) < 0.5f)
		{


			if (settings.y == 8)
			{
				volumeDensity = max(settings.x, volumeDensity);
			}
			else if (settings.y == 9)
			{
				volumeDensity += settings.x;
			}
			else if (settings.y == 10)
			{
				volumeDensity = min(settings.x, volumeDensity);
			}
			else if (settings.y == 11)
			{
				volumeDensity -= settings.x;
			}
		}
	}	
}

float GetVolumeDensity(float3 wpos, float volumeDensity)
{
#if SHADER_TARGET <= 30
	int c = 0;
	while (c < 10)
	{
		if (hxVolumeSettingsOld[c].y == -1) { return volumeDensity; }
		CalcualteDensityShape(volumeDensity, wpos, hxVolumeMatrixOld[c], hxVolumeSettingsOld[c]);
		c++;
	}
#else
	int c = 0;
	while (c < 50)
	{
		if (hxVolumeSettings[c].y == -1) { return volumeDensity; }
		CalcualteDensityShape(volumeDensity, wpos, hxVolumeMatrix[c], hxVolumeSettings[c]);
		c++;
	}
#endif

	return volumeDensity;
}





float Noise(float3 x)
{
#ifdef NOISE_ON
	x += NoiseOffset;

	return (((tex3Dlod(NoiseTexture3D, float4(x.x * NoiseScale.x, x.y * NoiseScale.y, x.z * NoiseScale.z, 1)).a - 0.5f) * max(hxNoiseContrast,0)) + 0.5f) * 2;
#else
	return 1;
#endif
}

float GetFogDensity(float3 worldPosition)
{
#ifdef HEIGHTFOG_ON
	float fogDensity = lerp(1 * FogHeights.z, 1, (smoothstep(FogHeights.y, FogHeights.x, worldPosition.y)));

	fogDensity = Noise(worldPosition) * fogDensity;

	return  GetVolumeDensity(worldPosition,fogDensity * Density.x);
#else
	return GetVolumeDensity(worldPosition, Noise(worldPosition) * Density.x);
#endif
}


#ifdef VTRANSPARENCY_ON

float4 TransparencySliceSettings;

float  GetLookupDepthTransparency(float  inViewSpaceDepth)
{
	return  pow((inViewSpaceDepth / TransparencySliceSettings.x), TransparencySliceSettings.y);
}

float  GetTransparencySliceIntensity(float  inSampleDepth, float  inIntensity, float  inSliceIndex)
{
	int  num_depth_slices = TransparencySliceSettings.z; 
	float sample_slice_pos = GetLookupDepthTransparency(inSampleDepth) * (num_depth_slices - 1);
	float sample_distance = sample_slice_pos - inSliceIndex;
	float sample_influence = 1.0 - saturate(sample_distance);
	return saturate(inIntensity * sample_influence);
}

void AddTransparency(inout vr vrout ,float lum, float rayWorldDis)
{
	vrout.col0 += float4(0, 0, 0, 0);
	vrout.col1 += float4(GetTransparencySliceIntensity(rayWorldDis, lum, 0), GetTransparencySliceIntensity(rayWorldDis, lum, 1), GetTransparencySliceIntensity(rayWorldDis, lum, 2),GetTransparencySliceIntensity(rayWorldDis, lum, 3)).rgba;
#ifndef S_4
	vrout.col2 += float4(GetTransparencySliceIntensity(rayWorldDis, lum, 4), GetTransparencySliceIntensity(rayWorldDis, lum, 5), GetTransparencySliceIntensity(rayWorldDis, lum, 6),GetTransparencySliceIntensity(rayWorldDis, lum, 7)).rgba;

#ifndef S_8

		vrout.col3 += float4(GetTransparencySliceIntensity(rayWorldDis, lum, 8), GetTransparencySliceIntensity(rayWorldDis, lum, 9), GetTransparencySliceIntensity(rayWorldDis, lum, 10), GetTransparencySliceIntensity(rayWorldDis, lum, 11)).rgba;

#ifdef S_16

			vrout.col4 += float4(GetTransparencySliceIntensity(rayWorldDis, lum, 12), GetTransparencySliceIntensity(rayWorldDis, lum, 13), GetTransparencySliceIntensity(rayWorldDis, lum, 14), GetTransparencySliceIntensity(rayWorldDis, lum, 15)).rgba;
		

			//if ((TransparencySliceSettings.z) > 16)
			//{
			//	
			//	vrout.col5 += float4(GetTransparencySliceIntensity(rayWorldDis, lum, 16), GetTransparencySliceIntensity(rayWorldDis, lum, 17), GetTransparencySliceIntensity(rayWorldDis, lum, 18), GetTransparencySliceIntensity//(rayWorldDis, lum, 19)).rgba;
			//
			//	if ((TransparencySliceSettings.z) > 20)
			//	{
			//	
			//		vrout.col6 += float4(GetTransparencySliceIntensity(rayWorldDis, lum, 20), GetTransparencySliceIntensity(rayWorldDis, lum, 21), GetTransparencySliceIntensity(rayWorldDis, lum, 22), //GetTransparencySliceIntensity(rayWorldDis, lum, 23)).rgba;
			//
			//		if ((TransparencySliceSettings.z) > 24)
			//		{
			//
			//			vrout.col7 += float4(GetTransparencySliceIntensity(rayWorldDis, lum, 24), GetTransparencySliceIntensity(rayWorldDis, lum, 25), GetTransparencySliceIntensity(rayWorldDis, lum, 26), //GetTransparencySliceIntensity(rayWorldDis, lum, 27)).rgba;
			//		}
			//	}
			//
			//}		
#endif
#endif
#endif
	return;
}

#endif

float4 SliceSettings;

sampler2D VolumetricDensityTexture0;
#ifndef DS_4
sampler2D VolumetricDensityTexture1;
#ifndef DS_8
sampler2D VolumetricDensityTexture2;
#ifdef DS_16
sampler2D VolumetricDensityTexture3;
#endif
#endif
#endif
//#if !defined(SHADER_API_D3D9)
//
//sampler2D VolumetricDensityTexture4;
//sampler2D VolumetricDensityTexture5;
//sampler2D VolumetricDensityTexture6;
//sampler2D VolumetricDensityTexture7;
//
//#endif

struct DensityMap
{
#ifdef DS_4
	float SliceData[4];
#endif
#ifdef DS_8
	float SliceData[8];
#endif
#ifdef DS_12
	float SliceData[12];
#endif
#ifdef DS_16
	float SliceData[16];
#endif
};







DensityMap LoadSliceData(float2 uv,float maxDepth)
{
	DensityMap dmout;

	float slice = ceil(pow((maxDepth / SliceSettings.x), SliceSettings.y) * (SliceSettings.z - 1));


	float4 Slice0 = tex2Dlod(VolumetricDensityTexture0, float4(uv, 0, 0));
#ifndef DS_4
	float4 Slice1 = float4(0.5f, 0.5f, 0.5f, 0.5f);
	if (slice > 3) { Slice1 = tex2Dlod(VolumetricDensityTexture1, float4(uv, 0, 0)); }

#ifndef DS_8
	float4 Slice2 = float4(0.5f, 0.5f, 0.5f, 0.5f);
	if (slice > 7) 
	{
		Slice2 = tex2Dlod(VolumetricDensityTexture2, float4(uv, 0, 0));
	}
#ifdef DS_16
	float4 Slice3 = float4(0.5f, 0.5f, 0.5f, 0.5f);
	if (slice > 11)
	{
		Slice3 = tex2Dlod(VolumetricDensityTexture3, float4(uv, 0, 0));
	}
#endif
#endif
#endif
	dmout.SliceData[0] = (Slice0.x - 0.5) * 2;
	dmout.SliceData[1] = (Slice0.y - 0.5) * 2;
	dmout.SliceData[2] = (Slice0.z - 0.5) * 2;
	dmout.SliceData[3] = (Slice0.w - 0.5) * 2;
#ifndef DS_4
	dmout.SliceData[4] = (Slice1.x - 0.5) * 2;
	dmout.SliceData[5] = (Slice1.y - 0.5) * 2;
	dmout.SliceData[6] = (Slice1.z - 0.5) * 2;
	dmout.SliceData[7] = (Slice1.w - 0.5) * 2;
#ifndef DS_8
	dmout.SliceData[8] = (Slice2.x - 0.5) * 2;
	dmout.SliceData[9] = (Slice2.y - 0.5) * 2;
	dmout.SliceData[10] = (Slice2.z - 0.5) * 2;
	dmout.SliceData[11] = (Slice2.w - 0.5) * 2;
#ifdef DS_16
	dmout.SliceData[12] = (Slice3.x - 0.5) * 2;
	dmout.SliceData[13] = (Slice3.y - 0.5) * 2;
	dmout.SliceData[14] = (Slice3.z - 0.5) * 2;
	dmout.SliceData[15] = (Slice3.w - 0.5) * 2;
#endif
#endif
#endif
	return dmout;
}

float4 DensitySliceDistance0;
#ifndef DS_4
float4 DensitySliceDistance1;
#ifndef DS_8
float4 DensitySliceDistance2;
#ifdef DS_16
float4 DensitySliceDistance3;
#endif
#endif
#endif
//float4 DensitySliceDistance4;
//float4 DensitySliceDistance5;
//float4 DensitySliceDistance6;
//float4 DensitySliceDistance7;

float AddDensity(float p1, float p2, float dis, float cSlice, float wSlice)
{
	float Den = 0;

	if (wSlice > cSlice)
	{
		float per = saturate((wSlice) - (cSlice ));

		Den += ((p1 + lerp(p1, p2, per)) / 2 * (dis * per));
	}
	return Den;
}

float DensityToPoint(DensityMap dm, float worldDistance)
{
	
	float slice = pow((worldDistance / SliceSettings.x), SliceSettings.y) * (SliceSettings.z - 1);

	
	float density = AddDensity(dm.SliceData[0], dm.SliceData[1], DensitySliceDistance0.x, 0, slice) +
		AddDensity(dm.SliceData[1], dm.SliceData[2], DensitySliceDistance0.x, 1, slice) +
		AddDensity(dm.SliceData[2], dm.SliceData[3], DensitySliceDistance0.y, 2, slice);
#ifndef DS_4
		density += AddDensity(dm.SliceData[3], dm.SliceData[4], DensitySliceDistance1.z, 3, slice) +
		AddDensity(dm.SliceData[4], dm.SliceData[5], DensitySliceDistance1.w, 4, slice) +
		AddDensity(dm.SliceData[5], dm.SliceData[6], DensitySliceDistance1.x, 5, slice) +
		AddDensity(dm.SliceData[6], dm.SliceData[7], DensitySliceDistance1.y, 6, slice);


#ifndef DS_8
	
		density += AddDensity(dm.SliceData[7], dm.SliceData[8], DensitySliceDistance2.z, 7, slice) +
			AddDensity(dm.SliceData[8], dm.SliceData[9], DensitySliceDistance2.w, 8, slice) +
			AddDensity(dm.SliceData[9], dm.SliceData[10], DensitySliceDistance2.x, 9, slice) +
			AddDensity(dm.SliceData[10], dm.SliceData[11], DensitySliceDistance2.y, 10, slice);


#ifdef DS_16
			density += AddDensity(dm.SliceData[11], dm.SliceData[12], DensitySliceDistance3.z, 11, slice) +
				AddDensity(dm.SliceData[12], dm.SliceData[13], DensitySliceDistance3.w, 12, slice) +
				AddDensity(dm.SliceData[13], dm.SliceData[14], DensitySliceDistance3.x, 13, slice) +
				AddDensity(dm.SliceData[14], dm.SliceData[15], DensitySliceDistance3.y, 14, slice);


#endif	
#endif
#endif
	return density;
}

float DensityFrom3DTexture(DensityMap dm, float worldDistance,inout float LowValue, inout float HighValue, inout float CurrentLow)
{
	float slice = pow((worldDistance / SliceSettings.x), SliceSettings.y) * (SliceSettings.z - 1);
	float lowSlice = floor(slice);
	if (lowSlice != CurrentLow)
	{
		LowValue = dm.SliceData[lowSlice];
		HighValue = dm.SliceData[lowSlice + 1];
	}

	return lerp(LowValue, HighValue, slice - lowSlice);
}


