Shader "Triangulation"
{
    Properties
    {
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

            struct Particle
            {
                float3 position;
                float3 velocity;
                float life;
            };
            
            // Particle's data, shared with the compute shader
            RWStructuredBuffer<Particle> particles : register(u1);
            Buffer<float> particleBuffer;
            Buffer<int> segmentBuffer;
            float _SkeletonSize;
            float _MaxSize;

            // Properties variables
            uniform sampler2D _MixTex;
            uniform float4 _MainTex_ST;
            uniform int _Width;
            uniform int _WidthTex;
            uniform int _Height;
            uniform int _HeightTex;
            uniform float3 _CamPos;
            uniform float _Rotation;
             float _RadiusParticles;
            float _Radius;
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
                if (instance_id % (20 + 100 * (int)(_MaxSize - _SkeletonSize)) > 0)
                {
                    o.keep.y = 0;
                }
                else
                {
                    o.keep.y = 1;
                }
                if (o.keep.y == 0)
                {
                    o.keep.x = 0;
                    return (o);
                }
                o.instance = int(instance_id);
                o.position = float4(particles[o.instance].position, 1.0);
                o.keep.x = 1;
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

            float ComputeXAngle(float4 q)
            {
                float sinr_cosp = 2 * (q.w * q.x + q.y * q.z);
                float cosr_cosp = 1 - 2 * (q.x * q.x + q.y * q.y);
                return atan2(sinr_cosp, cosr_cosp);
            }

            static const float PI = 3.14159265f;

            float ComputeYAngle(float4 q)
            {
                float sinp = 2 * (q.w * q.y - q.z * q.x);
                if (abs(sinp) >= 1)
                    return PI / 2 * sign(sinp); // use 90 degrees if out of range
                else
                    return asin(sinp);
            }

            float ComputeZAngle(float4 q)
            {
                float siny_cosp = 2 * (q.w * q.z + q.x * q.y);
                float cosy_cosp = 1 - 2 * (q.y * q.y + q.z * q.z);
                return atan2(siny_cosp, cosy_cosp);
            }

            float3 CoputeAngles(float4 q)
            {
                return float3(ComputeXAngle(q), ComputeYAngle(q), ComputeZAngle(q));
            }

            float4 FromAngles(float3 angles)
            {
                float cy = cos(angles.z * 0.5f);
                float sy = sin(angles.z * 0.5f);
                float cp = cos(angles.y * 0.5f);
                float sp = sin(angles.y * 0.5f);
                float cr = cos(angles.x * 0.5f);
                float sr = sin(angles.x * 0.5f);

                float4 q;
                q.w = cr * cp * cy + sr * sp * sy;
                q.x = sr * cp * cy - cr * sp * sy;
                q.y = cr * sp * cy + sr * cp * sy;
                q.z = cr * cp * sy - sr * sp * cy;

                return q;
            }

            float4 qmul(float4 q1, float4 q2)
            {
                return float4(
                    q2.xyz * q1.w + q1.xyz * q2.w + cross(q1.xyz, q2.xyz),
                    q1.w * q2.w - dot(q1.xyz, q2.xyz)
                );
            }

            // Vector rotation with a quaternion
            // http://mathworld.wolfram.com/Quaternion.html
            float3 rotate_vector(float3 v, float4 r)
            {
                float4 r_c = r * float4(-1, -1, -1, 1);
                return qmul(r, qmul(float4(v, 0), r_c)).xyz;
            }

            [maxvertexcount(6)]
            void geom(point PS_INPUT p[1], inout LineStream<PS_INPUT> triStream)
            {
                PS_INPUT o;
                o.instance = p[0].instance;
                if (p[0].keep.x == 0 || particles[o.instance].life <= 0)
                {
                    return;
                }
                o.keep.x = p[0].keep.x;
                o.keep.y = p[0].keep.y;
                float4 position = float4(p[0].position.x, p[0].position.y, p[0].position.z, p[0].position.w);
                float size = (rand(position.xyz) * 0.25 + 0.75) * _RadiusParticles * (_MaxSize - _SkeletonSize + 0.1f);
                float3 up = float3(0, 1, 0);
                float3 look = _WorldSpaceCameraPos - p[0].position;
                look.y = 0;
                look = normalize(look);
                float3 right = cross(up, look);
                right = rotate_vector(right, FromAngles(particles[p[0].instance].velocity));
                float halfS = 0.5f * size;
                float4 v[3];
                v[0] = float4(
                    position + halfS * right * rand(position.xy) - halfS * up * rand(position.yz), 1.0f);
                v[1] = float4(
                    position + halfS * right * rand(position.zx) + halfS * up * rand(position.xz), 1.0f);
                v[2] = float4(
                    position - halfS * right * rand(position.zy) - halfS * up * rand(position.yx), 1.0f);

                o.position = UnityObjectToClipPos(v[0]);
                o.uv = float2(0.0f, 0.0f);
                triStream.Append(o);

                o.position = UnityObjectToClipPos(v[1]);
                o.uv = float2(1.0f, 0.0f);
                triStream.Append(o);
                triStream.RestartStrip();

                o.position = UnityObjectToClipPos(v[1]);
                o.uv = float2(1.0f, 0.0f);
                triStream.Append(o);
                
                o.position = UnityObjectToClipPos(v[2]);
                o.uv = float2(0.0f, 1.0f);
                triStream.Append(o);
                triStream.RestartStrip();
                
                 o.position = UnityObjectToClipPos(v[2]);
                o.uv = float2(0.0f, 1.0f);
                triStream.Append(o);

                 o.position = UnityObjectToClipPos(v[0]);
                o.uv = float2(0.0f, 0.0f);
                triStream.Append(o);
                triStream.RestartStrip();
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
                                   float(i.instance / _WidthTex) / (float)_HeightTex); 
                float4 col = tex2D(_MixTex, uv);
                col = applyHSBEffect(col, float4(_Hue, _Sat, _Bri, _Con));
                col.xyzw *= 1.25;
                col = saturate(col);
                return (col.zyxw);
            }
            ENDCG
        }
    }
}