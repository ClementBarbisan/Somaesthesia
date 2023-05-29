
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
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert tessellate:tessEdge addshadow
        #include "Packages/jp.keijiro.noiseshader/Shader/Common.hlsl"
        #include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise3D.hlsl"
        #include "Tessellation.cginc"
        // Use shader model 3.0 target, to get nicer looking lighting
         #pragma target 4.6


        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
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

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        float _Speed;
        float _Amplitude;
        float _EdgeLength;
        float _Distance;

        float4 tessEdge (appdata_full v0, appdata_full v1, appdata_full v2)
        {
            return UnityEdgeLengthBasedTess (v0.vertex, v1.vertex, v2.vertex, _EdgeLength);
        }
        
        void vert(inout appdata_full data)
        {
  #ifdef SHADER_API_D3D11
            if (distance(_Skeleton[0].Pos, data.vertex) < _Distance)
            {
                data.vertex.xyz += sin(_Time * _Speed) * _Amplitude * PeriodicNoise(data.vertex * 10,
                float3(5,2, 0.1));
            }
 #endif
       }
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
