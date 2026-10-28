Shader "zxf/buildCardShader"
{
    Properties
    {
        _MainTex ("主纹理", 2D) = "white" {}
        _Color ("基础颜色", Color) = (1, 1, 1, 1)
        _ShadowColor ("卡渲阴影颜色", Color) = (0.45, 0.45, 0.55, 1)
        _ToonSteps ("卡渲分段数", Range(1, 6)) = 3
        _AmbientStrength ("环境光强度", Range(0, 2)) = 0.45
        _SpecularColor ("高光颜色", Color) = (1, 1, 1, 1)
        _SpecularStrength ("高光强度", Range(0, 1)) = 0.15
        _SpecularSize ("高光范围", Range(1, 128)) = 32
        _SlashColor ("闪白颜色", Color) = (1, 1, 1, 1)
        _FlashStrength ("闪白强度", Range(0, 1)) = 0
        [Toggle] _EnableOutline ("开启描边", Float) = 0
        _OutlineColor ("描边颜色", Color) = (0, 0, 0, 1)
        _OutlineWidth ("描边宽度", Range(0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
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
            fixed4 _ShadowColor;
            half _ToonSteps;
            half _AmbientStrength;
            fixed4 _SpecularColor;
            half _SpecularStrength;
            half _SpecularSize;
            fixed4 _SlashColor;
            half _FlashStrength;
            half _EnableOutline;
            fixed4 _OutlineColor;
            half _OutlineWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                // AutoLight.cginc 的 TRANSFER_SHADOW 宏要求裁剪空间字段名为 pos。
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

                // 使用 Unity 内置主光源方向，避免逐材质传入光照方向，保持材质合批友好。
                fixed3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
                fixed ndotl = saturate(dot(normal, lightDir));
                fixed shadow = SHADOW_ATTENUATION(i);

                // 将连续光照量量化为离散层级，形成卡通渲染的明暗分段。
                half steps = max(1.0h, _ToonSteps);
                half toonValue = ceil(ndotl * steps) / steps;
                fixed3 toonLight = lerp(_ShadowColor.rgb, _LightColor0.rgb, toonValue) * shadow;
                fixed3 ambient = ShadeSH9(float4(normal, 1.0)) * _AmbientStrength;
                fixed3 litColor = texColor.rgb * (ambient + toonLight);

                // 简单硬边高光，让城门等建筑材质保留明确的卡渲高光。
                fixed3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                fixed3 halfDir = normalize(lightDir + viewDir);
                fixed specular = step(1.0h - _SpecularStrength,
                    pow(saturate(dot(normal, halfDir)), _SpecularSize));
                litColor += _SpecularColor.rgb * specular * _SpecularStrength * shadow;

                // 闪白：_FlashStrength 为 0 时不影响正常卡渲颜色。
                litColor = lerp(litColor, _SlashColor.rgb, saturate(_FlashStrength));
                return fixed4(litColor, texColor.a);
            }
            ENDCG
        }

        // 反向外壳描边：沿模型法线向外扩张，并剔除正面，只显示背面轮廓。
        // _EnableOutline 关闭时直接丢弃片元，保留开关而不影响主卡渲 Pass。
        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On

            CGPROGRAM
            #pragma vertex outlineVert
            #pragma fragment outlineFrag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            half _EnableOutline;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _OutlineColor;
            half _OutlineWidth;

            struct outlineAppdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct outlineV2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            outlineV2f outlineVert(outlineAppdata v)
            {
                outlineV2f o;
                float3 expandedVertex = v.vertex.xyz + normalize(v.normal) * _OutlineWidth;
                o.vertex = UnityObjectToClipPos(float4(expandedVertex, 1.0));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 outlineFrag(outlineV2f i) : SV_Target
            {
                if (_EnableOutline < 0.5)
                {
                    discard;
                }
                return _OutlineColor;
            }
            ENDCG
        }

        Pass
        {
            Name "SHADOWCASTER"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct shadowAppdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct shadowV2f
            {
                V2F_SHADOW_CASTER;
                float2 uv : TEXCOORD1;
            };

            shadowV2f shadowVert(shadowAppdata v)
            {
                shadowV2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 shadowFrag(shadowV2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
