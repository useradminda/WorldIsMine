/*
Created by jiadong chen
https://jiadong-chen.medium.com/
*/

Shader "chenjd/BuiltIn/AnimMapShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_AnimMap ("AnimMap", 2D) ="white" {}
        _ICETex("Texture", 2D) = "white" {}
        _Mask("Texture", 2D) = "White" {}
        _MaskColor("MaskColor", Color) = (0, 0, 0, 1)
        _MaskValue("MaskValue", float) = 0

        _SlashColor("闪白叠加色", Color) = (0, 0, 0, 1)
        _ICEState("闪白值", float) = 0
        _WorldPoint("黑洞中心坐标", Vector) = (1,1,1,0)
        _BlackPower("吸力", float) = 0       
        _GravityValue("重力值",float) = 1
        

        _MainLightDir("MainLightDir", vector) = (1,1,1)
        _LightColor("LightColor", Color) = (1,1,1)
        _LightScale("LightScale", float) = 1
        _HalfLambertValue("HalfLambertValue", float) = 0.5

        _HighLightValue("HighLightValue", float) = 1
        

        // 假影子参数（实例化控制）
       
		//_AnimLen("Anim Length", Float) = 0
       // _CurrentTime("current time", Float) = 0
            //_Color("Color", Color) = (0, 0, 0, 1)

        _GroundY("地面Y高度 (外部传入)",float) = 0
        _Shadow_Color("影子颜色",Color) = (0,0,0,1)
        _Shadow_Length("影子长度",float) = 1
        _Shadow_Rotated("影子旋转角度",range(0,360)) = 0
	}
	
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Cull off
		LOD 100

        

        Pass
        { 


            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            //开启gpu instancing
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            struct appdata
            {
                float2 uv : TEXCOORD0;
                //float2 uv2: TEXCOORD1;
                float4 pos : POSITION;
                float4 normal: NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                float2 objectNormal:TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

           


            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _AnimMap;
            float4 _AnimMap_TexelSize;//x == 1/width

            sampler2D _ICETex;

            sampler2D _UVFireTex;
            float4 _UVFireTex_ST;

            sampler2D _Mask;
            float4 _MaskColor;

            float _MaskValue;

            float _HalfLambertValue;
            float _LightScale;
            float4 _LightColor;
            float4 _MainLightDir;

            float _HighLightValue;
            //float4 _WorldPoint;

            //float _BlackPower;
            //float4 _SlashColor;
            //float _AnimLen;
            //float _CurrentTime;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _AnimLen)
                UNITY_DEFINE_INSTANCED_PROP(float, _CurrentTime)
                UNITY_DEFINE_INSTANCED_PROP(float4, _SlashColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _ICEState)
                UNITY_DEFINE_INSTANCED_PROP(float4, _WorldPoint)
                UNITY_DEFINE_INSTANCED_PROP(float, _BlackPower)
                UNITY_DEFINE_INSTANCED_PROP(float, _TimeSinceLevelLoad)
                UNITY_DEFINE_INSTANCED_PROP(float, _GravityValue)               
            UNITY_INSTANCING_BUFFER_END(Props)
            
            v2f vert (appdata v, uint vid : SV_VertexID)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                //// 压扁系数 正常情况为1,压扁情况为0
                float gravityRadio = UNITY_ACCESS_INSTANCED_PROP(Props, _GravityValue);

                float len = UNITY_ACCESS_INSTANCED_PROP(Props, _AnimLen);
                float _timeSinceLevelLoad = UNITY_ACCESS_INSTANCED_PROP(Props, _TimeSinceLevelLoad);


                float _cT = UNITY_ACCESS_INSTANCED_PROP(Props, _CurrentTime);
                //float f = (_cT + _Time.y  - _timeSinceLevelLoad) / len;

                float invLen = rcp(len);

                float f =
                (
                    _cT +
                    _Time.y -
                    _timeSinceLevelLoad
                ) * invLen;


                if (gravityRadio == 0.2)
                {
                    f = 0.1;
                }


               // fmod(f, 1.0);
               f = frac(f);
               

                float animMap_x = (vid + 0.5) * _AnimMap_TexelSize.x ;
                float animMap_y = f;

                float4 pos = tex2Dlod(_AnimMap, float4(animMap_x, animMap_y, 0, 0));

                // 压扁系数
                pos.z *= gravityRadio;

                v2f o;
              
                UNITY_TRANSFER_INSTANCE_ID(v, o);
             
                // 正常的点
                o.vertex = UnityObjectToClipPos(pos);

                // 黑洞算出来的顶点偏移
                float4 blackHolePoint = UNITY_ACCESS_INSTANCED_PROP(Props, _WorldPoint);
                float blackPower = UNITY_ACCESS_INSTANCED_PROP(Props, _BlackPower);

                float4 objectToWorldPoint = mul(unity_ObjectToWorld, pos);
               // float _dis = distance(objectToWorldPoint.xyz, blackHolePoint.xyz);
                //float t = saturate(blackPower / (_dis * _dis));//saturate((_Time.y * _BlackPower)/ (_dis * _dis));               
                if (blackPower > 0.0001)
                {
                    float3 dir =
                        objectToWorldPoint.xyz
                        - blackHolePoint.xyz;

                    float disSqr =
                        dot(dir, dir);

                    float t =
                        saturate(
                            blackPower
                            *
                            rcp(
                                max(
                                    disSqr,
                                    0.0001)));

                    objectToWorldPoint.xyz =
                        lerp(
                            objectToWorldPoint.xyz,
                            blackHolePoint.xyz,
                            t);
                }


                o.vertex = mul(UNITY_MATRIX_VP, objectToWorldPoint);

                
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = mul(unity_ObjectToWorld, v.normal);
                o.objectNormal = v.normal;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
               
                fixed4 col = tex2D(_MainTex, i.uv);      

                // float3 lDir = normalize(_WorldSpaceLightPos0.xyz); // 获取真实光源
                float3 lDir = normalize(_MainLightDir.xyz);// mul(unity_ObjectToWorld, _MainLightDir);// ObjSpaceLightDir(i.vertex);
                //光照渐变 i.objectNormal => i.worldNormal 灯光不变 ，模型阴影变     i.objectNormal 跟着模型走，不管模型旋转方向
                float Ramp_light = saturate(dot(i.objectNormal, normalize(lDir))) * 0.5 + _HalfLambertValue;// saturate(dot(tangentNormal, tangentLight)) * 0.5 + _HalfLambertValue;// saturate(dot(i.worldNormal, _MainLightDir)) * 0.5 + _HalfLambertValue;//dot(mainLight.direction, i.normalWS) * 0.5 + _HalfLambertValue; //  
                col.rgb *= Ramp_light * _LightColor * _LightScale;//Ramp_light * mainLight.color;
                clip(col.a - 0.5);

               float high_light = saturate(dot(i.objectNormal, normalize(lDir)));
               //col.rgb *= Ramp_light * Ramp_light * _HighLightValue;//Ramp_light * mainLight.color;
               col.rgb *= _HighLightValue;

                // 闪白
                float4 _slashCol = UNITY_ACCESS_INSTANCED_PROP(Props, _SlashColor);
                col += _slashCol;
                
                // 冰冻
                // float _ICEStateValue = UNITY_ACCESS_INSTANCED_PROP(Props, _ICEState);
                // //if (_ICEStateValue == 1)
                // {
                //     fixed4 iceCol = tex2D(_ICETex, i.uv);
                //     col += iceCol * _ICEStateValue;
                // }

                 float _ICEStateValue = UNITY_ACCESS_INSTANCED_PROP(Props, _ICEState);
                if (_ICEStateValue > 0.001)
                {
                    fixed4 iceCol =
                        tex2D(
                            _ICETex,
                            i.uv);

                    col +=
                        iceCol
                        *
                        _ICEStateValue;
                }

                // 遮罩图
                float4 maskCol = tex2D(_Mask, i.uv);
                float4 maskR = (maskCol.r).xxxx;
                float4 minCol = min(maskR, _MaskColor);
                float4 lerpResult4 = lerp(col, ((saturate((col * minCol))) * _MaskValue), maskCol.r);

                col += lerpResult4;
                //col += maskCol.r * _MaskColor;

                //return UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                return col;

                //return UNITY_ACCESS_INSTANCED_PROP(Props, _SlashColor);
            }
            ENDCG
        }



        // pass
        // {
        //    Stencil{

        //        Ref 1
        //        //Comp取值依次为  0:Disabled  1:Never  2:Less  3:Equal  4:LessEqual  5:Greater  6:NotEqual  7:GreaterEqual  8:Always
        //        Comp Greater //或者改成NotEqual
        //        //Pass取值依次为  0:Keep  1:Zero  2:Replace  3:IncrementSaturate  4:DecrementSaturate  5:Invert  6:IncrementWrap  7:DecrementWrap
        //        Pass Replace
        //    }

        //    Blend SrcAlpha oneMinusSrcAlpha

        //        //因为和地面重叠所以做个偏移
        //        //也可以不做偏移,将传入的地面高度抬高一点即可


        //        CGPROGRAM

        //        #pragma vertex vert
        //        #pragma fragment frag
        //        #include "UnityCG.cginc"
        //         //开启gpu instancing
        //        //#pragma multi_compile_instancing
        //        struct appdata
        //        {
        //            float4 vertex : POSITION;
        //            UNITY_VERTEX_INPUT_INSTANCE_ID
        //        };

        //        struct v2f
        //        {
        //            float4 pos : SV_POSITION;
        //            //这里worldPos一定是float4,因为vert()中实际是手动两次空间变换如果是float3会导致w分量丢失,透视除法会出错
        //            //如果不参与变换,只是传到frag()中使用的话,比如进行Blinn-Phong光照计算V向量那么float3就够了
        //            float4 worldPos : TEXCOORD0;
        //            //做阴影插值和Clip地面以下阴影用
        //            float cacheWorldY : TEXCOORD1;
        //            UNITY_VERTEX_INPUT_INSTANCE_ID
        //        };

        //        half _GroundY;
        //        half4 _Shadow_Color;
        //        half _Shadow_Length;
        //        half _Shadow_Rotated;

        //        v2f vert(appdata v)
        //        {
        //            UNITY_SETUP_INSTANCE_ID(v);
        //            v2f o = (v2f)0;
        //            UNITY_TRANSFER_INSTANCE_ID(v, o);
        //            //获取世界空间的位置
        //            o.worldPos = mul(unity_ObjectToWorld,v.vertex);
        //            //缓存世界空间下的y分量,后续两点作用
        //            //第一点 : 做插值用做计算xz的偏移量的多少
        //            //第二点 : 防止在地面以下
        //            o.cacheWorldY = o.worldPos.y;

        //            //设置世界空间下y的值全部都设置为传入的地面高度值
        //            o.worldPos.y = _GroundY;

        //            //根据世界空间下模型y值减去传入的地面高度值_GroundY
        //            //以这个值为传入 lerp(0,_Shadow_Length) 进行线性插值
        //            //最后获取到模型y值由低到高的插值lerpVal
        //            //这个max()函数 假设腿部在地面以下则裁切掉腿部阴影,后续使用clip后无需Max
        //            //half lerpVal = lerp(0,_Shadow_Length,max(0,o.cacheWorldY-_GroundY));
        //            half lerpVal = lerp(0,_Shadow_Length,o.cacheWorldY - _GroundY);

        //            //常量PI
        //            //const float PI = 3.14159265;
        //            //角度转换成弧度
        //            half radian = _Shadow_Rotated / 180.0 * UNITY_PI;

        //            //旋转矩阵,对(0,1)向量进行旋转,计算旋转后的向量,该向量就是阴影方向
        //            //2D旋转矩阵如下
        //            // [x]        [ cosθ , -sinθ ]
        //            // [ ]  乘以  
        //            // [y]        [ sinθ , cosθ  ]
        //            // x' = xcosθ - ysinθ
        //            // y' = xsinθ + ycosθ
        //            half2 ratatedAngle = half2((0 * cos(radian) - 1 * sin(radian)),(0 * sin(radian) + 1 * cos(radian)));

        //            //用以y轴高度为参考计算的插值 lerpVal 去 乘以一个旋转后的方向向量,作为阴影的方向
        //            //最终得到偏移后的阴影位置
        //            o.worldPos.xz += lerpVal * ratatedAngle;

        //            //变换到裁剪空间
        //            o.pos = mul(UNITY_MATRIX_VP,o.worldPos);

        //            return o;
        //        }

        //        fixed4 frag(v2f i) : SV_TARGET
        //        {UNITY_SETUP_INSTANCE_ID(i);
        //            //剔除低于地面部分的片段
        //            clip(i.cacheWorldY - _GroundY);
        //        //用作阴影的Pass直接输出颜色即可
        //        return _Shadow_Color;
        //    }

        //    ENDCG
        // }
        //==================== 顶点偏移假影子Pass（半透明后置渲染） ====================
        //==================== 改造后的动画同步假影子Pass（保留你原有的旋转拉伸逻辑） ====================
        Pass
        {
            Tags
            {
                "Queue"="Transparent"
                "RenderType"="Transparent"
                "IgnoreProjector"="True"
            }
            // 预乘混合解决多层重叠发黑
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Offset 2, 2
            Cull Back // 剔除背面，减少重叠面片

            CGPROGRAM
            #pragma vertex vertShadowFake
            #pragma fragment fragShadowFake
            // 开启实例化保证合批
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            // 动画贴图相关（同步主体模型动画）
            sampler2D _AnimMap;
            float4 _AnimMap_TexelSize;
            sampler2D _MainTex;
            float4 _MainTex_ST;

            // 影子全局属性
            half _GroundY;
            half4 _Shadow_Color;
            half _Shadow_Length;
            half _Shadow_Rotated;

            // 实例化缓冲（所有动画/黑洞/重力参数同步主体）
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _AnimLen)
                UNITY_DEFINE_INSTANCED_PROP(float, _CurrentTime)
                UNITY_DEFINE_INSTANCED_PROP(float4, _WorldPoint)
                UNITY_DEFINE_INSTANCED_PROP(float, _BlackPower)
                UNITY_DEFINE_INSTANCED_PROP(float, _TimeSinceLevelLoad)
                UNITY_DEFINE_INSTANCED_PROP(float, _GravityValue)               
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint vid : SV_VertexID; // 顶点ID读取AnimMap
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 worldPos : TEXCOORD0;
                float cacheWorldY : TEXCOORD1;
                float2 uv : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vertShadowFake(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o = (v2f)0;
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                // ========== 1. 和主体完全同步AnimMap动画逻辑（解决影子不动） ==========
                float gravityRadio = UNITY_ACCESS_INSTANCED_PROP(Props, _GravityValue);
                float len = UNITY_ACCESS_INSTANCED_PROP(Props, _AnimLen);
                float _timeSinceLevelLoad = UNITY_ACCESS_INSTANCED_PROP(Props, _TimeSinceLevelLoad);
                float _cT = UNITY_ACCESS_INSTANCED_PROP(Props, _CurrentTime);
                float invLen = rcp(len);
                float f = (_cT + _Time.y - _timeSinceLevelLoad) * invLen;

                if (gravityRadio == 0.2)
                    f = 0.1;
                f = frac(f);

                // 根据顶点ID采样动画贴图，拿到动画后的本地顶点
                float animMap_x = (v.vid + 0.5) * _AnimMap_TexelSize.x ;
                float animMap_y = f;
                float4 animLocalPos = tex2Dlod(_AnimMap, float4(animMap_x, animMap_y, 0, 0));
                animLocalPos.z *= gravityRadio;

                // ========== 2. 同步黑洞吸力顶点偏移 ==========
                float4 worldPos = mul(unity_ObjectToWorld, animLocalPos);
                float4 blackHolePoint = UNITY_ACCESS_INSTANCED_PROP(Props, _WorldPoint);
                float blackPower = UNITY_ACCESS_INSTANCED_PROP(Props, _BlackPower);
                if (blackPower > 0.0001)
                {
                    float3 dir = worldPos.xyz - blackHolePoint.xyz;
                    float disSqr = dot(dir, dir);
                    float t = saturate(blackPower * rcp(max(disSqr, 0.0001)));
                    worldPos.xyz = lerp(worldPos.xyz, blackHolePoint.xyz, t);
                }

                // ========== 3. 完全保留你原来写的影子拉伸、旋转逻辑 ==========
                o.cacheWorldY = worldPos.y;
                float originalY = worldPos.y;
                worldPos.y = _GroundY;

                half lerpVal = lerp(0, _Shadow_Length, originalY - _GroundY);
                half radian = _Shadow_Rotated / 180.0 * UNITY_PI;
                half2 ratatedAngle = half2(
                    (0 * cos(radian) - 1 * sin(radian)),
                    (0 * sin(radian) + 1 * cos(radian))
                );
                worldPos.xz += lerpVal * ratatedAngle;

                o.worldPos = worldPos;
                o.pos = mul(UNITY_MATRIX_VP, worldPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 fragShadowFake(v2f i) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(i);
                // 剔除地面下方顶点
                clip(i.cacheWorldY - _GroundY);
                // 同步主体透明裁剪，镂空不画影子
                fixed4 mainTexCol = tex2D(_MainTex, i.uv);
                clip(mainTexCol.a - 0.5);

                // 预乘Alpha，解决多层面片重叠发黑（核心修复你截图斑驳问题）
                half4 shadowCol = _Shadow_Color;
                shadowCol.rgb *= shadowCol.a;
                return shadowCol;
            }
            ENDCG
        }
	}
}
