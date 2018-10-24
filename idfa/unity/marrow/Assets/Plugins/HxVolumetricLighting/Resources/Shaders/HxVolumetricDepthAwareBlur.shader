	Shader "Hidden/HxVolumetricDepthAwareBlur"
	{


			CGINCLUDE
#include "UnityCG.cginc"

		struct v2f
		{
			float4 pos : SV_POSITION;
			float4 uv : TEXCOORD0;
		};



		sampler2D _MainTex;
		float4 _MainTex_TexelSize;


		uniform sampler2D VolumetricDepth;
		uniform float BlurDepthFalloff;
		uniform sampler2D _CameraDepthTexture;
		uniform float4 _CameraDepthTexture_TexelSize;
		uniform float4 BlurDir;

		float4x4 VolumetricMVP;
		struct appdata {
			float4 vertex : POSITION;
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



		float4 AddResult(inout float totalWeight, float centralDepth, float2 uv, float weight, bool isSky)
		{
			float depth = (DecodeFloatRG(tex2Dlod(VolumetricDepth, float4(uv, 0, 0)).rg));

			float w = abs((depth)-(centralDepth)) * BlurDepthFalloff / max(centralDepth,depth);
			w = saturate(exp(-w* w)) * weight;

			if (isSky == ((depth) < 0.999f)){ w = 0; }
		
			totalWeight += w;
	
			return (tex2Dlod(_MainTex, float4(uv, 0, 0))) * w;
	
		}

		sampler2D _FullVolumetric;
		float4 AddResultFull(inout float totalWeight, float centralDepth, float2 uv, float weight, bool isSky)
		{
			float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, float4(uv, 0, 0)));

			float w = abs((depth)-(centralDepth)) * BlurDepthFalloff / max(centralDepth, depth);
			w = saturate(exp(-w* w)) * weight;

			if (isSky == ((depth) < 0.99995f)) { w = 0; }

			totalWeight += w;

			return tex2Dlod(_MainTex, float4(uv, 0, 0)) * w;
		}


	
		float4 fragGaussian (v2f input) : SV_Target
		{		
		
			float2 uv = input.uv.xy / input.uv.w;
			float centralDepthFull = (DecodeFloatRG(tex2Dlod(VolumetricDepth, float4(uv, 0, 0)).rg));

			bool isSky = ((centralDepthFull) > 0.999f);
			
			float4 result = float4(0, 0, 0, 0);
			
			float totalWeight = 0.12876;
	
			float2 offset = BlurDir.xy * _MainTex_TexelSize.xy;
						
	
			result += AddResult(totalWeight, centralDepthFull, uv + float2(-_MainTex_TexelSize.x * 2,-_MainTex_TexelSize.y * 2), 0.005865, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(-_MainTex_TexelSize.x,-_MainTex_TexelSize.y * 2), 0.018686, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(0,-_MainTex_TexelSize.y * 2), 0.027481, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(_MainTex_TexelSize.x,-_MainTex_TexelSize.y * 2), 0.018686, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(_MainTex_TexelSize.x * 2,-_MainTex_TexelSize.y * 2), 0.005865, isSky);
					  
			result += AddResult(totalWeight, centralDepthFull, uv + float2(-_MainTex_TexelSize.x * 2,-_MainTex_TexelSize.y), 0.018686, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(-_MainTex_TexelSize.x,-_MainTex_TexelSize.y), 0.059536, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(0,-_MainTex_TexelSize.y), 0.087555, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(_MainTex_TexelSize.x,-_MainTex_TexelSize.y), 0.059536, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(_MainTex_TexelSize.x * 2,-_MainTex_TexelSize.y), 0.018686, isSky);
					 
			result += AddResult(totalWeight, centralDepthFull, uv + float2(-_MainTex_TexelSize.x * 2,0), 0.027481, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(-_MainTex_TexelSize.x,0), 0.087555, isSky);

			result += (tex2Dlod(_MainTex, float4((uv).xy, 0, 0))) * 0.12876;

			result += AddResult(totalWeight, centralDepthFull, uv + float2(_MainTex_TexelSize.x,0), 0.087555, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(_MainTex_TexelSize.x * 2,0), 0.027481, isSky);
					  
			result += AddResult(totalWeight, centralDepthFull, uv + float2(-_MainTex_TexelSize.x * 2,_MainTex_TexelSize.y), 0.018686, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(-_MainTex_TexelSize.x,_MainTex_TexelSize.y), 0.059536, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(0,_MainTex_TexelSize.y), 0.087555, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(_MainTex_TexelSize.x,_MainTex_TexelSize.y), 0.059536, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(_MainTex_TexelSize.x* 2,_MainTex_TexelSize.y), 0.018686, isSky);
					  
			result += AddResult(totalWeight, centralDepthFull, uv + float2(-_MainTex_TexelSize.x * 2,_MainTex_TexelSize.y * 2), 0.005865, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(-_MainTex_TexelSize.x,_MainTex_TexelSize.y * 2), 0.018686, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(0,_MainTex_TexelSize.y * 2), 0.027481, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(_MainTex_TexelSize.x,_MainTex_TexelSize.y * 2), 0.018686, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(_MainTex_TexelSize.x * 2,_MainTex_TexelSize.y * 2), 0.005865, isSky);



			return result / totalWeight;
		}

		float4 fragFullGaussian (v2f input) : SV_Target
		{
			float2 uv = input.uv.xy / input.uv.w;
			float centralDepthFull = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, float4(uv, 0, 0)));
			
	


			bool isSky = ((centralDepthFull) > 0.99995f);
			
			float4 result = float4(0, 0, 0, 0);

				float totalWeight = 0.12876;
	
			float2 offset = BlurDir.xy * _CameraDepthTexture_TexelSize.xy;
						
	
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(-_CameraDepthTexture_TexelSize.x * 2,-_CameraDepthTexture_TexelSize.y * 2), 0.005865, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(-_CameraDepthTexture_TexelSize.x,-_CameraDepthTexture_TexelSize.y * 2), 0.018686, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(0,-_CameraDepthTexture_TexelSize.y * 2), 0.027481, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(_CameraDepthTexture_TexelSize.x,-_CameraDepthTexture_TexelSize.y * 2), 0.018686, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(_CameraDepthTexture_TexelSize.x * 2,-_CameraDepthTexture_TexelSize.y * 2), 0.005865, isSky);
					 
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(-_CameraDepthTexture_TexelSize.x * 2,-_CameraDepthTexture_TexelSize.y), 0.018686, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(-_CameraDepthTexture_TexelSize.x,-_CameraDepthTexture_TexelSize.y), 0.059536, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(0,-_CameraDepthTexture_TexelSize.y), 0.087555, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(_CameraDepthTexture_TexelSize.x,-_CameraDepthTexture_TexelSize.y), 0.059536, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(_CameraDepthTexture_TexelSize.x * 2,-_CameraDepthTexture_TexelSize.y), 0.018686, isSky);
					 
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(-_CameraDepthTexture_TexelSize.x * 2,0), 0.027481, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(-_CameraDepthTexture_TexelSize.x,0), 0.087555, isSky);

			result += (tex2Dlod(_MainTex, float4(uv, 0, 0))) * 0.12876;

			result += AddResultFull(totalWeight, centralDepthFull,uv + float2(_CameraDepthTexture_TexelSize.x,0), 0.087555, isSky);
			result += AddResultFull(totalWeight, centralDepthFull,uv + float2(_CameraDepthTexture_TexelSize.x * 2,0), 0.027481, isSky);
					  
			result += AddResultFull(totalWeight, centralDepthFull,uv + float2(-_CameraDepthTexture_TexelSize.x * 2, _CameraDepthTexture_TexelSize.y), 0.018686, isSky);
			result += AddResultFull(totalWeight, centralDepthFull,uv + float2(-_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y), 0.059536, isSky);
			result += AddResultFull(totalWeight, centralDepthFull,uv + float2(0, _CameraDepthTexture_TexelSize.y), 0.087555, isSky);
			result += AddResultFull(totalWeight, centralDepthFull,uv + float2(_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y), 0.059536, isSky);
			result += AddResultFull(totalWeight, centralDepthFull,uv + float2(_CameraDepthTexture_TexelSize.x* 2, _CameraDepthTexture_TexelSize.y), 0.018686, isSky);
					 
			result += AddResultFull(totalWeight, centralDepthFull,uv + float2(-_CameraDepthTexture_TexelSize.x * 2, _CameraDepthTexture_TexelSize.y * 2), 0.005865, isSky);
			result += AddResultFull(totalWeight, centralDepthFull,uv + float2(-_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y * 2), 0.018686, isSky);
			result += AddResultFull(totalWeight, centralDepthFull,uv + float2(0, _CameraDepthTexture_TexelSize.y * 2), 0.027481, isSky);
			result += AddResultFull(totalWeight, centralDepthFull,uv + float2(_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y * 2), 0.018686, isSky);
			result += AddResultFull(totalWeight, centralDepthFull,uv + float2(_CameraDepthTexture_TexelSize.x * 2, _CameraDepthTexture_TexelSize.y * 2), 0.005865, isSky);

			return result / totalWeight;
		}
		

		float4 fragAverage(v2f input) : SV_Target
		{		
			float2 uv = input.uv.xy / input.uv.w;

			float centralDepthFull = (DecodeFloatRG(tex2Dlod(VolumetricDepth, float4(uv, 0, 0)).rg));

			bool isSky = ((centralDepthFull) > 0.999f);
			
			float4 result = float4(0, 0, 0, 0);
			
			float totalWeight = 1;
	
	
			result += AddResult(totalWeight, centralDepthFull, uv + float2(-_MainTex_TexelSize.x * 2,-_MainTex_TexelSize.y * 2), 1, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(-_MainTex_TexelSize.x,-_MainTex_TexelSize.y * 2), 1, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(0,-_MainTex_TexelSize.y * 2), 1, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(_MainTex_TexelSize.x,-_MainTex_TexelSize.y * 2), 1, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(_MainTex_TexelSize.x * 2,-_MainTex_TexelSize.y * 2), 1, isSky);
			
			result += AddResult(totalWeight, centralDepthFull, uv + float2(-_MainTex_TexelSize.x * 2,-_MainTex_TexelSize.y), 1, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(-_MainTex_TexelSize.x,-_MainTex_TexelSize.y), 1, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(0,-_MainTex_TexelSize.y), 1, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(_MainTex_TexelSize.x,-_MainTex_TexelSize.y), 1, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(_MainTex_TexelSize.x * 2,-_MainTex_TexelSize.y), 1, isSky);
			
			result += AddResult(totalWeight, centralDepthFull, uv + float2(-_MainTex_TexelSize.x * 2,0), 1, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(-_MainTex_TexelSize.x,0), 1, isSky);

			result += (tex2Dlod(_MainTex, float4((uv).xy, 0, 0)));

			result += AddResult(totalWeight, centralDepthFull, uv + float2(_MainTex_TexelSize.x,0), 1, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(_MainTex_TexelSize.x * 2,0), 1, isSky);
			
			result += AddResult(totalWeight, centralDepthFull, uv + float2(-_MainTex_TexelSize.x * 2,_MainTex_TexelSize.y), 1, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(-_MainTex_TexelSize.x,_MainTex_TexelSize.y), 1, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(0,_MainTex_TexelSize.y), 1, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(_MainTex_TexelSize.x,_MainTex_TexelSize.y), 1, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(_MainTex_TexelSize.x* 2,_MainTex_TexelSize.y), 1, isSky);
			
			result += AddResult(totalWeight, centralDepthFull, uv + float2(-_MainTex_TexelSize.x * 2,_MainTex_TexelSize.y * 2), 1, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(-_MainTex_TexelSize.x,_MainTex_TexelSize.y * 2), 1, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(0,_MainTex_TexelSize.y * 2), 1, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(_MainTex_TexelSize.x,_MainTex_TexelSize.y * 2), 1, isSky);
			result += AddResult(totalWeight, centralDepthFull, uv + float2(_MainTex_TexelSize.x * 2,_MainTex_TexelSize.y * 2), 1, isSky);



			return result / totalWeight;
		}

		float4 fragFullAverage(v2f input) : SV_Target
		{

			float2 uv = input.uv;// input.uv.xy / input.uv.w;
			float centralDepthFull = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, float4(uv, 0, 0)));

			bool isSky = ((centralDepthFull) > 0.99995f);

			float4 result = float4(0, 0, 0, 0);

			float totalWeight = 1;



			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(-_CameraDepthTexture_TexelSize.x * 2,-_CameraDepthTexture_TexelSize.y * 2), 1, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(-_CameraDepthTexture_TexelSize.x,-_CameraDepthTexture_TexelSize.y * 2), 1, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(0,-_CameraDepthTexture_TexelSize.y * 2), 1, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(_CameraDepthTexture_TexelSize.x,-_CameraDepthTexture_TexelSize.y * 2), 1, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(_CameraDepthTexture_TexelSize.x * 2,-_CameraDepthTexture_TexelSize.y * 2), 1, isSky);

			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(-_CameraDepthTexture_TexelSize.x * 2,-_CameraDepthTexture_TexelSize.y), 1, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(-_CameraDepthTexture_TexelSize.x,-_CameraDepthTexture_TexelSize.y), 1, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(0,-_CameraDepthTexture_TexelSize.y), 1, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(_CameraDepthTexture_TexelSize.x,-_CameraDepthTexture_TexelSize.y), 1, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(_CameraDepthTexture_TexelSize.x * 2,-_CameraDepthTexture_TexelSize.y), 1, isSky);

			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(-_CameraDepthTexture_TexelSize.x * 2,0), 1, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(-_CameraDepthTexture_TexelSize.x,0), 1, isSky);

			result += (tex2Dlod(_MainTex, float4(uv, 0, 0)));

			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(_CameraDepthTexture_TexelSize.x,0), 1, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(_CameraDepthTexture_TexelSize.x * 2,0), 1, isSky);

			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(-_CameraDepthTexture_TexelSize.x * 2, _CameraDepthTexture_TexelSize.y), 1, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(-_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y), 1, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(0, _CameraDepthTexture_TexelSize.y), 1, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y), 1, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(_CameraDepthTexture_TexelSize.x * 2, _CameraDepthTexture_TexelSize.y), 1, isSky);

			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(-_CameraDepthTexture_TexelSize.x * 2, _CameraDepthTexture_TexelSize.y * 2), 1, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(-_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y * 2), 1, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(0, _CameraDepthTexture_TexelSize.y * 2), 1, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y * 2), 1, isSky);
			result += AddResultFull(totalWeight, centralDepthFull, uv + float2(_CameraDepthTexture_TexelSize.x * 2, _CameraDepthTexture_TexelSize.y * 2), 1, isSky);

			return result / totalWeight;
			//return float4(uv.x, uv.y, 0, 1);
		}


			ENDCG
			SubShader
		{
			Pass
			{
				ZTest Always Cull Off ZWrite Off
				blend one zero, one zero
				CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment fragAverage
				ENDCG
			}

			Pass
				{
					ZTest Greater  Cull Off ZWrite Off
					blend one zero, one zero
					CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment fragFullAverage

					ENDCG
				}

				Pass
			{
				ZTest Always Cull Off ZWrite Off
				blend one zero, one zero
				CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment fragGaussian
				ENDCG
			}

			Pass
				{
					ZTest Greater  Cull Off ZWrite Off
					blend one zero, one zero
					CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment fragFullGaussian

					ENDCG
				}

				Pass
			{
				ZTest Always Cull Off ZWrite Off
				blend one zero, one zero
				CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment fragFullAverage

				ENDCG
			}
				Pass
			{
				ZTest Always Cull Off ZWrite Off
				blend one zero, one zero
					CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment fragFullGaussian

					ENDCG
			}

		}
		Fallback off
	}

