Shader "Custom/Shader_Sword" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_EdgeColor("Edge Color", Color) = (1,1,1,1)
		_EmissionTex("Emmision texture", 2D) = "white" {}
		_HexTex("Hex texture", 2D) = "white" {}
		_HexPanSpeed("Hex Pan Speed", Vector) = (0,0,0,0)
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_Bias("Bias", Range(0,1)) = 0.0
		_Scale("Scale", Range(0,1)) = 0.0
		_Power("Power", Range(0,10)) = 0.0
	}
		SubShader{
		Pass{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
		CGPROGRAM

			// Pragmas
			#include "UnityCG.cginc"
			#pragma vertex vert
			#pragma fragment frag

			// User Defined Variables
			sampler2D _HexTex;
		uniform float4 _EdgeColor;
		uniform float4 _Color;
		float _Bias;
		float _Scale;
		float _Power;
		float2 _HexPanSpeed;

		// Base Input Structs
		struct vertexInput
		{
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float2 uv : TEXCOORD0;
		};
		struct vertexOutput
		{
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			float R : TEXCOORD1;
		};

		// Vertex Function
		vertexOutput vert(vertexInput v)
		{
			vertexOutput o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = v.uv;
			//o.uv.x -= frac(_Time * _HexPanSpeed.x);
			//o.uv.y -= frac(_Time * _HexPanSpeed.y);

			float3 posWorld = mul(unity_ObjectToWorld, v.vertex).xyz;
			float3 normWorld = mul(unity_ObjectToWorld, float4(v.normal, 0.0)).xyz;;

			float3 I = normalize(WorldSpaceViewDir(v.vertex));
			o.R = _Bias + _Scale * pow(1.0 + dot(I, normWorld), _Power);

			return o;
		}

		// Fragment Function
		float4 frag(vertexOutput i) : SV_Target
		{
			float4 baseCol = tex2D(_HexTex, i.uv).r * _Color * i.R;
			float4 edgeCol = _EdgeColor * (1 - i.R);
			return baseCol + edgeCol;
		}
			ENDCG
		}

		}
			FallBack "Diffuse"
}
