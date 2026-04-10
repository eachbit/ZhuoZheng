Shader "ZhuozhengYuan/Chapter01SimpleFlowWater"
{
    Properties
    {
        _MainTex ("Water Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (0.68, 0.9, 0.95, 0.7)
        _Alpha ("Alpha", Range(0, 1)) = 0.72
        _Tiling ("Tiling", Vector) = (3.5, 2.8, 0, 0)
        _FlowDirection ("Flow Direction", Vector) = (1, 0.2, 0, 0)
        _FlowSpeed ("Flow Speed", Range(0, 1.5)) = 0.22
        _FresnelStrength ("Fresnel Strength", Range(0, 1)) = 0.18
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
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
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
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

                float2 uv = i.uv * tiling;
                float timeOffset = _Time.y * _FlowSpeed;

                fixed4 sampleA = tex2D(_MainTex, uv + direction * timeOffset);
                fixed4 sampleB = tex2D(_MainTex, uv - direction * (timeOffset * 0.65) + float2(0.17, 0.11));
                fixed3 waterColor = lerp(sampleA.rgb, sampleB.rgb, 0.5) * _Tint.rgb;

                float3 worldNormal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.viewDir);
                float fresnel = pow(1.0 - saturate(dot(worldNormal, viewDir)), 3.0) * _FresnelStrength;
                waterColor += fresnel;

                return fixed4(waterColor, saturate(_Alpha * _Tint.a));
            }
            ENDCG
        }
    }
}
