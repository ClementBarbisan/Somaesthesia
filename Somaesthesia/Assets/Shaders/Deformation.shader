
Shader "Custom/Deformation"
{
    Properties
    {
        [PowerSlider(5.0)] _Speed ("Speed", Range (0.01, 100)) = 2
        [PowerSlider(5.0)] _Amplitude ("Amplitude", Range (0.01, 5)) = 0.25
        _Distance ("Distance", Range(0, 10)) = 1
        _Color ("Color", Color) = (1,1,1,1)
        _ColorDisrupt ("Color Change", Color) = (1,0,0,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
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
            #include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise3D.hlsl"
            #include "Tessellation.cginc"
            #pragma target 5.0

            struct Input {
                float4 screenPos;
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
            float _SkeletonSize;
            fixed4 _Color;
            float4 _ColorDisrupt;
            uniform sampler2D _MainTex;
            float4 _MainTex_ST;
            int _WidthTex;
            int _HeightTex;

            float _Speed;
            float _Amplitude;
            float _EdgeLength;
            float _Distance;

            float rand(in float2 uv)
            {
                float2 noise = (frac(sin(dot(uv, float2(12.9898, 78.233) * 2.0)) * 43758.5453));
                return abs(noise.x + noise.y) * 0.5;
            }
            
            float4 tessEdge(appdata_full v0, appdata_full v1, appdata_full v2)
            {
                return UnityEdgeLengthBasedTess(v0.vertex, v1.vertex, v2.vertex, _EdgeLength);
            }
            void vert(inout appdata_full data)
            {
                #ifdef SHADER_API_D3D11
                uint nb = 0;
                uint stride = 0;
                _Skeleton.GetDimensions(nb , stride);
                data.color.w = 0.95 - _SkeletonSize;
                for (int i = 0; i < nb; i++)
                {
                    float curDist = distance((_Skeleton[i].Pos), UnityObjectToClipPos(data.vertex));
                    if (curDist < _SkeletonSize * 2.5)
                    {
                        data.vertex.xyz += sin(_Time * _Speed) * _Amplitude * (1 / curDist) * (SimplexNoise(
                            data.vertex) / 2.5);
                        data.color.rgb = _ColorDisrupt;
                        data.color.rgb *= tex2Dlod(_MainTex, float4(data.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw, 0, 0));
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
