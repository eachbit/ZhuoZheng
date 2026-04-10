Shader "ZhuozhengYuan/Chapter01FlowTrail"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.45, 0.93, 1.0, 0.75)
        _AccentColor ("Accent Color", Color) = (0.95, 0.99, 1.0, 1.0)
        _FlowSpeed ("Flow Speed", Float) = 2.4
        _EdgePower ("Edge Power", Range(0.15, 4.0)) = 1.8
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
            Name "FlowTrailURP"

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
                half4 _BaseColor;
                half4 _AccentColor;
                float _FlowSpeed;
                float _EdgePower;
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
                float along = input.uv.x;
                float across = input.uv.y;
                float widthMask = saturate(1.0 - abs(across - 0.5) * 2.0);
                widthMask = pow(widthMask, max(0.15, _EdgePower));

                float streamA = 0.5 + 0.5 * sin(along * 26.0 - _Time.y * (_FlowSpeed * 6.0));
                float streamB = 0.5 + 0.5 * sin(along * 12.0 + _Time.y * (_FlowSpeed * 2.4) + across * 7.0);
                float crest = smoothstep(0.76, 0.98, streamA) * widthMask;
                float body = saturate(widthMask * 0.7 + streamB * 0.3);
                float shimmer = 0.82 + 0.18 * sin(_Time.y * (_FlowSpeed * 3.0) + along * 5.0);

                float3 color = _BaseColor.rgb * (0.35 + body * 0.85);
                color += _AccentColor.rgb * crest * _GlowBoost;

                float alpha = saturate((_BaseColor.a * (0.18 + widthMask * 0.92) + crest * 0.35) * shimmer * _AlphaScale);
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
            Name "FlowTrailBuiltin"

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

            fixed4 _BaseColor;
            fixed4 _AccentColor;
            float _FlowSpeed;
            float _EdgePower;
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
                float along = i.uv.x;
                float across = i.uv.y;
                float widthMask = saturate(1.0 - abs(across - 0.5) * 2.0);
                widthMask = pow(widthMask, max(0.15, _EdgePower));

                float streamA = 0.5 + 0.5 * sin(along * 26.0 - _Time.y * (_FlowSpeed * 6.0));
                float streamB = 0.5 + 0.5 * sin(along * 12.0 + _Time.y * (_FlowSpeed * 2.4) + across * 7.0);
                float crest = smoothstep(0.76, 0.98, streamA) * widthMask;
                float body = saturate(widthMask * 0.7 + streamB * 0.3);
                float shimmer = 0.82 + 0.18 * sin(_Time.y * (_FlowSpeed * 3.0) + along * 5.0);

                float3 color = _BaseColor.rgb * (0.35 + body * 0.85);
                color += _AccentColor.rgb * crest * _GlowBoost;

                float alpha = saturate((_BaseColor.a * (0.18 + widthMask * 0.92) + crest * 0.35) * shimmer * _AlphaScale);
                return fixed4(color, alpha);
            }
            ENDCG
        }
    }
}
