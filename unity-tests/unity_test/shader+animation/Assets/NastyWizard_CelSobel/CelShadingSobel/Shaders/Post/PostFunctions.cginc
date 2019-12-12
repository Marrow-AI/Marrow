#ifndef POSTFUNCS_INCLUDE
#define POSTFUNCS_INCLUDE

//////////////////////////////////////////////////////////////
//														   	//
//		This cginc file was written by Casey MacNeil		//	   
//														   	//
//////////////////////////////////////////////////////////////

#define PI 3.14159265359

#include "UnityCG.cginc"
// 5x5 to 3x3
inline void Get3x3From5x5(fixed3 tex[25], int index, out fixed3 result[9])
{
	fixed3 a[9] = { tex[index - 6], tex[index - 5], tex[index - 4],
		tex[index - 1], tex[index], tex[index + 1],
		tex[index + 4], tex[index + 5], tex[index + 6] };
	result = a;
}

inline void GetOffsets3x3(float resolution, out float2 result[9])
{
	float offset = 1.0 / resolution;
	float2 offsets[9] = {
		float2(-offset,  offset), // top-left
		float2(0.0,    offset), // top-center
		float2(offset,  offset), // top-right
		float2(-offset,  0.0),   // center-left
		float2(0.0,    0.0),   // center-center
		float2(offset,  0.0),   // center-right
		float2(-offset, -offset), // bottom-left
		float2(0.0,   -offset), // bottom-center
		float2(offset, -offset)  // bottom-right    
	};
	result = offsets;
}

inline void GetOffsets3x3(float resolutionX, float resolutionY, out float2 result[9])
{
	float offsetX = 1.0 / resolutionX;
	float offsetY = 1.0 / resolutionY;
	float2 offsets[9] = {
		float2(-offsetX,  offsetY), // top-left
		float2(0.0,    offsetY), // top-center
		float2( offsetX,  offsetY), // top-right
		float2(-offsetX,  0.0),   // center-left
		float2(0.0,    0.0),   // center-center
		float2( offsetX,  0.0),   // center-right
		float2(-offsetX, -offsetY), // bottom-left
		float2(0.0,   -offsetY), // bottom-center
		float2(offsetX, -offsetY)  // bottom-right    
	};
	result = offsets;
}


inline void GetOffsets5x5(float resolution, out float2 result[25])
{
	float offset = 1.0 / resolution;
	float2 offsets5x5[25] = {
		float2(-offset * 2,  offset * 2), // top-left
		float2(-offset,  offset * 2), // top-left
		float2(0.0,    offset * 2), // top-center
		float2(offset,  offset * 2), // top-right
		float2(offset * 2,  offset * 2), // top-right

		float2(-offset * 2,  offset), // top-left
		float2(-offset,  offset), // top-left
		float2(0.0,    offset), // top-center
		float2(offset,  offset), // top-right
		float2(offset * 2,  offset), // top-right

		float2(-offset * 2,  0.0),   // center-left
		float2(-offset,  0.0),   // center-left
		float2(0.0,    0.0),   // center-center
		float2(offset,  0.0),   // center-right
		float2(offset * 2,  0.0),   // center-right

		float2(-offset * 2, -offset), // bottom-left
		float2(-offset, -offset), // bottom-left
		float2(0.0,   -offset), // bottom-center
		float2(offset, -offset),  // bottom-right
		float2(offset * 2, -offset),  // bottom-right

		float2(-offset * 2, -offset * 2), // bottom-left
		float2(-offset, -offset * 2), // bottom-left
		float2(0.0,   -offset * 2), // bottom-center
		float2(offset, -offset * 2),  // bottom-right
		float2(offset * 2, -offset * 2)  // bottom-right
	};
	result = offsets5x5;
}

