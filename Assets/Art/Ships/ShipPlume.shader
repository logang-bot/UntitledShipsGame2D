// Engine flames for the 3D ship models (the "<engine>_plume_outer" and
// "_plume_core" cones of the design's engine()). Additive, double-sided, no
// depth write. Seen from straight above, a uniformly lit cone is a flat
// triangle, so the glow fades along the flame (brightest at the nozzle, the
// cone's apex, where the exported uv.y is 0; 1 at the open end) and toward the
// silhouette edges, where the view grazes the surface. ShipThrusters drives
// _EmissionColor per renderer; _FlashColor (alpha = amount) is ShipVisual's
// hit flash, as on ShipMesh.shader.
Shader "UntitledShips/ShipPlume"
{
    Properties
    {
        [HDR] _EmissionColor ("Emission", Color) = (1, 1, 1, 1)
        _TailFade ("Tail Fade (power along the flame)", Range(0.2, 4)) = 1.6
        _EdgeSoftness ("Edge Softness", Range(0, 1)) = 0.75
        _FlashColor ("Flash Color (alpha = amount)", Color) = (1, 1, 1, 0)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" }

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
                half4 _EmissionColor;
                half _TailFade;
                half _EdgeSoftness;
                half4 _FlashColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS, false);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
                output.uv = input.uv;
                return output;
            }

            half EdgeFade(float3 normalWS, float3 viewDirWS)
            {
                float len = length(normalWS);
                half facing = len > 0.001 ? abs(dot(normalWS / len, normalize(viewDirWS))) : 1.0;
                return lerp(1.0, facing, _EdgeSoftness);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half along = pow(saturate(1.0 - input.uv.y), _TailFade);
                half3 color = _EmissionColor.rgb * along * EdgeFade(input.normalWS, input.viewDirWS);
                color = lerp(color, _FlashColor.rgb * along, _FlashColor.a);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
