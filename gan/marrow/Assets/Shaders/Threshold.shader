Shader "Custom/GanShader"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _ColorTint ("Tint", Color) = (1,1,1,1)
        _Color1in ("Color 1 In", Color) = (1,1,1,1)
        _Color1out ("Color 1 Out", Color) = (1,1,1,1)
        _Threshold ("Threshold", Range (0, 3)) = 0.5
        _ClusterOne ("Cluster One", Float) = 0
        _ClusterTwo ("Cluster Two", Float) = 0
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        _Transparency("Transparency", Range(0.0,1.0)) = 0.25
        _Blend("Blend", Range(0.0,1.0)) = 1.0
    }
 
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
 
        Cull Off
        Lighting Off
        ZWrite Off
        Fog { Mode Off }
        Blend SrcAlpha OneMinusSrcAlpha
 
        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag          
            #pragma multi_compile DUMMY PIXELSNAP_ON
            #include "UnityCG.cginc"
         
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };
 
            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                half2 texcoord  : TEXCOORD0;
            };
         
            fixed4 _ColorTint;
            fixed4 _Color1in;
            fixed4 _Color1out;
            float _Threshold;
            float _Transparency;
            float _Blend;
 
            v2f vert(appdata_t IN)
            {
                v2f OUT;

                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;              
                OUT.color = IN.color * _ColorTint;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif

                return OUT;
            }

            // Texture 2D
            sampler2D _MainTex;
         
            fixed4 frag(v2f IN) : COLOR
            {
                // Get the main texture
                float4 texColor = tex2D( _MainTex, IN.texcoord );
                float4 origColor = texColor;
                if (origColor.r <= 0.01 && origColor.g <= 0.01 & origColor.b <= 0.01) {
                    origColor.a = 0.0;
                }
                

                // Change the rgb of the texture to black and white
                texColor.rgb = dot(texColor.rgb, float3(texColor.x, texColor.y, texColor.z));

                // returns 1 or 0 depending on if the threshold is higher than our color
                texColor.r = step(texColor.r, _Threshold);
                texColor.g = step(texColor.g, _Threshold);
                texColor.b = step(texColor.b, _Threshold);
                
                 if (texColor.r == 1.0) {
                   texColor.a = 0.0;
                } else {
                    texColor = _Color1in;
                    texColor.a = _Transparency;             
                }

               
                

                // Return to the texture color
                //return texColor * IN.color;
                return lerp(texColor, origColor, _Blend);
            }
        ENDCG
        }
    }
}
