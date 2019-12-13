Shader "Custom/Blend" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Main Albedo (RGB)", 2D) = "white" {}
		_SecTex ("Sec Albedo (RGB)", 2D) = "white" {}

		_Blend ("Blend value", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _SecTex;

		struct Input {
			float2 uv_MainTex;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		half _Blend;

		void surf (Input IN, inout SurfaceOutputStandard o) {
			fixed4 t1 = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			fixed4 t2 = tex2D (_SecTex, IN.uv_MainTex) * _Color;

			fixed4 c = lerp (t1, t2, _Blend);

			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
