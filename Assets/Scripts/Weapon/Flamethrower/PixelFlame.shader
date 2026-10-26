Shader "Omnia/Pixel Flame" {
    Properties { [PerRendererData] _MainTex ("Flame", 2D) = "white" {} }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="TransparentCutout" "CanUseSpriteAtlas"="False" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off Blend Off
        Pass {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            Varyings vert(Attributes input) {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 frag(Varyings input) : SV_Target {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(color.a - 0.5h);
                return half4(color.rgb, 1);
            }
            ENDHLSL
        }
    }
}
