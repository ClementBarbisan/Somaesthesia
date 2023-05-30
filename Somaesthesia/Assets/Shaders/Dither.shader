Shader "Ocias/Standard (Stipple Transparency)" {
    Properties{
        _Color("Color", Color) = (1,1,1,1)
        _ShadowColor("ShadowColor", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _BumpMap("Bumpmap", 2D) = "bump" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.5
        _Transparency("Transparency", Range(0,1)) = 1.0
    }
        SubShader{
            Tags { "RenderType" = "Opaque" }
            LOD 100
 
            CGPROGRAM
            // Physically based Standard lighting model, and enable shadows on all light types
            #pragma surface surf Standard fullforwardshadows addshadow
 
            // Use shader model 3.0 target, to get nicer looking lighting
            #pragma target 3.0
 
            sampler2D _MainTex;
            sampler2D _BumpMap;
 
            struct Input {
                float2 uv_MainTex;
                float2 uv_BumpMap;
                float4 screenPos;
            };
 
            half _Glossiness;
            half _Metallic;
            half _Transparency;
            fixed4 _Color;
            fixed4 _ShadowColor;
 
            void surf(Input IN, inout SurfaceOutputStandard o) {
                // Albedo comes from a texture tinted by color
                fixed4 c = _Color;
                o.Albedo = c.rgb;
                // Metallic and smoothness come from slider variables
                o.Metallic = _Metallic;
                o.Smoothness = _Glossiness;
                // o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
                o.Alpha = c.a;
 
                // Screen-door transparency: Discard pixel if below threshold.
                float4x4 thresholdMatrix =
                {  1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
                  13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
                   4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
                  16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
                };
                float4x4 _RowAccess = { 1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1 };
                float2 pos = IN.screenPos.xy / IN.screenPos.w;
                pos *= _ScreenParams.xy; // pixel position
                clip(_Transparency - thresholdMatrix[fmod(pos.x, 4)] * _RowAccess[fmod(pos.y, 4)]);
            }
            ENDCG
        }
 
        FallBack "Diffuse"
}