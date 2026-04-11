Shader "ZhuozhengYuan/Chapter01GlobalFlowWater"
{
    Properties
    {
        _MainTex ("Water Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (0.62, 0.84, 0.91, 0.68)
        _Alpha ("Alpha", Range(0, 1)) = 0.64
        _Tiling ("Tiling", Vector) = (2.4, 2.05, 0, 0)
        _FlowDirection ("Flow Direction", Vector) = (1, 0.12, 0, 0)
        _FlowSpeed ("Flow Speed", Range(0, 1.5)) = 0.1
        _FresnelStrength ("Fresnel Strength", Range(0, 1)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "GlobalFlowURP"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _Alpha;
                float4 _Tiling;
                float4 _FlowDirection;
                float _FlowSpeed;
                float _FresnelStrength;
                float4 _MainTex_ST;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionHCS = positionInputs.positionCS;
                output.uv = input.uv;
                output.worldNormal = normalInputs.normalWS;
                output.viewDir = GetWorldSpaceViewDir(positionInputs.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 tiling = max(_Tiling.xy, float2(0.01, 0.01));
                float2 direction = _FlowDirection.xy;
                if (dot(direction, direction) < 0.0001)
                {
                    direction = float2(1.0, 0.0);
                }
                direction = normalize(direction);
                float2 ortho = float2(-direction.y, direction.x);

                float2 uv = TRANSFORM_TEX(input.uv, _MainTex) * tiling;
                float flow = _Time.y * _FlowSpeed;

                float drift = sin(uv.y * 4.7 + flow * 1.6) * 0.014;
                float ripple = sin(uv.x * 7.3 - flow * 1.2 + uv.y * 2.1) * 0.018;
                float2 warp = ortho * drift + direction * ripple;

                half4 sampleA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + direction * flow + warp);
                half4 sampleB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - direction * (flow * 0.62) + ortho * 0.045 - warp * 0.35);

                float motion = 0.5 + 0.5 * sin(flow * 1.9 + uv.x * 2.5 + uv.y * 3.1);
                float3 waterColor = lerp(sampleA.rgb, sampleB.rgb, 0.46);
                waterColor *= lerp(0.92, 1.03, motion);
                waterColor *= _Tint.rgb;

                float3 worldNormal = normalize(input.worldNormal);
                float3 viewDir = normalize(input.viewDir);
                float fresnel = pow(1.0 - saturate(dot(worldNormal, viewDir)), 4.0) * _FresnelStrength;
                waterColor += fresnel * _Tint.rgb;

                float alpha = saturate((_Alpha * _Tint.a) * (0.84 + sampleA.a * 0.16));
                return half4(waterColor, alpha);
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "GlobalFlowBuiltin"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Tint;
            float _Alpha;
            float4 _Tiling;
            float4 _FlowDirection;
            float _FlowSpeed;
            float _FresnelStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = _WorldSpaceCameraPos.xyz - worldPos.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 tiling = max(_Tiling.xy, float2(0.01, 0.01));
                float2 direction = _FlowDirection.xy;
                if (dot(direction, direction) < 0.0001)
                {
                    direction = float2(1.0, 0.0);
                }
                direction = normalize(direction);
                float2 ortho = float2(-direction.y, direction.x);

                float2 uv = TRANSFORM_TEX(i.uv, _MainTex) * tiling;
                float flow = _Time.y * _FlowSpeed;

                float drift = sin(uv.y * 4.7 + flow * 1.6) * 0.014;
                float ripple = sin(uv.x * 7.3 - flow * 1.2 + uv.y * 2.1) * 0.018;
                float2 warp = ortho * drift + direction * ripple;

                fixed4 sampleA = tex2D(_MainTex, uv + direction * flow + warp);
                fixed4 sampleB = tex2D(_MainTex, uv - direction * (flow * 0.62) + ortho * 0.045 - warp * 0.35);

                float motion = 0.5 + 0.5 * sin(flow * 1.9 + uv.x * 2.5 + uv.y * 3.1);
                float3 waterColor = lerp(sampleA.rgb, sampleB.rgb, 0.46);
                waterColor *= lerp(0.92, 1.03, motion);
                waterColor *= _Tint.rgb;

                float3 worldNormal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.viewDir);
                float fresnel = pow(1.0 - saturate(dot(worldNormal, viewDir)), 4.0) * _FresnelStrength;
                waterColor += fresnel * _Tint.rgb;

                float alpha = saturate((_Alpha * _Tint.a) * (0.84 + sampleA.a * 0.16));
                return fixed4(waterColor, alpha);
            }
            ENDCG
        }
    }
}
