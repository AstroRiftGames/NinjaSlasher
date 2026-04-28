Shader "Custom/BlurOverlay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurIntensity ("Blur Intensity", Range(0, 10)) = 3
        _TintColor ("Tint Color", Color) = (0, 0, 0, 0.5)
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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
            float4 _MainTex_TexelSize;
            float _BlurIntensity;
            fixed4 _TintColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy * _BlurIntensity;
                
                fixed4 col = fixed4(0,0,0,0);
                col += tex2D(_MainTex, i.uv + float2(-texelSize.x, -texelSize.y));
                col += tex2D(_MainTex, i.uv + float2(0, -texelSize.y));
                col += tex2D(_MainTex, i.uv + float2(texelSize.x, -texelSize.y));
                col += tex2D(_MainTex, i.uv + float2(-texelSize.x, 0));
                col += tex2D(_MainTex, i.uv);
                col += tex2D(_MainTex, i.uv + float2(texelSize.x, 0));
                col += tex2D(_MainTex, i.uv + float2(-texelSize.x, texelSize.y));
                col += tex2D(_MainTex, i.uv + float2(0, texelSize.y));
                col += tex2D(_MainTex, i.uv + float2(texelSize.x, texelSize.y));
                
                col /= 9;
                
                col.rgb = lerp(col.rgb, _TintColor.rgb, _TintColor.a);
                col.a = max(col.a, _TintColor.a);
                
                return col;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}