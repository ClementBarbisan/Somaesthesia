
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
                "RenderType"="Transparent" "Queue"="Geometry" "IgnoreProjector"="True"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            zWrite On
            zTest LEqual
            zClip False

            CGPROGRAM
            #pragma surface surf Standard fullforwardshadows vertex:vert tessellate:tessEdge addshadow //alpha:fade
            #include "Packages/jp.keijiro.noiseshader/Shader/Common.hlsl"
            #include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise3D.hlsl"
            #include "Tessellation.cginc"
            #pragma target 4.6


            sampler2D _MainTex;

            struct Input {
                float2 uv_MainTex;
                float4 color : COLOR;
            };
            #ifdef SHADER_API_D3D11
            struct Joints
            {
                float3 Pos;
                float3 Dir;
            };

            StructuredBuffer<Joints> _Skeleton;
            #endif
            half _Glossiness;
            half _Metallic;
            fixed4 _Color;

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
                float dist = distance((_Skeleton[1].Pos), mul(unity_ObjectToWorld, data.vertex));
                if (dist < _Distance)
                {
                    data.vertex.xyz += sin(_Time * _Speed) * _Amplitude * PeriodicNoise(data.vertex * 10,
                        float3(5, 2, 0.1));
                    data.color.r = 1.0 / (dist + 1.0);
                    data.color.gb = 0;
                }
                #endif
            }

            void surf(Input IN, inout SurfaceOutputStandard o)
            {
                // Albedo comes from a texture tinted by color
                fixed4 c = _Color + IN.color;
                o.Albedo = c.rgb;
                // Metallic and smoothness come from slider variables
                o.Metallic = _Metallic;
                o.Smoothness = _Glossiness;
                o.Alpha = c.a;
            }
            ENDCG
    }
    Fallback "Legacy Shaders/Transparent/Cutout/VertexLit"
}
