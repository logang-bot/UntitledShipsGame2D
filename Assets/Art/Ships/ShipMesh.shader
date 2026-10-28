// Shading for the 3D ship models under the URP 2D Renderer. Mesh2D-Lit only
// lights through normal-map textures and ignores mesh normals, which would
// leave the models flat, so this does its own fixed-direction Lambert from
// the vertex normals, plus ambient and HDR emission (bloom picks up the
// neon). _FlashColor (alpha = amount) is set per renderer by ShipVisual for
// the hit flash. Render state copies Mesh2D-Lit-Default: Transparent queue,
// ZWrite On, Cull Back. Plumes switch _SrcBlend/_DstBlend/_ZWrite to
// additive (One, One, Off).
Shader "UntitledShips/ShipMesh"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.23, 0.27, 0.35, 1)
        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 1)
        _LightDir ("Light Direction (world, toward light)", Vector) = (-0.4, 0.6, -0.7, 0)
        _Ambient ("Ambient", Range(0, 1)) = 0.35
        _FlashColor ("Flash Color (alpha = amount)", Color) = (1, 1, 1, 0)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
                float4 _LightDir;
                half _Ambient;
                half4 _FlashColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS, false);
                return output;
            }

            half Lambert(float3 normalWS)
            {
                float len = length(normalWS);
                return len > 0.001 ? saturate(dot(normalWS / len, normalize(_LightDir.xyz))) : 1.0;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half light = _Ambient + (1.0 - _Ambient) * Lambert(input.normalWS);
                half3 color = _BaseColor.rgb * light + _EmissionColor.rgb;
                color = lerp(color, _FlashColor.rgb, _FlashColor.a);
                return half4(color, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
