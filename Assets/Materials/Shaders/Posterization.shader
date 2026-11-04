Shader "Omnia/Post Processing/Posterization"
{
    Properties
    {
        [HideInInspector] _PaletteCount ("Palette Count", Float) = 1
        [NoScaleOffset] _PaletteTexture ("Palette Texture", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Posterization"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #define MAX_PALETTE_COLORS 64

            CBUFFER_START(UnityPerMaterial)
                float _PaletteCount;
            CBUFFER_END

            TEXTURE2D(_PaletteTexture);

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
                float3 closest = LOAD_TEXTURE2D(_PaletteTexture, int2(0, 0)).rgb;
                float closestDistance = dot(source.rgb - closest, source.rgb - closest);
                int paletteCount = clamp((int)round(_PaletteCount), 1, MAX_PALETTE_COLORS);

                [loop]
                for (int index = 1; index < paletteCount; index++)
                {
                    float3 candidate = LOAD_TEXTURE2D(_PaletteTexture, int2(index, 0)).rgb;
                    float distance = dot(source.rgb - candidate, source.rgb - candidate);
                    if (distance < closestDistance)
                    {
                        closest = candidate;
                        closestDistance = distance;
                    }
                }

                return half4(closest, source.a);
            }
            ENDHLSL
        }
    }

    CustomEditor "Omnia.Editor.PosterizationShaderGUI"
}
