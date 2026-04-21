Shader "UI/Katana Slash Transition Preview"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (0,0,0,1)
        _HighlightColor ("Highlight Color", Color) = (1,0.96,0.82,0.9)
        _CoreLineColor ("Core Line Color", Color) = (1,1,1,1)
        _Opacity ("Opacity", Range(0,1)) = 0
        _Travel ("Travel", Range(0,1)) = 0
        _OpenProgress ("Open Progress", Range(0,1)) = 0
        _TravelPosition ("Travel Position", Float) = 1.65
        _TravelDirection ("Travel Direction", Range(-180,180)) = -135
        _Angle ("Angle", Range(-180,180)) = -32
        _CutPosition ("Cut Position", Range(-1.25,1.25)) = 0
        _FrontWidth ("Front Width", Float) = 0.055
        _TrailLength ("Trail Length", Float) = 0.22
        _OpenWidth ("Open Width", Float) = 1.1
        _EdgeSoftness ("Edge Softness", Float) = 0.035
        _LineThickness ("Line Thickness", Float) = 0.018
        _CoreLineIntensity ("Core Line Intensity", Float) = 1.35
        _GlowThickness ("Glow Thickness", Float) = 0.06
        _LineIrregularity ("Line Irregularity", Float) = 0.008
        _IrregularityFrequency ("Irregularity Frequency", Float) = 12
        _SweepIntensity ("Sweep Intensity", Float) = 0.72
        _Energy ("Energy", Range(0,1)) = 1
        _PostTraceIntensity ("Post Trace Intensity", Float) = 0.55
        _MaxAperture ("Max Aperture", Float) = 2.35
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
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _TextureSampleAdd;
            float4 _MainTex_ST;
            fixed4 _TintColor;
            fixed4 _HighlightColor;
            fixed4 _CoreLineColor;
            float _Opacity;
            float _Travel;
            float _OpenProgress;
            float _TravelPosition;
            float _TravelDirection;
            float _Angle;
            float _CutPosition;
            float _FrontWidth;
            float _TrailLength;
            float _OpenWidth;
            float _EdgeSoftness;
            float _LineThickness;
            float _CoreLineIntensity;
            float _GlowThickness;
            float _LineIrregularity;
            float _IrregularityFrequency;
            float _SweepIntensity;
            float _Energy;
            float _PostTraceIntensity;
            float _MaxAperture;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centeredUv = i.uv * 2.0 - 1.0;
                float angleRadians = radians(_Angle);
                float2 normal = normalize(float2(cos(angleRadians), sin(angleRadians)));
                float travelRadians = radians(_TravelDirection);
                float2 travelDir = normalize(float2(cos(travelRadians), sin(travelRadians)));
                float travelCoord = dot(centeredUv, travelDir);

                float microVariation =
                    sin((travelCoord * _IrregularityFrequency) + (_Travel * 7.3)) * 0.65 +
                    sin((travelCoord * (_IrregularityFrequency * 1.91)) - 0.7 + (_Travel * 4.1)) * 0.35;
                microVariation *= _LineIrregularity;

                float signedDistance = dot(centeredUv, normal) - _CutPosition + microVariation;
                float distanceFromCut = abs(signedDistance);

                float softness = max(_EdgeSoftness, 0.0005);
                float lineThickness = max(_LineThickness, 0.0001);
                float glowThickness = max(_GlowThickness, lineThickness + 0.0001);
                float frontWidth = max(_FrontWidth, lineThickness);

                float frontDistance = abs(travelCoord - _TravelPosition);
                float frontBand = 1.0 - smoothstep(frontWidth, frontWidth + softness, frontDistance);
                float trailBand = smoothstep(_TravelPosition - _TrailLength, _TravelPosition, travelCoord);
                trailBand *= 1.0 - smoothstep(_TravelPosition, _TravelPosition + frontWidth, travelCoord);
                float slashPresence = saturate(frontBand + trailBand);

                float openAnchor = _TravelPosition + (_OpenProgress * _TrailLength * 0.85);
                float openedBand = smoothstep(travelCoord, travelCoord + softness, openAnchor);
                float aperture = _OpenWidth * _OpenProgress * _MaxAperture;
                float openMask = openedBand * (1.0 - smoothstep(aperture, aperture + softness, distanceFromCut));
                float blackMask = saturate(_Opacity * (1.0 - openMask));

                float edgeDistance = distanceFromCut;

                float coreBand = 1.0 - smoothstep(lineThickness, lineThickness + softness, edgeDistance);
                float glowBand = 1.0 - smoothstep(glowThickness, glowThickness + (softness * 2.0), edgeDistance);
                float sweepGlow = trailBand * glowBand * _SweepIntensity * saturate(_Energy);
                float coreHighlight = coreBand * _CoreLineIntensity * slashPresence;
                float postTrace = glowBand * _PostTraceIntensity * saturate(_Energy) * slashPresence;

                fixed4 color = _TintColor;
                color.rgb += _HighlightColor.rgb * (sweepGlow + postTrace);
                color.rgb += _CoreLineColor.rgb * coreHighlight;

                float glowAlpha =
                    (sweepGlow * _HighlightColor.a) +
                    (postTrace * _HighlightColor.a * 0.5) +
                    (coreHighlight * _CoreLineColor.a);
                float alpha = saturate((blackMask * _Opacity) + glowAlpha);
                color.a = alpha;
                return color * i.color;
            }
            ENDCG
        }
    }
}
