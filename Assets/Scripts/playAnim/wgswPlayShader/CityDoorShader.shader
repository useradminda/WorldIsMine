Shader "wgsw/CityDoorShader"
{
    Properties
    {
        _MainTex ("主贴图", 2D) = "white" {}
        _Color ("颜色", Color) = (1, 1, 1, 1)
        _SlashColor ("闪白叠加色", Color) = (0, 0, 0, 0)
        _FlashStrength ("闪白强度", Range(0, 1)) = 1
        _Glossiness ("光滑度", Range(0, 1)) = 0.15
        _DoorSpecColor ("高光颜色", Color) = (0.2, 0.2, 0.2, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        LOD 200
        Cull Back

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _SlashColor;
            half _FlashStrength;
            half _Glossiness;
            fixed4 _DoorSpecColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                // TRANSFER_SHADOW 宏要求这里的字段名为 pos。
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                SHADOW_COORDS(3)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv) * _Color;
                fixed3 normal = normalize(i.worldNormal);

                // Unity 主方向光 + 阴影衰减。
                fixed3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
                fixed ndotl = saturate(dot(normal, lightDir));
                fixed shadow = SHADOW_ATTENUATION(i);
                fixed3 directLight = _LightColor0.rgb * ndotl * shadow;

                // 环境光，避免城门背光面完全变黑。
                fixed3 ambient = ShadeSH9(float4(normal, 1.0));
                fixed3 litColor = texColor.rgb * (ambient + directLight);

                // 很轻的直射高光，保持城门材质的金属边缘表现。
                if (_Glossiness > 0.001)
                {
                    fixed3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                    fixed3 halfDir = normalize(lightDir + viewDir);
                    fixed spec = pow(saturate(dot(normal, halfDir)), 32.0) * _Glossiness * shadow;
                    litColor += _DoorSpecColor.rgb * spec;
                }

                // 使用 _SlashColor 作为闪白叠加色，具体颜色由 C# 通过材质属性块传入。
                fixed flashAmount = saturate(max(max(_SlashColor.r, _SlashColor.g), _SlashColor.b) * _FlashStrength);
                litColor = lerp(litColor, _SlashColor.rgb, flashAmount);
                return fixed4(litColor, texColor.a);
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }

    FallBack "Diffuse"
}
