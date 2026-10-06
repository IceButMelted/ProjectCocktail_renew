// AtlasTileIndex.shader  (Universal Render Pipeline)
// Tiles ONE cell of a texture atlas, chosen by index.
//
// Index order (Columns = 4, Rows = 4), as you see the image:
//    0  1  2  3
//    4  5  6  7
//    8  9 10 11
//   12 13 14 15
//
// Features: baked lightmaps + Meta pass (works with Bake), shadowmask,
// main light + shadows, additional lights, light probes, fog,
// mip-seam fix (gradient sampling), padding against neighbor bleeding,
// SRP Batcher compatible, GPU instancing.

Shader "Custom/URP/AtlasTileIndex"
{
    Properties
    {
        [MainTexture] _BaseMap ("Atlas", 2D) = "white" {}
        [MainColor]   _BaseColor ("Tint", Color) = (1, 1, 1, 1)

        [Header(Atlas Cell)]
        _Index ("Index (0 = first)", Float) = 0
        [IntRange] _Columns ("Columns", Range(1, 32)) = 4
        [IntRange] _Rows ("Rows", Range(1, 32)) = 4
        [Toggle(_INDEX_FROM_BOTTOM)] _IndexFromBottom ("Count From Bottom-Left", Float) = 0

        [Header(Tiling)]
        _Tiling ("Tiling (XY)", Vector) = (1, 1, 0, 0)
        _Padding ("Padding (~0.5 / cell pixels)", Range(0, 0.05)) = 0.002
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        // Everything in one CBUFFER, so the SRP Batcher works
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            float  _Index;
            float  _Columns;
            float  _Rows;
            float4 _Tiling;
            float  _Padding;
        CBUFFER_END

        // Index -> tile size and offset in 0..1 atlas space
        void AtlasCellFromIndex(float index, float columns, float rows,
                                out float2 tileSize, out float2 tileOffset)
        {
            columns = max(columns, 1.0);
            rows    = max(rows, 1.0);
            float count = columns * rows;

            float idx = round(index);                         // guards against 2.9999 -> 2
            idx = fmod(fmod(idx, count) + count, count);      // wrap, also handles negatives

            float column = fmod(idx, columns);
            float row    = floor(idx / columns);

        #if !defined(_INDEX_FROM_BOTTOM)
            row = (rows - 1.0) - row;                         // Unity UV origin is bottom-left
        #endif

            tileSize   = 1.0 / float2(columns, rows);
            tileOffset = float2(column, row) * tileSize;
        }

        // Sample the selected cell, repeated by _Tiling, without mip seams
        half4 SampleAtlasCell(float2 uv)
        {
            float2 tileSize, tileOffset;
            AtlasCellFromIndex(_Index, _Columns, _Rows, tileSize, tileOffset);

            float2 tiled = uv * _Tiling.xy;
            float2 local = frac(tiled);
            local = lerp(_Padding.xx, 1.0 - _Padding.xx, local);

            float2 atlasUV = local * tileSize + tileOffset;

            // Gradients from the continuous (pre-frac) UVs remove seam lines
            float2 dx = ddx(tiled) * tileSize;
            float2 dy = ddy(tiled) * tileSize;

            return SAMPLE_TEXTURE2D_GRAD(_BaseMap, sampler_BaseMap, atlasUV, dx, dy);
        }
        ENDHLSL

        // -----------------------------------------------------------------
        // Forward lit pass
        // -----------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma shader_feature_local _INDEX_FROM_BOTTOM

            // Realtime lights and shadows
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            // Baked lighting
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS       : POSITION;
                float3 normalOS         : NORMAL;
                float2 uv               : TEXCOORD0;
                float2 staticLightmapUV : TEXCOORD1;   // lightmap UVs (UV2 in the mesh importer)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
                float2 lightmapUV : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   nrm = GetVertexNormalInputs(input.normalOS);

                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS   = nrm.normalWS;
                output.uv         = input.uv;
                output.fogFactor  = ComputeFogFactor(pos.positionCS.z);
                output.lightmapUV = input.staticLightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 albedo = SampleAtlasCell(input.uv) * _BaseColor;
                float3 n = normalize(input.normalWS);

                // ---- Baked / indirect light ----
            #if defined(LIGHTMAP_ON)
                half4 encodedLM = SAMPLE_TEXTURE2D(unity_Lightmap, samplerunity_Lightmap, input.lightmapUV);
                half3 bakedGI = DecodeLightmap(encodedLM,
                                   half4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0h, 0.0h));
            #else
                half3 bakedGI = SampleSH(n);                  // light probes / ambient
            #endif

                half4 shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);

                // ---- Main light (realtime or mixed) ----
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord, input.positionWS, shadowMask);

                MixRealtimeAndBakedGI(mainLight, n, bakedGI);  // handles Subtractive mode

                half3 lighting = bakedGI;
                lighting += mainLight.color
                          * (mainLight.distanceAttenuation * mainLight.shadowAttenuation)
                          * saturate(dot(n, mainLight.direction));

                // ---- Realtime / mixed point & spot lights ----
            #if defined(_ADDITIONAL_LIGHTS)
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < lightCount; ++i)
                {
                    Light light = GetAdditionalLight(i, input.positionWS, shadowMask);
                    lighting += light.color
                              * (light.distanceAttenuation * light.shadowAttenuation)
                              * saturate(dot(n, light.direction));
                }
            #endif

                half3 color = albedo.rgb * lighting;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // -----------------------------------------------------------------
        // Meta pass: tells the lightmapper the surface color (needed for Bake)
        // -----------------------------------------------------------------
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            #pragma vertex vertMeta
            #pragma fragment fragMeta
            #pragma shader_feature_local _INDEX_FROM_BOTTOM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv0        : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
                float2 uv2        : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings vertMeta(Attributes input)
            {
                Varyings output;
                output.positionCS = MetaVertexPosition(input.positionOS, input.uv1, input.uv2,
                                                       unity_LightmapST, unity_DynamicLightmapST);
                output.uv = input.uv0;
                return output;
            }

            half4 fragMeta(Varyings input) : SV_Target
            {
                MetaInput metaInput = (MetaInput)0;
                metaInput.Albedo   = (SampleAtlasCell(input.uv) * _BaseColor).rgb;
                metaInput.Emission = half3(0, 0, 0);
                return MetaFragment(metaInput);
            }
            ENDHLSL
        }

        // -----------------------------------------------------------------
        // Shadow caster pass (lets the object cast shadows)
        // -----------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vertShadow(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);

            #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 lightDir = normalize(_LightPosition - positionWS);
            #else
                float3 lightDir = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));

            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 fragShadow(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        // -----------------------------------------------------------------
        // Depth only pass (depth texture, SSAO, etc.)
        // -----------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vertDepth(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half fragDepth(Varyings input) : SV_Target { return input.positionCS.z; }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
