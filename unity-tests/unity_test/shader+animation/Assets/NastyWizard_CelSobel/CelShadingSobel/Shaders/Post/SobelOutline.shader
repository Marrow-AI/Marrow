Shader "Nasty-Screen/SobelOutline"
{

	//////////////////////////////////////////////////////////////
	//														   	//
	//			This shader was written by Casey MacNeil		//	   
	//														   	//
	//////////////////////////////////////////////////////////////

	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Outline ("Outline Color",Color) = (0,0,0,1)
		_ResX("Resolution X", Float) = 1024
		_ResY("Resolution Y", Float) = 1024
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }

		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "PostFunctions.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Outline;
			float _ResX;
			float _ResY;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			////////////////////////////////////////////////////

			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				float2 offsets[9];
				GetOffsets3x3(_ResX, _ResY, offsets);

				fixed3 textures[9];
				for (int j = 0; j < 9; j++)
				{
					textures[j] = tex2D(_MainTex, i.uv + offsets[j]).rgb;
				}

				fixed4 FragColor = ApplySobel(textures);

				_Outline = fixed4(fixed3(1, 1, 1) - _Outline.rgb, 1.0);  // invert outline
				FragColor = fixed4(FragColor.rgb * _Outline, 1.0);       // inverse outline * sobel
				FragColor = fixed4(fixed3(1, 1, 1) - FragColor.rgb, 1.0);// inverse sobel
				FragColor = fixed4(FragColor.rgb * textures[4], 1.0);

				return FragColor;
			}
			ENDCG
		}
	}
}
