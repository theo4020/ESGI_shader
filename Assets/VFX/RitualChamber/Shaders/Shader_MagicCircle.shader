Shader "Ritual/MagicCircle"
{
    Properties
    {
        _ChargeAmount  ("Charge Amount",     Range(0,1)) = 0
        _RotationSpeed ("Rotation Speed",    Float)      = 0.4
        _RingThickness ("Ring Thickness",    Float)      = 0.012
        _RuneCount     ("Rune Count",        Float)      = 8
        _Intensity     ("Emission Intensity",Float)      = 2.5
        _ColorRest     ("Color Rest",        Color)      = (0.15, 0.0, 0.4, 1)
        _ColorCharge   ("Color Charge",      Color)      = (0.85, 0.05, 0.28, 1)
        _ColorTeal     ("Color Teal",        Color)      = (0.0, 0.65, 0.6, 1)
        _ColorRune     ("Color Rune",        Color)      = (0.88, 0.80, 0.75, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "MagicCircle"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            Cull Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float  _ChargeAmount;
                float  _RotationSpeed;
                float  _RingThickness;
                float  _RuneCount;
                float  _Intensity;
                float4 _ColorRest;
                float4 _ColorCharge;
                float4 _ColorTeal;
                float4 _ColorRune;
            CBUFFER_END

            struct Attributes { float4 posOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 posHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.posHCS = TransformObjectToHClip(IN.posOS.xyz);
                OUT.uv     = IN.uv;
                return OUT;
            }

            float Hash(float n) { return frac(sin(n) * 43758.5453); }

            float Ring(float dist, float r, float w)
            {
                return 1.0 - smoothstep(0.0, w, abs(dist - r));
            }

            float Spoke(float normAngle, float count, float width)
            {
                float a = frac(normAngle * count);
                return 1.0 - smoothstep(0.0, width, min(a, 1.0 - a));
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 uv     = IN.uv - 0.5;
                float  r      = length(uv);
                if (r < 1e-5) return float4(0.0, 0.0, 0.0, 1.0);
                float  angle  = atan2(uv.y, uv.x);
                float  nAngle = angle / (2.0 * PI) + 0.5;

                float t      = _Time.y;
                float charge = _ChargeAmount;

                float rotCW  = nAngle + t * _RotationSpeed * 0.159;
                float rotMid = nAngle - t * _RotationSpeed * 0.111;

                float3 ringCol  = lerp(_ColorRest.rgb,        _ColorCharge.rgb,        charge);
                float3 innerCol = lerp(_ColorTeal.rgb,        _ColorCharge.rgb * 1.1,  charge);
                float3 runeCol  = lerp(_ColorRest.rgb * 0.35, _ColorRune.rgb,          charge);
                float3 spokeCol = lerp(_ColorTeal.rgb * 0.25, float3(1.0, 0.9, 0.7),   charge);
                float3 glowCol  = lerp(_ColorTeal.rgb,        _ColorCharge.rgb * 1.6,  charge);

                float3 col = 0;

                col += glowCol * exp(-r * r * 28.0) * (0.35 + charge * 1.3);

                float spokeV = Spoke(rotCW, 6.0, 0.055);
                float spokeR = saturate(r / 0.44) * (1.0 - smoothstep(0.40, 0.45, r));
                col += spokeCol * spokeV * spokeR * (0.25 + charge * 0.75);

                float runeMask = smoothstep(0.27, 0.30, r) * (1.0 - smoothstep(0.38, 0.41, r));
                float normMid  = frac(rotMid);
                float seg      = floor(normMid * _RuneCount);
                float localT   = frac(normMid * _RuneCount);

                float h0 = Hash(seg);
                float h1 = Hash(seg +  7.3);
                float h2 = Hash(seg + 13.7);
                float h3 = Hash(seg + 22.1);

                float mark1 = 1.0 - smoothstep(0.0, 0.10, abs(localT - (0.20 + h0 * 0.20)));
                float mark2 = 1.0 - smoothstep(0.0, 0.07, abs(localT - (0.50 + h1 * 0.20)));
                float mark3 = step(0.65, h2) *
                              (1.0 - smoothstep(0.0, 0.06, abs(localT - (0.75 + h3 * 0.15))));
                col += runeCol * saturate(mark1 + mark2 + mark3) * runeMask * 0.85;

                float innerR = 0.26 + charge * 0.03;
                col += innerCol * Ring(r, innerR, _RingThickness * 0.6) * (0.8 + charge * 0.5);

                col += ringCol * Ring(r, 0.44, _RingThickness)         * (1.0 + charge * 0.8);
                col += ringCol * Ring(r, 0.472, _RingThickness * 0.35) * 0.45;

                col *= 1.0 - smoothstep(0.47, 0.50, r);

                return float4(col * _Intensity, 1.0);
            }
            ENDHLSL
        }
    }
}
