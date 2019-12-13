// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "HxVolumetric/Sprites/Diffuse"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
	_Color("Tint", Color) = (1,1,1,1)
		[MaterialToggle] PixelSnap("Pixel snap", Float) = 0
	}

		SubShader
	{
		Tags
	{
		"Queue" = "Transparent"
		"IgnoreProjector" = "True"
		"RenderType" = "Transparent"
		"PreviewType" = "Plane"
		"CanUseSpriteAtlas" = "True"
	}

		Cull Back
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha


		// ------------------------------------------------------------
		// Surface shader code generated out of a CGPROGRAM block:


		// ---- forward rendering base pass:
		Pass{
		Name "FORWARD"
		Tags{ "LightMode" = "ForwardBase" }

		CGPROGRAM
		// compile directives
#pragma vertex vert_surf
#pragma fragment frag_surf
#pragma multi_compile _ PIXELSNAP_ON
#pragma multi_compile_fwdbase
#pragma multi_compile _	VTRANSPARENCY_ON
#include "HLSLSupport.cginc"
#include "UnityShaderVariables.cginc"
#include "Assets/Plugins/HxVolumetricLighting/BuiltIn-Replacement/HxVolumetricCore.cginc"
		// -------- variant for: <when no other keywords are defined>
#if !defined(PIXELSNAP_ON)
		// Surface shader code generated based on:
		// vertex modifier: 'vert'
		// writes to per-pixel normal: no
		// writes to emission: no
		// needs world space reflection vector: no
		// needs world space normal vector: no
		// needs screen space position: no
		// needs world space position: no
		// needs view direction: no
		// needs world space view direction: no
		// needs world space position for lighting: YES
		// needs world space view direction for lighting: no
		// needs world space view direction for lightmaps: no
		// needs vertex color: no
		// needs VFACE: no
		// passes tangent-to-world matrix to pixel shader: no
		// reads from normal: no
		// 1 texcoords actually used
		//   float2 _MainTex
#define UNITY_PASS_FORWARDBASE
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

		// Original surface shader snippet:
#line 24 ""
#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
#endif

		//#pragma surface surf Lambert vertex:vert nofog keepalpha
		//#pragma multi_compile _ PIXELSNAP_ON

		sampler2D _MainTex;
	fixed4 _Color;

	struct Input
	{
		float2 uv_MainTex;
		fixed4 color;
	};

	void vert(inout appdata_full v, out Input o)
	{
#if defined(PIXELSNAP_ON)
		v.vertex = UnityPixelSnap(v.vertex);
#endif

		UNITY_INITIALIZE_OUTPUT(Input, o);
		o.color = v.color * _Color;

	}

	void surf(Input IN, inout SurfaceOutput o)
	{
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
		o.Albedo = c.rgb * c.a;
		o.Alpha = c.a;
	}


	// vertex-to-fragment interpolation data
	// no lightmaps:
#ifdef LIGHTMAP_OFF
	struct v2f_surf {
		float4 pos : SV_POSITION;
		float2 pack0 : TEXCOORD0; // _MainTex
		half3 worldNormal : TEXCOORD1;
		float3 worldPos : TEXCOORD2;
		half4 custompack0 : TEXCOORD3; // color
		float4 projPos : TEXCOORD4;
#if UNITY_SHOULD_SAMPLE_SH
		half3 sh : TEXCOORD5; // SH
#endif
		SHADOW_COORDS(6)
#if SHADER_TARGET >= 30
			float4 lmap : TEXCOORD7;
#endif
	};
#endif
	// with lightmaps:
#ifndef LIGHTMAP_OFF
	struct v2f_surf {
		float4 pos : SV_POSITION;
		float2 pack0 : TEXCOORD0; // _MainTex
		half3 worldNormal : TEXCOORD1;
		float3 worldPos : TEXCOORD2;
		half4 custompack0 : TEXCOORD3; // color
		float4 lmap : TEXCOORD4;
		SHADOW_COORDS(5)
		float4 projPos : TEXCOORD6;
#ifdef DIRLIGHTMAP_COMBINED
			fixed3 tSpace0 : TEXCOORD7;
		fixed3 tSpace1 : TEXCOORD8;
		fixed3 tSpace2 : TEXCOORD9;
#endif
	};
#endif
	float4 _MainTex_ST;

	// vertex shader
	v2f_surf vert_surf(appdata_full v) {
		v2f_surf o;
		UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
		Input customInputData;
		vert(v, customInputData);
		o.custompack0.xyzw = customInputData.color;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
		float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
#if !defined(LIGHTMAP_OFF) && defined(DIRLIGHTMAP_COMBINED)
		fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
		fixed3 worldBinormal = cross(worldNormal, worldTangent) * v.tangent.w;
#endif
#if !defined(LIGHTMAP_OFF) && defined(DIRLIGHTMAP_COMBINED)
		o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
		o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
		o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
#endif
		o.worldPos = worldPos;
		o.worldNormal = worldNormal;
#ifndef DYNAMICLIGHTMAP_OFF
		o.lmap.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
#ifndef LIGHTMAP_OFF
		o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#endif

		// SH/ambient and vertex lights
#ifdef LIGHTMAP_OFF
#if UNITY_SHOULD_SAMPLE_SH
#if UNITY_SAMPLE_FULL_SH_PER_PIXEL
		o.sh = 0;
#elif (SHADER_TARGET < 30)
		o.sh = ShadeSH9(float4(worldNormal,1.0));
#else
		o.sh = ShadeSH3Order(half4(worldNormal, 1.0));
#endif
		// Add approximated illumination from non-important point lights
#ifdef VERTEXLIGHT_ON
		o.sh += Shade4PointLights(
			unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
			unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
			unity_4LightAtten0, worldPos, worldNormal);
#endif
#endif
#endif // LIGHTMAP_OFF

		TRANSFER_SHADOW(o); // pass shadow coordinates to pixel shader
#ifdef VTRANSPARENCY_ON
		o.projPos = ComputeScreenPos(o.pos);
		COMPUTE_EYEDEPTH(o.projPos.z);
#endif
		return o;
	}

	// fragment shader
	fixed4 frag_surf(v2f_surf IN) : SV_Target{
		// prepare and unpack data
		Input surfIN;
	UNITY_INITIALIZE_OUTPUT(Input,surfIN);
	surfIN.uv_MainTex.x = 1.0;
	surfIN.color.x = 1.0;
	surfIN.uv_MainTex = IN.pack0.xy;
	surfIN.color = IN.custompack0.xyzw;
	float3 worldPos = IN.worldPos;
#ifndef USING_DIRECTIONAL_LIGHT
	fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
#else
	fixed3 lightDir = _WorldSpaceLightPos0.xyz;
#endif
#ifdef UNITY_COMPILER_HLSL
	SurfaceOutput o = (SurfaceOutput)0;
#else
	SurfaceOutput o;
#endif
	o.Albedo = 0.0;
	o.Emission = 0.0;
	o.Specular = 0.0;
	o.Alpha = 0.0;
	o.Gloss = 0.0;
	fixed3 normalWorldVertex = fixed3(0,0,1);
	o.Normal = IN.worldNormal;
	normalWorldVertex = IN.worldNormal;

	// call surface function
	surf(surfIN, o);

	// compute lighting & shadowing factor
	UNITY_LIGHT_ATTENUATION(atten, IN, worldPos)
		fixed4 c = 0;

	// Setup lighting environment
	UnityGI gi;
	UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
	gi.indirect.diffuse = 0;
	gi.indirect.specular = 0;
#if !defined(LIGHTMAP_ON)
	gi.light.color = _LightColor0.rgb;
	gi.light.dir = lightDir;
	gi.light.ndotl = LambertTerm(o.Normal, gi.light.dir);
#endif
	// Call GI (lightmaps/SH/reflections) lighting function
	UnityGIInput giInput;
	UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
	giInput.light = gi.light;
	giInput.worldPos = worldPos;
	giInput.atten = atten;
#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
	giInput.lightmapUV = IN.lmap;
#else
	giInput.lightmapUV = 0.0;
#endif
#if UNITY_SHOULD_SAMPLE_SH
	giInput.ambient = IN.sh;
#else
	giInput.ambient.rgb = 0.0;
#endif
	giInput.probeHDR[0] = unity_SpecCube0_HDR;
	giInput.probeHDR[1] = unity_SpecCube1_HDR;
#if UNITY_SPECCUBE_BLENDING || UNITY_SPECCUBE_BOX_PROJECTION
	giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
#endif
#if UNITY_SPECCUBE_BOX_PROJECTION
	giInput.boxMax[0] = unity_SpecCube0_BoxMax;
	giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
	giInput.boxMax[1] = unity_SpecCube1_BoxMax;
	giInput.boxMin[1] = unity_SpecCube1_BoxMin;
	giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
#endif
	LightingLambert_GI(o, giInput, gi);

	// realtime lighting: call lighting function
	c += LightingLambert(o, gi);

#ifdef VTRANSPARENCY_ON
	return  VolumetricTransparencyBase(c, IN.projPos);
#else
	return c;
#endif
	}


#endif

		// -------- variant for: PIXELSNAP_ON 
#if defined(PIXELSNAP_ON)
		// Surface shader code generated based on:
		// vertex modifier: 'vert'
		// writes to per-pixel normal: no
		// writes to emission: no
		// needs world space reflection vector: no
		// needs world space normal vector: no
		// needs screen space position: no
		// needs world space position: no
		// needs view direction: no
		// needs world space view direction: no
		// needs world space position for lighting: YES
		// needs world space view direction for lighting: no
		// needs world space view direction for lightmaps: no
		// needs vertex color: no
		// needs VFACE: no
		// passes tangent-to-world matrix to pixel shader: no
		// reads from normal: no
		// 1 texcoords actually used
		//   float2 _MainTex
#define UNITY_PASS_FORWARDBASE
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

		// Original surface shader snippet:
#line 24 ""
#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
#endif

		//#pragma surface surf Lambert vertex:vert nofog keepalpha
		//#pragma multi_compile _ PIXELSNAP_ON

	sampler2D _MainTex;
	fixed4 _Color;

	struct Input
	{
		float2 uv_MainTex;
		fixed4 color;
	};

	void vert(inout appdata_full v, out Input o)
	{
#if defined(PIXELSNAP_ON)
		v.vertex = UnityPixelSnap(v.vertex);
#endif

		UNITY_INITIALIZE_OUTPUT(Input, o);
		o.color = v.color * _Color;
	}

	void surf(Input IN, inout SurfaceOutput o)
	{
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
		o.Albedo = c.rgb * c.a;
		o.Alpha = c.a;
	}


	// vertex-to-fragment interpolation data
	// no lightmaps:
#ifdef LIGHTMAP_OFF
	struct v2f_surf {
		float4 pos : SV_POSITION;
		float2 pack0 : TEXCOORD0; // _MainTex
		half3 worldNormal : TEXCOORD1;
		float3 worldPos : TEXCOORD2;
		half4 custompack0 : TEXCOORD3; // color
#if UNITY_SHOULD_SAMPLE_SH
		half3 sh : TEXCOORD4; // SH
#endif
		SHADOW_COORDS(5)
#if SHADER_TARGET >= 30
			float4 lmap : TEXCOORD6;
#endif
	};
#endif
	// with lightmaps:
#ifndef LIGHTMAP_OFF
	struct v2f_surf {
		float4 pos : SV_POSITION;
		float2 pack0 : TEXCOORD0; // _MainTex
		half3 worldNormal : TEXCOORD1;
		float3 worldPos : TEXCOORD2;
		half4 custompack0 : TEXCOORD3; // color
		float4 lmap : TEXCOORD4;
		SHADOW_COORDS(5)
#ifdef DIRLIGHTMAP_COMBINED
			fixed3 tSpace0 : TEXCOORD6;
		fixed3 tSpace1 : TEXCOORD7;
		fixed3 tSpace2 : TEXCOORD8;
#endif
	};
#endif
	float4 _MainTex_ST;

	// vertex shader
	v2f_surf vert_surf(appdata_full v) {
		v2f_surf o;
		UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
		Input customInputData;
		vert(v, customInputData);
		o.custompack0.xyzw = customInputData.color;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
		float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
#if !defined(LIGHTMAP_OFF) && defined(DIRLIGHTMAP_COMBINED)
		fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
		fixed3 worldBinormal = cross(worldNormal, worldTangent) * v.tangent.w;
#endif
#if !defined(LIGHTMAP_OFF) && defined(DIRLIGHTMAP_COMBINED)
		o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
		o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
		o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
#endif
		o.worldPos = worldPos;
		o.worldNormal = worldNormal;
#ifndef DYNAMICLIGHTMAP_OFF
		o.lmap.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
#ifndef LIGHTMAP_OFF
		o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#endif

		// SH/ambient and vertex lights
#ifdef LIGHTMAP_OFF
#if UNITY_SHOULD_SAMPLE_SH
#if UNITY_SAMPLE_FULL_SH_PER_PIXEL
		o.sh = 0;
#elif (SHADER_TARGET < 30)
		o.sh = ShadeSH9(float4(worldNormal,1.0));
#else
		o.sh = ShadeSH3Order(half4(worldNormal, 1.0));
#endif
		// Add approximated illumination from non-important point lights
#ifdef VERTEXLIGHT_ON
		o.sh += Shade4PointLights(
			unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
			unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
			unity_4LightAtten0, worldPos, worldNormal);
#endif
#endif
#endif // LIGHTMAP_OFF

		TRANSFER_SHADOW(o); // pass shadow coordinates to pixel shader
		return o;
	}

	// fragment shader
	fixed4 frag_surf(v2f_surf IN) : SV_Target{
		// prepare and unpack data
		Input surfIN;
	UNITY_INITIALIZE_OUTPUT(Input,surfIN);
	surfIN.uv_MainTex.x = 1.0;
	surfIN.color.x = 1.0;
	surfIN.uv_MainTex = IN.pack0.xy;
	surfIN.color = IN.custompack0.xyzw;
	float3 worldPos = IN.worldPos;
#ifndef USING_DIRECTIONAL_LIGHT
	fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
#else
	fixed3 lightDir = _WorldSpaceLightPos0.xyz;
#endif
#ifdef UNITY_COMPILER_HLSL
	SurfaceOutput o = (SurfaceOutput)0;
#else
	SurfaceOutput o;
#endif
	o.Albedo = 0.0;
	o.Emission = 0.0;
	o.Specular = 0.0;
	o.Alpha = 0.0;
	o.Gloss = 0.0;
	fixed3 normalWorldVertex = fixed3(0,0,1);
	o.Normal = IN.worldNormal;
	normalWorldVertex = IN.worldNormal;

	// call surface function
	surf(surfIN, o);

	// compute lighting & shadowing factor
	UNITY_LIGHT_ATTENUATION(atten, IN, worldPos)
		fixed4 c = 0;

	// Setup lighting environment
	UnityGI gi;
	UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
	gi.indirect.diffuse = 0;
	gi.indirect.specular = 0;
#if !defined(LIGHTMAP_ON)
	gi.light.color = _LightColor0.rgb;
	gi.light.dir = lightDir;
	gi.light.ndotl = LambertTerm(o.Normal, gi.light.dir);
#endif
	// Call GI (lightmaps/SH/reflections) lighting function
	UnityGIInput giInput;
	UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
	giInput.light = gi.light;
	giInput.worldPos = worldPos;
	giInput.atten = atten;
#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
	giInput.lightmapUV = IN.lmap;
#else
	giInput.lightmapUV = 0.0;
#endif
#if UNITY_SHOULD_SAMPLE_SH
	giInput.ambient = IN.sh;
#else
	giInput.ambient.rgb = 0.0;
#endif
	giInput.probeHDR[0] = unity_SpecCube0_HDR;
	giInput.probeHDR[1] = unity_SpecCube1_HDR;
#if UNITY_SPECCUBE_BLENDING || UNITY_SPECCUBE_BOX_PROJECTION
	giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
#endif
#if UNITY_SPECCUBE_BOX_PROJECTION
	giInput.boxMax[0] = unity_SpecCube0_BoxMax;
	giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
	giInput.boxMax[1] = unity_SpecCube1_BoxMax;
	giInput.boxMin[1] = unity_SpecCube1_BoxMin;
	giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
#endif
	LightingLambert_GI(o, giInput, gi);

	// realtime lighting: call lighting function
	c += LightingLambert(o, gi);
	return c;
	}


#endif


		ENDCG

	}

		// ---- forward rendering additive lights pass:
		Pass{
		Name "FORWARD"
		Tags{ "LightMode" = "ForwardAdd" }
		ZWrite Off Blend One One

		CGPROGRAM
		// compile directives
#pragma vertex vert_surf
#pragma fragment frag_surf
#pragma multi_compile _ PIXELSNAP_ON
#pragma multi_compile_fwdadd
#include "HLSLSupport.cginc"
#include "UnityShaderVariables.cginc"
		// -------- variant for: <when no other keywords are defined>
#if !defined(PIXELSNAP_ON)
		// Surface shader code generated based on:
		// vertex modifier: 'vert'
		// writes to per-pixel normal: no
		// writes to emission: no
		// needs world space reflection vector: no
		// needs world space normal vector: no
		// needs screen space position: no
		// needs world space position: no
		// needs view direction: no
		// needs world space view direction: no
		// needs world space position for lighting: YES
		// needs world space view direction for lighting: no
		// needs world space view direction for lightmaps: no
		// needs vertex color: no
		// needs VFACE: no
		// passes tangent-to-world matrix to pixel shader: no
		// reads from normal: no
		// 1 texcoords actually used
		//   float2 _MainTex
#define UNITY_PASS_FORWARDADD
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

		// Original surface shader snippet:
#line 24 ""
#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
#endif

		//#pragma surface surf Lambert vertex:vert nofog keepalpha
		//#pragma multi_compile _ PIXELSNAP_ON

		sampler2D _MainTex;
	fixed4 _Color;

	struct Input
	{
		float2 uv_MainTex;
		fixed4 color;
	};

	void vert(inout appdata_full v, out Input o)
	{
#if defined(PIXELSNAP_ON)
		v.vertex = UnityPixelSnap(v.vertex);
#endif

		UNITY_INITIALIZE_OUTPUT(Input, o);
		o.color = v.color * _Color;
	}

	void surf(Input IN, inout SurfaceOutput o)
	{
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
		o.Albedo = c.rgb * c.a;
		o.Alpha = c.a;
	}


	// vertex-to-fragment interpolation data
	struct v2f_surf {
		float4 pos : SV_POSITION;
		float2 pack0 : TEXCOORD0; // _MainTex
		half3 worldNormal : TEXCOORD1;
		float3 worldPos : TEXCOORD2;
		half4 custompack0 : TEXCOORD3; // color
		SHADOW_COORDS(4)
	};
	float4 _MainTex_ST;

	// vertex shader
	v2f_surf vert_surf(appdata_full v) {
		v2f_surf o;
		UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
		Input customInputData;
		vert(v, customInputData);
		o.custompack0.xyzw = customInputData.color;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
		float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
		o.worldPos = worldPos;
		o.worldNormal = worldNormal;

		TRANSFER_SHADOW(o); // pass shadow coordinates to pixel shader
		return o;
	}

	// fragment shader
	fixed4 frag_surf(v2f_surf IN) : SV_Target{
		// prepare and unpack data
		Input surfIN;
	UNITY_INITIALIZE_OUTPUT(Input,surfIN);
	surfIN.uv_MainTex.x = 1.0;
	surfIN.color.x = 1.0;
	surfIN.uv_MainTex = IN.pack0.xy;
	surfIN.color = IN.custompack0.xyzw;
	float3 worldPos = IN.worldPos;
#ifndef USING_DIRECTIONAL_LIGHT
	fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
#else
	fixed3 lightDir = _WorldSpaceLightPos0.xyz;
#endif
#ifdef UNITY_COMPILER_HLSL
	SurfaceOutput o = (SurfaceOutput)0;
#else
	SurfaceOutput o;
#endif
	o.Albedo = 0.0;
	o.Emission = 0.0;
	o.Specular = 0.0;
	o.Alpha = 0.0;
	o.Gloss = 0.0;
	fixed3 normalWorldVertex = fixed3(0,0,1);
	o.Normal = IN.worldNormal;
	normalWorldVertex = IN.worldNormal;

	// call surface function
	surf(surfIN, o);
	UNITY_LIGHT_ATTENUATION(atten, IN, worldPos)
		fixed4 c = 0;

	// Setup lighting environment
	UnityGI gi;
	UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
	gi.indirect.diffuse = 0;
	gi.indirect.specular = 0;
#if !defined(LIGHTMAP_ON)
	gi.light.color = _LightColor0.rgb;
	gi.light.dir = lightDir;
	gi.light.ndotl = LambertTerm(o.Normal, gi.light.dir);
#endif
	gi.light.color *= atten;
	c += LightingLambert(o, gi);
	return c;
	}


#endif

		// -------- variant for: PIXELSNAP_ON 
#if defined(PIXELSNAP_ON)
		// Surface shader code generated based on:
		// vertex modifier: 'vert'
		// writes to per-pixel normal: no
		// writes to emission: no
		// needs world space reflection vector: no
		// needs world space normal vector: no
		// needs screen space position: no
		// needs world space position: no
		// needs view direction: no
		// needs world space view direction: no
		// needs world space position for lighting: YES
		// needs world space view direction for lighting: no
		// needs world space view direction for lightmaps: no
		// needs vertex color: no
		// needs VFACE: no
		// passes tangent-to-world matrix to pixel shader: no
		// reads from normal: no
		// 1 texcoords actually used
		//   float2 _MainTex
#define UNITY_PASS_FORWARDADD
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

		// Original surface shader snippet:
#line 24 ""
#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
#endif

		//#pragma surface surf Lambert vertex:vert nofog keepalpha
		//#pragma multi_compile _ PIXELSNAP_ON

	sampler2D _MainTex;
	fixed4 _Color;

	struct Input
	{
		float2 uv_MainTex;
		fixed4 color;
	};

	void vert(inout appdata_full v, out Input o)
	{
#if defined(PIXELSNAP_ON)
		v.vertex = UnityPixelSnap(v.vertex);
#endif

		UNITY_INITIALIZE_OUTPUT(Input, o);
		o.color = v.color * _Color;
	}

	void surf(Input IN, inout SurfaceOutput o)
	{
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
		o.Albedo = c.rgb * c.a;
		o.Alpha = c.a;
	}


	// vertex-to-fragment interpolation data
	struct v2f_surf {
		float4 pos : SV_POSITION;
		float2 pack0 : TEXCOORD0; // _MainTex
		half3 worldNormal : TEXCOORD1;
		float3 worldPos : TEXCOORD2;
		half4 custompack0 : TEXCOORD3; // color
		SHADOW_COORDS(4)
	};
	float4 _MainTex_ST;

	// vertex shader
	v2f_surf vert_surf(appdata_full v) {
		v2f_surf o;
		UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
		Input customInputData;
		vert(v, customInputData);
		o.custompack0.xyzw = customInputData.color;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
		float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
		o.worldPos = worldPos;
		o.worldNormal = worldNormal;

		TRANSFER_SHADOW(o); // pass shadow coordinates to pixel shader
		return o;
	}

	// fragment shader
	fixed4 frag_surf(v2f_surf IN) : SV_Target{
		// prepare and unpack data
		Input surfIN;
	UNITY_INITIALIZE_OUTPUT(Input,surfIN);
	surfIN.uv_MainTex.x = 1.0;
	surfIN.color.x = 1.0;
	surfIN.uv_MainTex = IN.pack0.xy;
	surfIN.color = IN.custompack0.xyzw;
	float3 worldPos = IN.worldPos;
#ifndef USING_DIRECTIONAL_LIGHT
	fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
#else
	fixed3 lightDir = _WorldSpaceLightPos0.xyz;
#endif
#ifdef UNITY_COMPILER_HLSL
	SurfaceOutput o = (SurfaceOutput)0;
#else
	SurfaceOutput o;
#endif
	o.Albedo = 0.0;
	o.Emission = 0.0;
	o.Specular = 0.0;
	o.Alpha = 0.0;
	o.Gloss = 0.0;
	fixed3 normalWorldVertex = fixed3(0,0,1);
	o.Normal = IN.worldNormal;
	normalWorldVertex = IN.worldNormal;

	// call surface function
	surf(surfIN, o);
	UNITY_LIGHT_ATTENUATION(atten, IN, worldPos)
		fixed4 c = 0;

	// Setup lighting environment
	UnityGI gi;
	UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
	gi.indirect.diffuse = 0;
	gi.indirect.specular = 0;
#if !defined(LIGHTMAP_ON)
	gi.light.color = _LightColor0.rgb;
	gi.light.dir = lightDir;
	gi.light.ndotl = LambertTerm(o.Normal, gi.light.dir);
#endif
	gi.light.color *= atten;
	c += LightingLambert(o, gi);
	return c;
	}


#endif


		ENDCG

	}

		// ---- deferred lighting base geometry pass:
		Pass{
		Name "PREPASS"
		Tags{ "LightMode" = "PrePassBase" }

		CGPROGRAM
		// compile directives
#pragma vertex vert_surf
#pragma fragment frag_surf
#pragma multi_compile _ PIXELSNAP_ON

#include "HLSLSupport.cginc"
#include "UnityShaderVariables.cginc"
		// -------- variant for: <when no other keywords are defined>
#if !defined(PIXELSNAP_ON)
		// Surface shader code generated based on:
		// vertex modifier: 'vert'
		// writes to per-pixel normal: no
		// writes to emission: no
		// needs world space reflection vector: no
		// needs world space normal vector: no
		// needs screen space position: no
		// needs world space position: no
		// needs view direction: no
		// needs world space view direction: no
		// needs world space position for lighting: YES
		// needs world space view direction for lighting: no
		// needs world space view direction for lightmaps: no
		// needs vertex color: no
		// needs VFACE: no
		// passes tangent-to-world matrix to pixel shader: no
		// reads from normal: YES
		// 0 texcoords actually used
#define UNITY_PASS_PREPASSBASE
#include "UnityCG.cginc"
#include "Lighting.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

		// Original surface shader snippet:
#line 24 ""
#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
#endif

		//#pragma surface surf Lambert vertex:vert nofog keepalpha
		//#pragma multi_compile _ PIXELSNAP_ON

		sampler2D _MainTex;
	fixed4 _Color;

	struct Input
	{
		float2 uv_MainTex;
		fixed4 color;
	};

	void vert(inout appdata_full v, out Input o)
	{
#if defined(PIXELSNAP_ON)
		v.vertex = UnityPixelSnap(v.vertex);
#endif

		UNITY_INITIALIZE_OUTPUT(Input, o);
		o.color = v.color * _Color;
	}

	void surf(Input IN, inout SurfaceOutput o)
	{
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
		o.Albedo = c.rgb * c.a;
		o.Alpha = c.a;
	}


	// vertex-to-fragment interpolation data
	struct v2f_surf {
		float4 pos : SV_POSITION;
		half3 worldNormal : TEXCOORD0;
		float3 worldPos : TEXCOORD1;
		half4 custompack0 : TEXCOORD2; // color
	};

	// vertex shader
	v2f_surf vert_surf(appdata_full v) {
		v2f_surf o;
		UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
		Input customInputData;
		vert(v, customInputData);
		o.custompack0.xyzw = customInputData.color;
		o.pos = UnityObjectToClipPos(v.vertex);
		float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
		o.worldPos = worldPos;
		o.worldNormal = worldNormal;
		return o;
	}

	// fragment shader
	fixed4 frag_surf(v2f_surf IN) : SV_Target{
		// prepare and unpack data
		Input surfIN;
	UNITY_INITIALIZE_OUTPUT(Input,surfIN);
	surfIN.uv_MainTex.x = 1.0;
	surfIN.color.x = 1.0;
	surfIN.color = IN.custompack0.xyzw;
	float3 worldPos = IN.worldPos;
#ifndef USING_DIRECTIONAL_LIGHT
	fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
#else
	fixed3 lightDir = _WorldSpaceLightPos0.xyz;
#endif
#ifdef UNITY_COMPILER_HLSL
	SurfaceOutput o = (SurfaceOutput)0;
#else
	SurfaceOutput o;
#endif
	o.Albedo = 0.0;
	o.Emission = 0.0;
	o.Specular = 0.0;
	o.Alpha = 0.0;
	o.Gloss = 0.0;
	fixed3 normalWorldVertex = fixed3(0,0,1);
	o.Normal = IN.worldNormal;
	normalWorldVertex = IN.worldNormal;

	// call surface function
	surf(surfIN, o);

	// output normal and specular
	fixed4 res;
	res.rgb = o.Normal * 0.5 + 0.5;
	res.a = o.Specular;
	return res;
	}


#endif

		// -------- variant for: PIXELSNAP_ON 
#if defined(PIXELSNAP_ON)
		// Surface shader code generated based on:
		// vertex modifier: 'vert'
		// writes to per-pixel normal: no
		// writes to emission: no
		// needs world space reflection vector: no
		// needs world space normal vector: no
		// needs screen space position: no
		// needs world space position: no
		// needs view direction: no
		// needs world space view direction: no
		// needs world space position for lighting: YES
		// needs world space view direction for lighting: no
		// needs world space view direction for lightmaps: no
		// needs vertex color: no
		// needs VFACE: no
		// passes tangent-to-world matrix to pixel shader: no
		// reads from normal: YES
		// 0 texcoords actually used
#define UNITY_PASS_PREPASSBASE
#include "UnityCG.cginc"
#include "Lighting.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

		// Original surface shader snippet:
#line 24 ""
#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
#endif

		//#pragma surface surf Lambert vertex:vert nofog keepalpha
		//#pragma multi_compile _ PIXELSNAP_ON

	sampler2D _MainTex;
	fixed4 _Color;

	struct Input
	{
		float2 uv_MainTex;
		fixed4 color;
	};

	void vert(inout appdata_full v, out Input o)
	{
#if defined(PIXELSNAP_ON)
		v.vertex = UnityPixelSnap(v.vertex);
#endif

		UNITY_INITIALIZE_OUTPUT(Input, o);
		o.color = v.color * _Color;
	}

	void surf(Input IN, inout SurfaceOutput o)
	{
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
		o.Albedo = c.rgb * c.a;
		o.Alpha = c.a;
	}


	// vertex-to-fragment interpolation data
	struct v2f_surf {
		float4 pos : SV_POSITION;
		half3 worldNormal : TEXCOORD0;
		float3 worldPos : TEXCOORD1;
		half4 custompack0 : TEXCOORD2; // color
	};

	// vertex shader
	v2f_surf vert_surf(appdata_full v) {
		v2f_surf o;
		UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
		Input customInputData;
		vert(v, customInputData);
		o.custompack0.xyzw = customInputData.color;
		o.pos = UnityObjectToClipPos(v.vertex);
		float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
		o.worldPos = worldPos;
		o.worldNormal = worldNormal;
		return o;
	}

	// fragment shader
	fixed4 frag_surf(v2f_surf IN) : SV_Target{
		// prepare and unpack data
		Input surfIN;
	UNITY_INITIALIZE_OUTPUT(Input,surfIN);
	surfIN.uv_MainTex.x = 1.0;
	surfIN.color.x = 1.0;
	surfIN.color = IN.custompack0.xyzw;
	float3 worldPos = IN.worldPos;
#ifndef USING_DIRECTIONAL_LIGHT
	fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
#else
	fixed3 lightDir = _WorldSpaceLightPos0.xyz;
#endif
#ifdef UNITY_COMPILER_HLSL
	SurfaceOutput o = (SurfaceOutput)0;
#else
	SurfaceOutput o;
#endif
	o.Albedo = 0.0;
	o.Emission = 0.0;
	o.Specular = 0.0;
	o.Alpha = 0.0;
	o.Gloss = 0.0;
	fixed3 normalWorldVertex = fixed3(0,0,1);
	o.Normal = IN.worldNormal;
	normalWorldVertex = IN.worldNormal;

	// call surface function
	surf(surfIN, o);

	// output normal and specular
	fixed4 res;
	res.rgb = o.Normal * 0.5 + 0.5;
	res.a = o.Specular;
	return res;
	}


#endif


		ENDCG

	}

		// ---- deferred lighting final pass:
		Pass{
		Name "PREPASS"
		Tags{ "LightMode" = "PrePassFinal" }
		ZWrite Off

		CGPROGRAM
		// compile directives
#pragma vertex vert_surf
#pragma fragment frag_surf
#pragma multi_compile _ PIXELSNAP_ON
#pragma multi_compile_prepassfinal
#include "HLSLSupport.cginc"
#include "UnityShaderVariables.cginc"
		// -------- variant for: <when no other keywords are defined>
#if !defined(PIXELSNAP_ON)
		// Surface shader code generated based on:
		// vertex modifier: 'vert'
		// writes to per-pixel normal: no
		// writes to emission: no
		// needs world space reflection vector: no
		// needs world space normal vector: no
		// needs screen space position: no
		// needs world space position: no
		// needs view direction: no
		// needs world space view direction: no
		// needs world space position for lighting: YES
		// needs world space view direction for lighting: no
		// needs world space view direction for lightmaps: no
		// needs vertex color: no
		// needs VFACE: no
		// passes tangent-to-world matrix to pixel shader: no
		// reads from normal: no
		// 1 texcoords actually used
		//   float2 _MainTex
#define UNITY_PASS_PREPASSFINAL
#include "UnityCG.cginc"
#include "Lighting.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

		// Original surface shader snippet:
#line 24 ""
#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
#endif

		//#pragma surface surf Lambert vertex:vert nofog keepalpha
		//#pragma multi_compile _ PIXELSNAP_ON

		sampler2D _MainTex;
	fixed4 _Color;

	struct Input
	{
		float2 uv_MainTex;
		fixed4 color;
	};

	void vert(inout appdata_full v, out Input o)
	{
#if defined(PIXELSNAP_ON)
		v.vertex = UnityPixelSnap(v.vertex);
#endif

		UNITY_INITIALIZE_OUTPUT(Input, o);
		o.color = v.color * _Color;
	}

	void surf(Input IN, inout SurfaceOutput o)
	{
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
		o.Albedo = c.rgb * c.a;
		o.Alpha = c.a;
	}


	// vertex-to-fragment interpolation data
	struct v2f_surf {
		float4 pos : SV_POSITION;
		float2 pack0 : TEXCOORD0; // _MainTex
		float3 worldPos : TEXCOORD1;
		half4 custompack0 : TEXCOORD2; // color
		float4 screen : TEXCOORD3;
		float4 lmap : TEXCOORD4;
#ifdef LIGHTMAP_OFF
		float3 vlight : TEXCOORD5;
#else
#ifdef DIRLIGHTMAP_OFF
		float4 lmapFadePos : TEXCOORD5;
#endif
#endif
#if !defined(LIGHTMAP_OFF) && defined(DIRLIGHTMAP_COMBINED)
		fixed3 tSpace0 : TEXCOORD6;
		fixed3 tSpace1 : TEXCOORD7;
		fixed3 tSpace2 : TEXCOORD8;
#endif
	};
	float4 _MainTex_ST;

	// vertex shader
	v2f_surf vert_surf(appdata_full v) {
		v2f_surf o;
		UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
		Input customInputData;
		vert(v, customInputData);
		o.custompack0.xyzw = customInputData.color;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
		float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
#if !defined(LIGHTMAP_OFF) && defined(DIRLIGHTMAP_COMBINED)
		fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
		fixed3 worldBinormal = cross(worldNormal, worldTangent) * v.tangent.w;
#endif
#if !defined(LIGHTMAP_OFF) && defined(DIRLIGHTMAP_COMBINED)
		o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
		o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
		o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
#endif
		o.worldPos = worldPos;
		o.screen = ComputeScreenPos(o.pos);
#ifndef DYNAMICLIGHTMAP_OFF
		o.lmap.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#else
		o.lmap.zw = 0;
#endif
#ifndef LIGHTMAP_OFF
		o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#ifdef DIRLIGHTMAP_OFF
		o.lmapFadePos.xyz = (mul(unity_ObjectToWorld, v.vertex).xyz - unity_ShadowFadeCenterAndType.xyz) * unity_ShadowFadeCenterAndType.w;
		o.lmapFadePos.w = (-mul(UNITY_MATRIX_MV, v.vertex).z) * (1.0 - unity_ShadowFadeCenterAndType.w);
#endif
#else
		o.lmap.xy = 0;
		float3 worldN = UnityObjectToWorldNormal(v.normal);
		o.vlight = ShadeSH9(float4(worldN,1.0));
#endif
		return o;
	}
	sampler2D _LightBuffer;
#if defined (SHADER_API_XBOX360) && defined (UNITY_HDR_ON)
	sampler2D _LightSpecBuffer;
#endif
#ifdef LIGHTMAP_ON
	float4 unity_LightmapFade;
#endif
	fixed4 unity_Ambient;

	// fragment shader
	fixed4 frag_surf(v2f_surf IN) : SV_Target{
		// prepare and unpack data
		Input surfIN;
	UNITY_INITIALIZE_OUTPUT(Input,surfIN);
	surfIN.uv_MainTex.x = 1.0;
	surfIN.color.x = 1.0;
	surfIN.uv_MainTex = IN.pack0.xy;
	surfIN.color = IN.custompack0.xyzw;
	float3 worldPos = IN.worldPos;
#ifndef USING_DIRECTIONAL_LIGHT
	fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
#else
	fixed3 lightDir = _WorldSpaceLightPos0.xyz;
#endif
#ifdef UNITY_COMPILER_HLSL
	SurfaceOutput o = (SurfaceOutput)0;
#else
	SurfaceOutput o;
#endif
	o.Albedo = 0.0;
	o.Emission = 0.0;
	o.Specular = 0.0;
	o.Alpha = 0.0;
	o.Gloss = 0.0;
	fixed3 normalWorldVertex = fixed3(0,0,1);

	// call surface function
	surf(surfIN, o);
	half4 light = tex2Dproj(_LightBuffer, UNITY_PROJ_COORD(IN.screen));
#if defined (SHADER_API_MOBILE)
	light = max(light, half4(0.001, 0.001, 0.001, 0.001));
#endif
#ifndef UNITY_HDR_ON
	light = -log2(light);
#endif
#if defined (SHADER_API_XBOX360) && defined (UNITY_HDR_ON)
	light.w = tex2Dproj(_LightSpecBuffer, UNITY_PROJ_COORD(IN.screen)).r;
#endif
#ifndef LIGHTMAP_OFF
#ifdef DIRLIGHTMAP_OFF
	// single lightmap
	fixed4 lmtex = UNITY_SAMPLE_TEX2D(unity_Lightmap, IN.lmap.xy);
	fixed3 lm = DecodeLightmap(lmtex);
	light.rgb += lm;
#elif DIRLIGHTMAP_COMBINED
	// directional lightmaps
	fixed4 lmtex = UNITY_SAMPLE_TEX2D(unity_Lightmap, IN.lmap.xy);
	half4 lm = half4(DecodeLightmap(lmtex), 0);
	light += lm;
#elif DIRLIGHTMAP_SEPARATE
	// directional with specular - no support
#endif // DIRLIGHTMAP_OFF
#else
	light.rgb += IN.vlight;
#endif // !LIGHTMAP_OFF

#ifndef DYNAMICLIGHTMAP_OFF
	fixed4 dynlmtex = UNITY_SAMPLE_TEX2D(unity_DynamicLightmap, IN.lmap.zw);
	light.rgb += DecodeRealtimeLightmap(dynlmtex);
#endif

	half4 c = LightingLambert_PrePass(o, light);
	return c;
	}


#endif

		// -------- variant for: PIXELSNAP_ON 
#if defined(PIXELSNAP_ON)
		// Surface shader code generated based on:
		// vertex modifier: 'vert'
		// writes to per-pixel normal: no
		// writes to emission: no
		// needs world space reflection vector: no
		// needs world space normal vector: no
		// needs screen space position: no
		// needs world space position: no
		// needs view direction: no
		// needs world space view direction: no
		// needs world space position for lighting: YES
		// needs world space view direction for lighting: no
		// needs world space view direction for lightmaps: no
		// needs vertex color: no
		// needs VFACE: no
		// passes tangent-to-world matrix to pixel shader: no
		// reads from normal: no
		// 1 texcoords actually used
		//   float2 _MainTex
#define UNITY_PASS_PREPASSFINAL
#include "UnityCG.cginc"
#include "Lighting.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

		// Original surface shader snippet:
#line 24 ""
#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
#endif

		//#pragma surface surf Lambert vertex:vert nofog keepalpha
		//#pragma multi_compile _ PIXELSNAP_ON

	sampler2D _MainTex;
	fixed4 _Color;

	struct Input
	{
		float2 uv_MainTex;
		fixed4 color;
	};

	void vert(inout appdata_full v, out Input o)
	{
#if defined(PIXELSNAP_ON)
		v.vertex = UnityPixelSnap(v.vertex);
#endif

		UNITY_INITIALIZE_OUTPUT(Input, o);
		o.color = v.color * _Color;
	}

	void surf(Input IN, inout SurfaceOutput o)
	{
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
		o.Albedo = c.rgb * c.a;
		o.Alpha = c.a;
	}


	// vertex-to-fragment interpolation data
	struct v2f_surf {
		float4 pos : SV_POSITION;
		float2 pack0 : TEXCOORD0; // _MainTex
		float3 worldPos : TEXCOORD1;
		half4 custompack0 : TEXCOORD2; // color
		float4 screen : TEXCOORD3;
		float4 lmap : TEXCOORD4;
#ifdef LIGHTMAP_OFF
		float3 vlight : TEXCOORD5;
#else
#ifdef DIRLIGHTMAP_OFF
		float4 lmapFadePos : TEXCOORD5;
#endif
#endif
#if !defined(LIGHTMAP_OFF) && defined(DIRLIGHTMAP_COMBINED)
		fixed3 tSpace0 : TEXCOORD6;
		fixed3 tSpace1 : TEXCOORD7;
		fixed3 tSpace2 : TEXCOORD8;
#endif
	};
	float4 _MainTex_ST;

	// vertex shader
	v2f_surf vert_surf(appdata_full v) {
		v2f_surf o;
		UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
		Input customInputData;
		vert(v, customInputData);
		o.custompack0.xyzw = customInputData.color;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
		float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
#if !defined(LIGHTMAP_OFF) && defined(DIRLIGHTMAP_COMBINED)
		fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
		fixed3 worldBinormal = cross(worldNormal, worldTangent) * v.tangent.w;
#endif
#if !defined(LIGHTMAP_OFF) && defined(DIRLIGHTMAP_COMBINED)
		o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
		o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
		o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
#endif
		o.worldPos = worldPos;
		o.screen = ComputeScreenPos(o.pos);
#ifndef DYNAMICLIGHTMAP_OFF
		o.lmap.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#else
		o.lmap.zw = 0;
#endif
#ifndef LIGHTMAP_OFF
		o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#ifdef DIRLIGHTMAP_OFF
		o.lmapFadePos.xyz = (mul(unity_ObjectToWorld, v.vertex).xyz - unity_ShadowFadeCenterAndType.xyz) * unity_ShadowFadeCenterAndType.w;
		o.lmapFadePos.w = (-mul(UNITY_MATRIX_MV, v.vertex).z) * (1.0 - unity_ShadowFadeCenterAndType.w);
#endif
#else
		o.lmap.xy = 0;
		float3 worldN = UnityObjectToWorldNormal(v.normal);
		o.vlight = ShadeSH9(float4(worldN,1.0));
#endif
		return o;
	}
	sampler2D _LightBuffer;
#if defined (SHADER_API_XBOX360) && defined (UNITY_HDR_ON)
	sampler2D _LightSpecBuffer;
#endif
#ifdef LIGHTMAP_ON
	float4 unity_LightmapFade;
#endif
	fixed4 unity_Ambient;

	// fragment shader
	fixed4 frag_surf(v2f_surf IN) : SV_Target{
		// prepare and unpack data
		Input surfIN;
	UNITY_INITIALIZE_OUTPUT(Input,surfIN);
	surfIN.uv_MainTex.x = 1.0;
	surfIN.color.x = 1.0;
	surfIN.uv_MainTex = IN.pack0.xy;
	surfIN.color = IN.custompack0.xyzw;
	float3 worldPos = IN.worldPos;
#ifndef USING_DIRECTIONAL_LIGHT
	fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
#else
	fixed3 lightDir = _WorldSpaceLightPos0.xyz;
#endif
#ifdef UNITY_COMPILER_HLSL
	SurfaceOutput o = (SurfaceOutput)0;
#else
	SurfaceOutput o;
#endif
	o.Albedo = 0.0;
	o.Emission = 0.0;
	o.Specular = 0.0;
	o.Alpha = 0.0;
	o.Gloss = 0.0;
	fixed3 normalWorldVertex = fixed3(0,0,1);

	// call surface function
	surf(surfIN, o);
	half4 light = tex2Dproj(_LightBuffer, UNITY_PROJ_COORD(IN.screen));
#if defined (SHADER_API_MOBILE)
	light = max(light, half4(0.001, 0.001, 0.001, 0.001));
#endif
#ifndef UNITY_HDR_ON
	light = -log2(light);
#endif
#if defined (SHADER_API_XBOX360) && defined (UNITY_HDR_ON)
	light.w = tex2Dproj(_LightSpecBuffer, UNITY_PROJ_COORD(IN.screen)).r;
#endif
#ifndef LIGHTMAP_OFF
#ifdef DIRLIGHTMAP_OFF
	// single lightmap
	fixed4 lmtex = UNITY_SAMPLE_TEX2D(unity_Lightmap, IN.lmap.xy);
	fixed3 lm = DecodeLightmap(lmtex);
	light.rgb += lm;
#elif DIRLIGHTMAP_COMBINED
	// directional lightmaps
	fixed4 lmtex = UNITY_SAMPLE_TEX2D(unity_Lightmap, IN.lmap.xy);
	half4 lm = half4(DecodeLightmap(lmtex), 0);
	light += lm;
#elif DIRLIGHTMAP_SEPARATE
	// directional with specular - no support
#endif // DIRLIGHTMAP_OFF
#else
	light.rgb += IN.vlight;
#endif // !LIGHTMAP_OFF

#ifndef DYNAMICLIGHTMAP_OFF
	fixed4 dynlmtex = UNITY_SAMPLE_TEX2D(unity_DynamicLightmap, IN.lmap.zw);
	light.rgb += DecodeRealtimeLightmap(dynlmtex);
#endif

	half4 c = LightingLambert_PrePass(o, light);
	return c;
	}


#endif


		ENDCG

	}

		// ---- deferred shading pass:
		Pass{
		Name "DEFERRED"
		Tags{ "LightMode" = "Deferred" }

		CGPROGRAM
		// compile directives
#pragma vertex vert_surf
#pragma fragment frag_surf
#pragma multi_compile _ PIXELSNAP_ON
#pragma exclude_renderers nomrt
#pragma multi_compile_prepassfinal
#include "HLSLSupport.cginc"
#include "UnityShaderVariables.cginc"
		// -------- variant for: <when no other keywords are defined>
#if !defined(PIXELSNAP_ON)
		// Surface shader code generated based on:
		// vertex modifier: 'vert'
		// writes to per-pixel normal: no
		// writes to emission: no
		// needs world space reflection vector: no
		// needs world space normal vector: no
		// needs screen space position: no
		// needs world space position: no
		// needs view direction: no
		// needs world space view direction: no
		// needs world space position for lighting: YES
		// needs world space view direction for lighting: no
		// needs world space view direction for lightmaps: no
		// needs vertex color: no
		// needs VFACE: no
		// passes tangent-to-world matrix to pixel shader: no
		// reads from normal: YES
		// 1 texcoords actually used
		//   float2 _MainTex
#define UNITY_PASS_DEFERRED
#include "UnityCG.cginc"
#include "Lighting.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

		// Original surface shader snippet:
#line 24 ""
#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
#endif

		//#pragma surface surf Lambert vertex:vert nofog keepalpha
		//#pragma multi_compile _ PIXELSNAP_ON

		sampler2D _MainTex;
	fixed4 _Color;

	struct Input
	{
		float2 uv_MainTex;
		fixed4 color;
	};

	void vert(inout appdata_full v, out Input o)
	{
#if defined(PIXELSNAP_ON)
		v.vertex = UnityPixelSnap(v.vertex);
#endif

		UNITY_INITIALIZE_OUTPUT(Input, o);
		o.color = v.color * _Color;
	}

	void surf(Input IN, inout SurfaceOutput o)
	{
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
		o.Albedo = c.rgb * c.a;
		o.Alpha = c.a;
	}


	// vertex-to-fragment interpolation data
	struct v2f_surf {
		float4 pos : SV_POSITION;
		float2 pack0 : TEXCOORD0; // _MainTex
		half3 worldNormal : TEXCOORD1;
		float3 worldPos : TEXCOORD2;
		half4 custompack0 : TEXCOORD3; // color
#ifndef DIRLIGHTMAP_OFF
		half3 viewDir : TEXCOORD4;
#endif
		float4 lmap : TEXCOORD5;
#ifdef LIGHTMAP_OFF
#if UNITY_SHOULD_SAMPLE_SH
		half3 sh : TEXCOORD6; // SH
#endif
#else
#ifdef DIRLIGHTMAP_OFF
		float4 lmapFadePos : TEXCOORD7;
#endif
#endif
	};
	float4 _MainTex_ST;

	// vertex shader
	v2f_surf vert_surf(appdata_full v) {
		v2f_surf o;
		UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
		Input customInputData;
		vert(v, customInputData);
		o.custompack0.xyzw = customInputData.color;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
		float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
		o.worldPos = worldPos;
		o.worldNormal = worldNormal;
		float3 viewDirForLight = UnityWorldSpaceViewDir(worldPos);
#ifndef DIRLIGHTMAP_OFF
		o.viewDir = viewDirForLight;
#endif
#ifndef DYNAMICLIGHTMAP_OFF
		o.lmap.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#else
		o.lmap.zw = 0;
#endif
#ifndef LIGHTMAP_OFF
		o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#ifdef DIRLIGHTMAP_OFF
		o.lmapFadePos.xyz = (mul(unity_ObjectToWorld, v.vertex).xyz - unity_ShadowFadeCenterAndType.xyz) * unity_ShadowFadeCenterAndType.w;
		o.lmapFadePos.w = (-mul(UNITY_MATRIX_MV, v.vertex).z) * (1.0 - unity_ShadowFadeCenterAndType.w);
#endif
#else
		o.lmap.xy = 0;
#if UNITY_SHOULD_SAMPLE_SH
#if UNITY_SAMPLE_FULL_SH_PER_PIXEL
		o.sh = 0;
#elif (SHADER_TARGET < 30)
		o.sh = ShadeSH9(float4(worldNormal,1.0));
#else
		o.sh = ShadeSH3Order(half4(worldNormal, 1.0));
#endif
#endif
#endif
		return o;
	}
#ifdef LIGHTMAP_ON
	float4 unity_LightmapFade;
#endif
	fixed4 unity_Ambient;

	// fragment shader
	void frag_surf(v2f_surf IN,
		out half4 outDiffuse : SV_Target0,
		out half4 outSpecSmoothness : SV_Target1,
		out half4 outNormal : SV_Target2,
		out half4 outEmission : SV_Target3) {
		// prepare and unpack data
		Input surfIN;
		UNITY_INITIALIZE_OUTPUT(Input,surfIN);
		surfIN.uv_MainTex.x = 1.0;
		surfIN.color.x = 1.0;
		surfIN.uv_MainTex = IN.pack0.xy;
		surfIN.color = IN.custompack0.xyzw;
		float3 worldPos = IN.worldPos;
#ifndef USING_DIRECTIONAL_LIGHT
		fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
#else
		fixed3 lightDir = _WorldSpaceLightPos0.xyz;
#endif
#ifdef UNITY_COMPILER_HLSL
		SurfaceOutput o = (SurfaceOutput)0;
#else
		SurfaceOutput o;
#endif
		o.Albedo = 0.0;
		o.Emission = 0.0;
		o.Specular = 0.0;
		o.Alpha = 0.0;
		o.Gloss = 0.0;
		fixed3 normalWorldVertex = fixed3(0,0,1);
		o.Normal = IN.worldNormal;
		normalWorldVertex = IN.worldNormal;

		// call surface function
		surf(surfIN, o);
		fixed3 originalNormal = o.Normal;
		half atten = 1;

		// Setup lighting environment
		UnityGI gi;
		UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
		gi.indirect.diffuse = 0;
		gi.indirect.specular = 0;
		gi.light.color = 0;
		gi.light.dir = half3(0,1,0);
		gi.light.ndotl = LambertTerm(o.Normal, gi.light.dir);
		// Call GI (lightmaps/SH/reflections) lighting function
		UnityGIInput giInput;
		UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
		giInput.light = gi.light;
		giInput.worldPos = worldPos;
		giInput.atten = atten;
#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
		giInput.lightmapUV = IN.lmap;
#else
		giInput.lightmapUV = 0.0;
#endif
#if UNITY_SHOULD_SAMPLE_SH
		giInput.ambient = IN.sh;
#else
		giInput.ambient.rgb = 0.0;
#endif
		giInput.probeHDR[0] = unity_SpecCube0_HDR;
		giInput.probeHDR[1] = unity_SpecCube1_HDR;
#if UNITY_SPECCUBE_BLENDING || UNITY_SPECCUBE_BOX_PROJECTION
		giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
#endif
#if UNITY_SPECCUBE_BOX_PROJECTION
		giInput.boxMax[0] = unity_SpecCube0_BoxMax;
		giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
		giInput.boxMax[1] = unity_SpecCube1_BoxMax;
		giInput.boxMin[1] = unity_SpecCube1_BoxMin;
		giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
#endif
		LightingLambert_GI(o, giInput, gi);

		// call lighting function to output g-buffer
		outEmission = LightingLambert_Deferred(o, gi, outDiffuse, outSpecSmoothness, outNormal);
#ifndef UNITY_HDR_ON
		outEmission.rgb = exp2(-outEmission.rgb);
#endif
	}


#endif

	// -------- variant for: PIXELSNAP_ON 
#if defined(PIXELSNAP_ON)
	// Surface shader code generated based on:
	// vertex modifier: 'vert'
	// writes to per-pixel normal: no
	// writes to emission: no
	// needs world space reflection vector: no
	// needs world space normal vector: no
	// needs screen space position: no
	// needs world space position: no
	// needs view direction: no
	// needs world space view direction: no
	// needs world space position for lighting: YES
	// needs world space view direction for lighting: no
	// needs world space view direction for lightmaps: no
	// needs vertex color: no
	// needs VFACE: no
	// passes tangent-to-world matrix to pixel shader: no
	// reads from normal: YES
	// 1 texcoords actually used
	//   float2 _MainTex
#define UNITY_PASS_DEFERRED
#include "UnityCG.cginc"
#include "Lighting.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

	// Original surface shader snippet:
#line 24 ""
#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
#endif

	//#pragma surface surf Lambert vertex:vert nofog keepalpha
	//#pragma multi_compile _ PIXELSNAP_ON

	sampler2D _MainTex;
	fixed4 _Color;

	struct Input
	{
		float2 uv_MainTex;
		fixed4 color;
	};

	void vert(inout appdata_full v, out Input o)
	{
#if defined(PIXELSNAP_ON)
		v.vertex = UnityPixelSnap(v.vertex);
#endif

		UNITY_INITIALIZE_OUTPUT(Input, o);
		o.color = v.color * _Color;
	}

	void surf(Input IN, inout SurfaceOutput o)
	{
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
		o.Albedo = c.rgb * c.a;
		o.Alpha = c.a;
	}


	// vertex-to-fragment interpolation data
	struct v2f_surf {
		float4 pos : SV_POSITION;
		float2 pack0 : TEXCOORD0; // _MainTex
		half3 worldNormal : TEXCOORD1;
		float3 worldPos : TEXCOORD2;
		half4 custompack0 : TEXCOORD3; // color
#ifndef DIRLIGHTMAP_OFF
		half3 viewDir : TEXCOORD4;
#endif
		float4 lmap : TEXCOORD5;
#ifdef LIGHTMAP_OFF
#if UNITY_SHOULD_SAMPLE_SH
		half3 sh : TEXCOORD6; // SH
#endif
#else
#ifdef DIRLIGHTMAP_OFF
		float4 lmapFadePos : TEXCOORD7;
#endif
#endif
	};
	float4 _MainTex_ST;

	// vertex shader
	v2f_surf vert_surf(appdata_full v) {
		v2f_surf o;
		UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
		Input customInputData;
		vert(v, customInputData);
		o.custompack0.xyzw = customInputData.color;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
		float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
		o.worldPos = worldPos;
		o.worldNormal = worldNormal;
		float3 viewDirForLight = UnityWorldSpaceViewDir(worldPos);
#ifndef DIRLIGHTMAP_OFF
		o.viewDir = viewDirForLight;
#endif
#ifndef DYNAMICLIGHTMAP_OFF
		o.lmap.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#else
		o.lmap.zw = 0;
#endif
#ifndef LIGHTMAP_OFF
		o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#ifdef DIRLIGHTMAP_OFF
		o.lmapFadePos.xyz = (mul(unity_ObjectToWorld, v.vertex).xyz - unity_ShadowFadeCenterAndType.xyz) * unity_ShadowFadeCenterAndType.w;
		o.lmapFadePos.w = (-mul(UNITY_MATRIX_MV, v.vertex).z) * (1.0 - unity_ShadowFadeCenterAndType.w);
#endif
#else
		o.lmap.xy = 0;
#if UNITY_SHOULD_SAMPLE_SH
#if UNITY_SAMPLE_FULL_SH_PER_PIXEL
		o.sh = 0;
#elif (SHADER_TARGET < 30)
		o.sh = ShadeSH9(float4(worldNormal,1.0));
#else
		o.sh = ShadeSH3Order(half4(worldNormal, 1.0));
#endif
#endif
#endif
		return o;
	}
#ifdef LIGHTMAP_ON
	float4 unity_LightmapFade;
#endif
	fixed4 unity_Ambient;

	// fragment shader
	void frag_surf(v2f_surf IN,
		out half4 outDiffuse : SV_Target0,
		out half4 outSpecSmoothness : SV_Target1,
		out half4 outNormal : SV_Target2,
		out half4 outEmission : SV_Target3) {
		// prepare and unpack data
		Input surfIN;
		UNITY_INITIALIZE_OUTPUT(Input,surfIN);
		surfIN.uv_MainTex.x = 1.0;
		surfIN.color.x = 1.0;
		surfIN.uv_MainTex = IN.pack0.xy;
		surfIN.color = IN.custompack0.xyzw;
		float3 worldPos = IN.worldPos;
#ifndef USING_DIRECTIONAL_LIGHT
		fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
#else
		fixed3 lightDir = _WorldSpaceLightPos0.xyz;
#endif
#ifdef UNITY_COMPILER_HLSL
		SurfaceOutput o = (SurfaceOutput)0;
#else
		SurfaceOutput o;
#endif
		o.Albedo = 0.0;
		o.Emission = 0.0;
		o.Specular = 0.0;
		o.Alpha = 0.0;
		o.Gloss = 0.0;
		fixed3 normalWorldVertex = fixed3(0,0,1);
		o.Normal = IN.worldNormal;
		normalWorldVertex = IN.worldNormal;

		// call surface function
		surf(surfIN, o);
		fixed3 originalNormal = o.Normal;
		half atten = 1;

		// Setup lighting environment
		UnityGI gi;
		UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
		gi.indirect.diffuse = 0;
		gi.indirect.specular = 0;
		gi.light.color = 0;
		gi.light.dir = half3(0,1,0);
		gi.light.ndotl = LambertTerm(o.Normal, gi.light.dir);
		// Call GI (lightmaps/SH/reflections) lighting function
		UnityGIInput giInput;
		UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
		giInput.light = gi.light;
		giInput.worldPos = worldPos;
		giInput.atten = atten;
#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
		giInput.lightmapUV = IN.lmap;
#else
		giInput.lightmapUV = 0.0;
#endif
#if UNITY_SHOULD_SAMPLE_SH
		giInput.ambient = IN.sh;
#else
		giInput.ambient.rgb = 0.0;
#endif
		giInput.probeHDR[0] = unity_SpecCube0_HDR;
		giInput.probeHDR[1] = unity_SpecCube1_HDR;
#if UNITY_SPECCUBE_BLENDING || UNITY_SPECCUBE_BOX_PROJECTION
		giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
#endif
#if UNITY_SPECCUBE_BOX_PROJECTION
		giInput.boxMax[0] = unity_SpecCube0_BoxMax;
		giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
		giInput.boxMax[1] = unity_SpecCube1_BoxMax;
		giInput.boxMin[1] = unity_SpecCube1_BoxMin;
		giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
#endif
		LightingLambert_GI(o, giInput, gi);

		// call lighting function to output g-buffer
		outEmission = LightingLambert_Deferred(o, gi, outDiffuse, outSpecSmoothness, outNormal);
#ifndef UNITY_HDR_ON
		outEmission.rgb = exp2(-outEmission.rgb);
#endif
	}


#endif


	ENDCG

	}

		// ---- meta information extraction pass:
		Pass{
		Name "Meta"
		Tags{ "LightMode" = "Meta" }
		Cull Off

		CGPROGRAM
		// compile directives
#pragma vertex vert_surf
#pragma fragment frag_surf
#pragma multi_compile _ PIXELSNAP_ON

#include "HLSLSupport.cginc"
#include "UnityShaderVariables.cginc"
		// -------- variant for: <when no other keywords are defined>
#if !defined(PIXELSNAP_ON)
		// Surface shader code generated based on:
		// vertex modifier: 'vert'
		// writes to per-pixel normal: no
		// writes to emission: no
		// needs world space reflection vector: no
		// needs world space normal vector: no
		// needs screen space position: no
		// needs world space position: no
		// needs view direction: no
		// needs world space view direction: no
		// needs world space position for lighting: YES
		// needs world space view direction for lighting: no
		// needs world space view direction for lightmaps: no
		// needs vertex color: no
		// needs VFACE: no
		// passes tangent-to-world matrix to pixel shader: no
		// reads from normal: no
		// 1 texcoords actually used
		//   float2 _MainTex
#define UNITY_PASS_META
#include "UnityCG.cginc"
#include "Lighting.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

		// Original surface shader snippet:
#line 24 ""
#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
#endif

		//#pragma surface surf Lambert vertex:vert nofog keepalpha
		//#pragma multi_compile _ PIXELSNAP_ON

		sampler2D _MainTex;
	fixed4 _Color;

	struct Input
	{
		float2 uv_MainTex;
		fixed4 color;
	};

	void vert(inout appdata_full v, out Input o)
	{
#if defined(PIXELSNAP_ON)
		v.vertex = UnityPixelSnap(v.vertex);
#endif

		UNITY_INITIALIZE_OUTPUT(Input, o);
		o.color = v.color * _Color;
	}

	void surf(Input IN, inout SurfaceOutput o)
	{
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
		o.Albedo = c.rgb * c.a;
		o.Alpha = c.a;
	}

#include "UnityMetaPass.cginc"

	// vertex-to-fragment interpolation data
	struct v2f_surf {
		float4 pos : SV_POSITION;
		float2 pack0 : TEXCOORD0; // _MainTex
		float3 worldPos : TEXCOORD1;
		half4 custompack0 : TEXCOORD2; // color
	};
	float4 _MainTex_ST;

	// vertex shader
	v2f_surf vert_surf(appdata_full v) {
		v2f_surf o;
		UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
		Input customInputData;
		vert(v, customInputData);
		o.custompack0.xyzw = customInputData.color;
		o.pos = UnityMetaVertexPosition(v.vertex, v.texcoord1.xy, v.texcoord2.xy, unity_LightmapST, unity_DynamicLightmapST);
		o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
		float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
		o.worldPos = worldPos;
		return o;
	}

	// fragment shader
	fixed4 frag_surf(v2f_surf IN) : SV_Target{
		// prepare and unpack data
		Input surfIN;
	UNITY_INITIALIZE_OUTPUT(Input,surfIN);
	surfIN.uv_MainTex.x = 1.0;
	surfIN.color.x = 1.0;
	surfIN.uv_MainTex = IN.pack0.xy;
	surfIN.color = IN.custompack0.xyzw;
	float3 worldPos = IN.worldPos;
#ifndef USING_DIRECTIONAL_LIGHT
	fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
#else
	fixed3 lightDir = _WorldSpaceLightPos0.xyz;
#endif
#ifdef UNITY_COMPILER_HLSL
	SurfaceOutput o = (SurfaceOutput)0;
#else
	SurfaceOutput o;
#endif
	o.Albedo = 0.0;
	o.Emission = 0.0;
	o.Specular = 0.0;
	o.Alpha = 0.0;
	o.Gloss = 0.0;
	fixed3 normalWorldVertex = fixed3(0,0,1);

	// call surface function
	surf(surfIN, o);
	UnityMetaInput metaIN;
	UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaIN);
	metaIN.Albedo = o.Albedo;
	metaIN.Emission = o.Emission;
	return UnityMetaFragment(metaIN);
	}


#endif

		// -------- variant for: PIXELSNAP_ON 
#if defined(PIXELSNAP_ON)
		// Surface shader code generated based on:
		// vertex modifier: 'vert'
		// writes to per-pixel normal: no
		// writes to emission: no
		// needs world space reflection vector: no
		// needs world space normal vector: no
		// needs screen space position: no
		// needs world space position: no
		// needs view direction: no
		// needs world space view direction: no
		// needs world space position for lighting: YES
		// needs world space view direction for lighting: no
		// needs world space view direction for lightmaps: no
		// needs vertex color: no
		// needs VFACE: no
		// passes tangent-to-world matrix to pixel shader: no
		// reads from normal: no
		// 1 texcoords actually used
		//   float2 _MainTex
#define UNITY_PASS_META
#include "UnityCG.cginc"
#include "Lighting.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

		// Original surface shader snippet:
#line 24 ""
#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
#endif

		//#pragma surface surf Lambert vertex:vert nofog keepalpha
		//#pragma multi_compile _ PIXELSNAP_ON

	sampler2D _MainTex;
	fixed4 _Color;

	struct Input
	{
		float2 uv_MainTex;
		fixed4 color;
	};

	void vert(inout appdata_full v, out Input o)
	{
#if defined(PIXELSNAP_ON)
		v.vertex = UnityPixelSnap(v.vertex);
#endif

		UNITY_INITIALIZE_OUTPUT(Input, o);
		o.color = v.color * _Color;
	}

	void surf(Input IN, inout SurfaceOutput o)
	{
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
		o.Albedo = c.rgb * c.a;
		o.Alpha = c.a;
	}

#include "UnityMetaPass.cginc"

	// vertex-to-fragment interpolation data
	struct v2f_surf {
		float4 pos : SV_POSITION;
		float2 pack0 : TEXCOORD0; // _MainTex
		float3 worldPos : TEXCOORD1;
		half4 custompack0 : TEXCOORD2; // color
	};
	float4 _MainTex_ST;

	// vertex shader
	v2f_surf vert_surf(appdata_full v) {
		v2f_surf o;
		UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
		Input customInputData;
		vert(v, customInputData);
		o.custompack0.xyzw = customInputData.color;
		o.pos = UnityMetaVertexPosition(v.vertex, v.texcoord1.xy, v.texcoord2.xy, unity_LightmapST, unity_DynamicLightmapST);
		o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
		float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
		o.worldPos = worldPos;
		return o;
	}

	// fragment shader
	fixed4 frag_surf(v2f_surf IN) : SV_Target{
		// prepare and unpack data
		Input surfIN;
	UNITY_INITIALIZE_OUTPUT(Input,surfIN);
	surfIN.uv_MainTex.x = 1.0;
	surfIN.color.x = 1.0;
	surfIN.uv_MainTex = IN.pack0.xy;
	surfIN.color = IN.custompack0.xyzw;
	float3 worldPos = IN.worldPos;
#ifndef USING_DIRECTIONAL_LIGHT
	fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
#else
	fixed3 lightDir = _WorldSpaceLightPos0.xyz;
#endif
#ifdef UNITY_COMPILER_HLSL
	SurfaceOutput o = (SurfaceOutput)0;
#else
	SurfaceOutput o;
#endif
	o.Albedo = 0.0;
	o.Emission = 0.0;
	o.Specular = 0.0;
	o.Alpha = 0.0;
	o.Gloss = 0.0;
	fixed3 normalWorldVertex = fixed3(0,0,1);

	// call surface function
	surf(surfIN, o);
	UnityMetaInput metaIN;
	UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaIN);
	metaIN.Albedo = o.Albedo;
	metaIN.Emission = o.Emission;
	return UnityMetaFragment(metaIN);
	}


#endif


		ENDCG

	}

		// ---- end of surface shader generated code

		#LINE 55

	}

		Fallback "Transparent/VertexLit"
}
