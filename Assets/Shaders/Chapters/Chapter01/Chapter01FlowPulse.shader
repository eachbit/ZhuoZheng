Shader "ZhuozhengYuan/Chapter01FlowPulse"
{
    Properties
    {
        _Tint ("Tint", Color) = (0.45, 0.93, 1.0, 0.8)
        _CoreColor ("Core Color", Color) = (0.95, 0.99, 1.0, 1.0)
        _RingProgress ("Ring Progress", Range(0.0, 1.4)) = 0.55
        _RingSoftness ("Ring Softness", Range(0.02, 0.4)) = 0.16
        _CenterBoost ("Center Boost", Range(0.0, 6.0)) = 2.0
        _AlphaScale ("Alpha Scale", Range(0.0, 2.5)) = 1.0
        _GlowBoost ("Glow Boost", Range(0.0, 8.0)) = 2.8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha One
        Cull Off
        ZWrite Off

        Pass
        {
            Name "FlowPulseURP"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half4 _CoreColor;
                float _RingProgress;
                float _RingSoftness;
                float _CenterBoost;
                float _AlphaScale;
                float _GlowBoost;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positionInputs.positionCS;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 centered = (input.uv - 0.5) * 2.0;
                float distanceFromCenter = length(centered);
                float halo = saturate(1.0 - distanceFromCenter);
                float inner = smoothstep(_RingProgress - _RingSoftness, _RingProgress, distanceFromCenter);
                float outer = 1.0 - smoothstep(_RingProgress, _RingProgress + _RingSoftness, distanceFromCenter);
                float ring = saturate(inner * outer);
                float core = pow(halo, 3.0) * _CenterBoost;
                float sparkle = 0.9 + 0.1 * sin(_Time.y * 4.0 + distanceFromCenter * 9.0);

                float3 color = _Tint.rgb * (halo * 0.35 + ring * 1.2);
                color += _CoreColor.rgb * core * _GlowBoost;
                float alpha = saturate((halo * 0.25 + ring * 0.95 + core * 0.35) * _Tint.a * _AlphaScale * sparkle);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha One
        Cull Off
        ZWrite Off

        Pass
        {
            Name "FlowPulseBuiltin"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Tint;
            fixed4 _CoreColor;
            float _RingProgress;
            float _RingSoftness;
            float _CenterBoost;
            float _AlphaScale;
            float _GlowBoost;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centered = (i.uv - 0.5) * 2.0;
                float distanceFromCenter = length(centered);
                float halo = saturate(1.0 - distanceFromCenter);
                float inner = smoothstep(_RingProgress - _RingSoftness, _RingProgress, distanceFromCenter);
                float outer = 1.0 - smoothstep(_RingProgress, _RingProgress + _RingSoftness, distanceFromCenter);
                float ring = saturate(inner * outer);
                float core = pow(halo, 3.0) * _CenterBoost;
                float sparkle = 0.9 + 0.1 * sin(_Time.y * 4.0 + distanceFromCenter * 9.0);

                float3 color = _Tint.rgb * (halo * 0.35 + ring * 1.2);
                color += _CoreColor.rgb * core * _GlowBoost;
                float alpha = saturate((halo * 0.25 + ring * 0.95 + core * 0.35) * _Tint.a * _AlphaScale * sparkle);
                return fixed4(color, alpha);
            }
            ENDCG
        }
    }
}
