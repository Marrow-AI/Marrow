#include "UnityCG.cginc"
#include "Assets/Plugins/HxVolumetricLighting/Resources/Shaders/HxQualitySettings.cginc"
sampler2D VolumetricTexture;


#pragma target 3.0
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


#pragma multi_compile __ VTRANSPARENCY_ON
float4 Density;
#ifdef VTRANSPARENCY_ON
float2 MaxRayDistance;
float4 TransparencySliceSettings;
sampler2D VolumetricTransparencyTexture0;
#ifndef S_4
sampler2D VolumetricTransparencyTexture1;
#ifndef S_8
sampler2D VolumetricTransparencyTexture2;
#ifndef S_12
sampler2D VolumetricTransparencyTexture3;
#endif
#endif
#endif

//sampler2D VolumetricTransparencyTexture4;
//sampler2D VolumetricTransparencyTexture5;
//sampler2D VolumetricTransparencyTexture6;
//#endif




#define TRANSFER_VOLUMETRIC(input, output) output.projPos = ComputeScreenPos(input.vertex); COMPUTE_EYEDEPTH(output.projPos.z); output.projPos;

#ifdef DENSITYPARTICLES_ON
float4 DensitySliceDistance0;
float4 DensitySliceDistance1;
sampler2D VolumetricDensityTexture0;
#ifndef DS_4
sampler2D VolumetricDensityTexture1;
#ifndef DS_8
float4 DensitySliceDistance2;
sampler2D VolumetricDensityTexture2;
#ifdef DS_16
float4 DensitySliceDistance3;
sampler2D VolumetricDensityTexture3;
#endif
#endif
#endif
float4 SliceSettings;


float AddDensity(float p1, float p2, float dis, float cSlice, float wSlice)
{
	float Den = 0;
	if (wSlice > cSlice)
	{
		float per = saturate(wSlice - cSlice);

		Den += ((p1 + lerp(p1, p2, per)) / 2 * (dis * per));
	}
	return Den;
}

float DensityToPoint(float2 uv, float worldDistance)
{
	float slice = pow((worldDistance / SliceSettings.x), SliceSettings.y) * (SliceSettings.z - 1);
	float sliceHigh = ceil(slice);
	float4 Slice0 = tex2Dlod(VolumetricDensityTexture0, float4(uv, 0, 0));

	float density = AddDensity(Slice0.r, Slice0.g, DensitySliceDistance0.x, 0, slice) +
		AddDensity(Slice0.g, Slice0.b, DensitySliceDistance0.x, 1, slice) +
		AddDensity(Slice0.b, Slice0.a, DensitySliceDistance0.y, 2, slice);
#ifndef DS_4
	if (sliceHigh > 3)
	{
		float4 Slice1 = tex2Dlod(VolumetricDensityTexture1, float4(uv, 0, 0));
		density += AddDensity(Slice0.a, Slice1.r, DensitySliceDistance1.z, 3, slice) +
			AddDensity(Slice1.r, Slice1.g, DensitySliceDistance1.w, 4, slice) +
			AddDensity(Slice1.g, Slice1.b, DensitySliceDistance1.x, 5, slice) +
			AddDensity(Slice1.b, Slice1.a, DensitySliceDistance1.y, 6, slice);

#ifndef DS_8
		if (sliceHigh > 7)
		{
			float4 Slice2 = tex2Dlod(VolumetricDensityTexture2, float4(uv, 0, 0));
			density += AddDensity(Slice1.a, Slice2.r, DensitySliceDistance2.z, 7, slice) +
				AddDensity(Slice2.r, Slice2.g, DensitySliceDistance2.w, 8, slice) +
				AddDensity(Slice2.g, Slice2.b, DensitySliceDistance2.x, 9, slice) +
				AddDensity(Slice2.b, Slice2.a, DensitySliceDistance2.y, 10, slice);

#ifdef DS_16
			if (sliceHigh > 11)
			{
				float4 Slice3 = tex2Dlod(VolumetricDensityTexture3, float4(uv, 0, 0));
				density += AddDensity(Slice2.a, Slice3.r, DensitySliceDistance3.z, 11, slice) +
					AddDensity(Slice3.r, Slice3.g, DensitySliceDistance3.w, 12, slice) +
					AddDensity(Slice3.g, Slice3.b, DensitySliceDistance3.x, 13, slice) +
					AddDensity(Slice3.b, Slice3.a, DensitySliceDistance3.y, 14, slice);



			}
#endif
		}
#endif
	}
#endif
	return density;
}


#endif
//float4 DensitySliceDistance4;
//float4 DensitySliceDistance5;
//float4 DensitySliceDistance6;
//float4 DensitySliceDistance7;