inline void GetOffsets5x5(float resolutionx, float resolutiony, out float2 result[25])
{
	float offsetx = 1.0 / resolutionx;
	float offsety = 1.0 / resolutiony;
	float2 offsets5x5[25] = {
		float2(-offsetx * 2,  offsety * 2), // top-left
		float2(-offsetx,  offsety * 2), // top-left
		float2(0.0,    offsety * 2), // top-center
		float2(offsetx,  offsety * 2), // top-right
		float2(offsetx * 2,  offsety * 2), // top-right

		float2(-offsetx * 2,  offsety), // top-left
		float2(-offsetx,  offsety), // top-left
		float2(0.0,    offsety), // top-center
		float2(offsetx,  offsety), // top-right
		float2(offsetx * 2,  offsety), // top-right

		float2(-offsetx * 2,  0.0),   // center-left
		float2(-offsetx,  0.0),   // center-left
		float2(0.0,    0.0),   // center-center
		float2(offsetx,  0.0),   // center-right
		float2(offsetx * 2,  0.0),   // center-right

		float2(-offsetx * 2, -offsety), // bottom-left
		float2(-offsetx, -offsety), // bottom-left
		float2(0.0,   -offsety), // bottom-center
		float2(offsetx, -offsety),  // bottom-right
		float2(offsetx * 2, -offsety),  // bottom-right

		float2(-offsetx * 2, -offsety * 2), // bottom-left
		float2(-offsetx, -offsety * 2), // bottom-left
		float2(0.0,   -offsety * 2), // bottom-center
		float2(offsetx, -offsety * 2),  // bottom-right
		float2(offsetx * 2, -offsety * 2)  // bottom-right
	};
	result = offsets5x5;
}


inline fixed3 ApplyKernel3x3(float kernel[9], fixed3 textures[9]) // function for applying basic kernels
{ 
	fixed3 col = fixed3(0.0, 0.0, 0.0);
	for (int j = 0; j < 9; j++)
	{
		col += textures[j] * (kernel[j]);
	}
	return col;
}

inline fixed3 ApplyKernel5x5(float kernel[25], fixed3 textures[25]) // function for applying basic kernels, 5x5
{
	fixed3 col = fixed3(0.0, 0.0, 0.0);
	for (int j = 0; j < 25; j++)
	{
		col += textures[j] * (kernel[j]);
	}
	return col;
}
// GAUSSIAN BLUR

inline fixed3 GaussianBlur5x5(fixed3 textures[25])
{
	float KERNEL_GBLUR5X5[25] = {
		2.0 / 159.0,	4.0 / 159.0,	5.0 / 159.0,	4.0 / 159.0,	2.0 / 159.0,
		4.0 / 159.0,	9.0 / 159.0,	12.0 / 159.0,	9.0 / 159.0,	4.0 / 159.0,
		5.0 / 159.0,	12.0 / 159.0,	15.0 / 159.0,	12.0 / 159.0,	5.0 / 159.0,
		4.0 / 159.0,	9.0 / 159.0,	12.0 / 159.0,	9.0 / 159.0,	4.0 / 159.0,
		2.0 / 159.0,	4.0 / 159.0,	5.0 / 159.0,	4.0 / 159.0,	2.0 / 159.0
	};

	return ApplyKernel5x5(KERNEL_GBLUR5X5, textures);

}

// GRAYSCALE
inline fixed3 Grayscale(fixed3 frag)
{
	float average = 0.2126 * frag.r + 0.7152 * frag.g + 0.0722 * frag.b;
	return fixed3(average, average, average);
}

// SOBEL CALCULATION 
inline fixed4 ApplySobel(fixed3 textures[9])
{
	float3x3 KERNEL_SOBELX = float3x3(
		-1.0, 0.0, 1.0,
		-2.0, 0.0, 2.0,
		-1.0, 0.0, 1.0
		);
	float3x3 KERNEL_SOBELY = float3x3(
		1.0, 2.0, 1.0,
		0.0, 0.0, 0.0,
		-1.0, -2.0, -1.0
		);

	float mag = 0.0;

	float mGx = 0.0;
	float mGy = 0.0;

	for (int i = 0, k = 0; i < 3; i++)
	{
		for (int j = 0; j < 3; j++, k++)
		{
			float Gx = KERNEL_SOBELX[i][j] * Grayscale(textures[k]);
			float Gy = KERNEL_SOBELY[i][j] * Grayscale(textures[k]);

			mGx += Gx;
			mGy += Gy;
		}
	}

	mag = sqrt(pow(mGx, 2) + pow(mGy, 2));
	float orientation = degrees(atan2(mGy, mGx)) / 360.0;
	fixed3 mag3 = fixed3(mag, mag, mag);
	return fixed4(mag3, orientation);

}

#endif