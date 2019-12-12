Shader "Nasty/CelShadingTerrain" 
{
	//////////////////////////////////////////////////////////////
	//														   	//
	//			This shader was written by Casey MacNeil		//	   
	//														   	//
	//////////////////////////////////////////////////////////////
	Properties{

		_Ambient("Ambient Strength", Range(0,1)) = 0.1

		// Splat Map Control Texture
		[HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}

		// Textures
		[HideInInspector] _Splat3("Layer 3 (A)", 2D) = "white" {}
		[HideInInspector] _Splat2("Layer 2 (B)", 2D) = "white" {}
		[HideInInspector] _Splat1("Layer 1 (G)", 2D) = "white" {}
		[HideInInspector] _Splat0("Layer 0 (R)", 2D) = "white" {}

		// Normal Maps
		[HideInInspector] _Normal3("Normal 3 (A)", 2D) = "bump" {}
		[HideInInspector] _Normal2("Normal 2 (B)", 2D) = "bump" {}
		[HideInInspector] _Normal1("Normal 1 (G)", 2D) = "bump" {}
		[HideInInspector] _Normal0("Normal 0 (R)", 2D) = "bump" {}
	}
	SubShader{
		Tags{ "RenderType" = "Opaque" }
		//LOD 200

		CGPROGRAM
		#include "CelShadingIncludes.cginc"
		#pragma surface surf CelShading fullforwardshadows


		struct Input {
			float2 uv_Control;
			float2 uv_Splat0;
		};

		sampler2D _Control;
		sampler2D _Splat0;
		sampler2D _Splat1;
		sampler2D _Splat2;
		sampler2D _Splat3;


		sampler2D _Normal0;
		sampler2D _Normal1;
		sampler2D _Normal2;
		sampler2D _Normal3;

		void surf(Input IN, inout SurfaceOutput o) {

			half4 c = tex2D(_Control, IN.uv_Control);
			half4 splat0 = tex2D(_Splat0, IN.uv_Splat0);
			half4 splat1 = tex2D(_Splat1, IN.uv_Splat0);
			half4 splat2 = tex2D(_Splat2, IN.uv_Splat0);
			half4 splat3 = tex2D(_Splat3, IN.uv_Splat0);
			o.Albedo = splat0 * c.r + splat1 * c.g + splat2 * c.b + splat3 * c.a;

			half3 norm0 = UnpackNormal(tex2D(_Normal0, IN.uv_Splat0));
			half3 norm1 = UnpackNormal(tex2D(_Normal1, IN.uv_Splat0));
			half3 norm2 = UnpackNormal(tex2D(_Normal2, IN.uv_Splat0));
			half3 norm3 = UnpackNormal(tex2D(_Normal3, IN.uv_Splat0));

			o.Normal = norm0 * c.r + norm1 * c.g + norm2 * c.b + norm3 * c.a;
		}
		ENDCG
	}
		Fallback "Diffuse"
}