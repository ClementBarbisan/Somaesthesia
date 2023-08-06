// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "Particle"
{
    Properties
    {
        _MainTex ("Texture Mix", 2D) = "white" {}
        _ParticleTex ("PointTex", 2D) = "white" {}
        _RadiusParticles ("Size particles", Range(0, 1)) = 0.05
        _Radius ("Size Strokes", Range(0, 20)) = 12
        _Offset ("Offset Surround", Range(0, 50)) = 5
        _SizeCube ("Size cubes skeleton", Range(0, 2)) = 0.25
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
            #include "Packages/jp.keijiro.noiseshader/Shader/Common.hlsl"
            #include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise3D.hlsl"
            #include "UnityCG.cginc"

            // Pixel shader input
            struct PS_INPUT
            {
                float4 position : SV_POSITION;
                uint instance : SV_InstanceID;
                float2 keep : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            // Particle's data, shared with the compute shader
            Buffer<float> particleBuffer;
            Buffer<int> segmentBuffer;
            RWBuffer<float3> oldParticles;
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
            uniform  sampler2D _ParticleTex;
            uniform sampler2D _MixTex;
            uniform float4 _MainTex_ST;
            uniform int _Width;
            uniform int _WidthTex;
            uniform int _Height;
            uniform int _HeightTex;
            uniform float3 _CamPos;
            uniform float _Rotation;
            float _Radius;
            float _RadiusParticles;
            uint _MaxFrame;
            int _CurrentFrame;
            float3 _SpherePosition;

            float rand(in float2 uv)
            {
                float2 noise = (frac(sin(dot(uv, float2(12.9898, 78.233) * 2.0)) * 43758.5453));
                return abs(noise.x + noise.y) * 0.5;
            }

            PS_INPUT vert(uint instance_id : SV_instanceID)
            {
                PS_INPUT o = (PS_INPUT)0;
                uint index = 0;
                for (uint j = _CurrentFrame; index < _MaxFrame; j = j == 0 ? _MaxFrame - 1 : j - 1)
                {
                    if (segmentBuffer[_Width * _Height * j + instance_id] != 1 || instance_id % (30 * int(index + 1)) > 0)
                    {
                        o.keep.y = 0;
                    }
                    else
                    {
                        o.keep.y = index + 1;
                        break;
                    }
                    index++;
                }
                if (o.keep.y == 0)
                {
                    o.keep.x = 0;
                    return (o);
                }
                o.instance = int(instance_id);
                o.position = float4((_CamPos.x + _Width / 2.0) / 200.0 - instance_id % _Width / 200.0,
                                    (_CamPos.y + _Height / 2.0) / 200.0 - instance_id / _Width / 200.0,
                                    _CamPos.z - particleBuffer[_Width * _Height * o.keep.y + instance_id] / 3000.0 - 2.0, 1.0f);
                float3 pos = UnityObjectToClipPos(o.position);
                float3 posSphere = UnityObjectToClipPos(_SpherePosition);
                for (uint i = 0; i < 18; i++)
                {
                    float3 posSkelet = UnityObjectToClipPos(_Skeleton[i].Pos);
                    float dist = distance(posSkelet, pos);
                    if (o.keep.y > 0 && dist < _SkeletonSize)
                    {
                        o.keep.x = saturate(dist * (_SkeletonSize / 2));
                       
                        break;
                    }
                    else
                    {
                        o.keep.x = 0;
                    }
                }
                if (o.keep.x == 0)
                {
                    return o;
                }
              
                if (distance(posSphere, o.position) < _SkeletonSize / 2)
                {
                    o.position.xyz = posSphere +  normalize(o.position - posSphere) * (_SkeletonSize / 1.5);
                }
                o.keep.y = index;
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
                float3 intensity = dot(outputColor.rgb, float3(0.299,0.587,0.114));
                outputColor.rgb = lerp(intensity, outputColor.rgb, _Saturation);
             
                return outputColor;
            }

            [maxvertexcount(4)]
            void geom(point PS_INPUT p[1], inout TriangleStream<PS_INPUT> triStream)
            {
                PS_INPUT o;
                o.instance = p[0].instance;
                if (p[0].keep.x == 0)
                {
                    return;
                }
                o.keep.x = p[0].keep.x;
                o.keep.y = p[0].keep.y;
                float4 position = float4(p[0].position.x, p[0].position.y, p[0].position.z, p[0].position.w) + ClassicNoise(p[0].position.xyz) * (_SkeletonSize / 5);
                float size = _RadiusParticles * (rand(position.xyz) * 0.25 + 0.75) / (1 + (float)o.keep.y / 20);
                float3 up = float3(0, 1, 0);
                float3 look = _WorldSpaceCameraPos - p[0].position;
                look.y = 0;
                look = normalize(look);
                float3 right = cross(up, look);
                float halfS = 0.5f * size;
                float4 v[4];
                v[0] = float4(position + halfS * right * rand(position.xy) * 1.5 - halfS * up * rand(position.yz) * 1.5, 1.0f);
                v[1] = float4(position + halfS * right * rand(position.zx) * 1.5 + halfS * up * rand(position.xz) * 1.5, 1.0f);
                v[2] = float4(position - halfS * right * rand(position.zy) * 1.5 - halfS * up * rand(position.yx) * 1.5, 1.0f);
                v[3] = float4(position - halfS * right * rand(position.xz) * 1.5 + halfS * up * rand(position.zy) * 1.5, 1.0f);

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
                if (i.keep.x == 0)
                {
                    discard;
                }
                float2 uv = float2(float(i.instance % _WidthTex) / (float)_WidthTex,
                                   float(i.instance / _WidthTex) / (float)_HeightTex); //i.uv;
                float4 tex = tex2D(_ParticleTex, i.uv);// * tex2D(_MainTex, (uv * _MainTex_ST.xy + _SinTime.yz)));
                // tex.w /=  1 + i.keep.y / 20;
                 if (tex.w == 0)
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
        Pass
        {
            Tags
            {
                "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"
            }
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull front
            LOD 100
            CGPROGRAM
            #pragma target 4.6

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag multi_compile_instancing
            #include "Packages/jp.keijiro.noiseshader/Shader/Common.hlsl"
            #include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise3D.hlsl"
            #include "UnityCG.cginc"

            // Pixel shader input
            struct PS_INPUT
            {
                float4 position : SV_POSITION;
                uint instance : SV_InstanceID;
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
            float  _SizeCube;
            int _CurrentFrame;

            float rand(in float2 uv)
            {
                float2 noise = (frac(sin(dot(uv, float2(12.9898, 78.233) * 2.0)) * 43758.5453));
                return abs(noise.x + noise.y) * 0.5;
            }

            // Vertex shader
            PS_INPUT vert(uint instance_id : SV_instanceID)
            {
                PS_INPUT o;
                // Position
                o.position = float4(_Skeleton[instance_id].Pos, 1.0);
                o.instance = int(instance_id);
                return o;
            }

            float map(float value, float min1, float max1, float min2, float max2)
            {
                // Convert the current value to a percentage
                // 0% - min1, 100% - max1
                float perc = (value - min1) / (max1 - min1);

                // Do the same operation backwards with min2 and max2
                float val = perc * (max2 - min2) + min2;
                return (val);
            }


            void AddLine(point PS_INPUT o, inout LineStream<PS_INPUT> lineStream, float4 pos1, float4 pos2,
                           int nbVertex)
            {
                float4 x = pos1;
                float4 y = pos2;
                o.position = x;
                lineStream.Append(o);
                float4 dir = y - x;
                int cNb = clamp(nbVertex, 0, 12);
                for (int i = 1; i < cNb; i++)
                {
                    float4 p = x + dir * ((float)i / (float)cNb);
                    o.position = p + PeriodicNoise(float3(rand(p.xy), rand(p.yz), rand(p.xz)),
                                                   sin(_Time.xxx / 1000) * 20) / 15;
                    lineStream.Append(o);
                }
                o.position = y;
                lineStream.Append(o);
                lineStream.RestartStrip();
            }

            [maxvertexcount(204)]
            void geom(point PS_INPUT p[1], inout LineStream<PS_INPUT> lineStream)
            {
                PS_INPUT o;
                o.instance = p[0].instance;
                const int nbVertex = clamp(_SkeletonSize * 10, 0, 20);
                const float size = _SizeCube;
                float3 top = size / 2;
                float3 bottom = -size / 2;
                float4 pos = p[0].position;
                float3x3 mat = _Skeleton[p[0].instance].Matrice;
                const float4 A = UnityObjectToClipPos(mul(mat, float3(bottom.x, top.y, top.z)) + pos); // + noise1;
                const float4 B = UnityObjectToClipPos(mul(mat, float3(top.x, top.y, top.z)) + pos); // + noise1;
                const float4 C = UnityObjectToClipPos(mul(mat, float3(bottom.x, top.y, bottom.z)) + pos); // + noise1;
                const float4 D = UnityObjectToClipPos(mul(mat, float3(top.x, top.y, bottom.z)) + pos); // + noise1;
                const float4 E = UnityObjectToClipPos(mul(mat, float3(top.x, bottom.y, bottom.z)) + pos); // + noise2;
                const float4 F = UnityObjectToClipPos(mul(mat, float3(top.x, bottom.y, top.z)) + pos); // + noise2;
                const float4 G = UnityObjectToClipPos(mul(mat, float3(bottom.x, bottom.y, top.z)) + pos); // + noise2;
                const float4 H = UnityObjectToClipPos(mul(mat, float3(bottom.x, bottom.y, bottom.z)) + pos);
                AddLine(o, lineStream, A, G, nbVertex);
                AddLine(o, lineStream, G, F, nbVertex);
                AddLine(o, lineStream, F, B, nbVertex);
                AddLine(o, lineStream, B, A, nbVertex);
                AddLine(o, lineStream, C, H, nbVertex);
                AddLine(o, lineStream, H, E, nbVertex);
                AddLine(o, lineStream, E, D, nbVertex);
                AddLine(o, lineStream, D, C, nbVertex);
                AddLine(o, lineStream, A, C, nbVertex);
                AddLine(o, lineStream, G, H, nbVertex);
                AddLine(o, lineStream, F, E, nbVertex);
                AddLine(o, lineStream, B, D, nbVertex);

                for (int i = 0; i < 18; i++)
                {
                    if (i != p[0].instance)
                    {
                        AddLine(o, lineStream, UnityObjectToClipPos(p[0].position),
                          UnityObjectToClipPos(float4(_Skeleton[i].Pos, 1.0)), nbVertex);
                    }
                }
                
            }

            // Pixel shader
            float4 frag(PS_INPUT i) : COLOR
            {
                float w = 1.0 - _SkeletonSize / 3.5;
                if (w < 0.01)
                {
                    discard;
                }
                return (float4(1.0, 1.0, 1.0, w));
            }
            ENDCG
        }
        Pass
        {
            Tags
            {
                "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"
            }
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull front
            LOD 100
            CGPROGRAM
            #pragma target 4.6

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag multi_compile_instancing
            #include "Packages/jp.keijiro.noiseshader/Shader/Common.hlsl"
            #include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise3D.hlsl"
            #include "UnityCG.cginc"

           	struct PS_INPUT
			{
				float4 position : SV_POSITION;
				uint instance : SV_InstanceID;
				float2 keep : TEXCOORD0;
			};
            float _SkeletonSize;

		    Buffer<float> particleBuffer;
		    Buffer<int> segmentBuffer;
            float rand(in float2 uv)
            {
                float2 noise = (frac(sin(dot(uv, float2(12.9898, 78.233) * 2.0)) * 43758.5453));
                return abs(noise.x + noise.y) * 0.5;
            }

            uniform sampler2D _MainTex;
            uniform int _Width;
            uniform int _WidthTex;
            uniform int _Height;
            uniform int _HeightTex;
            uniform float3 _CamPos;
            int _Offset;
            int _CurrentFrame;
            
            PS_INPUT vert(uint instance_id : SV_instanceID)
            {
                PS_INPUT o = (PS_INPUT)0;
                const uint initPoint = _Width * _Height * _CurrentFrame;
                o.position = float4((_CamPos.x + _Width / 2.0) / 200.0 - instance_id % _Width / 200.0,
                                    (_CamPos.y + _Height / 2.0) / 200.0 - instance_id / _Width / 200.0,
                                    _CamPos.z - particleBuffer[initPoint + instance_id] / 3000.0 - 2.0, 1.0f);
                // instance_id = instance_id / 2;
                o.instance = int(instance_id);
                
                if (segmentBuffer[initPoint + instance_id] == 0)
                {
                    o.keep = float2(-1, 0);
                }
                else if ((instance_id % _Offset == 0 || instance_id % (_WidthTex * _Offset) == 0) && (instance_id + _Offset)
                    < (_WidthTex * _HeightTex) && ((int)instance_id - _Offset) >= 0 &&(instance_id + _WidthTex * _Offset) <
                    (_WidthTex * _HeightTex) && ((int)instance_id - _WidthTex * _Offset) >= 0)
                {
                    o.keep = float2(-1, 0);
                    if (segmentBuffer[(initPoint + instance_id - _Offset)] == 0 && segmentBuffer[(initPoint + instance_id + _Offset)] > 0)
                    {
                        o.keep.y = (float)(instance_id + _WidthTex * _Offset);
                        o.keep.x = initPoint;
                    }
                    else if (segmentBuffer[(initPoint + instance_id + _Offset)] == 0 && segmentBuffer[(initPoint + instance_id - _Offset)] > 0)
                    {
                        o.keep.y = (float)(instance_id - _WidthTex * _Offset);
                        o.keep.x = initPoint;
                    }
                    else if (segmentBuffer[(initPoint + instance_id - _WidthTex * _Offset)] == 0 && segmentBuffer[(initPoint + instance_id + _WidthTex * _Offset)] > 0)
                    {
                         o.keep.y = (float)(instance_id + _Offset);
                         o.keep.x = initPoint;
                    }
                    else if (segmentBuffer[(initPoint + instance_id + _WidthTex * _Offset)] == 0 && segmentBuffer[(initPoint + instance_id - _WidthTex * _Offset)] > 0)
                    {
                         o.keep.y = (float)(instance_id - _Offset);
                         o.keep.x = initPoint;
                    }
                }
                else
                {
                    o.keep = float2(-1, 0);
                }
                return o;
            }
            
            void AddLine(point PS_INPUT o, inout LineStream<PS_INPUT> lineStream, float4 pos1, float4 pos2,
                           int nbVertex)
            {
                float4 x = pos1;
                float4 y = pos2;
                o.position = x + _SinTime.z * (_SkeletonSize / 20);
                lineStream.Append(o);
                float4 dir = y - x;
                for (int i = 1; i < nbVertex; i++)
                {
                    float4 p = x + dir * ((float)i / (float)nbVertex);
                    o.position = p + _SinTime.z* (_SkeletonSize / 20);
                    lineStream.Append(o);
                }
                o.position = y + _SinTime.z * (_SkeletonSize / 20);
                lineStream.Append(o);
                lineStream.RestartStrip();
            }
            
            [maxvertexcount(12)]
            void geom(point PS_INPUT p[1], inout LineStream<PS_INPUT> lineStream)
            {
                PS_INPUT o;
                o.keep = p[0].keep;
                if (o.keep.x == -1)
                {
                    return;
                }
                o.instance = p[0].instance;
                float4 pos2 = float4((_CamPos.x + _Width / 2.0) / 200.0 - o.keep.y % _Width / 200.0,
                                    (_CamPos.y + _Height / 2.0) / 200.0 - o.keep.y / _Width / 200.0,
                                    _CamPos.z - particleBuffer[(int)o.keep.x + o.keep.y] / 3000.0 - 2.0, 1.0f);
                const int nbVertex = clamp(_SkeletonSize, 0, 10);
                float4 pos1 = UnityObjectToClipPos(p[0].position);
                float4 pos2Clip = UnityObjectToClipPos(pos2);
                AddLine(o, lineStream,  pos1 + ClassicNoise(pos1.xyz) * (_SkeletonSize / 20),
                           pos2Clip + ClassicNoise(pos2Clip.xyz) * (_SkeletonSize / 20), nbVertex);
            }

            float CalcLuminance(float3 color)
            {
                return dot(color, float3(0.299f, 0.587f, 0.114f)) * 3;
            }

            struct region
            {
                int x1, y1, x2, y2;
            };

            float4 frag(PS_INPUT i) : COLOR
            {
                if (i.keep.x == -1)
                {
                    discard;
                    return (float4(0,0,0,0));
                }
                // float2 uv = float2(float(i.instance % _WidthTex) / (float)_WidthTex,
                //                    float(i.instance / _WidthTex) / (float)_HeightTex);
                // float4 col = tex2D(_MainTex, uv);
                // col = saturate(col) * CalcLuminance(col);
                // col.xyz = max(col.x, max(col.y, col.z));
                return (float4(1,1,1,1));
            }
            ENDCG
        }
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
            #include "Packages/jp.keijiro.noiseshader/Shader/Common.hlsl"
            #include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise3D.hlsl"
            #include "UnityCG.cginc"

            // Pixel shader input
            struct PS_INPUT
            {
                float4 position : SV_POSITION;
                uint instance : SV_InstanceID;
                float2 keep : TEXCOORD0;
                float2 alpha : TEXCOORD1;
            };

            // Particle's data, shared with the compute shader
            Buffer<float> particleBuffer;
            Buffer<int> segmentBuffer;
            RWBuffer<float3> oldParticles;
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
            uniform int _Width;
            uniform int _Height;
            uniform float3 _CamPos;
            uniform float _Rotation;
            float _Radius;
            float _RadiusParticles;
            uint _MaxFrame;
            int _CurrentFrame;

            float rand(in float2 uv)
            {
                float2 noise = (frac(sin(dot(uv, float2(12.9898, 78.233) * 2.0)) * 43758.5453));
                return abs(noise.x + noise.y) * 0.5;
            }

            PS_INPUT vert(uint instance_id : SV_instanceID)
            {
                PS_INPUT o = (PS_INPUT)0;
                uint index = 0;
                for (uint j = _CurrentFrame; index < _MaxFrame; j = j == 0 ? _MaxFrame - 1 : j - 1)
                {
                    if (segmentBuffer[_Width * _Height * j + instance_id] != 1 || instance_id % (30 * int(index + 1)) > 0)
                    {
                        o.keep.y = 0;
                    }
                    else
                    {
                        o.keep.y = index + 1;
                        break;
                    }
                    index++;
                }
                if (o.keep.y == 0)
                {
                    o.keep.x = 0;
                    return (o);
                }
                o.instance = int(instance_id);
                o.position = float4((_CamPos.x + _Width / 2.0) / 200.0 - instance_id % _Width / 200.0,
                                    (_CamPos.y + _Height / 2.0) / 200.0 - instance_id / _Width / 200.0,
                                    _CamPos.z - particleBuffer[_Width * _Height * o.keep.y + instance_id] / 3000.0 - 2.0, 1.0f);
                float3 pos = UnityObjectToClipPos(o.position);

                for (uint i = 0; i < 18; i++)
                {
                    float3 posSkelet = UnityObjectToClipPos(_Skeleton[i].Pos);
                    float dist = distance(posSkelet, pos);
                    if (o.keep.y > 0 && dist < _SkeletonSize)
                    {
                        o.keep.x = saturate(dist * (_SkeletonSize / 2));
                       
                        break;
                    }
                    else
                    {
                        o.keep.x = 0;
                    }
                }
                if (o.keep.x == 0)
                {
                    return o;
                }
             
                o.keep.y = index;
                return o;
            }

            void AddLine(point PS_INPUT o, inout LineStream<PS_INPUT> lineStream, float4 pos, int index, int nbVertex, int stride)
            {
                o.position = pos;
                o.alpha.x = 0.5;
                lineStream.Append(o);
                for (int i = 1; i < nbVertex; i++)
                {
                    o.alpha.x -= o.alpha.x / nbVertex;
                    float3 x = UnityObjectToClipPos(_Skeleton[index + stride * (i)].Pos).xyz;
                    float3 y = UnityObjectToClipPos(_Skeleton[index + stride * (i - 1)].Pos).xyz;
                    if (distance(x, y) > 0.2 || distance(x, y) < 0.05)
                    {
                        break;
                    }
                    pos.xyz += (x - y);
                    o.position = pos;
                    lineStream.Append(o);
                }
                // lineStream.RestartStrip();
            }
            
            [maxvertexcount(30)]
            void geom(point PS_INPUT p[1], inout LineStream<PS_INPUT> lineStream)
            {
                PS_INPUT o;
                o.instance = p[0].instance;
                if (p[0].keep.x == 0)
                {
                    return;
                }
                o.keep.x = p[0].keep.x;
                o.keep.y = p[0].keep.y;
                float4 position = UnityObjectToClipPos(float4(p[0].position.x, p[0].position.y, p[0].position.z, p[0].position.w));
                int index = 0;
                float distTotal = 5;
                for (int i = 0; i < 18; i++)
                {
                    float3 posSkelet = UnityObjectToClipPos(_Skeleton[i].Pos);
                    float dist = distance(posSkelet, position);
                    if (dist < distTotal)
                    {
                        index = i;
                        distTotal = dist;
                    }
                }
                o.alpha.y = 0;
                AddLine(o, lineStream, position, index, _MaxFrame, 18);
            }
            

            float4 frag(PS_INPUT i) : COLOR
            {
                if (i.keep.x == 0 || i.alpha.x == 0)
                {
                    discard;
                }
                return (float4(1, 1, 1, i.alpha.x));
            }
            ENDCG
        }
    }
    Fallback Off
}