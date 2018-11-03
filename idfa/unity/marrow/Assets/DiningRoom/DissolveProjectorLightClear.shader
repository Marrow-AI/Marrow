// Upgrade NOTE: replaced '_Projector' with 'unity_Projector'
// Upgrade NOTE: replaced '_ProjectorClip' with 'unity_ProjectorClip'

Shader "Custom/Projector/Dissolve Light Clear" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
        _Strength ("Strength", Float) = 1.0
		_ShadowTex ("Cookie", 2D) = "" {}
        _SecondShadowTex ("Second Cookie", 2D) = "" {}
        _MaskTex ("Mask", 2D) = "" {}
		_FalloffTex ("FallOff", 2D) = "" {}
        _Blend ("Blend", Range(0,1)) = 0
	}
	
	Subshader {
		Tags {"Queue"="Transparent"}
		Pass {
			ZWrite Off
			ColorMask RGB
			Blend SrcColor One
			Offset -1, -1
	
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#include "UnityCG.cginc"
			
			struct v2f {
				float4 uvShadow : TEXCOORD0;
				float4 uvFalloff : TEXCOORD1;
				UNITY_FOG_COORDS(2)
				float4 pos : SV_POSITION;
			};
			
			float4x4 unity_Projector;
			float4x4 unity_ProjectorClip;
			
			v2f vert (float4 vertex : POSITION)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(vertex);
				o.uvShadow = mul (unity_Projector, vertex);
				o.uvFalloff = mul (unity_ProjectorClip, vertex);
				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}
			
			fixed4 _Color;
            float _Strength;
			sampler2D _ShadowTex;
            sampler2D _SecondShadowTex;
            sampler2D _MaskTex;
			sampler2D _FalloffTex;
            float _Blend;
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 texS = tex2Dproj (_ShadowTex, UNITY_PROJ_COORD(i.uvShadow));
				//texS.rgb *= _Color.rgb;
                //texS.rgb *= _Strength;
                fixed4 texS2 = tex2Dproj (_SecondShadowTex, UNITY_PROJ_COORD(i.uvShadow));
                texS.rgb = lerp(texS.rgb, texS2.rgb, _Blend);

                texS.rgb *= _Color.rgb;
                texS.rgb *= _Strength;

                fixed4 maskS = tex2Dproj (_MaskTex, UNITY_PROJ_COORD(i.uvShadow));
                texS.rgb *= maskS.rgb;
				texS.a = 1.0-texS.a;
	
				fixed4 texF = tex2Dproj (_FalloffTex, UNITY_PROJ_COORD(i.uvFalloff));
				fixed4 res = texS * texF.a;

				UNITY_APPLY_FOG_COLOR(i.fogCoord, res, fixed4(0,0,0,0));
				return res;
			}
			ENDCG
		}
	}
}
