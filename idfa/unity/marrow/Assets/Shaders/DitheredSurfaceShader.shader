Shader "Custom/DitheredSurfaceShader" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_NoiseIntensity("Noise Intensity", Range(0, 5)) = 0.0
		_NoiseScale("Noise Scale", Range(0, 1000)) = 0.0

	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows finalcolor:dithercolor

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
			float4 screenPos;
			float _Time;
			float4 _ShadowCoord;
		};

		float _NoiseIntensity;
		float _NoiseScale;
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		float3 ScreenSpaceDither(float2 vScreenPos, float time)
		{
			// Iestyn's RGB dither (7 asm instructions) from Portal 2 X360, slightly modified for VR
			float3 vDither = dot(float2(171.0, 231.0), vScreenPos.xy + time).xxx;
			vDither.rgb = frac(vDither.rgb / float3(103.0, 71.0, 97.0)) - float3(0.5, 0.5, 0.5);
			return (vDither.rgb / 255.0) * _NoiseIntensity;
		}

		void dithercolor(Input IN, SurfaceOutputStandard o, inout fixed4 color)
		{
			float2 wcoord = (IN.screenPos.xy / IN.screenPos.w) * _NoiseScale;
			color.rgb += ScreenSpaceDither(wcoord, IN._Time);
		}

		void surf (Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
