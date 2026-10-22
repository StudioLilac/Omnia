Shader "Omnia/Sprite Pixel Silhouette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _SpriteUVRect ("Sprite UV Rect", Vector) = (0, 0, 1, 1)
        [HideInInspector] _PixelUV ("UV Per Sprite Pixel (xy: X, zw: Y)", Vector) = (0, 0, 0, 0)
        [HideInInspector] _Direction ("Direction Index", Float) = 0
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

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _SpriteUVRect;
                float4 _PixelUV;
                float _Direction;
            CBUFFER_END

            // Order must match PixelOutliner.Directions.
            static const float2 Directions[4] = { float2(-1, 0), float2(1, 0), float2(0, 1), float2(0, -1) };

            half SampleAlpha(float2 uv)
            {
                half inside = all(uv >= _SpriteUVRect.xy) && all(uv <= _SpriteUVRect.zw);
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a * inside;
            }

            float2 PixelStep(float2 offset)
            {
                return _PixelUV.xy * offset.x + _PixelUV.zw * offset.y;
            }

            Varyings SilhouetteVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();

                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(UnityFlipSprite(input.positionOS, unity_SpriteProps.xy));
                output.uv = input.uv;
                output.color = input.color * unity_SpriteColor;
                return output;
            }

            half4 SilhouetteFragment(Varyings input) : SV_Target
            {
                int index = (int)_Direction;
                float2 direction = Directions[index];
                half alpha = SampleAlpha(input.uv) * (1 - SampleAlpha(input.uv + PixelStep(direction)));
                // A lower-index direction already draws this pixel; skipping it prevents double blending.
                [unroll] for (int j = 0; j < 3; j++)
                    if (j < index) alpha *= 1 - SampleAlpha(input.uv + PixelStep(direction - Directions[j]));
                return half4(input.color.rgb, input.color.a * alpha);
            }
            ENDHLSL
        }
    }
}
