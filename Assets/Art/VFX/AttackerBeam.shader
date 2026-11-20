// The Attacker's Nova Beam body under the URP 2D Renderer: an additive,
// HDR column drawn on a quad (uv.x across the beam, uv.y along it). A white
// core band, warm edge bands whose hue drifts through three colors, noise
// streaks flowing upward, and a flickering, wobbling width. The colors,
// _Intensity (0..1 ramp) and _Length (world units, so the noise doesn't
// stretch with the beam) come from AttackerBeamVisual through a
// MaterialPropertyBlock. Render state like ShipMesh's additive plumes:
// Transparent queue, Blend One One, ZWrite Off.
Shader "UntitledShips/AttackerBeam"
{
    Properties
    {
        [HDR] _CoreColor ("Core", Color) = (3, 3, 3, 1)
        [HDR] _EdgeColorA ("Edge A", Color) = (2, 0.8, 0.3, 1)
        [HDR] _EdgeColorB ("Edge B", Color) = (2, 1.2, 0.4, 1)
        [HDR] _EdgeColorC ("Edge C", Color) = (2, 0.4, 0.15, 1)
        _Intensity ("Intensity", Range(0, 1)) = 1
        _Length ("Length (world units)", Float) = 8
        _CoreWidth ("Core Width", Range(0.05, 0.9)) = 0.38
        _FlowSpeed ("Flow Speed", Float) = 9
        _HueSpeed ("Hue Speed", Float) = 0.7
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _EdgeColorA;
                half4 _EdgeColorB;
                half4 _EdgeColorC;
                half _Intensity;
                float _Length;
                half _CoreWidth;
                float _FlowSpeed;
                float _HueSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Smooth value noise: random values on a unit grid, blended
            // with a smoothstep curve between the four corners.
            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash(cell);
                float b = Hash(cell + float2(1, 0));
                float c = Hash(cell + float2(0, 1));
                float d = Hash(cell + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Walks a looping orange -> amber -> red-gold cycle; h in [0, 1).
            half3 WarmHue(float h)
            {
                float s = frac(h) * 3.0;
                if (s < 1.0) return lerp(_EdgeColorA.rgb, _EdgeColorB.rgb, s);
                if (s < 2.0) return lerp(_EdgeColorB.rgb, _EdgeColorC.rgb, s - 1.0);
                return lerp(_EdgeColorC.rgb, _EdgeColorA.rgb, s - 2.0);
            }

            // 0 at the beam's centre line, 1 at its edge, with a wobble so the
            // width ripples as it flows.
            float EdgeDistance(float2 uv, float along, float t)
            {
                float wobble = 0.06 * sin(along * 7.0 - t * 38.0) + 0.08 * (ValueNoise(float2(along * 2.5 - t * 14.0, 0.5)) - 0.5);
                return abs(uv.x - 0.5) * 2.0 + wobble;
            }

            half3 BeamColor(float2 uv, float along, float t)
            {
                float edge = EdgeDistance(uv, along, t);
                float streak = ValueNoise(float2(uv.x * 7.0, along * 1.6 - t * _FlowSpeed));
                float glow = pow(saturate(1.0 - edge), 1.6);
                float core = smoothstep(_CoreWidth, _CoreWidth * 0.35, edge);
                half3 warm = WarmHue(t * _HueSpeed + along * 0.12 + streak * 0.35);
                return warm * glow * (0.55 + 0.9 * streak) + _CoreColor.rgb * core * (0.8 + 0.35 * streak);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float t = _Time.y;
                float along = input.uv.y * _Length;
                float flicker = 0.92 + 0.08 * sin(t * 83.0) * sin(t * 51.0);
                float muzzleFade = smoothstep(0.0, 0.015, input.uv.y);
                return half4(BeamColor(input.uv, along, t) * flicker * muzzleFade * _Intensity, 1.0);
            }
            ENDHLSL
        }
    }
}
