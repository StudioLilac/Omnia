Shader "Omnia/Sprite Pixel Silhouette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _SpriteUVRect ("Sprite UV Rect", Vector) = (0, 0, 1, 1)
        [HideInInspector] _PixelUV ("UV Per Sprite Pixel (xy: X, zw: Y)", Vector) = (0, 0, 0, 0)
        [HideInInspector] _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex SilhouetteVertex
            #pragma fragment SilhouetteFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _SpriteUVRect;
                float4 _PixelUV;
                half4 _OutlineColor;
            CBUFFER_END

            half SampleAlpha(float2 uv)
            {
                half inside = all(uv >= _SpriteUVRect.xy) && all(uv <= _SpriteUVRect.zw);
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a * inside;
            }

            half IsVisible(float2 uv)
            {
                return step(0.1, SampleAlpha(uv));
            }

            Varyings SilhouetteVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);

                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            half4 SilhouetteFragment(Varyings input) : SV_Target
            {
                half neighbors = max(max(IsVisible(input.uv - _PixelUV.xy), IsVisible(input.uv + _PixelUV.xy)),
                                     max(IsVisible(input.uv - _PixelUV.zw), IsVisible(input.uv + _PixelUV.zw)));
                half alpha = neighbors * (1 - IsVisible(input.uv));
                return half4(_OutlineColor.rgb, _OutlineColor.a * alpha);
            }
            ENDHLSL
        }
    }
}
