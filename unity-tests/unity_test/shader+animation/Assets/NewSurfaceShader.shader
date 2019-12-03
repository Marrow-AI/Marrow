Shader "Vertex Displacement" {
   Properties {
      _MainTex ("Main Texture", 2D) = "white" {}
      _DisplacementTex ("Displacement Texture", 2D) = "white" {}
      _MaxDisplacement ("Max Displacement", Float) = 1.0
   }
   SubShader {
      Pass {    
         CGPROGRAM

         #pragma vertex vert
         #pragma fragment frag
   
         uniform sampler2D _MainTex;
         uniform sampler2D _DisplacementTex;
         uniform float _MaxDisplacement;
   
         struct vertexInput {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float4 texcoord : TEXCOORD0;
         };
   
         struct vertexOutput {
            float4 position : SV_POSITION;
            float4 texcoord : TEXCOORD0;
         };
   
         vertexOutput vert(vertexInput i) {
            vertexOutput o;
            
            // get color from displacement map, and convert to float from 0 to _MaxDisplacement
            float4 dispTexColor = tex2Dlod(_DisplacementTex, float4(i.texcoord.xy, 0.0, 0.0));
            float displacement = dot(float3(0.21, 0.72, 0.07), dispTexColor.rgb) * _MaxDisplacement;
            
            // displace vertices along surface normal vector
            float4 newVertexPos = i.vertex + float4(i.normal * displacement, 0.0);   

            // output data            
            o.position = UnityObjectToClipPos(newVertexPos); 
            o.texcoord = i.texcoord;
            return o;
   
         }
   
         float4 frag(vertexOutput i) : COLOR
         {
            return tex2D(_MainTex, i.texcoord.xy);
         }
   
         ENDCG
      }
   }
}
