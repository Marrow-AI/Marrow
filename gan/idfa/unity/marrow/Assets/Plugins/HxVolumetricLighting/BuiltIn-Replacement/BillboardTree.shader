// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/TerrainEngine/BillboardTree" {
	Properties{
		_MainTex("Base (RGB) Alpha (A)", 2D) = "white" {}
	}

		SubShader{
		Tags{ "Queue" = "Transparent-100" "IgnoreProjector" = "True" "RenderType" = "TreeBillboard" }

		Pass{
		ColorMask rgb
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off Cull Off

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fog
#pragma multi_compile __ VTRANSPARENCY_ON
#pragma target 3.0
#include "UnityCG.cginc"
#include "TerrainEngine.cginc"
#include "Assets/Plugins/HxVolumetricLighting/BuiltIn-Replacement/HxVolumetricCore.cginc"

	struct v2f {
		float4 pos : SV_POSITION;
		fixed4 color : COLOR0;
		float2 uv : TEXCOORD0;
		UNITY_FOG_COORDS(1)

#ifdef VTRANSPARENCY_ON
			float4 projPos : TEXCOORD2;
#endif
	};

	v2f vert(appdata_tree_billboard v) {
		v2f o;
		TerrainBillboardTree(v.vertex, v.texcoord1.xy, v.texcoord.y);
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv.x = v.texcoord.x;
		o.uv.y = v.texcoord.y > 0;
		o.color = v.color;
		UNITY_TRANSFER_FOG(o,o.pos);
#ifdef VTRANSPARENCY_ON
		o.projPos = ComputeScreenPos(o.pos);
		COMPUTE_EYEDEPTH(o.projPos.z);
#endif
		return o;
	}

	sampler2D _MainTex;
	fixed4 frag(v2f input) : SV_Target
	{
		fixed4 col = tex2D(_MainTex, input.uv);
	col.rgb *= input.color.rgb;
	clip(col.a);
	UNITY_APPLY_FOG(input.fogCoord, col);

#ifdef VTRANSPARENCY_ON
	return VolumetricTransparency(col, input.projPos);
#else
	return col;
#endif
	}
		ENDCG
	}
	}

		Fallback Off
}
