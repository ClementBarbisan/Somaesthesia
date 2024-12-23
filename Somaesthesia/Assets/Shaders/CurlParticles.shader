Shader "CurlParticles"
{
    Properties
    {
        _MainTex ("Texture Mix", 2D) = "white" {}
        _ParticleTex ("PointTex", 2D) = "white" {}
        _RadiusParticles ("Size particles", Range(0, 1)) = 0.05
        _Radius ("Size Strokes", Range(0, 20)) = 12
        _Hue ("Hue", Range(0, 2)) = 0.5
        _Sat ("Saturation", Range(0, 1)) = 0.5
        _Bri ("Brightness", Range(0, 1)) = 0.5
        _Con ("Contrast", Range(0, 20)) = 0.5
    }
    SubShader
    {
        Pass
        {
            Tags
            {
                "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"
            }
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull back
            LOD 100
            CGPROGRAM
            #pragma target 4.6

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag multi_compile_instancing
            #include "UnityCG.cginc"

            // Pixel shader input
            struct PS_INPUT
            {
                float4 position : SV_POSITION;
                uint instance : SV_InstanceID;
                float2 keep : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            struct Particle
            {
                float3 position;
                float3 velocity;
                float life;
            };

            // Particle's data, shared with the compute shader
            RWStructuredBuffer<Particle> particles : register(u1);
            Buffer<float> depth;
            Buffer<int> segmentBuffer;
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
            // Properties variables
            uniform sampler2D _MainTex;
            uniform sampler2D _ParticleTex;
            uniform sampler2D _MixTex;
            uniform float4 _MainTex_ST;
            uniform int _Width;
            uniform int _WidthTex;
            uniform int _Height;
            uniform int _HeightTex;
            float _Radius;
            float _RadiusParticles;
            uint _CurrentFrame;
            float3 _CamPos;
            
            float rand(in float2 uv)
            {
                float2 noise = (frac(sin(dot(uv, float2(12.9898, 78.233) * 2.0)) * 43758.5453));
                return abs(noise.x + noise.y) * 0.5;
            }

            PS_INPUT vert(uint instance_id : SV_instanceID)
            {
                PS_INPUT o = (PS_INPUT)0;
                o.instance = int(instance_id);
                o.position = float4(particles[o.instance].position, 1.0);
                o.keep.x = saturate(_SkeletonSize / 2);
                // for (uint i = 0; i < 18; i++)
                // {
                    // float3 posSkelet = UnityObjectToClipPos(_Skeleton[i].Pos);
                    // float dist = distance(posSkelet, pos);
                    // if (dist < _SkeletonSize)
                    // {
                        // o.keep.x = saturate(dist * (_SkeletonSize / 2)) + 0.1;
                        // break;
                    // }
                    // else
                    // {
                        // o.keep.x = 0;
                    // }
                // }
                return o;
            }

            inline float3 applyHue(float3 aColor, float aHue)
            {
                float angle = radians(aHue);
                float3 k = float3(0.57735, 0.57735, 0.57735);
                float cosAngle = cos(angle);
                //Rodrigues' rotation formula
                return aColor * cosAngle + cross(k, aColor) * sin(angle) + k * dot(k, aColor) * (1 - cosAngle);
            }


            inline float4 applyHSBEffect(float4 startColor, fixed4 hsbc)
            {
                float _Hue = 360 * hsbc.r;
                float _Brightness = hsbc.g * 2 - 1;
                float _Contrast = hsbc.b * 2;
                float _Saturation = hsbc.a * 2;

                float4 outputColor = startColor;
                outputColor.rgb = applyHue(outputColor.rgb, _Hue);
                outputColor.rgb = (outputColor.rgb - 0.5f) * (_Contrast) + 0.5f;
                outputColor.rgb = outputColor.rgb + _Brightness;
                float3 intensity = dot(outputColor.rgb, float3(0.299, 0.587, 0.114));
                outputColor.rgb = lerp(intensity, outputColor.rgb, _Saturation);

                return outputColor;
            }

            [maxvertexcount(4)]
            void geom(point PS_INPUT p[1], inout TriangleStream<PS_INPUT> triStream)
            {
                PS_INPUT o;
                o.instance = p[0].instance;
                o.keep.x = p[0].keep.x;
                o.keep.y = p[0].keep.y;
                if (p[0].keep.x <= 0 || particles[o.instance].life <= 0)
                {
                    return;
                }
                float4 position = float4(p[0].position.x, p[0].position.y, p[0].position.z, p[0].position.w);
                float size = _RadiusParticles * (rand(position.xyz) * 0.25 + 0.75);
                float3 up = float3(0, 1, 0);
                float3 look = _WorldSpaceCameraPos - p[0].position;
                look.y = 0;
                look = normalize(look);
                float3 right = cross(up, look);
                float halfS = 0.5f * size;
                float4 v[4];
                v[0] = float4(position + halfS * right * rand(position.xy) - halfS * up * rand(position.yz),
                              1.0f);
                v[1] = float4(position + halfS * right * rand(position.zx) + halfS * up * rand(position.xz),
                              1.0f);
                v[2] = float4(position - halfS * right * rand(position.zy) - halfS * up * rand(position.yx),
                              1.0f);
                v[3] = float4(position - halfS * right * rand(position.xz) + halfS * up * rand(position.zy),
                              1.0f);

                o.position = UnityObjectToClipPos(v[0]);
                o.uv = float2(1.0f, 0.0f);
                triStream.Append(o);

                o.position = UnityObjectToClipPos(v[1]);
                o.uv = float2(1.0f, 1.0f);
                triStream.Append(o);

                o.position = UnityObjectToClipPos(v[2]);
                o.uv = float2(0.0f, 0.0f);
                triStream.Append(o);

                o.position = UnityObjectToClipPos(v[3]);
                o.uv = float2(0.0f, 1.0f);
                triStream.Append(o);
            }

            half3 AdjustContrastCurve(half3 color, half contrast)
            {
                return pow(abs(color * 2 - 1), 1 / max(contrast, 0.0001)) * sign(color - 0.5) + 0.5;
            }


            float CalcLuminance(float3 color)
            {
                return dot(color, float3(0.299f, 0.587f, 0.114f)) * 3;
            }

            struct region
            {
                int x1, y1, x2, y2;
            };

            float _Hue;
            float _Sat;
            float _Bri;
            float _Con;

            float4 frag(PS_INPUT i) : COLOR
            {
                if (i.keep.x <= 0)
                {
                    discard;
                }
                float2 uv = float2(float(i.instance % _WidthTex) / (float)_WidthTex,
                                   float(i.instance / _WidthTex) / (float)_HeightTex); //i.uv;
                float4 tex = tex2D(_ParticleTex, i.uv); // * tex2D(_MainTex, (uv * _MainTex_ST.xy + _SinTime.yz)));
                // tex.w /=  1 + i.keep.y / 20;
                if (tex.w <= 0)
                {
                    discard;
                }
                float n = float((_Radius + 1) * (_Radius + 1));
                float4 col = tex2D(_MixTex, uv);
                // col.b = 0;
                const float4 colTint = col * applyHSBEffect(tex2D(_MainTex, (uv * _MainTex_ST.xy + _SinTime.yz)),
                                                            float4(_Hue, _Sat, _Bri, _Con));
                float3 m[4];
                float3 s[4];

                for (int j = 0; j < 4; ++j)
                {
                    m[j] = float3(0, 0, 0);
                    s[j] = float3(0, 0, 0);
                }

                region R[4] = {
                    {-_Radius, -_Radius, 0, 0},
                    {0, -_Radius, _Radius, 0},
                    {0, 0, _Radius, _Radius},
                    {-_Radius, 0, 0, _Radius}
                };

                for (int k = 0; k < 4; ++k)
                {
                    for (int j = R[k].y1; j <= R[k].y2; ++j)
                    {
                        for (int l = R[k].x1; l <= R[k].x2; ++l)
                        {
                            float3 c = tex2D(
                                    _MixTex,
                                    uv + (float2(l * (1.0 / (float)_WidthTex), j * (1.0 / (float)_HeightTex)))).
                                rgb;
                            m[k] += c;
                            s[k] += c * c;
                        }
                    }
                }

                float min = 1e+2;
                float s2;
                for (int l = 0; l < 4; ++l)
                {
                    m[l] /= n;
                    s[l] = abs(s[l] / n - m[l] * m[l]);

                    s2 = s[l].r + s[l].g + s[l].b;
                    if (s2 < min)
                    {
                        min = s2;
                        col.rgb = m[l].rgb;
                    }
                }
                col.w = i.keep.x * tex.w;
                if (col.w == 0)
                {
                    discard;
                }
                col = applyHSBEffect(col, float4(_Hue, _Sat, _Bri, _Con)) * (colTint.xyzw + 0.25);
                col *= pow(tex, 2);
                col.xyzw *= 1.25;
                col = saturate(col);
                return (col.zyxw);
            }
            ENDCG
        }
    }
    Fallback Off
}