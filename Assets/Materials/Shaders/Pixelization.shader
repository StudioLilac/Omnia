Shader "Omnia/Post Processing/Pixelization"
{
    Properties
    {
        _PixelWidth ("Pixel Grid Width", Range(64, 640)) = 320
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Pixelization"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _PixelWidth;
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Keep approximately square pixels at any camera aspect ratio.
                float width = min(max(round(_PixelWidth), 1.0), _ScaledScreenParams.x);
                float height = max(round(width * _ScaledScreenParams.y / _ScaledScreenParams.x), 1.0);
                float2 grid = float2(width, height);

                // Select one source texel per cell, without blending neighboring colors.
                float2 uv = (floor(input.texcoord * grid) + 0.5) / grid;
                return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv, 0);
            }
            ENDHLSL
        }
    }
}
