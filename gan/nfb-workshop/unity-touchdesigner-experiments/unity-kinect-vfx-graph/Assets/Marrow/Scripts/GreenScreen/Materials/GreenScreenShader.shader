// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "DX11/GreenScreenShader" {
	SubShader {
		Pass {

			CGPROGRAM

			#pragma target 5.0

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			UNITY_DECLARE_TEX2D(_MainTex);

			sampler SampleType;

			struct vs_input {
				float4 pos : POSITION;
				float2 tex : TEXCOORD0;
			};

			StructuredBuffer<float2> depthCoordinates;
			StructuredBuffer<float> bodyIndexBuffer;

			struct ps_input {
				float4 pos : SV_POSITION;
				float2 tex : TEXCOORD0;
			};

			ps_input vert (vs_input v)
			{
				ps_input o;
				o.pos = UnityObjectToClipPos (v.pos);
				o.tex = v.tex;
				// Flip x texture coordinate to mimic mirror.
				o.tex.x = 1 - v.tex.x;
				return o;
			}

			float4 frag (ps_input i, in uint id : SV_InstanceID) : COLOR
			{
				float4 o;
	
				int colorWidth = (int)(i.tex.x * (float)1920);
				int colorHeight = (int)(i.tex.y * (float)1080);
				int colorIndex = (int)(colorWidth + colorHeight * (float)1920);
	
				o = float4(0, 1, 0, 1);
	
				if ((!isinf(depthCoordinates[colorIndex].x) && !isnan(depthCoordinates[colorIndex].x) && depthCoordinates[colorIndex].x != 0) || 
					!isinf(depthCoordinates[colorIndex].y) && !isnan(depthCoordinates[colorIndex].y) && depthCoordinates[colorIndex].y != 0)
				{
					// We have valid depth data coordinates from our coordinate mapper.  Find player mask from corresponding depth points.
					float player = bodyIndexBuffer[(int)depthCoordinates[colorIndex].x + (int)(depthCoordinates[colorIndex].y * 512)];
					if (player != 255)
					{
						o = UNITY_SAMPLE_TEX2D(_MainTex, i.tex );
					}
				}
	
				return o;
			}

			ENDCG

		}
	}

	Fallback Off
}