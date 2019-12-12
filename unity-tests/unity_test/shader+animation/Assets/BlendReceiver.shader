Shader "Hidden/Marrow/BlendReceiver"
{
    Properties
    {
        _MainTex("", 2D) = "" {}
        _BackTex("BackTex", 2D) = "" {}
        _BlendFactor ("BlendFactor", float) = 0.5
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _MainTex;
    sampler2D _BackTex;
    float4 _MainTex_TexelSize;
    float _BlendFactor;

    // Adobe-esque HDTV Rec.709 (2.2 gamma, 16-235 limit)
    half3 YUV2RGB(half3 yuv)
    {
        const half K_B = 0.0722;
        const half K_R = 0.2126;

        half y = (yuv.x - 16.0 / 255) * 255 / 219;
        half u = (yuv.y - 128.0 / 255) * 255 / 112;
        half v = (yuv.z - 128.0 / 255) * 255 / 112;

        half r = y + v * (1 - K_R);
        half g = y - v * K_R / (1 - K_R) - u * K_B / (1 - K_B);
        half b = y + u * (1 - K_B);

        return GammaToLinearSpace(half3(r, g, b));
    }
    // 4:2:2 subsampling
    half3 SampleUYVY(float2 uv)
    {
        half4 uyvy = tex2D(_MainTex, uv);
        bool sel = frac(uv.x * _MainTex_TexelSize.z) < 0.5;
        half3 yuv = sel ? uyvy.yxz : uyvy.wxz;
        return YUV2RGB(yuv);
    }

    half4 Fragment_UYVY(v2f_img input) : SV_Target
    {
        return half4(SampleUYVY(float2(input.uv.x, 1 - input.uv.y)), 1);
    }

    half4 Fragment_Blend(v2f_img input) : SV_Target
    {
        half3 front = tex2D(_MainTex, float2(input.uv.x / 3, input.uv.y)).rgb;
        half3 back = tex2D(_BackTex, float2(input.uv.x / 3, input.uv.y)).rgb;

        half3 blend = lerp(front, back, _BlendFactor);

        return half4(blend, 1);
    }
    half4 Fragment_BlendTwo(v2f_img input) : SV_Target
    { 
        half3 front = tex2D(_MainTex, float2((2 * input.uv.x + 1)/ 3, input.uv.y)).rgb;
        half3 back = tex2D(_BackTex, float2((2 * input.uv.x + 1)/ 3, input.uv.y)).rgb;
        //half3 front = tex2D(_MainTex, float2(input.uv.x, input.uv.y)).rgb;
        //half3 back = tex2D(_BackTex, float2(input.uv.x, input.uv.y)).rgb;

        half3 blend = lerp(front, back, _BlendFactor);

        return half4(blend, 1);
    }

    ENDCG

    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Fragment_UYVY
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Fragment_Blend
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Fragment_BlendTwo
            ENDCG
        }
    }
}
