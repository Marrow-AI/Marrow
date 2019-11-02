Shader "Custom/DiscardColor"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
		_TransparentColor("Transparent Color", Color) = (1,1,1,1)
		_Threshold("Threshhold", Range(0,1)) = 0.1
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
		fixed4 _TransparentColor;
		half _Threshold;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;

			// Calculate the difference between the texture color and the transparent color
			// Note: we use 'dot' instead of length(transparent_diff) as its faster, and
			// Although it'll really give the length squared, its good enough for our purposes!
			half3 transparent_diff = c.xyz - _TransparentColor.xyz;
			half transparent_diff_squared = dot(transparent_diff, transparent_diff);

			// If colour is too close to the transparent one, discard it.
			//note: you could do cleverer things like fade out the alpha
			if (transparent_diff_squared < _Threshold)
				discard;

			// Output albedo and alpha just like a normal shader
			o.Albedo = c.rgb;
			o.Alpha = c.a;

			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
