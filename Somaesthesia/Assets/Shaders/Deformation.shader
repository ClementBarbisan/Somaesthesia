
Shader "Custom/Deformation"
{
    Properties
    {
        [PowerSlider(5.0)] _Speed ("Speed", Range (0.01, 100)) = 2
        [PowerSlider(5.0)] _Amplitude ("Amplitude", Range (0.01, 5)) = 0.25
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
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // #pragma tessellate tessEdge
            #pragma multi_compile_fog
   //Standard fullforwardshadows addshadow //alpha:fade
            #include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise3D.hlsl"
            // #include "Tessellation.cginc"
            #include "UnityCG.cginc"
            #pragma target 5.0

            struct appdata
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };
            
             struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float4 screenPos: TEXCOORD1;
            };
            struct Joints
            {
                float3 Pos;
                float3x3 Matrice;
                float Size;
            };

            StructuredBuffer<Joints> _Skeleton;
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

            float rand(in float2 uv)
            {
                float2 noise = (frac(sin(dot(uv, float2(12.9898, 78.233) * 2.0)) * 43758.5453));
                return abs(noise.x + noise.y) * 0.5;
            }
            
            // float4 tessEdge(appdata v0, appdata v1, appdata v2)
            // {
            //     return UnityEdgeLengthBasedTess(v0.vertex, v1.vertex, v2.vertex, _EdgeLength);
            // }
            
            v2f vert(appdata data)
            {
                v2f o;
                uint nb = 0;
                uint stride = 0;
                _Skeleton.GetDimensions(nb , stride);
                float lengthSkel = length(tex2Dlod(_MainTex, float4(data.texcoord.xy, 0, 0)));
                data.color = _Color;
                data.color.w = 1 - _SkeletonSize;
                for (int i = 0; i < nb; i++)
                {
                    float curDist = distance((_Skeleton[i].Pos), UnityObjectToClipPos(data.vertex));
                    if (curDist < _SkeletonSize * lengthSkel)
                    {
                        data.vertex.xyz += sin(_Time * _Speed) * _Amplitude * (1 / curDist) * (SimplexNoise(
                            data.vertex) / 2.5);
                        float val = 1 - (curDist / (_SkeletonSize * lengthSkel));
                        data.color.rgb = _ColorDisrupt * val;
                        data.color.w -= val;
                        data.color.rgb *= tex2Dlod(_MainTex, float4(data.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw, 0, 0));
                        o.color = data.color;
                        o.vertex = UnityObjectToClipPos(data.vertex);
                        o.screenPos = ComputeScreenPos(o.vertex);
                        o.uv = data.texcoord;
                        return (o);
                    }
                }
                o.color = data.color;
                o.vertex = UnityObjectToClipPos(data.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                o.uv = data.texcoord;
                return (o);
            }

            float4 frag(v2f i) : COLOR
            {
                float4 c = _Color * i.color;
                float4x4 thresholdMatrix =
                    {  1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
                        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
                        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
                        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
                    };
                float4x4 _RowAccess = { 1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1 };
                float2 pos = i.screenPos.xy / i.screenPos.w;
                pos *= _ScreenParams.xy; // pixel position
                clip(c.w - thresholdMatrix[fmod(pos.x, 4)] * _RowAccess[fmod(pos.y, 4)]);
                return (c);
            }
            ENDCG
        }
 
    }
//    Fallback "Diffuse"
}
