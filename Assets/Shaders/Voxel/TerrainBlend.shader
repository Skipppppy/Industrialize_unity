Shader "Voxel/TerrainBlend"
{
    Properties
    {
        _Tex0("Dirt",2D)="white"{}
        _Tex1("Grass",2D)="white"{}
        _Tex2("Sand",2D)="white"{}
        _Tex3("Clay",2D)="white"{}
        _Tex4("Quartz Sand",2D)="white"{}
        _Tex5("Peat",2D)="white"{}
        _Tex6("Coal",2D)="white"{}
        _Tex7("Copper",2D)="white"{}
        _Tex8("Iron",2D)="white"{}
        _Tex9("Magnetite",2D)="white"{}
        _Tex10("Mica",2D)="white"{}
        _Tex11("Rock Salt",2D)="white"{}
        _Tex12("Graphite",2D)="white"{}
        _Tex13("Cassiterite",2D)="white"{}
        _Tex14("Bauxite",2D)="white"{}
        _Tex15("Water",2D)="white"{}

        _Color0("Dirt Color",Color)=(1,1,1,1)
        _Color1("Grass Color",Color)=(1,1,1,1)
        _Color2("Sand Color",Color)=(1,1,1,1)
        _Color3("Clay Color",Color)=(1,1,1,1)
        _Color4("Quartz Sand Color",Color)=(1,1,1,1)
        _Color5("Peat Color",Color)=(1,1,1,1)
        _Color6("Coal Color",Color)=(1,1,1,1)
        _Color7("Copper Color",Color)=(1,1,1,1)
        _Color8("Iron Color",Color)=(1,1,1,1)
        _Color9("Magnetite Color",Color)=(1,1,1,1)
        _Color10("Mica Color",Color)=(1,1,1,1)
        _Color11("Rock Salt Color",Color)=(1,1,1,1)
        _Color12("Graphite Color",Color)=(1,1,1,1)
        _Color13("Cassiterite Color",Color)=(1,1,1,1)
        _Color14("Bauxite Color",Color)=(1,1,1,1)
        _Color15("Water Color",Color)=(1,1,1,1)

        _Smoothness("Smoothness",Range(0,1))=0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "ForwardLit"

            Tags
            {
                "LightMode"="UniversalForward"
            }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_Tex0);
            SAMPLER(sampler_Tex0);

            TEXTURE2D(_Tex1);
            SAMPLER(sampler_Tex1);

            TEXTURE2D(_Tex2);
            SAMPLER(sampler_Tex2);

            TEXTURE2D(_Tex3);
            SAMPLER(sampler_Tex3);

            TEXTURE2D(_Tex4);
            SAMPLER(sampler_Tex4);

            TEXTURE2D(_Tex5);
            SAMPLER(sampler_Tex5);

            TEXTURE2D(_Tex6);
            SAMPLER(sampler_Tex6);

            TEXTURE2D(_Tex7);
            SAMPLER(sampler_Tex7);

            TEXTURE2D(_Tex8);
            SAMPLER(sampler_Tex8);

            TEXTURE2D(_Tex9);
            SAMPLER(sampler_Tex9);

            TEXTURE2D(_Tex10);
            SAMPLER(sampler_Tex10);

            TEXTURE2D(_Tex11);
            SAMPLER(sampler_Tex11);

            TEXTURE2D(_Tex12);
            SAMPLER(sampler_Tex12);

            TEXTURE2D(_Tex13);
            SAMPLER(sampler_Tex13);

            TEXTURE2D(_Tex14);
            SAMPLER(sampler_Tex14);

            TEXTURE2D(_Tex15);
            SAMPLER(sampler_Tex15);

            CBUFFER_START(UnityPerMaterial)

            float4 _Color0;
            float4 _Color1;
            float4 _Color2;
            float4 _Color3;
            float4 _Color4;
            float4 _Color5;
            float4 _Color6;
            float4 _Color7;
            float4 _Color8;
            float4 _Color9;
            float4 _Color10;
            float4 _Color11;
            float4 _Color12;
            float4 _Color13;
            float4 _Color14;
            float4 _Color15;

            float _Smoothness;

            CBUFFER_END

            struct Attributes
            {
                float4 positionOS:POSITION;
                float3 normalOS:NORMAL;
                float2 uv:TEXCOORD0;
                float4 blend:TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionHCS:SV_POSITION;
                float3 positionWS:TEXCOORD0;
                float3 normalWS:TEXCOORD1;
                float2 uv:TEXCOORD2;
                float4 blend:TEXCOORD3;
            };

            Varyings vert(Attributes _input)
            {
                Varyings _output;

                VertexPositionInputs _position=
                    GetVertexPositionInputs(
                        _input.positionOS.xyz
                    );

                VertexNormalInputs _normal=
                    GetVertexNormalInputs(
                        _input.normalOS
                    );

                _output.positionHCS=
                    _position.positionCS;

                _output.positionWS=
                    _position.positionWS;

                _output.normalWS=
                    NormalizeNormalPerVertex(
                        _normal.normalWS
                    );

                _output.uv=
                    _input.uv;

                _output.blend=
                    _input.blend;

                return _output;
            }

            float4 sampletexture(
                float _id,
                float2 _uv)
            {
                int _index=(int)round(_id);

                if(_index==0)
                    return SAMPLE_TEXTURE2D(
                        _Tex0,
                        sampler_Tex0,
                        _uv
                    )*_Color0;

                if(_index==1)
                    return SAMPLE_TEXTURE2D(
                        _Tex1,
                        sampler_Tex1,
                        _uv
                    )*_Color1;

                if(_index==2)
                    return SAMPLE_TEXTURE2D(
                        _Tex2,
                        sampler_Tex2,
                        _uv
                    )*_Color2;

                if(_index==3)
                    return SAMPLE_TEXTURE2D(
                        _Tex3,
                        sampler_Tex3,
                        _uv
                    )*_Color3;

                if(_index==4)
                    return SAMPLE_TEXTURE2D(
                        _Tex4,
                        sampler_Tex4,
                        _uv
                    )*_Color4;

                if(_index==5)
                    return SAMPLE_TEXTURE2D(
                        _Tex5,
                        sampler_Tex5,
                        _uv
                    )*_Color5;

                if(_index==6)
                    return SAMPLE_TEXTURE2D(
                        _Tex6,
                        sampler_Tex6,
                        _uv
                    )*_Color6;

                if(_index==7)
                    return SAMPLE_TEXTURE2D(
                        _Tex7,
                        sampler_Tex7,
                        _uv
                    )*_Color7;

                if(_index==8)
                    return SAMPLE_TEXTURE2D(
                        _Tex8,
                        sampler_Tex8,
                        _uv
                    )*_Color8;

                if(_index==9)
                    return SAMPLE_TEXTURE2D(
                        _Tex9,
                        sampler_Tex9,
                        _uv
                    )*_Color9;

                if(_index==10)
                    return SAMPLE_TEXTURE2D(
                        _Tex10,
                        sampler_Tex10,
                        _uv
                    )*_Color10;

                if(_index==11)
                    return SAMPLE_TEXTURE2D(
                        _Tex11,
                        sampler_Tex11,
                        _uv
                    )*_Color11;

                if(_index==12)
                    return SAMPLE_TEXTURE2D(
                        _Tex12,
                        sampler_Tex12,
                        _uv
                    )*_Color12;

                if(_index==13)
                    return SAMPLE_TEXTURE2D(
                        _Tex13,
                        sampler_Tex13,
                        _uv
                    )*_Color13;

                if(_index==14)
                    return SAMPLE_TEXTURE2D(
                        _Tex14,
                        sampler_Tex14,
                        _uv
                    )*_Color14;

                return SAMPLE_TEXTURE2D(
                    _Tex15,
                    sampler_Tex15,
                    _uv
                )*_Color15;
            }

            half4 frag(Varyings _input):SV_Target
            {
                float2 _uv=
                    _input.uv;

                float _materiala=
                    _input.blend.x;

                float _materialb=
                    _input.blend.y;

                float _blend=
                    saturate(
                        _input.blend.z
                    );

                half4 _a=
                    sampletexture(
                        _materiala,
                        _uv
                    );

                half4 _b=
                    sampletexture(
                        _materialb,
                        _uv
                    );

                half4 _surface=
                    lerp(
                        _a,
                        _b,
                        _blend
                    );

                float3 _normal=
                    normalize(
                        _input.normalWS
                    );

                float3 _lightdirection=
                    normalize(
                        _MainLightPosition.xyz
                    );

                float _ndotl=
                    saturate(
                        dot(
                            _normal,
                            _lightdirection
                        )
                    );

                float3 _lighting=
                    _MainLightColor.rgb*
                    (
                        0.35+
                        _ndotl*
                        0.65
                    );

                float3 _ambient=
                    SampleSH(
                        _normal
                    );

                float3 _finalcolor=
                    _surface.rgb*
                    (
                        _lighting+
                        _ambient
                    );

                return half4(
                    _finalcolor,
                    1
                );
            }

            ENDHLSL
        }
    }
}