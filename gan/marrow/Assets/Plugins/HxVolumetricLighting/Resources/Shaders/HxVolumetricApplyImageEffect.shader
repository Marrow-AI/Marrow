// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/HxVolumetricApply" {

	Properties
	{
		_MainTex("", any) = "" {}

	}

		CGINCLUDE

#include "UnityCG.cginc"
	struct appdata {
		float4 vertex : POSITION;
	};


	struct v2f
	{
		float4 pos : SV_POSITION;
		float4 uv : TEXCOORD0;
	};

	struct v2ff
	{
		float4 pos : SV_POSITION;
		float4 uv : TEXCOORD0;
		float4 uvFlipped : TEXCOORD2;
	};

	sampler2D _MainTex;
	sampler2D _CameraDepthTexture;
	sampler2D VolumetricDepth;
	float4 VolumetricDepth_TexelSize;
	sampler2D VolumetricTexture;
	float4 VolumetricTexture_TexelSize;
	float4 VolumetricTexture_ST;
	float ExtinctionEffect;
	//float4 VolumetricTexture_TexelSize;


	float4 _MainTex_TexelSize; // (1.0/width, 1.0/height, width, height)
	float4 _CameraDepthTexture_TexelSize;

	uniform float BlurDepthFalloff;
	float DepthThreshold;
	float4x4 VolumetricMVP;


	v2f vert(appdata v)
	{
		v2f o = (v2f)0;
		//o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		//o.uv = v.vertex.xy;//v.texcoord;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = ComputeScreenPos(o.pos);



		return o;
	}

	v2ff vertFull(appdata_img v)
	{
		v2ff o = (v2ff)0;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = ComputeScreenPos(o.pos);


		o.uvFlipped = o.uv;

#if UNITY_UV_STARTS_AT_TOP
		if (_MainTex_TexelSize.y < 0)
			o.uvFlipped.y = 1 - o.uvFlipped.y;
#endif

		return o;
	}

	void UpdateNearestSample(inout float MinDist,
		inout float2 NearestUV,
		float Z,
		float2 UV,
		float ZFull
		)
	{
		float Dist = abs((Z)-ZFull);
		if (Dist < MinDist)
		{
			MinDist = Dist;
			NearestUV = UV;
		}
	}

	void UpdateNearestSample2(inout float MinDist,
		inout float2 NearestUV, inout float MinDist2,
		inout float2 NearestUV2, inout float az, inout float az2,
		float Z,
		float2 UV,
		float ZFull
		)
	{
		float Dist = abs((Z)-(ZFull));

		if (Z <= ZFull && Dist < MinDist)
		{
			az = Z;
			MinDist = Dist;
			NearestUV = UV;
		}

		if (Z >= ZFull && Dist < MinDist2)
		{
			az2 = Z;
			MinDist2 = Dist;
			NearestUV2 = UV;
		}

	}

	float4 GetNearestDepthFullSample(float2 uv)
	{
		return tex2Dlod(_MainTex, float4(uv, 0, 0));
	}

	float nrand(float2 uv)
	{
		return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
	}

	float4 CustomLerp(float4 c1, float4 c2, float d1, float d2, float z, inout float w)
	{
		if (d1 < d2)
		{
			w = smoothstep(d1, d2, z);
			return lerp(c1, c2, w);
		}

		if (d1 > d2)
		{
			w = smoothstep(d2, d1, z);
			return lerp(c2, c1, w);
		}

		w = 0.5f;
		return (c1 + c2) / 2;
	}

	float4 GetNearestDepthSample(float2 uv, out float blur)
	{
		//read full resolution depth
		float Z = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
		float LZ = Linear01Depth(Z);

		const float2 lowResTexelSize = VolumetricDepth_TexelSize.xy;
		const float depthTreshold = DepthThreshold;
		const float2 lowResTexelSize2 = lowResTexelSize * 2;

		//return tex2Dlod(_MainTex, float4(uv, 0, 0));


		float2 lowResUV = uv;

		float MinDist = 100000;
		float MinDist2 = 100000;


		float2 UV00 = lowResUV - (lowResTexelSize * 0.5f);
		UV00 = float2((floor((UV00.x) / lowResTexelSize.x)) * lowResTexelSize.x + lowResTexelSize.x / 2, (floor((UV00.y) / lowResTexelSize.y)) * lowResTexelSize.y + lowResTexelSize.y / 2);

		float2 mid = lowResUV;
		mid = float2((floor((mid.x) / lowResTexelSize.x)) * lowResTexelSize.x + lowResTexelSize.x / 2, (floor((mid.y) / lowResTexelSize.y)) * lowResTexelSize.y + lowResTexelSize.y / 2);

		float2 offsetTexel = float2(lowResTexelSize.x * (-1 + (2 * (UV00.x != mid.x))), lowResTexelSize.y * (-1 + (2 * (UV00.y != mid.y))));

		float2 NearestUV = mid;
		float2 NearestUV2 = mid;


		float Z00 = (tex2Dlod(VolumetricDepth, float4(mid.xy, 0, 0)).r); float az = Z00; float az2 = Z00;
		UpdateNearestSample2(MinDist, NearestUV, MinDist2, NearestUV2, az, az2, Z00, mid, LZ);

		float2 UV10 = float2(mid.x - offsetTexel.x, mid.y);
		float Z10 = (tex2Dlod(VolumetricDepth, float4(UV10, 0, 0)).r);
		UpdateNearestSample2(MinDist, NearestUV, MinDist2, NearestUV2, az, az2, Z10, UV10, LZ);

		float2 UV01 = float2(mid.x, mid.y - offsetTexel.y);
		float Z01 = (tex2Dlod(VolumetricDepth, float4(UV01, 0, 0)).r);
		UpdateNearestSample2(MinDist, NearestUV, MinDist2, NearestUV2, az, az2, Z01, UV01, LZ);

		float2 UV11 = mid - offsetTexel;
		float Z11 = (tex2Dlod(VolumetricDepth, float4(UV11, 0, 0)).r);
		UpdateNearestSample2(MinDist, NearestUV, MinDist2, NearestUV2, az, az2, Z11, UV11, LZ);


		bool sky1 = (Z00 < 0.99995);
		bool sky2 = (Z10 < 0.99995);
		bool sky3 = (Z01 < 0.99995);
		bool sky4 = (Z11 < 0.99995);

		float dt = depthTreshold * az;



		if ((abs(Z00 - (LZ)) < depthTreshold * Z00 &&
			abs(Z10 - (LZ)) < depthTreshold * Z10 &&
			abs(Z01 - (LZ)) < depthTreshold * Z01 &&
			abs(Z11 - (LZ)) < depthTreshold * Z11) && (sky1 == sky2 && sky1 == sky3 && sky1 == sky4))
		{
			blur = 1000;
			return  tex2Dlod(_MainTex, float4(uv, 0, 0));
		}
		else
		{
			float2 UV20 = float2(mid.x + offsetTexel.x, mid.y + offsetTexel.y);
			float Z20 = (tex2Dlod(VolumetricDepth, float4(UV20, 0, 0)).r);
			UpdateNearestSample2(MinDist, NearestUV, MinDist2, NearestUV2, az, az2, Z20, UV20, LZ);

			float2 UV21 = float2(mid.x, mid.y + offsetTexel.y);
			float Z21 = (tex2Dlod(VolumetricDepth, float4(UV21, 0, 0)).r);
			UpdateNearestSample2(MinDist, NearestUV, MinDist2, NearestUV2, az, az2, Z21, UV21, LZ);

			float2 UVN1 = float2(mid.x + offsetTexel.x, mid.y);
			float ZN1 = (tex2Dlod(VolumetricDepth, float4(UVN1, 0, 0)).r);
			UpdateNearestSample2(MinDist, NearestUV, MinDist2, NearestUV2, az, az2, ZN1, UVN1, LZ);

			float2 UVN0 = float2(mid.x - offsetTexel.x, mid.y + offsetTexel.y);
			float ZN0 = (tex2Dlod(VolumetricDepth, float4(UVN0, 0, 0)).r);
			UpdateNearestSample2(MinDist, NearestUV, MinDist2, NearestUV2, az, az2, ZN0, UVN0, LZ);

			float2 UV02 = float2(mid.x + offsetTexel.x, mid.y - offsetTexel.y);
			float Z02 = (tex2Dlod(VolumetricDepth, float4(UV02, 0, 0)).r);
			UpdateNearestSample2(MinDist, NearestUV, MinDist2, NearestUV2, az, az2, Z02, UV02, LZ);



			//return (tex2Dlod(_MainTex, float4(uv, 0, 0)));
			//Linear01Depth 

			//float c1 = (MinDist > 99999);
			//float c2 = (MinDist <= 99999);
			//az = az2 * c1 + az * c2;
			//NearestUV = NearestUV2 * c1 + NearestUV * c2;
			//
			//c1 = (MinDist2 > 99999);
			//c2 = (MinDist2 <= 99999);
			//az2 = az * c1 + az2 * c2;
			//NearestUV2 = NearestUV * c1 + NearestUV2 * c2;


			blur = 0;

			return lerp(tex2Dlod(_MainTex, float4(NearestUV, 0, 0)), tex2Dlod(_MainTex, float4(NearestUV2, 0, 0)), (saturate(smoothstep((az), (az2), (LZ)))));

			//fogSample = tex2Dlod(_MainTex, float4(uv, 0, 0));
		}


	}

	inline half3 hxGammaToLinearSpace(half3 sRGB)
	{
		// Approximate version from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
		return sRGB * (sRGB * (sRGB * 0.305306011h + 0.682171111h) + 0.012522878h);

		
	}


	inline half3 hxLinearToGammaSpace(half3 linRGB)
	{
		linRGB = max(linRGB, half3(0.h, 0.h, 0.h));
		// An almost-perfect approximation from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
		return max(1.055h * pow(linRGB, 0.416666667h) - 0.055h, 0.h);

	}

	void frag(v2f input, out fixed4 outColor : SV_Target, out float outDepth : SV_Depth)
	{
		float blur = 1;
		outColor = GetNearestDepthSample(input.uv.xy / input.uv.w, blur); //GetNearestDepthSample
		outDepth = blur;
	}

	float4 fragFull(v2ff input) : SV_Target
	{
		float4 fogSample = tex2D(VolumetricTexture, input.uv.xy / input.uv.w);

		float random = -3.267973856209150326797385620915e-4 + (nrand(input.uv.xy / input.uv.w) * 6.5359477124183006535947712418301e-4f);

		fogSample += float4(random.xxxx);

		return (float4(fogSample.r, fogSample.g, fogSample.b, 0)) + (tex2D(_MainTex, input.uvFlipped.xy / input.uvFlipped.w) * (1 - fogSample.a));
	}

		float4 fragFullNN(v2ff input) : SV_Target
	{
		float4 fogSample = tex2D(VolumetricTexture, input.uv.xy / input.uv.w);
		


		return (float4(fogSample.r, fogSample.g, fogSample.b, 0)) + (tex2D(_MainTex, input.uvFlipped.xy / input.uvFlipped.w) * (1 - fogSample.a));
	}



		float4 fragFullGamma(v2ff input) : SV_Target
	{
		float4 fogSample = tex2D(VolumetricTexture, input.uv.xy / input.uv.w);

		float random = (nrand(input.uv.xy / input.uv.w) * 0.0039215686274509803921568627451f);

		half4 ScreenColor = tex2D(_MainTex, input.uvFlipped.xy / input.uvFlipped.w);
		return float4(fogSample.rgb + (ScreenColor.rgb * (1 - fogSample.a)) + random.xxx, ScreenColor.a);
		
		//return float4(hxLinearToGammaSpace(hxGammaToLinearSpace(fogSample.rgb) + (hxGammaToLinearSpace(ScreenColor.rgb) * (1 - fogSample.a))) + random.xxx, ScreenColor.a);
		
		//return float4(pow(fogSample.rgb + pow(ScreenColor.rgb,2.2) * (1 - fogSample.a),0.4545f) + random.xxx, ScreenColor.a);

	}

		float4 fragFullGammaNN(v2ff input) : SV_Target
	{
		float4 fogSample = tex2D(VolumetricTexture, input.uv.xy / input.uv.w);


		half4 ScreenColor = tex2D(_MainTex, input.uvFlipped.xy / input.uvFlipped.w);
		return float4(fogSample.rgb + (ScreenColor.rgb * (1 - fogSample.a)), ScreenColor.a);
		//return float4(pow(fogSample.rgb + pow(ScreenColor.rgb,2.2) * (1 - fogSample.a),0.4545), ScreenColor.a);

	}

	sampler2D hxBlur1;
	sampler2D hxBlur2;
	sampler2D hxBlur3;
	float hxBlurScale;

	float3 BlurredColor(float blurAmount,float2 UV)
	{
		return tex2D(_MainTex, UV);
	}

		float4 fragFullBlur(v2ff input) : SV_Target
	{
		float4 fogSample = tex2D(VolumetricTexture, input.uv.xy / input.uv.w);

		float random = -3.267973856209150326797385620915e-4 + (nrand(input.uv.xy / input.uv.w) * 6.5359477124183006535947712418301e-4f);

		fogSample += float4(random.xxxx);



		return float4(fogSample.rgb + (BlurredColor(fogSample.a * hxBlurScale, input.uvFlipped.xy / input.uvFlipped.w) * (1 - fogSample.a)),1);
	}

		float4 fragFullNNBlur(v2ff input) : SV_Target
	{
		float4 fogSample = tex2D(VolumetricTexture, input.uv.xy / input.uv.w);



		return float4(fogSample.rgb + (BlurredColor(fogSample.a * hxBlurScale, input.uvFlipped.xy / input.uvFlipped.w) * (1 - fogSample.a)),1);
	}



		float4 fragFullGammaBlur(v2ff input) : SV_Target
	{
		float4 fogSample = tex2D(VolumetricTexture, input.uv.xy / input.uv.w);

		float random = (nrand(input.uv.xy / input.uv.w) * 0.0039215686274509803921568627451f);

		
		return float4(fogSample.rgb + (BlurredColor(fogSample.a * hxBlurScale, input.uvFlipped.xy / input.uvFlipped.w) * (1 - fogSample.a)) + random.xxx, 1);

		//return float4(hxLinearToGammaSpace(hxGammaToLinearSpace(fogSample.rgb) + (hxGammaToLinearSpace(ScreenColor.rgb) * (1 - fogSample.a))) + random.xxx, ScreenColor.a);

		//return float4(pow(fogSample.rgb + pow(ScreenColor.rgb,2.2) * (1 - fogSample.a),0.4545f) + random.xxx, ScreenColor.a);

	}

		float4 fragFullGammaNNBlur(v2ff input) : SV_Target
	{
		float4 fogSample = tex2D(VolumetricTexture, input.uv.xy / input.uv.w);
		half4 ScreenColor = tex2D(_MainTex, input.uvFlipped.xy / input.uvFlipped.w);
		return float4(fogSample.rgb + (BlurredColor(fogSample.a * hxBlurScale, input.uvFlipped.xy / input.uvFlipped.w) * (1 - fogSample.a)), 1);
		//return float4(pow(fogSample.rgb + pow(ScreenColor.rgb,2.2) * (1 - fogSample.a),0.4545), ScreenColor.a);

	}

		ENDCG
		SubShader
	{
		Pass
		{
			ZTest Always Cull Off ZWrite On

			CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 3.0
			ENDCG
		}

			Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
#pragma vertex vertFull
#pragma fragment fragFull
#pragma target 3.0
			ENDCG
		}
			Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
#pragma vertex vertFull
#pragma fragment fragFullGamma
#pragma target 3.0
			ENDCG
		}
			Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
#pragma vertex vertFull
#pragma fragment fragFullNN
#pragma target 3.0
			ENDCG
		}
			Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
#pragma vertex vertFull
#pragma fragment fragFullGammaNN
#pragma target 3.0
			ENDCG
		}
			Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
#pragma vertex vertFull
#pragma fragment fragFullBlur
#pragma target 3.0
			ENDCG
		}
			Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
#pragma vertex vertFull
#pragma fragment fragFullGammaBlur
#pragma target 3.0
			ENDCG
		}
			Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
#pragma vertex vertFull
#pragma fragment fragFullNNBlur
#pragma target 3.0
			ENDCG
		}
			Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
#pragma vertex vertFull
#pragma fragment fragFullGammaNNBlur
#pragma target 3.0
			ENDCG
		}

	}
	Fallback off
}
