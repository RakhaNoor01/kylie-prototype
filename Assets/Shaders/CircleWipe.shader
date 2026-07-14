Shader "UI/CircleWipe"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0,0,0,1)
        _Radius ("Radius", Range(0, 2)) = 1.0
        _Softness ("Edge Softness", Range(0, 0.1)) = 0.01
        _AspectRatio ("Aspect Ratio", Float) = 1.777
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _Radius;
                float  _Softness;
                float  _AspectRatio;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // UV dari 0-1, geser ke center (-0.5 .. 0.5)
                float2 uv = IN.uv - 0.5;

                // Koreksi aspect ratio supaya lingkaran tidak oval
                uv.x *= _AspectRatio;

                // Jarak dari center
                float dist = length(uv);

                // Smoothstep: dalam radius = transparan (keliatan), luar = hitam
                float alpha = smoothstep(_Radius - _Softness, _Radius + _Softness, dist);

                return half4(_Color.rgb, alpha * _Color.a);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
}