//#if !defined(SHADER_API_D3D9)
//
//sampler2D VolumetricDensityTexture4;
////sampler2D VolumetricDensityTexture5;
////sampler2D VolumetricDensityTexture6;
////sampler2D VolumetricDensityTexture7;
//#endif
inline bool HxIsGammaSpace() //soo unity doesnt have this function in older versions soooo.
{
#if defined(UNITY_NO_LINEAR_COLORSPACE)
	return true;
#else
	// unity_ColorSpaceLuminance.w == 1 when in Linear space, otherwise == 0
	return unity_ColorSpaceLuminance.w == 0;
#endif
}

float ExtinctionEffect;

half4 VolumetricTransparencyBase(float4 finalColor, float4 data)
{
	//	data.z -= _ProjectionParams.y;
	float2 uv = float2(data.xy / data.w);

	float slice = pow((data.z / TransparencySliceSettings.x), TransparencySliceSettings.y) * (TransparencySliceSettings.z - 1);

	float lowSlice = min(max(floor(slice), 0), TransparencySliceSettings.z - 2);
	float highSlice = lowSlice + 1;

	float4  VLBufferColor = tex2Dlod(VolumetricTexture, float4(uv, 0, 0));





	float VLBufferIntensity = Luminance(VLBufferColor.rgb);

	float lowLum = 0;
	float highLum = 0;

	if (lowSlice < 4 || highSlice < 4)
	{
		float4 results0 = tex2Dlod(VolumetricTransparencyTexture0, float4(uv, 0, 0));
		lowLum += results0.r * (lowSlice == 0) + results0.g * (lowSlice == 1) + results0.b * (lowSlice == 2) + results0.a * (lowSlice == 3);
		highLum += results0.r * (highSlice == 0) + results0.g * (highSlice == 1) + results0.b * (highSlice == 2) + results0.a * (highSlice == 3);
	}
#ifndef S_4
	if ((lowSlice >= 4 && lowSlice < 8) || (highSlice >= 4 && highSlice < 8))
	{
		float4 results1 = tex2Dlod(VolumetricTransparencyTexture1, float4(uv, 0, 0));
		lowLum += results1.r * (lowSlice == 4) + results1.g * (lowSlice == 5) + results1.b * (lowSlice == 6) + results1.a * (lowSlice == 7);
		highLum += results1.r * (highSlice == 4) + results1.g * (highSlice == 5) + results1.b * (highSlice == 6) + results1.a * (highSlice == 7);
	}

#ifndef S_8

	if ((lowSlice >= 8 && lowSlice < 12) || (highSlice >= 8 && highSlice < 12))
	{
		float4 results2 = tex2Dlod(VolumetricTransparencyTexture2, float4(uv, 0, 0));
		lowLum += results2.r * (lowSlice == 8) + results2.g * (lowSlice == 9) + results2.b * (lowSlice == 10) + results2.a * (lowSlice == 11);
		highLum += results2.r * (highSlice == 8) + results2.g * (highSlice == 9) + results2.b * (highSlice == 10) + results2.a * (highSlice == 11);
	}

#ifdef S_16

	if ((lowSlice >= 12 && lowSlice < 16) || (highSlice >= 12 && highSlice < 16))
	{
		float4 results3 = tex2Dlod(VolumetricTransparencyTexture3, float4(uv, 0, 0));
		lowLum += results3.r * (lowSlice == 12) + results3.g * (lowSlice == 13) + results3.b * (lowSlice == 14) + results3.a * (lowSlice == 15);
		highLum += results3.r * (highSlice == 12) + results3.g * (highSlice == 13) + results3.b * (highSlice == 14) + results3.a * (highSlice == 15);
	}
#endif
#endif
#endif

	float TotalDensity = min(data.z, MaxRayDistance) * Density.x * Density.w;

#ifdef DENSITYPARTICLES_ON
	TotalDensity += DensityToPoint(uv, data.z) * Density.w;
#endif




	float3 ColorInfront = VLBufferColor * lerp(smoothstep(0, VLBufferIntensity, saturate(lerp(lowLum, highLum, (slice - lowSlice)))), 1, saturate(slice - (TransparencySliceSettings.z - 1)));


	//return half4(ColorInfront.rgb, 1);
	//if (HxIsGammaSpace())
	//{
	//	return half4(pow((pow(finalColor.rgb, 2.2) * lerp(saturate(exp(-TotalDensity)), 1, 1 - ExtinctionEffect)) - ((ColorInfront)* (1 - finalColor.a)) + ColorInfront, 0.4545f), finalColor.a);
	//}

	return half4(((finalColor.rgb * lerp(saturate(exp(-TotalDensity)), 1, 1 - ExtinctionEffect)) - (( ColorInfront)* (1 - finalColor.a)) + ColorInfront), finalColor.a);
}

