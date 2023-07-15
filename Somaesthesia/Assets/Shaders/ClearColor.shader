Shader "Custom/ClearColor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "RenderType"="Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZTest Always
    	ZWrite Off
        
         Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.6
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            StructuredBuffer<float4> _UVs;
            
            float SkeletonSize;
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                uint nb = 0;
                uint stride = 0;
                _UVs.GetDimensions(nb, stride);
                for (int j = 0; j < nb; j++)
                {
                    if (length(_UVs[j].x) + length(_UVs[j].y) > 1 && distance(_UVs[j].xy / _ScreenParams.xy, i.uv)
                        <= length(_UVs[j].z) / (_ScreenParams.x / 2) + length(_UVs[j].w) / (_ScreenParams.y / 2))
                    {
                        float val = distance(_UVs[j].xy / _ScreenParams.xy, i.uv) / (length(_UVs[j].z) /
                            (_ScreenParams.x / 2) + length(_UVs[j].w) / (_ScreenParams.y / 2));
                        float3 colGray = (col.r * 0.299 + col.g * 0.587 + col.b * 0.114) * val;
                        return float4(col.rgb * (1 - val) + colGray.rgb, 1);
                    }
                }
                col.rgb = col.r * 0.299 + col.g * 0.587 + col.b * 0.114;
                return col;
            }
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float4 _Color;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
            }
            ENDCG
        }
    }
}
