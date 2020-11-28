// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Procedural Geometry"
{
	Properties
	{
		_Color ("Main Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		Cull Back

		Pass
		{
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma instancing_options procedural:setup

			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"

			struct Vertex
			{
				float3 vPosition;
				float3 vNormal;
			};

			struct Triangle
			{
				Vertex v[3];
			};

			uniform RWStructuredBuffer<Vertex> vertices;
			uniform RWStructuredBuffer<int> indices;
			uniform float4x4 model;

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
				half3 worldNormal : TEXCOORD0;
				fixed4 diff : COLOR0;
			};

			v2f vert(uint id : SV_VertexID)
			{
				//uint pid = id / 3;
				//uint vid = id % 3;

				v2f o;

				o.vertex = mul(UNITY_MATRIX_VP, mul(model, float4(vertices[indices[id]].vPosition, 1)));
				o.normal = mul(unity_ObjectToWorld, vertices[indices[id]].vNormal);
				o.worldNormal = UnityObjectToWorldNormal(o.normal);

				half nl = max(0, dot(o.worldNormal, _WorldSpaceLightPos0.xyz));
				o.diff = nl * _LightColor0;
				o.diff.rgb += ShadeSH9(half4(o.worldNormal, 1));

				return o;
			}
			fixed4 _Color;
			float4 frag(v2f i) : SV_Target
			{
				float4 col = _Color;
				col *= i.diff;
				return col;
			}
			ENDCG
		}
	}
}