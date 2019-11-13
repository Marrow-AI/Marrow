// Upgrade NOTE: commented out 'float4x4 _CameraToWorld', a built-in variable
// Upgrade NOTE: replaced '_CameraToWorld' with '_CameraToWorld'

Shader "Hidden/HxTransparencyBlur"
{

		CGINCLUDE

#pragma multi_compile _ UNITY_SINGLE_PASS_STEREO
#include "UnityCG.cginc"

		sampler2D _MainTex;
	float4 _MainTex_TexelSize;
	float4x4 hxLastVP;
	float4x4 hxLastVP2;
	float4x4 InverseProjectionMatrix;
	float4x4 InverseProjectionMatrix2;
	// float4x4 _CameraToWorld;
	sampler2D hxLastVolumetric;
	float2 hxTemporalSettings;
	sampler2D _CameraDepthTexture;
	float4x4 hxInverseP1;
	float4x4 hxInverseP2;
	float3 hxCameraPosition;
	float3 hxCameraPosition2;
	float4x4 hxCameraToWorld;
	float4x4 hxCameraToWorld2; //second eye
	struct v2f
	{
		float4 pos : SV_POSITION;
		float4 uv : TEXCOORD0;
	};


	v2f vert(appdata_img v)
	{
			v2f o = (v2f)0;
			//o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
			//o.uv =  ComputeScreenPos(o.pos);
			o.pos = v.vertex;
			o.uv = ComputeScreenPos(o.pos);
			return o;
	}


	// 9-tap Gaussian filter with linear sampling
	// http://rastergrid.com/blog/2010/09/efficient-gaussian-blur-with-linear-sampling/
	half4 gaussian_filter(float2 uv, float2 stride)
	{
		half4 s = tex2D(_MainTex, uv) * 0.227027027;

		float2 d1 = stride * 1.3846153846;
		s += tex2D(_MainTex, uv + d1) * 0.3162162162;
		s += tex2D(_MainTex, uv - d1) * 0.3162162162;

		float2 d2 = stride * 3.2307692308;
		s += tex2D(_MainTex, uv + d2) * 0.0702702703;
		s += tex2D(_MainTex, uv - d2) * 0.0702702703;

		return s;
	}


	// Separable Gaussian filters
	half4 frag_blur_h(v2f i) : SV_Target
	{
		return gaussian_filter(i.uv.xy / i.uv.w, float2(_MainTex_TexelSize.x, 0));
	}

	half4 frag_blur_v(v2f i) : SV_Target
	{
		return gaussian_filter(i.uv.xy / i.uv.w, float2(0, _MainTex_TexelSize.y));
	}

	half4 frag_blur_v_LDR(v2f i) : SV_Target
	{
		float4 texColor =  gaussian_filter(i.uv.xy / i.uv.w, float2(0, _MainTex_TexelSize.y));

			return texColor;
	}


	half4 frag_HDR_LDR(v2f i) : SV_Target
	{


		float4 texColor = tex2D(_MainTex, i.uv.xy / i.uv.w);
		float lum = max(Luminance(texColor.rgb), 0.00001f);
		float lumTm = lum * 1;
		float scale = lumTm / (1 + lumTm);

	
		return half4((texColor.rgb * scale / lum), texColor.a);
	}
		void TemporalSample(float depth, float2 uv, inout float4 outColor)
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
			//blur = 0.0f;
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

			outColor = lerp(texel0, TempColor, feedback);

		}

	}

	half4 frag_HDR_LDR_temp (v2f i) : SV_Target
	{

		float centerDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv / i.uv.w);
		float4 texColor = tex2D(_MainTex, i.uv.xy / i.uv.w);
		TemporalSample(centerDepth,i.uv.xy / i.uv.w, texColor);

		float lum = max(Luminance(texColor.rgb), 0.00001f);
		float lumTm = lum * 1;
		float scale = lumTm / (1 + lumTm);


		return half4((texColor.rgb * scale / lum), texColor.a);
	}

	half4 frag_temp(v2f i) : SV_Target
	{

	float centerDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv / i.uv.w);
	float4 texColor = tex2D(_MainTex, i.uv.xy / i.uv.w);
	TemporalSample(centerDepth,i.uv.xy / i.uv.w, texColor);

	return texColor;
	}

	
		ENDCG

		Subshader
	{

		Pass
		{
			ZTest Always Cull Off ZWrite Off
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag_blur_h
#pragma target 3.0
			ENDCG
		}
			Pass
		{
			ZTest Always Cull Off ZWrite Off
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag_blur_v
#pragma target 3.0
			ENDCG
		}

			Pass
		{
			ZTest Always Cull Off ZWrite Off
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag_blur_v_LDR
#pragma target 3.0
			ENDCG
		}
			Pass
		{
			ZTest Always Cull Off ZWrite Off
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag_HDR_LDR
#pragma target 3.0
			ENDCG
		}

		Pass
		{
			ZTest Always Cull Off ZWrite Off
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag_HDR_LDR_temp
#pragma target 3.0
			ENDCG
		}

			Pass
		{
			ZTest Always Cull Off ZWrite Off
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag_temp
#pragma target 3.0
			ENDCG
		}
	}
}