half4 VolumetricTransparency(float4 finalColor, float4 data)
{
	
	//	data.z -= _ProjectionParams.y;
	float2 uv = float2(data.xy / data.w);

	float slice = pow((data.z / TransparencySliceSettings.x), TransparencySliceSettings.y) * (TransparencySliceSettings.z - 1);

	float lowSlice = min(max(floor(slice), 0), TransparencySliceSettings.z - 2);
	float highSlice = lowSlice + 1;

	float4  VLBufferColor = tex2Dlod(VolumetricTexture, float4(uv, 0, 0));


	float VLBufferIntensity = Luminance(VLBufferColor.rgb);

	
	float lowLum = 0;
	float highLum = 0;

	if (lowSlice < 4 || highSlice < 4)
	{
		float4 results0 = tex2Dlod(VolumetricTransparencyTexture0, float4(uv, 0, 0));
		lowLum += results0.r * (lowSlice == 0) + results0.g * (lowSlice == 1) + results0.b * (lowSlice == 2) + results0.a * (lowSlice == 3);
		highLum += results0.r * (highSlice == 0) + results0.g * (highSlice == 1) + results0.b * (highSlice == 2) + results0.a * (highSlice == 3);
	}
#ifndef S_4
	if ((lowSlice >= 4 && lowSlice < 8) || (highSlice >= 4 && highSlice < 8))
	{
		float4 results1 = tex2Dlod(VolumetricTransparencyTexture1, float4(uv, 0, 0));
		lowLum += results1.r * (lowSlice == 4) + results1.g * (lowSlice == 5) + results1.b * (lowSlice == 6) + results1.a * (lowSlice == 7);
		highLum += results1.r * (highSlice == 4) + results1.g * (highSlice == 5) + results1.b * (highSlice == 6) + results1.a * (highSlice == 7);
	}

#ifndef S_8

	if ((lowSlice >= 8 && lowSlice < 12) || (highSlice >= 8 && highSlice < 12))
	{
		float4 results2 = tex2Dlod(VolumetricTransparencyTexture2, float4(uv, 0, 0));
		lowLum += results2.r * (lowSlice == 8) + results2.g * (lowSlice == 9) + results2.b * (lowSlice == 10) + results2.a * (lowSlice == 11);
		highLum += results2.r * (highSlice == 8) + results2.g * (highSlice == 9) + results2.b * (highSlice == 10) + results2.a * (highSlice == 11);
	}

#ifdef S_16

	if ((lowSlice >= 12 && lowSlice < 16) || (highSlice >= 12 && highSlice < 16))
	{
		float4 results3 = tex2Dlod(VolumetricTransparencyTexture3, float4(uv, 0, 0));
		lowLum += results3.r * (lowSlice == 12) + results3.g * (lowSlice == 13) + results3.b * (lowSlice == 14) + results3.a * (lowSlice == 15);
		highLum += results3.r * (highSlice == 12) + results3.g * (highSlice == 13) + results3.b * (highSlice == 14) + results3.a * (highSlice == 15);
	}
#endif

#endif

#endif

	float TotalDensity = min(data.z, MaxRayDistance) * Density.x * Density.w;

#ifdef DENSITYPARTICLES_ON
	TotalDensity += DensityToPoint(uv, data.z) * Density.w;
#endif



	//VLBufferColor = pow(VLBufferColor, 2.2f);
	float3 ColorInfront = VLBufferColor * lerp(smoothstep(0, VLBufferIntensity, lerp(lowLum, highLum, (slice - lowSlice))), 1, saturate(slice - (TransparencySliceSettings.z - 1)));

	//if (HxIsGammaSpace())
	//{		
	//	return half4(pow((pow(finalColor.rgb,2.2) * lerp(saturate(exp(-TotalDensity)), 1, 1 - ExtinctionEffect)) - ((VLBufferColor - ColorInfront)* (1 - finalColor.a)) + ColorInfront,0.4545f), finalColor.a);
	//}
	
	
		return half4(((finalColor.rgb * lerp(saturate(exp(-TotalDensity)), 1, 1 - ExtinctionEffect)) - ((VLBufferColor - ColorInfront)* (1 - finalColor.a)) + ColorInfront), finalColor.a);
		
}

half4 VolumetricTransparencyAdd(float4 finalColor, float4 data)
{
	if (ExtinctionEffect == 0)
	{
		return finalColor;
	}
	float TotalDensity = min(data.z, MaxRayDistance) * Density.x * Density.w;
	float2 uv = float2(data.xy / data.w);
#ifdef DENSITYPARTICLES_ON
	TotalDensity += DensityToPoint(uv, data.z) * Density.w;
#endif

	return half4((finalColor.rgb *  lerp(saturate(exp(-TotalDensity)), 1, 1 - ExtinctionEffect)), finalColor.a);
}

half4 VolumetricTransparencyBlend(float4 finalColor, float4 data)
{
	if (ExtinctionEffect == 0)
	{
		return finalColor;
	}
	float TotalDensity = min(data.z, MaxRayDistance) * Density.x * Density.w;
	float2 uv = float2(data.xy / data.w);
#ifdef DENSITYPARTICLES_ON
	TotalDensity += DensityToPoint(uv, data.z) * Density.w;
#endif

	return half4((finalColor.rgb), finalColor.a  *  lerp(saturate(exp(-TotalDensity)), 1, 1 - ExtinctionEffect));
}
#endif
