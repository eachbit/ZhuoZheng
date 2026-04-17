Shader "ZhuozhengYuan/GuideRibbon"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.9, 0.82, 0.55, 0.75)
        _AccentColor ("Accent Color", Color) = (1.0, 0.98, 0.86, 1.0)
        _FlowSpeed ("Flow Speed", Float) = 2.2
        _PulseSpeed ("Pulse Speed", Float) = 1.25
        _EdgeSoftness ("Edge Softness", Range(0.1, 3.0)) = 1.3
        _AlphaScale ("Alpha Scale", Range(0.0, 2.0)) = 1.0
        _GlowBoost ("Glow Boost", Range(0.0, 6.0)) = 3.0
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

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "GuideRibbonURP"

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
                float _PulseSpeed;
                float _EdgeSoftness;
                float _AlphaScale;
                float _GlowBoost;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = vertexInput.positionCS;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float edgeMask = saturate(1.0 - abs(input.uv.x - 0.5) * 2.0);
                edgeMask = pow(edgeMask, max(0.15, _EdgeSoftness));
                float centerMask = smoothstep(0.08, 0.96, edgeMask);

                float flow = 0.5 + 0.5 * sin(input.uv.y * 18.0 - _Time.y * (_FlowSpeed * 4.0));
                float subFlow = 0.5 + 0.5 * sin(input.uv.y * 8.0 + _Time.y * (_FlowSpeed * 1.6));
                float drift = 0.5 + 0.5 * sin(input.uv.y * 5.5 + _Time.y * (_FlowSpeed * 1.15) + (input.uv.x - 0.5) * 6.0);
                float pulse = 0.82 + 0.18 * sin(_Time.y * (_PulseSpeed * 2.0) + input.uv.y * 3.5);
                float crest = smoothstep(0.76, 0.98, flow) * edgeMask;
                float halo = saturate(edgeMask * 0.52 + subFlow * 0.24 + drift * 0.24);
                float centerRibbon = smoothstep(0.58, 0.95, drift) * centerMask;
                float sideBloom = smoothstep(0.35, 0.9, subFlow) * saturate(1.0 - centerMask * 0.68);

                float3 color = _BaseColor.rgb * (0.42 + halo * 0.58);
                color += _AccentColor.rgb * centerRibbon * (_GlowBoost * 0.34);
                color += _AccentColor.rgb * crest * _GlowBoost;
                color += _AccentColor.rgb * sideBloom * 0.08;

                float alpha = saturate((_BaseColor.a * (0.24 + edgeMask * 0.64 + centerRibbon * 0.2) + crest * 0.4) * pulse * _AlphaScale);
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

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "GuideRibbonBuiltin"

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
            float _PulseSpeed;
            float _EdgeSoftness;
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
                float edgeMask = saturate(1.0 - abs(i.uv.x - 0.5) * 2.0);
                edgeMask = pow(edgeMask, max(0.15, _EdgeSoftness));
                float centerMask = smoothstep(0.08, 0.96, edgeMask);

                float flow = 0.5 + 0.5 * sin(i.uv.y * 18.0 - _Time.y * (_FlowSpeed * 4.0));
                float subFlow = 0.5 + 0.5 * sin(i.uv.y * 8.0 + _Time.y * (_FlowSpeed * 1.6));
                float drift = 0.5 + 0.5 * sin(i.uv.y * 5.5 + _Time.y * (_FlowSpeed * 1.15) + (i.uv.x - 0.5) * 6.0);
                float pulse = 0.82 + 0.18 * sin(_Time.y * (_PulseSpeed * 2.0) + i.uv.y * 3.5);
                float crest = smoothstep(0.76, 0.98, flow) * edgeMask;
                float halo = saturate(edgeMask * 0.52 + subFlow * 0.24 + drift * 0.24);
                float centerRibbon = smoothstep(0.58, 0.95, drift) * centerMask;
                float sideBloom = smoothstep(0.35, 0.9, subFlow) * saturate(1.0 - centerMask * 0.68);

                float3 color = _BaseColor.rgb * (0.42 + halo * 0.58);
                color += _AccentColor.rgb * centerRibbon * (_GlowBoost * 0.34);
                color += _AccentColor.rgb * crest * _GlowBoost;
                color += _AccentColor.rgb * sideBloom * 0.08;

                float alpha = saturate((_BaseColor.a * (0.24 + edgeMask * 0.64 + centerRibbon * 0.2) + crest * 0.4) * pulse * _AlphaScale);
                return fixed4(color, alpha);
            }
            ENDCG
        }
    }
}
