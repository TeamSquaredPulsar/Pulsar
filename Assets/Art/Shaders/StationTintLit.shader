// Pulsar: a copy of URP's Sprite-Lit-Default with one addition, the station colour.
// Station tiles are grey where they take the station colour. The sprite's
// secondary texture "_TintMask" says how colourful each pixel is (0 = keep its
// own colour, like glass, the core gem and embers). The shader rebuilds each
// masked pixel in _StationTint at the pixel's own brightness, so dark stays
// dark and pale stays pale (the grey art was fitted to the green tiles with
// the same maths). _StationTint is set per tile by StationTintable (a
// MaterialPropertyBlock), so the SpriteRenderer's Color stays free for Tile's
// selection highlight.
Shader "Pulsar/Station Tint Lit"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _TintMask("Tint Mask", 2D) = "black" {}
        _StationTint("Station Tint", Color) = (1,1,1,1)
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex LitVertex
            #pragma fragment LitFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color        : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _StationTint;
            CBUFFER_END

            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonLitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;

                return o;
            }


            // LinearToSRGB / SRGBToLinear (has an include guard, safe to repeat).
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            TEXTURE2D(_TintMask);
            SAMPLER(sampler_TintMask);

            // The grey pixel's brightness L, rebuilt in the station colour:
            // below the colour's own brightness it's a darker shade of it,
            // above it's blended toward white. "amount" (the mask) mixes
            // between plain grey and that colour. Maths done in sRGB like the art.
            half3 StationRecolour(half3 rgb, half amount)
            {
                if (amount <= 0)
                {
                    return rgb;
                }
            #if defined(UNITY_COLORSPACE_GAMMA)
                half3 grey = rgb;
                half3 tint = _StationTint.rgb;
            #else
                half3 grey = LinearToSRGB(rgb);
                half3 tint = LinearToSRGB(_StationTint.rgb);
            #endif
                half L = grey.g;
                half tintL = max(dot(tint, half3(0.299, 0.587, 0.114)), 0.001);
                half3 shade = L <= tintL
                    ? tint * (L / tintL)
                    : lerp(tint, half3(1, 1, 1), (L - tintL) / max(1 - tintL, 0.001));
                half3 result = lerp(half3(L, L, L), shade, amount);
            #if defined(UNITY_COLORSPACE_GAMMA)
                return result;
            #else
                return SRGBToLinear(result);
            #endif
            }

            half4 StationTexture(float2 uv)
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half amount = SAMPLE_TEXTURE2D(_TintMask, sampler_TintMask, uv).r;
                tex.rgb = StationRecolour(tex.rgb, amount);
                return tex;
            }

            // CommonLitFragment from Lit2DCommon.hlsl, with the recoloured texture.
            half4 LitFragment(Varyings input) : SV_Target
            {
                const half4 main = input.color * StationTexture(input.uv);
                const half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                const half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));

                SurfaceData2D surfaceData;
                InputData2D inputData;

                InitializeSurfaceData(main.rgb, main.a, mask, normalTS, surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);

            #if defined(DEBUG_DISPLAY)
                SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);
                surfaceData.normalWS = input.normalWS;
            #endif

                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                float4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                half4   color           : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START( UnityPerMaterial )
                half4 _Color;
                half4 _StationTint;
            CBUFFER_END

            Varyings NormalsRenderingVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonNormalsVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;

                return o;
            }

            half4 NormalsRenderingFragment(Varyings input) : SV_Target
            {
                return CommonNormalsFragment(input, input.color);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
          
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _StationTint;
            CBUFFER_END

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color *_Color * unity_SpriteColor;
                return o;
            }


            // LinearToSRGB / SRGBToLinear (has an include guard, safe to repeat).
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            TEXTURE2D(_TintMask);
            SAMPLER(sampler_TintMask);

            // The grey pixel's brightness L, rebuilt in the station colour:
            // below the colour's own brightness it's a darker shade of it,
            // above it's blended toward white. "amount" (the mask) mixes
            // between plain grey and that colour. Maths done in sRGB like the art.
            half3 StationRecolour(half3 rgb, half amount)
            {
                if (amount <= 0)
                {
                    return rgb;
                }
            #if defined(UNITY_COLORSPACE_GAMMA)
                half3 grey = rgb;
                half3 tint = _StationTint.rgb;
            #else
                half3 grey = LinearToSRGB(rgb);
                half3 tint = LinearToSRGB(_StationTint.rgb);
            #endif
                half L = grey.g;
                half tintL = max(dot(tint, half3(0.299, 0.587, 0.114)), 0.001);
                half3 shade = L <= tintL
                    ? tint * (L / tintL)
                    : lerp(tint, half3(1, 1, 1), (L - tintL) / max(1 - tintL, 0.001));
                half3 result = lerp(half3(L, L, L), shade, amount);
            #if defined(UNITY_COLORSPACE_GAMMA)
                return result;
            #else
                return SRGBToLinear(result);
            #endif
            }

            half4 StationTexture(float2 uv)
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half amount = SAMPLE_TEXTURE2D(_TintMask, sampler_TintMask, uv).r;
                tex.rgb = StationRecolour(tex.rgb, amount);
                return tex;
            }

            // CommonUnlitFragment from 2DCommon.hlsl, with the recoloured texture.
            half4 UnlitFragment(Varyings input) : SV_Target
            {
                float4 mainTex = input.color * StationTexture(input.uv);

            #if defined(DEBUG_DISPLAY)
                SurfaceData2D surfaceData;
                InputData2D inputData;
                half4 debugColor = 0;

                InitializeSurfaceData(mainTex.rgb, mainTex.a, surfaceData);
                InitializeInputData(input.uv, inputData);
                SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);

                if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
                {
                    return debugColor;
                }
            #endif

                return mainTex;
            }
            ENDHLSL
        }
    }
}
