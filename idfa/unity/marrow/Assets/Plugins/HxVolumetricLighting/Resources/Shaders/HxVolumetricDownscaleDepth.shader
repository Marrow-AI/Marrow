Shader "Hidden/HxVolumetricDownscaleDepth" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
	}
	CGINCLUDE

#include "UnityCG.cginc"

	struct v2f
	{
		float4 pos : SV_POSITION;
		float4 uv : TEXCOORD0;
	};



	sampler2D _CameraDepthTexture;
	float4 _CameraDepthTexture_TexelSize; // (1.0/width, 1.0/height, width, height)
	sampler2D _MainTex;
	float4 _MainTex_TexelSize;
	float4x4 InverseProjectionMatrix;
	v2f vert(appdata_img v)
	{
		v2f o = (v2f)0;
		//o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		//o.uv =  ComputeScreenPos(o.pos);
		o.pos = v.vertex;
		o.uv = ComputeScreenPos(o.pos);
		return o;
	}
	float4x4 VolumetricMVP;

	struct appdata {
		float4 vertex : POSITION;
	};

	struct v2fblit
	{
		float4 pos : SV_POSITION;
		float4 uv : TEXCOORD0;
	};

	v2fblit vertBlit(appdata v)
	{
		v2fblit o;
		o.pos = v.vertex;
		o.uv = ComputeScreenPos(o.pos);

		return o;
	}


	void fragFull(v2fblit input, out fixed4 outColor : SV_Target, out float outDepth : SV_Depth)
	{
		float2 uv = input.uv.xy / input.uv.w;
		outDepth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, uv));
		outColor = float4(0,0, 0, 0);//have to write to color.
	}

	void TestDepthMin(float2 uv,inout float depth, inout float2 depthuv)
	{
		float nd = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv.xy, 0, 0));
		if(nd < depth)
		{
			depth = nd;
			depthuv = uv;
		}
	}

	void TestDepthMax(float2 uv,inout float depth, inout float2 depthuv)
	{
		float nd = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv.xy, 0, 0));
		if(nd > depth)
		{
			depth = nd;
			depthuv = uv;
		}
	}

	void MaxAreaNew(float2 center, float2 halftexelSize,inout float depth, inout float2 depthuv)
	{		
		TestDepthMax((center + float2(-1, -1)*halftexelSize).xy,depth,depthuv);
		TestDepthMax((center + float2(1, -1)*halftexelSize).xy,depth,depthuv);
		TestDepthMax((center + float2(1, 1)*halftexelSize).xy,depth,depthuv);
		TestDepthMax((center + float2(-1, 1)*halftexelSize).xy,depth,depthuv);
	}



	void MinAreaNew(float2 center, float2 halftexelSize,inout float depth, inout float2 depthuv)
	{		
		TestDepthMin((center + float2(-1, -1)*halftexelSize).xy,depth,depthuv);
		TestDepthMin((center + float2(1, -1)*halftexelSize).xy,depth,depthuv);
		TestDepthMin((center + float2(1, 1)*halftexelSize).xy,depth,depthuv);
		TestDepthMin((center + float2(-1, 1)*halftexelSize).xy,depth,depthuv);
	}

	float MinArea(float2 center, float2 halftexelSize)
	{

		float depth1 = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4((center + float2(-1, -1)*halftexelSize).xy, 0, 0));
		float depth2 = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4((center + float2(1, -1)*halftexelSize).xy, 0, 0));
		float depth3 = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4((center + float2(1, 1)*halftexelSize).xy, 0, 0));
		float depth4 = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4((center + float2(-1, 1)*halftexelSize).xy, 0, 0));
		return min(depth1, min(depth2, min(depth3, depth4)));
	}

	float MaxArea(float2 center, float2 halftexelSize)
	{

		float depth1 = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4((center + float2(-1, -1)*halftexelSize).xy, 0, 0));
		float depth2 = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4((center + float2(1, -1)*halftexelSize).xy, 0, 0));
		float depth3 = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4((center + float2(1, 1)*halftexelSize).xy, 0, 0));
		float depth4 = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4((center + float2(-1, 1)*halftexelSize).xy, 0, 0));
		return max(depth1, max(depth2, max(depth3, depth4)));
	}

	float MinAreaStep(float2 center, float2 halftexelSize)
	{
		float depth1 = tex2Dlod(_MainTex, float4((center + float2(-1, -1)*halftexelSize).xy, 0, 0));
		float depth2 = tex2Dlod(_MainTex, float4((center + float2(1, -1)*halftexelSize).xy, 0, 0));
		float depth3 = tex2Dlod(_MainTex, float4((center + float2(1, 1)*halftexelSize).xy, 0, 0));
		float depth4 = tex2Dlod(_MainTex, float4((center + float2(-1, 1)*halftexelSize).xy, 0, 0));
		return min(depth1, min(depth2, min(depth3, depth4)));
	}

	float MaxAreaStep(float2 center, float2 halftexelSize)
	{
		float depth1 = tex2Dlod(_MainTex, float4((center + float2(-1, -1)*halftexelSize).xy, 0, 0));
		float depth2 = tex2Dlod(_MainTex, float4((center + float2(1, -1)*halftexelSize).xy, 0, 0));
		float depth3 = tex2Dlod(_MainTex, float4((center + float2(1, 1)*halftexelSize).xy, 0, 0));
		float depth4 = tex2Dlod(_MainTex, float4((center + float2(-1, 1)*halftexelSize).xy, 0, 0));
		return max(depth1, max(depth2, max(depth3, depth4)));
	}

	void fragHalfCamera(v2fblit input, out fixed4 outColor : SV_Target, out float outDepth : SV_Depth)
	{
		float2 uv = input.uv.xy / input.uv.w;
		float2 texelSize =  _CameraDepthTexture_TexelSize.xy * 0.5;
		//input.uv += texelSize;
		float2 outUV = uv;
		float x = abs(floor(uv.x / _CameraDepthTexture_TexelSize.x * 0.5));
		float y = abs(floor(uv.y / _CameraDepthTexture_TexelSize.y * 0.5));
	
		float finalDepth = 0;
		if ((x + y) % 2)
		{		
			finalDepth = 0;
			MaxAreaNew(uv, texelSize,finalDepth,outUV);
		}
		else
		{			
			finalDepth = 100000;
			MinAreaNew(uv, texelSize,finalDepth,outUV);			
		}

		outDepth = finalDepth;
		outColor = float4(EncodeFloatRG(Linear01Depth(max(0.0000001f,min(finalDepth, 0.9999999f)))), float2(1 - outUV.x, 1 - outUV.y));//have to write to color.		
	}



	void fragQuaterCamera(v2fblit input, out fixed4 outColor : SV_Target, out float outDepth : SV_Depth)
	{
		float2 uv = input.uv.xy / input.uv.w;
		float2 texelSize =  _CameraDepthTexture_TexelSize.xy * 0.5f;

		float x = abs(floor(uv.x / _CameraDepthTexture_TexelSize.x * 0.25));
		float y = abs(floor(uv.y / _CameraDepthTexture_TexelSize.y * 0.25));
		float2 outUV = float2(0, 0);// input.uv;
		float finalDepth = 0;
		if ((x + y) % 2)
		{
			finalDepth = 0;
			MaxAreaNew(uv + _CameraDepthTexture_TexelSize.xy, texelSize,finalDepth,outUV);
			MaxAreaNew(uv - _CameraDepthTexture_TexelSize.xy, texelSize,finalDepth,outUV);
			MaxAreaNew(uv + float2(-_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y), texelSize,finalDepth,outUV);
			MaxAreaNew(uv + float2(_CameraDepthTexture_TexelSize.x, -_CameraDepthTexture_TexelSize.y), texelSize,finalDepth,outUV);			
		}
		else
		{
			finalDepth = 100000;
			MinAreaNew(uv + _CameraDepthTexture_TexelSize.xy, texelSize,finalDepth,outUV);
			MinAreaNew(uv - _CameraDepthTexture_TexelSize.xy, texelSize,finalDepth,outUV);
			MinAreaNew(uv + float2(-_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y), texelSize,finalDepth,outUV);
			MinAreaNew(uv + float2(_CameraDepthTexture_TexelSize.x, -_CameraDepthTexture_TexelSize.y), texelSize,finalDepth,outUV);		
		}
	
		outDepth = finalDepth;	
		outColor = float4(EncodeFloatRG(Linear01Depth(max(0.0000001f, min(finalDepth, 0.9999999f)))), float2(1 - outUV.x, 1 - outUV.y));//have to write to color.			
		
	}

	void fragEighthCamera(v2f input, out fixed4 outColor : SV_Target, out float outDepth : SV_Depth)
	{
		float2 uv = input.uv.xy / input.uv.w;
		float2 texelSize = _CameraDepthTexture_TexelSize.xy * 0.5f;
			//input.uv += texelSize;

		float x = abs(floor(uv.x / _CameraDepthTexture_TexelSize.x * 0.125));
		float y = abs(floor(uv.y / _CameraDepthTexture_TexelSize.y * 0.125));
		float finalDepth = 0;
		float2 outUV = input.uv;
		if ((x + y) % 2)
		{
		finalDepth = 0;
			MaxAreaNew(uv + _CameraDepthTexture_TexelSize.xy, texelSize,finalDepth,outUV);
			MaxAreaNew(uv - _CameraDepthTexture_TexelSize.xy, texelSize,finalDepth,outUV);
			MaxAreaNew(uv + float2(-_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y), texelSize,finalDepth,outUV);
			MaxAreaNew(uv + float2(_CameraDepthTexture_TexelSize.x, -_CameraDepthTexture_TexelSize.y), texelSize,finalDepth,outUV);			


			MaxAreaNew(uv + float2(_CameraDepthTexture_TexelSize.x * -3, _CameraDepthTexture_TexelSize.y * -3), texelSize,finalDepth,outUV);
			MaxAreaNew(uv + float2(_CameraDepthTexture_TexelSize.x * -1, _CameraDepthTexture_TexelSize.y * -3), texelSize,finalDepth,outUV);
			MaxAreaNew(uv + float2(_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y * -3), texelSize,finalDepth,outUV);
			MaxAreaNew(uv + float2(_CameraDepthTexture_TexelSize.x * 3, _CameraDepthTexture_TexelSize.y * -3), texelSize,finalDepth,outUV);			


			MaxAreaNew(uv + float2(_CameraDepthTexture_TexelSize.x * -3, _CameraDepthTexture_TexelSize.y * -1), texelSize,finalDepth,outUV);
			MaxAreaNew(uv + float2(_CameraDepthTexture_TexelSize.x * -3, _CameraDepthTexture_TexelSize.y), texelSize,finalDepth,outUV);
			MaxAreaNew(uv + float2(_CameraDepthTexture_TexelSize.x * -3, _CameraDepthTexture_TexelSize.y * 3), texelSize,finalDepth,outUV);
			MaxAreaNew(uv + float2(_CameraDepthTexture_TexelSize.x * -1, _CameraDepthTexture_TexelSize.y * 3), texelSize,finalDepth,outUV);		

			MaxAreaNew(uv + float2(_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y * 3), texelSize,finalDepth,outUV);
			MaxAreaNew(uv + float2(_CameraDepthTexture_TexelSize.x * 3, _CameraDepthTexture_TexelSize.y * 3), texelSize,finalDepth,outUV);
			MaxAreaNew(uv + float2(_CameraDepthTexture_TexelSize.x * 3, _CameraDepthTexture_TexelSize.y), texelSize,finalDepth,outUV);
			MaxAreaNew(uv + float2(_CameraDepthTexture_TexelSize.x * 3, _CameraDepthTexture_TexelSize.y * -1), texelSize,finalDepth,outUV);		
		}
		else
		{
			finalDepth = 100000;
			MinAreaNew(input.uv + _CameraDepthTexture_TexelSize.xy, texelSize,finalDepth,outUV);
			MinAreaNew(input.uv - _CameraDepthTexture_TexelSize.xy, texelSize,finalDepth,outUV);
			MinAreaNew(input.uv + float2(-_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y), texelSize,finalDepth,outUV);
			MinAreaNew(input.uv + float2(_CameraDepthTexture_TexelSize.x, -_CameraDepthTexture_TexelSize.y), texelSize,finalDepth,outUV);			

			MinAreaNew(input.uv + float2(_CameraDepthTexture_TexelSize.x * -3, _CameraDepthTexture_TexelSize.y * -3), texelSize,finalDepth,outUV);
			MinAreaNew(input.uv + float2(_CameraDepthTexture_TexelSize.x * -1, _CameraDepthTexture_TexelSize.y * -3), texelSize,finalDepth,outUV);
			MinAreaNew(input.uv + float2(_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y * -3), texelSize,finalDepth,outUV);
			MinAreaNew(input.uv + float2(_CameraDepthTexture_TexelSize.x * 3, _CameraDepthTexture_TexelSize.y * -3), texelSize,finalDepth,outUV);			

			MinAreaNew(input.uv + float2(_CameraDepthTexture_TexelSize.x * -3, _CameraDepthTexture_TexelSize.y * -1), texelSize,finalDepth,outUV);
			MinAreaNew(input.uv + float2(_CameraDepthTexture_TexelSize.x * -3, _CameraDepthTexture_TexelSize.y), texelSize,finalDepth,outUV);
			MinAreaNew(input.uv + float2(_CameraDepthTexture_TexelSize.x * -3, _CameraDepthTexture_TexelSize.y * 3), texelSize,finalDepth,outUV);
			MinAreaNew(input.uv + float2(_CameraDepthTexture_TexelSize.x * -1, _CameraDepthTexture_TexelSize.y * 3), texelSize,finalDepth,outUV);		

			MinAreaNew(input.uv + float2(_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y * 3), texelSize,finalDepth,outUV);
			MinAreaNew(input.uv + float2(_CameraDepthTexture_TexelSize.x * 3, _CameraDepthTexture_TexelSize.y * 3), texelSize,finalDepth,outUV);
			MinAreaNew(input.uv + float2(_CameraDepthTexture_TexelSize.x * 3, _CameraDepthTexture_TexelSize.y), texelSize,finalDepth,outUV);
			MinAreaNew(input.uv + float2(_CameraDepthTexture_TexelSize.x * 3, _CameraDepthTexture_TexelSize.y * -1), texelSize,finalDepth,outUV);		
		}
		
	
		outDepth = finalDepth;

	
	//have to write to color.		
		outDepth = finalDepth;
		outColor = float4(EncodeFloatRG(Linear01Depth(max(0.0000001f, min(finalDepth, 0.9999999f)))), float2(1 - outUV.x, 1 - outUV.y));//have to write to color.	
	}

	void fragHalf(v2f input, out fixed4 outColor : SV_Target, out float outDepth : SV_Depth)
	{
		float2 texelSize = _MainTex_TexelSize.xy * 0.5f;
		//input.uv += texelSize;

		float x = abs(floor(input.uv.x / _MainTex_TexelSize.x * 0.5));
		float y = abs(floor(input.uv.y / _MainTex_TexelSize.y * 0.5));
		float finalDepth = 0;
		if ((x + y) % 2)
		{
			finalDepth = 0;
			outDepth = MaxAreaStep(input.uv, texelSize);
		}
		else
		{
			finalDepth = 100000;
			outDepth = MinAreaStep(input.uv, texelSize);
		}


		outColor = float4((outDepth), 0, 0, 0);//have to write to color.
	}

		ENDCG
		SubShader
	{
		Pass
		{
			ZTest Always Cull Off ZWrite On

			CGPROGRAM
#pragma vertex vertBlit
#pragma target 3.0
#pragma fragment fragFull
			ENDCG
		}

		Pass
		{
			ZTest Always Cull Off ZWrite On

			CGPROGRAM
#pragma vertex vertBlit
#pragma target 3.0
#pragma fragment fragHalfCamera
			ENDCG
		}

		Pass
			{
				ZTest Always Cull Off ZWrite On

				CGPROGRAM
#pragma vertex vertBlit
#pragma target 3.0
#pragma fragment fragQuaterCamera
				ENDCG
			}

			Pass
				{
					ZTest Always Cull Off ZWrite On

					CGPROGRAM
#pragma vertex vert
#pragma target 3.0
#pragma fragment fragEighthCamera
					ENDCG
				}

				Pass
					{
						ZTest Always Cull Off ZWrite On

						CGPROGRAM
#pragma vertex vert
#pragma target 3.0
#pragma fragment fragHalf
						ENDCG
					}
	}
	
	Fallback off
}
