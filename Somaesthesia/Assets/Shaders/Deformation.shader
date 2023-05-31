
Shader "Custom/Deformation"
{
    Properties
    {
        [PowerSlider(5.0)] _Speed ("Speed", Range (0.01, 100)) = 2
        [PowerSlider(5.0)] _Amplitude ("Amplitude", Range (0.01, 5)) = 0.25
        _Distance ("Distance", Range(0, 10)) = 1
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _EdgeLength ("Edge length", Range(1,150)) = 15
    }
    SubShader
    {
            Tags
            {
                "RenderType"="Opaque"
            }
            LOD 100
            CGPROGRAM
            #pragma surface surf Standard fullforwardshadows vertex:vert tessellate:tessEdge addshadow //alpha:fade
            #include "Packages/jp.keijiro.noiseshader/Shader/Common.hlsl"
            #include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise3D.hlsl"
            #include "Tessellation.cginc"
            #pragma target 4.6

            struct Input {
                float4 screenPos;
                float3 texcoord1 : TEXCOORD1;
                float4 color : COLOR;
            };
            #ifdef SHADER_API_D3D11
            struct Joints
            {
                float3 Pos;
                float3x3 Matrice;
                float Size;
            };

            StructuredBuffer<Joints> _Skeleton;
            #endif
            half _Glossiness;
            half _Metallic;
            fixed4 _Color;
            float _Transparency;

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_INSTANCING_BUFFER_END(Props)

            float _Speed;
            float _Amplitude;
            float _EdgeLength;
            float _Distance;
         
            float4 tessEdge(appdata_full v0, appdata_full v1, appdata_full v2)
            {
                return UnityEdgeLengthBasedTess(v0.vertex, v1.vertex, v2.vertex, _EdgeLength);
            }

            void vert(inout appdata_full data)
            {
                #ifdef SHADER_API_D3D11
                data.color.w = 1;
                for (int i = 0; i < 18; i++)
                {
                    float dist = distance((_Skeleton[i].Pos), mul(unity_ObjectToWorld, data.vertex));
                    if (dist < _Distance)
                    {
                        data.color.w = pow(dist / _Distance, 2);
                        data.vertex.xyz += sin(_Time * _Speed) * _Amplitude * PeriodicNoise(data.vertex * 10,
                            float3(5, 2, 0.1));
                        data.texcoord = ComputeScreenPos(UnityWorldToClipPos(data.vertex));
                        data.color.rgb = float3(1, 0, 0);
                        return;
                    }
                }
                #endif
            }

            void surf(Input IN, inout SurfaceOutputStandard o)
            {
                // Albedo comes from a texture tinted by color
                fixed3 c = _Color * IN.color;
                // fixed4 c = _Color + IN.color;
                o.Albedo = c.rgb;
                // Metallic and smoothness come from slider variables
                o.Metallic = _Metallic;
                o.Smoothness = _Glossiness;
                // o.Alpha = c.a;
                float4x4 thresholdMatrix =
                    {  1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
                        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
                        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
                        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
                    };
                float4x4 _RowAccess = { 1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1 };
                float2 pos = IN.screenPos.xy / IN.screenPos.w;
                pos *= _ScreenParams.xy; // pixel position
                clip(IN.color.w - thresholdMatrix[fmod(pos.x, 4)] * _RowAccess[fmod(pos.y, 4)]);
            }
            ENDCG
    }
    Fallback "Diffuse"
}
