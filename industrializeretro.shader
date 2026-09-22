Shader "Industrialize/Retro"
{
    Properties
    {
        _PixelWidth ("Pixel Width", Float) = 320
        _PixelHeight ("Pixel Height", Float) = 180

        _ColorLevels ("Color Levels", Range(2, 64)) = 12

        _Gamma ("Gamma", Range(0.1, 3.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Retro"

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
                float _PixelHeight;

                float _ColorLevels;

                float _Gamma;

            CBUFFER_END


            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;


                // =========================================
                // PIXELATION
                // =========================================

                float2 resolution =
                    float2(
                        _PixelWidth,
                        _PixelHeight
                    );

                // Convert 0-1 UV into virtual pixel coordinates
                float2 pixelUV =
                    floor(uv * resolution);

                // Convert back into 0-1 UV
                pixelUV =
                    (pixelUV + 0.5) / resolution;


                // =========================================
                // SAMPLE SCREEN
                // =========================================

                float4 color =
                    SAMPLE_TEXTURE2D_X(
                        _BlitTexture,
                        sampler_PointClamp,
                        pixelUV
                    );


                // =========================================
                // GAMMA
                // =========================================

                color.rgb =
                    pow(
                        max(color.rgb, 0.0001),
                        1.0 / _Gamma
                    );


                // =========================================
                // POSTERIZATION
                // =========================================

                color.rgb =
                    floor(
                        color.rgb * _ColorLevels
                    )
                    / _ColorLevels;


                return color;
            }

            ENDHLSL
        }
    }
}