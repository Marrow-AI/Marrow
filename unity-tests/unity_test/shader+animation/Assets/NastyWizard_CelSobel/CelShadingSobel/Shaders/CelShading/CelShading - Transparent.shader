Shader "Nasty/CelShading - Transparent" 
{
	//////////////////////////////////////////////////////////////
	//														   	//
	//			This shader was written by Casey MacNeil		//	   
	//														   	//
	//////////////////////////////////////////////////////////////
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_NormalMap("Normal Map", 2D) = "bump" {}
		_Ambient("Ambient Strength", Range(0,1)) = 0.1

		[Space(25)][Toggle]_Specular("Use Specular", Float) = 0
		_SpecularMap("Specular", 2D) = "white" {}
		[IntRange] _Gloss("Specular Intensity", Range(0, 256)) = 0

		[Space(25)]_EmissiveMap("Emissive Texture", 2D) = "black" {}
		_EmissiveBlend("Emissive Blend", Color) = (1, 1, 1, 1)

		[Space(25)]_ColorBlend("Color", Color) = (1, 1, 1, 1)
	}
	SubShader{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		ZWrite off
		Blend DstColor SrcColor
		AlphaToMask On
		CGPROGRAM
		#include "CelShadingIncludes.cginc"
		#pragma surface surf CelShading fullforwardshadows alpha:fade
		#pragma target 3.0

		half _Specular;
		fixed _Gloss;
		half3 vDir = half3(0,0,0);

		struct Input {
			float2 uv_MainTex;
			float2 uv_NormalMap;
			float2 uv_SpecularMap;
			float2 uv_EmissiveMap;
			float3 viewDir;
			float3 worldPos;
		};

		sampler2D _MainTex;
		sampler2D _NormalMap;
		sampler2D _SpecularMap;
		sampler2D _EmissiveMap;
		half4 _ColorBlend;

		void surf(Input IN, inout SurfaceOutput o) {
			half4 texel = tex2D(_MainTex, IN.uv_MainTex);

			vDir = IN.viewDir;

			o.Gloss = _Gloss;
			o.Specular = _Specular * tex2D(_SpecularMap, IN.uv_SpecularMap).r;
			texel *= _ColorBlend;

			clip(texel.a - 0.1);
			o.Albedo = texel.rgb;
			o.Alpha = texel.a;
			o.Emission = tex2D(_EmissiveMap, IN.uv_EmissiveMap).rgb;
			o.Normal = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap));
		}
		ENDCG
	}
		Fallback "Diffuse"
}