Shader "Hidden/HxShadowCasterFix" 
{
Properties{
	_Color("Main Color", Color) = (1,1,1,1) //note: required but not used
}
SubShader{
	Tags{ "RenderType" = "hxShadowFix" "Queue" = "Geometry" "IgnoreProjector" = "True" }
	Pass{
	Tags{ "LightMode" = "ForwardAdd" }
	Blend One One
	Fog{ Color(0,0,0,0) }
	ZWrite Off
	CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fwdadd_fullshadows
#include "UnityCG.cginc"
#include "AutoLight.cginc"

struct v2f {

	float4 pos : SV_POSITION;

};

v2f vert(appdata_full v) {
	v2f o;
	o.pos = float4(0, 0, 0, 0);// mul(UNITY_MATRIX_MVP, v.vertex);
	
	return o;
}

float4 frag(v2f i) : COLOR{
	discard;
return float4(0, 0, 0, 0);
}
ENDCG
} //Pass

Pass{
	Tags{ "LightMode" = "ForwardBase" "IgnoreProjector" = "True" }
	Blend One One
	Fog{ Color(0,0,0,0) }
	ZWrite Off
	CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fwdadd_fullshadows
#include "UnityCG.cginc"
#include "AutoLight.cginc"

struct v2f {
	float4 pos : SV_POSITION;
};

v2f vert(appdata_full v) {
	v2f o;
	o.pos = float4(0, 0, 0, 0);// mul(UNITY_MATRIX_MVP, v.vertex);
	return o;
}

float4 frag(v2f i) : COLOR{
	discard;
return float4(0, 0, 0, 0);
}
ENDCG
} //Pass

Pass{
	Tags{ "LightMode" = "ShadowCaster" "IgnoreProjector" = "True" }
	Blend One One
	Fog{ Color(0,0,0,0) }
	ZWrite Off
	CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fwdadd_fullshadows
#include "UnityCG.cginc"
#include "AutoLight.cginc"

struct v2f {
	float4 pos : SV_POSITION;
};

v2f vert(appdata_full v) {
	v2f o;
	o.pos = float4(0, 0, 0, 0);// mul(UNITY_MATRIX_MVP, v.vertex);
	return o;
}

float4 frag(v2f i) : COLOR{
	discard;
return float4(0, 0, 0, 0);
}
ENDCG
} //Pass

} //SubShader
FallBack Off //note: for passes: ForwardBase, ShadowCaster, ShadowCollector
}
