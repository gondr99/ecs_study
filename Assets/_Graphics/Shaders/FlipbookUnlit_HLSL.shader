// 가로 한 줄 스프라이트 시트(flipbook)를 프레임 번호로 재생하는 Unlit 셰이더.
// Entities Graphics(BatchRendererGroup)로 그려지려면 DOTS Instancing을 지원해야 한다.
// _Frame은 엔티티마다 다른 값(FlipbookFrame 컴포넌트)을 받도록 DOTS instanced property로 선언한다.
// 공부용 HLSL 버전. 실제 머티리얼은 FlipbookUnlit.shadergraph를 사용한다.
Shader "ECSStudy/FlipbookUnlit (HLSL)"
{
    Properties
    {
        [MainTexture] _BaseMap("Flipbook", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,1)
        [NoScaleOffset] _EmissionMap("Emission Map", 2D) = "black" {}
        [HDR] _EmissionColor("Emission Color", Color) = (1,1,1,1)
        _FrameCount("Frame Count", Float) = 12
        _Frame("Frame", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            // LightMode 태그가 없으면 SRPDefaultUnlit로 취급되어 2D Renderer도 이 패스를 그린다.
            Name "Unlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            // DOTS_INSTANCING_ON 키워드 + target 4.5
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);     SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

            // SRP Batcher / DOTS Instancing 모두 머티리얼 프로퍼티가 UnityPerMaterial CBUFFER에 있어야 한다.
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _EmissionColor;
                float _FrameCount;
                float _Frame;
            CBUFFER_END

            // 여기 선언된 프로퍼티만 엔티티별 값(MaterialProperty 컴포넌트)으로 덮어쓸 수 있다.
            // 엔티티에 해당 컴포넌트가 없으면 머티리얼 값(default)이 쓰인다.
            // (Shader Graph에서는 프로퍼티의 Override Property Declaration > Hybrid Per Instance가 이 블록을 만든다)
            #ifdef UNITY_DOTS_INSTANCING_ENABLED
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float, _Frame)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

                #define _Frame UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Frame)
            #endif

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                // 인스턴스 ID를 세팅해야 월드 행렬과 _Frame을 이 엔티티의 값으로 읽는다.
                UNITY_SETUP_INSTANCE_ID(input);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                // 프레임 계산을 버텍스에서 끝내서 프래그먼트는 인스턴스 데이터가 필요 없게 한다.
                float frameCount = max(_FrameCount, 1.0);
                float frame = clamp(floor(_Frame), 0.0, frameCount - 1.0);
                output.uv = float2((input.uv.x + frame) / frameCount, input.uv.y);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb;
                // HDR 값(1 초과)이 되면 Bloom이 이 픽셀을 번지게 한다.
                return half4(baseCol.rgb + emission * baseCol.a, baseCol.a);
            }
            ENDHLSL
        }
    }
}
