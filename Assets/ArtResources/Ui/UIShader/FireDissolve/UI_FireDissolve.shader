Shader "UI/FireDissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("主贴图", 2D) = "white" {}
        _Color ("颜色", Color) = (1,1,1,1)
        [NoScaleOffset] _BurnTex ("火焰噪声贴图", 2D) = "gray" {}
        _DissolveProgress ("消散进度", Range(0,0.95)) = 0
        _BurnEdgeWidth ("火焰边缘宽度", Range(0.001,0.5)) = 0.08
        _BurnEdgeColor ("火焰边缘颜色", Color) = (1,0.22,0.02,1)
        _BurnEdgeIntensity ("火焰边缘强度", Range(0,8)) = 2
        _StencilComp ("模板比较", Float) = 8
        _Stencil ("模板值", Float) = 0
        _StencilOp ("模板操作", Float) = 0
        _StencilWriteMask ("模板写入遮罩", Float) = 255
        _StencilReadMask ("模板读取遮罩", Float) = 255
        _ColorMask ("颜色写入遮罩", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _BurnTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _DissolveProgress;
            float _BurnEdgeWidth;
            fixed4 _BurnEdgeColor;
            float _BurnEdgeIntensity;
            float4 _ClipRect;

            // 将 UI 顶点转换到裁剪空间，并传递贴图和遮罩所需的坐标。
            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            // 根据噪声阈值计算火焰溶解、边缘发光和 UI 裁剪结果。
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseColor = tex2D(_MainTex, i.uv) * i.color;
                // 将消散进度限制在 0.95，避免接近 1 时整张图片只剩边缘变色。
                float threshold = min(saturate(_DissolveProgress), 0.95);
                float edge = max(_BurnEdgeWidth, 0.001);
                float4 noiseColor = tex2D(_BurnTex, i.uv);

                // ????????????????????????????
                clip(noiseColor.r - threshold);

                // ????????????????????????????
                float burnRate = 1.0 - smoothstep(0.0, edge, noiseColor.r - threshold);
                burnRate *= step(0.001, threshold);
                fixed3 burnColor = _BurnEdgeColor.rgb * _BurnEdgeIntensity;
                float visible = 1.0;

                #ifdef UNITY_UI_CLIP_RECT
                visible *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                // 保留原图颜色，火焰只作为边缘高光叠加，避免整张 UI 被染色。
                baseColor.rgb = lerp(baseColor.rgb, burnColor, saturate(burnRate));
                baseColor.a *= saturate(visible);

                #ifdef UNITY_UI_ALPHACLIP
                clip(baseColor.a - 0.001);
                #endif
                return baseColor;
            }
            ENDCG
        }
    }
}
