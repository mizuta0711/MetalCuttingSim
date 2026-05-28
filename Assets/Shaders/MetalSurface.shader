// 役割: GPU Marching Cubes が生成した StructuredBuffer<Triangle> を URP でプロシージャル描画する
// 依存: StructuredBuffer は MaterialPropertyBlock 経由で MarchingCubesRenderer.cs がセット

Shader "MetalCuttingSim/MetalSurface"
{
    Properties
    {
        _BaseColor  ("Base Color",  Color)       = (0.60, 0.62, 0.65, 1)
        _Metallic   ("Metallic",    Range(0,1))  = 0.85
        _Smoothness ("Smoothness",  Range(0,1))  = 0.55
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags  { "LightMode"="UniversalForward" }
            Cull  Off
            ZWrite On
            ZTest  LEqual

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Triangle は ComputeShader と同じレイアウト（72 bytes: 頂点×3 + 勾配法線×3）
            struct MCSTriangle { float3 v0, n0, v1, n1, v2, n2; };

            // StructuredBuffer は CBUFFER 外（MaterialPropertyBlock でセット可能）
            StructuredBuffer<MCSTriangle> _Triangles;

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half  _Metallic;
                half  _Smoothness;
            CBUFFER_END

            struct Varyings
            {
                float4 posCS  : SV_POSITION;
                float3 posWS  : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };

            Varyings vert(uint vid : SV_VertexID)
            {
                uint ti = vid / 3u;
                uint li = vid % 3u;
                MCSTriangle t = _Triangles[ti];

                // v1/v2 を入れ替えて巻き順を外向きに修正。法線も同じ順で対応させる
                float3 posWS, nrm;
                if      (li == 0u) { posWS = t.v0; nrm = t.n0; }
                else if (li == 1u) { posWS = t.v2; nrm = t.n2; }
                else               { posWS = t.v1; nrm = t.n1; }

                // 面法線（v1↔v2 反転済みの外向き基準）と勾配法線が逆向きならフリップ
                float3 faceN = normalize(cross(t.v2 - t.v0, t.v1 - t.v0));
                if (dot(nrm, faceN) < 0.0) nrm = -nrm;

                Varyings o;
                o.posCS  = TransformWorldToHClip(posWS);
                o.posWS  = posWS;
                o.normal = nrm;
                return o;
            }

            // SV_IsFrontFace で両面ライティング（掘削穴の内壁も正しく照明）
            half4 frag(Varyings i, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                float3 N = normalize(i.normal);
                if (!isFrontFace) N = -N;

                // 影対応
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    float4 sc = TransformWorldToShadowCoord(i.posWS);
                    Light  ml = GetMainLight(sc);
                #else
                    Light  ml = GetMainLight();
                #endif

                float  NdotL   = saturate(dot(N, ml.direction));
                half3  diffuse = _BaseColor.rgb * ml.color.rgb * NdotL * ml.shadowAttenuation;

                // DrawProceduralIndirect は per-object SH 係数が未設定のため SampleSH() が 0 を返す。
                // 代わりに固定アンビエントフロアを使用して全方向から見えるようにする。
                half3  ambient = _BaseColor.rgb * 0.6h;
                half3  color   = diffuse + ambient;

                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        // ShadowCaster パス（影を落とす）
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest  LEqual
            Cull   Back
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   vertShadow
            #pragma fragment fragShadow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct MCSTriangle { float3 v0, n0, v1, n1, v2, n2; };
            StructuredBuffer<MCSTriangle> _Triangles;

            float4 vertShadow(uint vid : SV_VertexID) : SV_POSITION
            {
                uint ti = vid / 3u;
                uint li = vid % 3u;
                MCSTriangle t = _Triangles[ti];
                float3 posWS;
                if      (li == 0u) posWS = t.v0;
                else if (li == 1u) posWS = t.v2;
                else               posWS = t.v1;
                return TransformWorldToHClip(posWS);
            }
            half4 fragShadow() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
