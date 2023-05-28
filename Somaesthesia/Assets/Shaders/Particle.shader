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
		uniform float _MinZ;
		uniform float _MaxZ;
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
			o.position = float4((_CamPos.x + _Width / 2.0) / 100.0 - instance_id % _Width / 100.0,
				(_CamPos.y + _Height / 2.0) / 100.0 - instance_id / _Width / 100.0,
				_CamPos.z - particleBuffer[instance_id] / 1500.0 - 3.0, 1.0f);
			o.instance = int(instance_id);
			if (segmentBuffer[instance_id] == 0)
			{
				o.keep.x = 0;
			}
			else
				o.keep.x = 1;
			return o;
		}

		[maxvertexcount(24)]
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
			return (float4(col.z, col.y, col.x, col.w));
		}

		ENDCG
		}
	}
	Fallback Off
}
