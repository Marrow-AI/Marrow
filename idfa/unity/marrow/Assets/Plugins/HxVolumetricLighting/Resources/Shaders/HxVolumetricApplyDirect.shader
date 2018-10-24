// Upgrade NOTE: commented out 'float4x4 _CameraToWorld', a built-in variable
// Upgrade NOTE: replaced '_CameraToWorld' with '_CameraToWorld'

Shader "Hidden/HxVolumetricApplyDirect" {

	Properties
	{
		_MainTex("", any) = "" {}

	}

		CGINCLUDE
#pragma multi_compile _ UNITY_SINGLE_PASS_STEREO
#include "UnityCG.cginc"
	struct appdata {
		float4 vertex : POSITION;
	};

	struct v2cf
	{
		float4 pos : SV_POSITION;
		float4 uv : TEXCOORD0;
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
	
	};

	struct v2r
	{
		float4 pos : SV_POSITION;
		float4 uv : TEXCOORD0;
	};
	float4x4 hxInverseP1;
	float4x4 hxInverseP2;
	float4 _MainTex_ST;
	float3 hxCameraPosition;
	float3 hxCameraPosition2;
	float4x4 hxCameraToWorld;
	float4x4 hxCameraToWorld2; //second eye
	float2 hxTemporalSettings;
	float4x4 InverseProjectionMatrix;
	float4x4 InverseProjectionMatrix2;
	sampler2D hxLastVolumetric;
	sampler2D _MainTex;
	sampler2D _LastCameraDepthTexture;
	sampler2D _CameraDepthTexture;
	sampler2D VolumetricDepth;
	float4 VolumetricDepth_TexelSize;
	sampler2D VolumetricTexture;
	sampler2D VolumetricTextureLast;
	float4 VolumetricTexture_TexelSize;
	float ExtinctionEffect;
	float4x4 hxLastVP;
	float4x4 hxLastVP2;



	//float4 VolumetricTexture_TexelSize;

	// float4x4 _CameraToWorld;
	float4 _MainTex_TexelSize; // (1.0/width, 1.0/height, width, height)
	float4 _CameraDepthTexture_TexelSize;

	uniform float BlurDepthFalloff;
	float DepthThreshold;
	float4x4 VolumetricMVP;

	

	v2cf vertTemp(appdata v)
	{
		v2cf o = (v2cf)0;
		o.pos = v.vertex;		
	//	o.pos = float4(v.vertex.x * 2 - 1, v.vertex.y * 2 - 1, v.vertex.z, v.vertex.w);
		o.uv = ComputeScreenPos(o.pos);
//		o.uv2 = v.vertex.xy;
//
//#if defined (UNITY_UV_STARTS_AT_TOP) //not sure if i need this
//		o.uv2.y = 1 - o.uv2.y;
//#endif
		return o;
	}

	v2cf vertCB(appdata v)
	{
		v2cf o = (v2cf)0;

		o.pos = v.vertex;
		o.uv = ComputeScreenPos(o.pos);

		return o;
	}


	v2f vert(appdata v)
	{
		v2f o = (v2f)0;
		o.pos = v.vertex; // mul(UNITY_MATRIX_MVP, v.vertex);

		o.uv = ComputeScreenPos(o.pos);
		return o;

	}

	v2ff vertFull(appdata_img v)
	{
		v2ff o = (v2ff)0;
		o.pos = v.vertex;
		o.uv = ComputeScreenPos(o.pos);

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
		return (c1 + c2) / 2.0;
	}

	float4 GetNearestDepthSample(float LZ, float2 uv, out float blur)
	{
		//read full resolution depth

		const float2 lowResTexelSize = VolumetricDepth_TexelSize.xy;
		const float depthTreshold = DepthThreshold * LZ;
		const float2 lowResTexelSize2 = lowResTexelSize * 2.0;

		//return tex2Dlod(_MainTex, float4(uv, 0, 0));


		float2 lowResUV = uv;

		float MinDist = 100000;
		float MinDist2 = 100000;


		float2 UV00 = lowResUV - (lowResTexelSize * 0.5f);
		UV00 = float2((floor((UV00.x) / lowResTexelSize.x)) * lowResTexelSize.x + lowResTexelSize.x / 2.0, (floor((UV00.y) / lowResTexelSize.y)) * lowResTexelSize.y + lowResTexelSize.y / 2.0);

		float2 mid = lowResUV;
		mid = float2((floor((mid.x) / lowResTexelSize.x)) * lowResTexelSize.x + lowResTexelSize.x / 2.0, (floor((mid.y) / lowResTexelSize.y)) * lowResTexelSize.y + lowResTexelSize.y / 2.0);

		float2 offsetTexel = float2(lowResTexelSize.x * (-1.0 + (2.0 * (UV00.x != mid.x))), lowResTexelSize.y * (-1.0 + (2.0 * (UV00.y != mid.y))));

		float2 NearestUV = mid;
		float2 NearestUV2 = mid;


		float Z00 = (DecodeFloatRG(tex2Dlod(VolumetricDepth, float4(mid.xy, 0.0, 0.0)).rg)); float az = Z00; float az2 = Z00;
		UpdateNearestSample2(MinDist, NearestUV, MinDist2, NearestUV2, az, az2, Z00, mid, LZ);

		float2 UV10 = float2(mid.x - offsetTexel.x, mid.y);
		float Z10 = (DecodeFloatRG(tex2Dlod(VolumetricDepth, float4(UV10, 0.0, 0.0)).rg) );
		UpdateNearestSample2(MinDist, NearestUV, MinDist2, NearestUV2, az, az2, Z10, UV10, LZ);

		float2 UV01 = float2(mid.x, mid.y - offsetTexel.y);
		float Z01 = (DecodeFloatRG(tex2Dlod(VolumetricDepth, float4(UV01, 0.0, 0.0)).rg) );
		UpdateNearestSample2(MinDist, NearestUV, MinDist2, NearestUV2, az, az2, Z01, UV01, LZ);

		float2 UV11 = mid - offsetTexel;
		float Z11 = (DecodeFloatRG(tex2Dlod(VolumetricDepth, float4(UV11, 0.0, 0.0)).rg) );
		UpdateNearestSample2(MinDist, NearestUV, MinDist2, NearestUV2, az, az2, Z11, UV11, LZ);

							 
		bool sky1 = (Z00 < 0.999);
		bool sky2 = (Z10 < 0.999);
		bool sky3 = (Z01 < 0.999);
		bool sky4 = (Z11 < 0.999);



		if ((abs(Z00 - (LZ)) < depthTreshold && abs(Z10 - (LZ)) < depthTreshold && abs(Z01 - (LZ)) < depthTreshold && abs(Z11 - (LZ)) < depthTreshold) && (sky1 == sky2 && sky1 == sky3 && sky1 == sky4 && (LZ < 0.999 == sky1)))

		{

		
			blur = 1.0;

			return  tex2Dlod(_MainTex, float4(uv, 0.0, 0.0));
		}
		else
		{
			float2 UV20 = float2(mid.x + offsetTexel.x, mid.y + offsetTexel.y);
			float Z20 = (DecodeFloatRG(tex2Dlod(VolumetricDepth, float4(UV20, 0.0, 0.0)).rg));
			UpdateNearestSample2(MinDist, NearestUV, MinDist2, NearestUV2, az, az2, Z20, UV20, LZ);
			
			float2 UV21 = float2(mid.x, mid.y + offsetTexel.y);
			float Z21 = (DecodeFloatRG(tex2Dlod(VolumetricDepth, float4(UV21, 0.0, 0.0)).rg));
			UpdateNearestSample2(MinDist, NearestUV, MinDist2, NearestUV2, az, az2, Z21, UV21, LZ);
			
			float2 UVN1 = float2(mid.x + offsetTexel.x, mid.y);
			float ZN1 = (DecodeFloatRG(tex2Dlod(VolumetricDepth, float4(UVN1, 0.0, 0.0)).rg));
			UpdateNearestSample2(MinDist, NearestUV, MinDist2, NearestUV2, az, az2, ZN1, UVN1, LZ);
			
			float2 UVN0 = float2(mid.x - offsetTexel.x, mid.y + offsetTexel.y);
			float ZN0 = (DecodeFloatRG(tex2Dlod(VolumetricDepth, float4(UVN0, 0.0, 0.0)).rg));
			UpdateNearestSample2(MinDist, NearestUV, MinDist2, NearestUV2, az, az2, ZN0, UVN0, LZ);
			
			float2 UV02 = float2(mid.x + offsetTexel.x, mid.y - offsetTexel.y);
			float Z02 = (DecodeFloatRG(tex2Dlod(VolumetricDepth, float4(UV02, 0.0, 0.0)).rg));
			UpdateNearestSample2(MinDist, NearestUV, MinDist2, NearestUV2, az, az2, Z02, UV02, LZ);
			blur = 0.0;

			if (MinDist > 99999.0) {
				NearestUV = NearestUV2;	az = az2;
			}
			if (MinDist2 > 99999.0) { NearestUV2 = NearestUV; az2 = az;
			}

			return lerp(tex2Dlod(_MainTex, float4(NearestUV, 0.0, 0.0)), tex2Dlod(_MainTex, float4(NearestUV2, 0.0, 0.0)), (saturate(smoothstep((az), (az2), (LZ)))));

			//fogSample = tex2Dlod(_MainTex, float4(uv, 0, 0));
		}


		//float random = nrand(uv) * 0.00390625f * 0.15;
		//return fogSample;// +float4(random, random, random, 1);

	}


	struct bvr {
#if defined(SHADER_API_PSSL)
		fixed4 col0 : S_TARGET_OUTPUT;
#else
		fixed4 col0 : COLOR0;
#endif

#if defined(SHADER_API_PSSL)
		fixed4 col1 : S_TARGET_OUTPUT1;
#else
		fixed4 col1 : COLOR1;
#endif

		float depth0 : SV_Depth;
	};

	struct temp {
#if defined(SHADER_API_PSSL)
		fixed4 col0 : S_TARGET_OUTPUT;
#else
		fixed4 col0 : COLOR0;
#endif

#if defined(SHADER_API_PSSL)
		fixed4 col1 : S_TARGET_OUTPUT1;
#else
		fixed4 col1 : COLOR1;
#endif
	};

	void frag(v2f input, out fixed4 outColor : SV_Target, out float outDepth : SV_Depth)
	{
		float blur = 1.0f;
		outColor = GetNearestDepthSample(Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, input.uv)),input.uv.xy, blur);
		outDepth = blur;
	}

	void TemporalSample(float depth, float2 uv, inout float4 outColor, inout float blur)
	{
		
#ifdef UNITY_SINGLE_PASS_STEREO	
		bool Left = uv.x < 0.5f;
		float2 data = uv.xy;
		data.x = frac(data.x * 2.0f);


		float4 clipPos = float4(data * 2.0 - 1.0, 1.0, 1.0);
		float4 cameraRay = mul((Left ? hxInverseP1 : hxInverseP2), clipPos);
		cameraRay = (cameraRay / cameraRay.w);
		cameraRay.xyz = cameraRay.xyz / cameraRay.z;
		float3 wpos = mul(!Left ? hxCameraToWorld2 : hxCameraToWorld, float4(-cameraRay.xyz * LinearEyeDepth(depth), 1)).xyz;
		float4 fragPrevClip = mul((!Left ? hxLastVP : hxLastVP2), float4(wpos, 1.0f));

#else
		float4 clipPos = float4(uv * 2.0 - 1.0, 1.0, 1.0);
		float4 cameraRay = mul((InverseProjectionMatrix), clipPos);
		cameraRay = (cameraRay / cameraRay.w);
		cameraRay.xyz = cameraRay.xyz / cameraRay.z;
		float3 wpos = mul(hxCameraToWorld, float4(-cameraRay.xyz * LinearEyeDepth(depth), 1.0f)).xyz;
		float4 fragPrevClip = mul(hxLastVP, float4(wpos, 1.0f));



		//float4 clipPos = float4(uv.xy * 2.0 - 1.0, -1.0, -1.0);
		//float4 cameraRay = mul(InverseProjectionMatrix, clipPos);
		//cameraRay = (cameraRay / cameraRay.w);
		//cameraRay.xyz = cameraRay.xyz / cameraRay.z;
		//float3 wpos = mul(_CameraToWorld, float4(cameraRay.xyz * LinearEyeDepth(depth), 1)).xyz;
		//float4 fragPrevClip = mul(hxLastVP, float4(wpos, 1.0f));
#endif

	
		
		fragPrevClip.xyz /= fragPrevClip.w;

		float2 lastUV = (fragPrevClip.xy + 1.0f) * 0.5f;

#if defined (UNITY_UV_STARTS_AT_TOP) //not sure if i need this
		//lastUV.y = 1 - lastUV.y;
#endif

		
		if (lastUV.x < 0 || lastUV.x > 1 || lastUV.y < 0 || lastUV.y > 1)
		{
			blur = 0.0f;
			//outColor = float4(1,0, 0, 0);
		}
		else
		{
#ifdef UNITY_SINGLE_PASS_STEREO	
			
			lastUV = float2((lastUV.x * 0.5f) + (0.5 * !Left), lastUV.y);
#endif
			
		

			float4 TempColor = tex2D(hxLastVolumetric, lastUV);			
			float4 texel0 = outColor;
			
			float lum0 = Luminance(outColor.rgb);
			float lum1 = Luminance(TempColor.rgb);

			float LumPer = (hxTemporalSettings.x *((max(lum0, lum1) + min(lum0, lum1)) / 2));
			//float LumPer = (hxTemporalSettings.x * min(lum0, lum1));
			float diff = saturate(1 - (1.0f / LumPer * abs(lum0 - lum1)));// max(abs(lum0 - lum1), 0.0001f) * min(lum0, lum1));


			float feedback = lerp(0, hxTemporalSettings.y, diff);// 								
		//	float feedback = lerp(hxTemporalSettings.x, hxTemporalSettings.y, lerp(1, 0, saturate(abs((LastDepth - depth) * 1000))));

			outColor =  lerp(texel0, TempColor, feedback);
			
		}

	}

	void fragTemp(v2cf input, out fixed4 outColor : SV_Target, out float outDepth : SV_Depth)
	{
		//float centerDepth;	
		//float blur;
		//float4 SampledColor;
		float centerDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, input.uv.xy / input.uv.w);
		float blur = 1.0f;
		float4 SampledColor = GetNearestDepthSample(Linear01Depth(centerDepth),input.uv.xy / input.uv.w, blur);
		TemporalSample((centerDepth), input.uv / input.uv.w, SampledColor, blur);
		outDepth = blur;
		outColor = SampledColor;
	}

	bvr frag2Temp(v2cf input)
	{
		bvr outbvr;
		fixed blur = 1.0f;
		float centerDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, input.uv);
		fixed4 outcome = GetNearestDepthSample(Linear01Depth(centerDepth), input.uv.xy, blur);
		TemporalSample((centerDepth), input.uv, outcome, blur);
		outbvr.col0 = outcome;
		outbvr.col1 = outcome;
		outbvr.depth0 = blur;
		return outbvr;
	}

	bvr frag2(v2cf input)
	{
		bvr outbvr;
		float blur = 1.0f;
		float4 outcome = GetNearestDepthSample(Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, input.uv)),input.uv.xy, blur);
		outbvr.col0 = outcome;
		outbvr.col1 = outcome;
		outbvr.depth0 = blur;
		return outbvr;
	}


	float4 fragTempCopy(v2ff input) : SV_Target
	{
		return tex2D(VolumetricTexture, input.uv);
	}


	float4 fragFull(v2ff input) : SV_Target
	{
		float4 fogSample = tex2D(VolumetricTexture, input.uv.xy);

		float random = -3.267973856209150326797385620915e-4 + (nrand(input.uv) * 6.5359477124183006535947712418301e-4f);


		fogSample += float4(random.xxxx);

		return float4(fogSample.r, fogSample.g, fogSample.b, fogSample.a);
	}

		float4 fragFullNN(v2ff input) : SV_Target
	{
		float4 fogSample = tex2D(VolumetricTexture, input.uv.xy);



		return float4(fogSample.r, fogSample.g, fogSample.b,fogSample.a);
	}


		float4 fragFullGamma(v2ff input) : SV_Target
	{
		float4 fogSample = tex2D(VolumetricTexture, input.uv.xy);

		float random = (nrand(input.uv) * 0.0039215686274509803921568627451f);

		return float4(pow(fogSample.rgb ,0.4545f) + random.xxx, fogSample.a);

	}

	float4 fragFullGammaNN(v2ff input) : SV_Target
	{
		float4 fogSample = tex2D(VolumetricTexture, input.uv.xy);
		
		return float4(pow(fogSample.rgb,0.4545), fogSample.a);

	}


		ENDCG
		SubShader
	{
		Pass
		{
			ZTest Always Cull Off ZWrite On

			CGPROGRAM
#pragma vertex vertCB
#pragma fragment frag
#pragma target 3.0
			ENDCG
		}

			Pass
		{
			ZTest Always Cull Off ZWrite Off
			Blend One OneMinusSrcAlpha
			CGPROGRAM
#pragma vertex vertFull
#pragma fragment fragFull
#pragma target 3.0
			ENDCG
		}
			Pass
		{
			ZTest Always Cull Off ZWrite Off
			Blend One OneMinusSrcAlpha
			CGPROGRAM
#pragma vertex vertFull
#pragma fragment fragFullGamma
#pragma target 3.0
			ENDCG
		}
			Pass
		{
			ZTest Always Cull Off ZWrite Off
			Blend One OneMinusSrcAlpha
			CGPROGRAM
#pragma vertex vertFull
#pragma fragment fragFullNN
#pragma target 3.0
			ENDCG
		}
			Pass
		{
			ZTest Always Cull Off ZWrite Off
			Blend One OneMinusSrcAlpha
			CGPROGRAM
#pragma vertex vertFull
#pragma fragment fragFullGammaNN
#pragma target 3.0
			ENDCG
		}
			Pass
		{
			ZTest Always Cull Off ZWrite On

			CGPROGRAM
#pragma vertex vertCB
#pragma fragment frag2
#pragma target 3.0
			ENDCG
		}

			Pass
		{
			ZTest Always Cull Off ZWrite On

			CGPROGRAM
#pragma vertex vertTemp
#pragma fragment fragTemp
#pragma target 3.0
			ENDCG
		}

			Pass
		{
			ZTest Always Cull Off ZWrite On

			CGPROGRAM
#pragma vertex vertCB
#pragma fragment frag2Temp
#pragma target 3.0
			ENDCG
		}

			Pass
		{
			ZTest Always Cull Off ZWrite On

			CGPROGRAM
#pragma vertex vertCB
#pragma fragment fragTempCopy
#pragma target 3.0
			ENDCG
		}

	}
	Fallback off
}
