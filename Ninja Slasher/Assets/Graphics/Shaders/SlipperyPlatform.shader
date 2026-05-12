Shader "Custom/URP/SlimePlatform"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _HighlightColor ("Highlight Color", Color) = (1, 1, 1, 1)
        _HighlightStrength ("Highlight Strength", Range(0, 1)) = 0.6
        _HighlightSpeed ("Highlight Speed", Range(0, 3)) = 0.3
        _HighlightWidth ("Highlight Width", Range(0.05, 0.45)) = 0.24
        _PulseStrength ("Pulse Strength", Range(0, 0.25)) = 0.04
        _PulseSpeed ("Pulse Speed", Range(0, 3)) = 0.45
        _BlobStrength ("Blob Strength", Range(0, 1)) = 0.3
        _BlobScale ("Blob Scale", Range(0.5, 4)) = 1.8
        _WobbleStrength ("Wobble Strength", Range(0, 0.05)) = 0.008
        _WobbleSpeed ("Wobble Speed", Range(0, 3)) = 0.4
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "SlimeUnlit"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            half4 _BaseColor;
            half4 _HighlightColor;
            half _HighlightStrength;
            half _HighlightSpeed;
            half _HighlightWidth;
            half _PulseStrength;
            half _PulseSpeed;
            half _BlobStrength;
            half _BlobScale;
            half _WobbleStrength;
            half _WobbleSpeed;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half GetFillMask(half luma, half alpha)
            {
                half mask = smoothstep(0.18h, 0.42h, luma);
                mask *= smoothstep(0.08h, 0.18h, alpha);
                return saturate(mask);
            }

            half GetBlobMask(float2 uv, float time)
            {
                float blobA = sin(uv.x * (_BlobScale * 2.2h) + time) * sin(uv.y * (_BlobScale * 1.45h) - time * 0.9h);
                float blobB = sin((uv.x * 1.15h + uv.y * 0.8h) * (_BlobScale * 1.75h) - time * 0.55h);
                half blobField = (half)(blobA * 0.55 + blobB * 0.45) * 0.5h + 0.5h;
                return smoothstep(0.38h, 0.88h, blobField);
            }

            half GetSweepBand(float value, half width)
            {
                half wave = (half)(sin(value) * 0.5 + 0.5);
                return smoothstep(1.0h - width, 1.0h, wave);
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 tint = input.color * _BaseColor;
                half alpha = sprite.a * tint.a;
                half3 color = sprite.rgb * tint.rgb;

                half baseLuma = dot(sprite.rgb, half3(0.299h, 0.587h, 0.114h));
                half fillMask = GetFillMask(baseLuma, sprite.a);

                float time = _Time.y;
                float wobbleTime = time * _WobbleSpeed;
                float2 motionUV = input.uv;
                motionUV.x += sin(input.uv.y * 6.0 + wobbleTime * 1.15) * _WobbleStrength;
                motionUV.y += sin(input.uv.x * 5.0 - wobbleTime) * (_WobbleStrength * 0.55h);

                half pulseWave = (half)(sin(time * _PulseSpeed) * 0.5 + 0.5);
                half pulse = pulseWave * _PulseStrength;
                half colorLuma = dot(color, half3(0.299h, 0.587h, 0.114h));
                half3 saturatedPulse = lerp(colorLuma.xxx, color, 1.0h + pulse * 1.4h);
                color = lerp(color, saturatedPulse, pulse * fillMask);
                color *= 1.0h + pulse * 0.12h * fillMask;

                half blobMask = GetBlobMask(motionUV, time * (_WobbleSpeed * 0.6h + 0.18h)) * fillMask * _BlobStrength;
                color = lerp(color, saturate(color * 1.08h), blobMask * 0.35h);
                color += _HighlightColor.rgb * (blobMask * 0.18h);

                const float twoPi = 6.2831853;
                float sweepA = (motionUV.x * 1.1 + motionUV.y * 0.55 + time * _HighlightSpeed * 0.22) * twoPi;
                float sweepB = (motionUV.x * 0.8 - motionUV.y * 0.35 + time * _HighlightSpeed * 0.16 + 0.37) * twoPi;
                half bandA = GetSweepBand(sweepA, _HighlightWidth);
                half bandB = GetSweepBand(sweepB, _HighlightWidth * 0.72h);
                half highlightMask = saturate(bandA * 0.75h + bandB * 0.35h);
                highlightMask *= fillMask * _HighlightStrength;
                highlightMask *= 0.85h + blobMask * 0.5h;

                color += _HighlightColor.rgb * highlightMask;
                color = saturate(color);

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
