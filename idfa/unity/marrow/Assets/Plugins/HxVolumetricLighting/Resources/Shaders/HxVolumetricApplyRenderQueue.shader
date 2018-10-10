// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'



Shader "Hidden/HxVolumetricApplyRenderQueue"
{
	CGINCLUDE



#include "UnityCG.cginc"
#include "UnityDeferredLibrary.cginc"
#include "UnityPBSLighting.cginc"


		float nrand(float2 uv)
	{
		return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
	}
	 
		float4x4 InverseProjectionMatrix;
	float4x4 VolumetricMVP;
	float4x4 VolumetricMV;
	float3 CameraFoward;
	sampler2D VolumetricTexture;

	struct appdata {
		float4 vertex : POSITION;
	};

	struct v2f
	{
		float4 pos : SV_POSITION;
		float4 uv : TEXCOORD0;

	};

	v2f vert(appdata v)
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = ComputeScreenPos(o.pos);

		return o;
	}

	float4 Frag(v2f i) : SV_TARGET
	{
		float2 uv = i.uv.xy / i.uv.w;

		float random = -3.267973856209150326797385620915e-4 + (nrand(uv) * 6.5359477124183006535947712418301e-4f);
		float4 final = tex2D(VolumetricTexture, uv);
		return float4(final.rgb + random.xxx, final.a);
	}



		ENDCG
		SubShader
	{ 
		Tags{ "Queue" = "Transparent-5" }
			Pass
		{
			ZTest Always Cull Back ZWrite Off
			
			Blend One One

			CGPROGRAM
#pragma vertex vert
#pragma fragment Frag
#pragma target 3.0
			ENDCG
		}
	}
	Fallback off
}
