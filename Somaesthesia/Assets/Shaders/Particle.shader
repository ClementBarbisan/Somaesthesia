// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "Particle"
{
	Properties
	{
	}

	SubShader 
	{
		Pass 
		{
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
			#pragma target 5.0

			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma Standard fullforwardshadows addshadow
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
		StructuredBuffer<float> particleBuffer;
		StructuredBuffer<int> segmentBuffer;


		// Properties variables
		uniform sampler2D _MainTex;
		uniform int _Width;
		uniform int _WidthTex;
		uniform int _Height;
		uniform int _HeightTex;
		uniform float3 _CamPos;
		uniform float _Rotation;

		float rand(in float2 uv)
		{
			float2 noise = (frac(sin(dot(uv, float2(12.9898, 78.233)*2.0)) * 43758.5453));
			return abs(noise.x + noise.y) * 0.5;
		}

		// Vertex shader
		PS_INPUT vert(uint instance_id : SV_instanceID)
		{
			PS_INPUT o = (PS_INPUT)0;
			// Position
			o.position = float4((_CamPos.x + _Width / 2.0) / 200.0 - instance_id % _Width / 200.0,
				(_CamPos.y + _Height / 2.0) / 200.0 - instance_id / _Width / 200.0,
				_CamPos.z - particleBuffer[instance_id] / 3000.0 - 2.0, 1.0f);
			o.instance = int(instance_id);
			if (segmentBuffer[instance_id] == 0)
			{
				o.keep.x = 0;
			}
			else
				o.keep.x = 1;
			return o;
		}

		[maxvertexcount(4)]
		void geom(point PS_INPUT p[1], inout TriangleStream<PS_INPUT> triStream)
		{
			PS_INPUT o;
			o.instance = p[0].instance;
			o.uv = float2(0, 0);
			if (p[0].keep.x == 0)
			{
				return;
			}
			o.keep.x = 1;
			o.keep.y = p[0].keep.y;
			float4 position = float4(p[0].position.x , p[0].position.y , p[0].position.z, p[0].position.w);
			float size = 0.05;
			float3 up = float3(0, 1, 0);
			float3 look = _WorldSpaceCameraPos - p[0].position;
			look.y = 0;
			look = normalize(look);
			float3 right = cross(up, look);
			float halfS = 0.5f * size;
			float4 v[4];
			v[0] = float4(position + halfS * right - halfS * up, 1.0f);
			v[1] = float4(position + halfS * right + halfS * up, 1.0f);
			v[2] = float4(position - halfS * right - halfS * up, 1.0f);
			v[3] = float4(position - halfS * right + halfS * up, 1.0f);

			o.position = UnityObjectToClipPos(v[0]);
			o.uv = float2(1.0f, 0.0f);
			triStream.Append(o);

			o.position =  UnityObjectToClipPos(v[1]);
			o.uv = float2(1.0f, 1.0f);
			triStream.Append(o);

			o.position = UnityObjectToClipPos( v[2]);
			o.uv = float2(0.0f, 0.0f);
			triStream.Append(o);

			o.position =  UnityObjectToClipPos( v[3]);
			o.uv = float2(0.0f, 1.0f);
			triStream.Append(o);
		}

		float CalcLuminance(float3 color)
		{
			return dot(color, float3(0.299f, 0.587f, 0.114f)) * 3;
		}

		// Pixel shader
		float4 frag(PS_INPUT i) : COLOR
		{
			if (i.keep.x == 0)
			{
				discard;
			}
			half2 fw = fwidth(i.uv);
			half2 edge2 = min(smoothstep(0, fw * 2, i.uv),
				smoothstep(0, fw * 2, 1 - i.uv));
			half edge = 1 - min(edge2.x, edge2.y);
			float2 uv =  float2(float(i.instance % _WidthTex) / (float)_WidthTex, float(i.instance / _WidthTex) / (float)_HeightTex);
			float4 col = tex2D(_MainTex,uv);
			return (float4(col.z, col.y, col.x,0));// col.w));
		}

		ENDCG
		}
		Pass 
		{
			 Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		    ZWrite Off
		    Blend SrcAlpha OneMinusSrcAlpha
		    Cull front 
			LOD 100
			CGPROGRAM
			#pragma target 5.0
			
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma Standard fullforwardshadows addshadow
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
			
			//sampler2D _MainTex;

			// Properties variables
			uniform float _Rotation;
			
			float rand(in float2 uv)
			{
				float2 noise = (frac(sin(dot(uv, float2(12.9898, 78.233)*2.0)) * 43758.5453));
				return abs(noise.x + noise.y) * 0.5;
			}
			// Vertex shader
			PS_INPUT vert(uint instance_id : SV_instanceID)
			{
				PS_INPUT o = (PS_INPUT)0;
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

			
			void AddVertex(point PS_INPUT o,inout LineStream<PS_INPUT> lineStream, float4 pos1, float4 pos2, int nbVertex)
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
					o.position = p + PeriodicNoise(float3(rand(p.xy), rand(p.yz), rand(p.xz)), sin(_Time.xxx / 1000) * 20) / 15;
					lineStream.Append(o);
				}
				o.position = y;
				lineStream.Append(o);
				lineStream.RestartStrip();
			}

			[maxvertexcount(204)]
			void geom(point PS_INPUT p[1], inout LineStream<PS_INPUT> lineStream)
			{
				uint nb = 0;
				uint stride = 0;
				_Skeleton.GetDimensions(nb, stride);
				PS_INPUT o;
				o.instance = p[0].instance;
				// float2 screenPos = ComputeScreenPos(p[0].position);
				const uint index = (uint)((p[0].instance + 1) % nb);
				// if (i == p[0].instance)
				// {
					// i = (i + 1) % 18;
				// }
				// const int instanceDiv = clamp(nb / (_Time.z), 1, nb);
				const int nbVertex = clamp(_Time.w, 0, nb);
				const float size = _Skeleton[p[0].instance].Size;
				float3 top = size / 2;
				float3 bottom = -size / 2;
				// for (int i = o.instance; i < o.instance + instanceDiv; i++)
				// {
				// 	float3 pos = _Skeleton[i].Pos;
				// 	top.xyz = max(top.xyz, pos.xyz);
				// 	bottom.xyz = min(bottom.xyz, pos.xyz);
				// }
				// top.z = top.z / 5 + size;
				// bottom.z = bottom.z / 5 - size;
				// top.xy = top.xy / 2 + size;
				// bottom.xy = bottom.xy / 2 - size;
				float4 pos = p[0].position;
				float3x3 mat = _Skeleton[p[0].instance].Matrice;
				// const float noise1 = sin(_Time.x / 20) / 50 * nbVertex;
				// const float noise2 = cos(_Time.x / 20) / 50 * nbVertex;
				const float4 A = UnityObjectToClipPos(mul(mat , float3(bottom.x, top.y, top.z)) + pos);// + noise1;
				const float4 B = UnityObjectToClipPos(mul(mat, float3(top.x, top.y, top.z)) + pos);// + noise1;
				const float4 C = UnityObjectToClipPos(mul(mat, float3(bottom.x, top.y, bottom.z)) + pos);// + noise1;
				const float4 D = UnityObjectToClipPos(mul(mat, float3(top.x, top.y, bottom.z)) + pos);// + noise1;
				const float4 E = UnityObjectToClipPos(mul(mat, float3(top.x, bottom.y, bottom.z)) + pos);// + noise2;
				const float4 F = UnityObjectToClipPos(mul(mat, float3(top.x, bottom.y, top.z)) + pos);// + noise2;
				const float4 G = UnityObjectToClipPos(mul(mat, float3(bottom.x, bottom.y, top.z)) + pos);// + noise2;
				const float4 H = UnityObjectToClipPos(mul(mat, float3(bottom.x, bottom.y, bottom.z)) + pos);// + noise2;
				AddVertex(o, lineStream, A, G, nbVertex);
				AddVertex(o, lineStream, G, F, nbVertex);
				AddVertex(o, lineStream, F, B, nbVertex);
				AddVertex(o, lineStream, B, A, nbVertex);
				AddVertex(o, lineStream, C, H, nbVertex);
				AddVertex(o, lineStream, H, E, nbVertex);
				AddVertex(o, lineStream, E, D, nbVertex);
				AddVertex(o, lineStream, D, C, nbVertex);
				AddVertex(o, lineStream, A, C, nbVertex);
				AddVertex(o, lineStream, G, H, nbVertex);
				AddVertex(o, lineStream, F, E, nbVertex);
				AddVertex(o, lineStream, B, D, nbVertex);


				AddVertex(o, lineStream, UnityObjectToClipPos(p[0].position),
				UnityObjectToClipPos(float4(_Skeleton[index].Pos, 1.0)), nbVertex);
			}
			
			// Pixel shader
			float4 frag(PS_INPUT i) : COLOR
			{
				return (float4(1.0f, 1.0f, 1.0f, 1.0f));
			}
			
			ENDCG
		}
	}
	Fallback Off
}
