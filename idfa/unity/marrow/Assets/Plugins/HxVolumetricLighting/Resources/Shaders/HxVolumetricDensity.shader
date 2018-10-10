// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'



Shader "Hidden/HxDensityShader" {
	Properties{
		_MainTex("Particle Texture", 2D) = "white" {}
	_InvFade("Soft Particles Factor", Range(0.01,3.0)) = 1.0
	}

		CGINCLUDE
#include "HxVolumetricLightCore.cginc"
		float  GetLookupDepth(float  inViewSpaceDepth)
	{
		return  pow((inViewSpaceDepth / SliceSettings.x), SliceSettings.y);
	}

	float particleDensity;
	struct appdata_t {
		float4 vertex : POSITION;
		fixed4 color : COLOR;
		float2 texcoord : TEXCOORD0;
	};

	sampler2D _MainTex;
	fixed4 _TintColor;




	struct v2f {
		float4 vertex : SV_POSITION;
		fixed4 color : COLOR;
		float2 texcoord : TEXCOORD0;
		float4 projPos : TEXCOORD2;
		//float4 w1 : TEXCOORD3;
		//float4 w2 : TEXCOORD4;
		//float4 w3 : TEXCOORD5;
		//float4 w4 : TEXCOORD6;
	};

	float4 _MainTex_ST;


	float  GetPerValue(float  inParticleDepth, int  inSliceIndex)
	{
		float particle_slice_pos = GetLookupDepth(inParticleDepth) * (SliceSettings.z - 1);
		float distance = abs(particle_slice_pos - inSliceIndex);
		return (1.0f - saturate(distance));
	}

	v2f vert(appdata_t v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);

		o.projPos = ComputeScreenPos(o.vertex);

		COMPUTE_EYEDEPTH(o.projPos.z);

		o.color = v.color;
		o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);


		//o.w1 = float4(GetPerValue(o.projPos.z, 0), GetPerValue(o.projPos.z, 1), GetPerValue(o.projPos.z, 2), GetPerValue(o.projPos.z, 3));
		//o.w2 = float4(GetPerValue(o.projPos.z, 4), GetPerValue(o.projPos.z, 5), GetPerValue(o.projPos.z, 6), GetPerValue(o.projPos.z, 7));
		//o.w3 = float4(GetPerValue(o.projPos.z, 8), GetPerValue(o.projPos.z, 9), GetPerValue(o.projPos.z, 10), GetPerValue(o.projPos.z, 11));
		//o.w4 = float4(GetPerValue(o.projPos.z, 12), GetPerValue(o.projPos.z, 13), GetPerValue(o.projPos.z, 14), GetPerValue(o.projPos.z, 15));


		return o;
	}

	struct f4s {
#if defined(SHADER_API_PSSL)		
		fixed4 col0 : S_TARGET_OUTPUT0;
#else
		fixed4 col0 : COLOR0;
#endif
#if defined(SHADER_API_PSSL)		
		fixed4 col1 : S_TARGET_OUTPUT1;
#else
		fixed4 col1 : COLOR1;
#endif
#if !defined (DS_8)
#if defined(SHADER_API_PSSL)		
		fixed4 col2 : S_TARGET_OUTPUT2;
#else
		fixed4 col2 : COLOR2;
#endif
#if !defined (DS_12)
#if defined(SHADER_API_PSSL)		
		fixed4 col3 : S_TARGET_OUTPUT3;
#else
		fixed4 col3 : COLOR3;
#endif
#endif
#endif

	};



	float  GetAmountValue(float  inParticleDepth, float  inScatteringAmount, int  inSliceIndex)
	{
		float particle_slice_pos = GetLookupDepth(inParticleDepth) * (SliceSettings.z - 1);
		float distance = abs(particle_slice_pos - inSliceIndex);
		return (inScatteringAmount)* (1.0f - saturate(distance));
	}


	sampler2D_float _CameraDepthTexture;
	float _InvFade;

	f4s frag(v2f i)
	{
		f4s fout;


		float a = tex2D(_MainTex, i.texcoord).a * particleDensity * i.color.a;
		//
		//fout.col0 = i.w1 * a;
		//
		//fout.col1 = i.w2 * a;
		//
		//fout.col2 = i.w3 * a;
		//
		//fout.col3 = i.w4 * a;


		fout.col0 = float4(
			GetAmountValue((i.projPos.z), a, 0),
			GetAmountValue((i.projPos.z), a, 1),
			GetAmountValue((i.projPos.z), a, 2),
			GetAmountValue((i.projPos.z), a, 3));

		fout.col1 = float4(
			GetAmountValue((i.projPos.z), a, 4),
			GetAmountValue((i.projPos.z), a, 5),
			GetAmountValue((i.projPos.z), a, 6),
			GetAmountValue((i.projPos.z), a, 7));

#if !defined (DS_8)
		fout.col2 = float4(
			GetAmountValue((i.projPos.z), a, 8),
			GetAmountValue((i.projPos.z), a, 9),
			GetAmountValue((i.projPos.z), a, 10),
			GetAmountValue((i.projPos.z), a, 11));

#if !defined (DS_12)
		fout.col3 = float4(
			GetAmountValue((i.projPos.z), a, 12),
			GetAmountValue((i.projPos.z), a, 13),
			GetAmountValue((i.projPos.z), a, 14),
			GetAmountValue((i.projPos.z), a, 15));
#endif

#endif

		//return float4(col.a, col.a, col.a, col.a);
		return fout;
	}

	f4s fragMax(v2f i)
	{
		f4s fout;


		float a = tex2D(_MainTex, i.texcoord).a * particleDensity * i.color.a;
		//
		//fout.col0 = i.w1 * a;
		//
		//fout.col1 = i.w2 * a;
		//
		//fout.col2 = i.w3 * a;
		//
		//fout.col3 = i.w4 * a;


		fout.col0 = float4(
			GetAmountValue((i.projPos.z), a, 0) + 0.5f,
			GetAmountValue((i.projPos.z), a, 1) + 0.5f,
			GetAmountValue((i.projPos.z), a, 2) + 0.5f,
			GetAmountValue((i.projPos.z), a, 3) + 0.5f);

		fout.col1 = float4(
			GetAmountValue((i.projPos.z), a, 4) + 0.5f,
			GetAmountValue((i.projPos.z), a, 5) + 0.5f,
			GetAmountValue((i.projPos.z), a, 6) + 0.5f,
			GetAmountValue((i.projPos.z), a, 7) + 0.5f);

#if !defined (DS_8)
		fout.col2 = float4(
			GetAmountValue((i.projPos.z), a, 8) + 0.5f,
			GetAmountValue((i.projPos.z), a, 9) + 0.5f,
			GetAmountValue((i.projPos.z), a, 10) + 0.5f,
			GetAmountValue((i.projPos.z), a, 11) + 0.5f);

#if !defined (DS_12)
		fout.col3 = float4(
			GetAmountValue((i.projPos.z), a, 12) + 0.5f,
			GetAmountValue((i.projPos.z), a, 13) + 0.5f,
			GetAmountValue((i.projPos.z), a, 14) + 0.5f,
			GetAmountValue((i.projPos.z), a, 15) + 0.5f);
#endif

#endif

		//return float4(col.a, col.a, col.a, col.a);
		return fout;
	}

	f4s fragMin(v2f i)
	{
		f4s fout;


		float a = tex2D(_MainTex, i.texcoord).a * particleDensity * i.color.a;
		//
		//fout.col0 = i.w1 * a;
		//
		//fout.col1 = i.w2 * a;
		//
		//fout.col2 = i.w3 * a;
		//
		//fout.col3 = i.w4 * a;


		fout.col0 = float4(1 -
			GetAmountValue((i.projPos.z), a, 0), 1 -
			GetAmountValue((i.projPos.z), a, 1), 1 -
			GetAmountValue((i.projPos.z), a, 2), 1 -
			GetAmountValue((i.projPos.z), a, 3));

		fout.col1 = float4(1 -
			GetAmountValue((i.projPos.z), a, 4), 1 -
			GetAmountValue((i.projPos.z), a, 5), 1 -
			GetAmountValue((i.projPos.z), a, 6), 1 -
			GetAmountValue((i.projPos.z), a, 7));

#if !defined (DS_8)
		fout.col2 = float4(1 -
			GetAmountValue((i.projPos.z), a, 8), 1 -
			GetAmountValue((i.projPos.z), a, 9), 1 -
			GetAmountValue((i.projPos.z), a, 10), 1 -
			GetAmountValue((i.projPos.z), a, 11));

#if !defined (DS_12)
		fout.col3 = float4(1 -
			GetAmountValue((i.projPos.z), a, 12), 1 -
			GetAmountValue((i.projPos.z), a, 13), 1 -
			GetAmountValue((i.projPos.z), a, 14), 1 -
			GetAmountValue((i.projPos.z), a, 15));
#endif

#endif

		//return float4(col.a, col.a, col.a, col.a);
		return fout;
	}
	ENDCG

	


	

	SubShader{
	Pass{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		blend one one
		BlendOp Max
		Cull Off Lighting Off ZWrite Off ZTest Always
		CGPROGRAM

		
#pragma vertex vert
#pragma fragment fragMax
#pragma multi_compile_particles

#pragma target 3.0
		//#pragma multi_compile DensityDepth8 DensityDepth12 DensityDepth16// DensityDepth20 DensityDepth24 DensityDepth28 DensityDepth32
		#include "UnityCG.cginc"
	

				ENDCG
			}

		Pass{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		blend one one
		BlendOp Add
		Cull Off Lighting Off ZWrite Off ZTest Always
		CGPROGRAM


#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_particles

#pragma target 3.0
		//#pragma multi_compile DensityDepth8 DensityDepth12 DensityDepth16// DensityDepth20 DensityDepth24 DensityDepth28 DensityDepth32
#include "UnityCG.cginc"


		ENDCG
	}
		Pass{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		blend one one
		BlendOp Min
		Cull Off Lighting Off ZWrite Off ZTest Always
		CGPROGRAM


#pragma vertex vert
#pragma fragment fragMin
#pragma multi_compile_particles

#pragma target 3.0
		//#pragma multi_compile DensityDepth8 DensityDepth12 DensityDepth16// DensityDepth20 DensityDepth24 DensityDepth28 DensityDepth32
#include "UnityCG.cginc"


		ENDCG
	}

		Pass{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		blend one one
		BlendOp RevSub
		Cull Off Lighting Off ZWrite Off ZTest Always
		CGPROGRAM


#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_particles

#pragma target 3.0
		//#pragma multi_compile DensityDepth8 DensityDepth12 DensityDepth16// DensityDepth20 DensityDepth24 DensityDepth28 DensityDepth32
#include "UnityCG.cginc"


		ENDCG
	}
					
		}
	
		Fallback off
}
