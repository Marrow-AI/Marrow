 Shader "Sprites/Low Res - Diffuse With Shadows"
 {
     Properties
     {
         [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
         _Cutoff ("Alpha Cutoff", Range (0,1)) = 0.0001
         _BlurAmount ("Blur Amount", Range (0, 1)) = 0.055
     }
 
     SubShader
     {
   
         Tags
         { 
             "Queue"="Transparent"
             "RenderType"="Transparent" 
             "PreviewType"="Plane"
             "CanUseSpriteAtlas"="True"
         }
         
         
         LOD 300
         Cull Off
         Lighting Off
         ZWrite Off          
         
         CGPROGRAM
         #pragma surface surf ToonRamp vertex:vert alpha alphatest:_Cutoff addshadow
         #pragma lighting ToonRamp
         
         
         sampler2D _MainTex;
         float _BlurAmount;
         float4 _MainTex_TexelSize;
         
         
         // for hard double-sided proximity lighting
         inline half4 LightingToonRamp (SurfaceOutput s, half3 lightDir, half atten)
         {
             half4 c;
             c.rgb = s.Albedo * _LightColor0.rgb * sqrt(atten);
             c.a = s.Alpha;
             return c;
         }
 
 
         struct Input 
         {
             float2 uv_MainTex : TEXCOORD0;
             fixed4 color;
         };
         
         void vert (inout appdata_full v, out Input o)
         {
             UNITY_INITIALIZE_OUTPUT(Input, o);
             o.color = v.color;
         }
         
         void surf (Input IN, inout SurfaceOutput o) 
         {
             half4 original = tex2D(_MainTex, IN.uv_MainTex);
             half4 finalOutput = tex2D(_MainTex, IN.uv_MainTex);
             
             float amount = _BlurAmount;
             float2 up = float2(0.0, _MainTex_TexelSize.y) * amount;
             float2 right = float2(_MainTex_TexelSize.x, 0.0) * amount;
         
             for(int i=0;i<3;i++)
             {
                 finalOutput += tex2D(_MainTex, IN.uv_MainTex + up);
                 finalOutput += tex2D(_MainTex, IN.uv_MainTex - up);
                 finalOutput += tex2D(_MainTex, IN.uv_MainTex + right);
                 finalOutput += tex2D(_MainTex, IN.uv_MainTex - right);
                 finalOutput += tex2D(_MainTex, IN.uv_MainTex + right + up);
                 finalOutput += tex2D(_MainTex, IN.uv_MainTex - right + up);
                 finalOutput += tex2D(_MainTex, IN.uv_MainTex - right - up);
                 finalOutput += tex2D(_MainTex, IN.uv_MainTex + right - up);
                 
                 amount += amount;
                 up = float2(0.0, _MainTex_TexelSize.y) * amount;
                 right = float2(_MainTex_TexelSize.x, 0.0) * amount;
             }
             
             finalOutput = (finalOutput) / 25;
             
             fixed3 finalOutput2 = lerp((0,0,0,0), finalOutput.rgb, finalOutput.a);
             finalOutput2 = lerp(finalOutput2, finalOutput.rgb, original.a);
             finalOutput2 *= IN.color;
             
             o.Alpha = (finalOutput.a * IN.color.a);
             
             o.Albedo = finalOutput2;
             
         }
         
         ENDCG
     }
 }