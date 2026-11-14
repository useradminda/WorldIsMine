Shader "WorldIsMine/IceShatter"
{
    Properties
    {
        _MainTex("原始角色纹理", 2D) = "white" {}
        _ICETex("角色冰冻纹理", 2D) = "white" {}
        _ICEState("冰冻值", Float) = 0.5
        _Color("原始颜色", Color) = (1, 1, 1, 1)
        _IceTint("冰晶颜色", Color) = (0.42, 0.82, 1, 1)
        _IceStrength("冰冻纹理强度", Range(0, 1)) = 1
        _CutColor("切割面颜色", Color) = (0.8, 0.96, 1, 1)
        _CutStrength("切割面亮度", Range(0, 1)) = 0.95
        _LightColor("主光颜色", Color) = (1, 1, 1, 1)
        _LightScale("主光强度", Float) = 1.2
        _HalfLambertValue("半兰伯特基准", Float) = 0.5
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
        Cull Off
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _ICETex;
            fixed4 _Color;
            fixed4 _LightColor;
            half _LightScale;
            half _HalfLambertValue;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _ICEState)
                UNITY_DEFINE_INSTANCED_PROP(float4, _IceTint)
                UNITY_DEFINE_INSTANCED_PROP(float, _IceStrength)
                UNITY_DEFINE_INSTANCED_PROP(float4, _CutColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _CutStrength)
            UNITY_INSTANCING_BUFFER_END(Props)

            /// <summary>将碎片顶点转换到裁剪空间并传递 UV 与法线。</summary>
            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            /// <summary>保留原始 UV 纹理，同时叠加冰晶色；背面使用更亮的切割面颜色。</summary>
            fixed4 frag(v2f i, float faceSign : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 sourceColor = tex2D(_MainTex, i.uv) * _Color;
                fixed3 lightDirection = normalize(UnityWorldSpaceLightDir(float3(0, 0, 0)));
                half lighting = saturate(dot(normalize(i.normal), lightDirection)) * 0.5h + _HalfLambertValue;
                sourceColor.rgb *= lighting * _LightColor.rgb * _LightScale;

                float iceState = saturate(UNITY_ACCESS_INSTANCED_PROP(Props, _ICEState));
                // 旧实现：使用统一冰晶颜色插值，无法还原角色冰冻时的纹理细节。
                // fixed3 iceColor = lerp(sourceColor.rgb, _IceTint.rgb, _IceStrength);
                // 新实现：与角色 AnimMap Shader 一致，将角色冰冻纹理按 _ICEState 叠加到受光后的角色颜色上。
                fixed4 iceTexture = tex2D(_ICETex, i.uv);
                fixed3 iceColor = sourceColor.rgb + iceTexture.rgb * iceState;
                half isBackFace = step(faceSign, 0.0h);
                fixed4 cutColor = UNITY_ACCESS_INSTANCED_PROP(Props, _CutColor);
                half cutStrength = UNITY_ACCESS_INSTANCED_PROP(Props, _CutStrength);
                fixed3 finalColor = lerp(iceColor, cutColor.rgb, isBackFace * cutStrength);
                clip(sourceColor.a - 0.5h);
                return fixed4(finalColor, 1.0h);
            }
            ENDCG
        }
    }
}
