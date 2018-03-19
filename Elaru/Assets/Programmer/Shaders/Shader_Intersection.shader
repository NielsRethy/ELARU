Shader "Custom/Intersection"
{
	Properties
	{
		_Color("Focefield Main Color", Color) = (0,0,0,0)
		_EdgeFactor("EdgeFactor", float) = 0.5
		_PulseSpeed("Pulse speed", float) = 50.0
	}
		SubShader
	{
		Tags{ "RenderType" = "Transparent" }
		LOD 100
		ZWrite Off
		Blend One One
		Cull Off

		Pass
	{
		CGPROGRAM
		#pragma target 4.0
		#pragma vertex vert
		#pragma fragment frag
		// make fog work
		#pragma multi_compile_fog

		#include "UnityCG.cginc"

	struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
		float4 normal : NORMAL;
		float4 color : COLOR;
	};

	struct v2f
	{
		float2 uv : TEXCOORD0;
		float4 wVertex : SV_POSITION;
		float4 color : COLOR;
	};

	float4 _Color;
	float _EdgeFactor;
	float _PulseSpeed;

	v2f vert(appdata v)
	{
		v2f o;
		o.wVertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		o.color = v.color;
		return o;
	}

	sampler2D _CameraDepthTexture;

	fixed4 frag(v2f i) : SV_Target
	{
		float2 screenPos = i.wVertex.xy;
		screenPos /= _ScreenParams.xy;

		float camDepth = LinearEyeDepth(tex2D(_CameraDepthTexture, screenPos).r);
		float ownDepth = LinearEyeDepth(i.wVertex.z);

		float depthDelta = camDepth - ownDepth;
		depthDelta = abs(depthDelta);
		depthDelta *= _EdgeFactor + (sin(_Time * _PulseSpeed) + 1) / 2.f;;
		depthDelta = saturate(depthDelta);
		depthDelta = 1 - depthDelta;
		fixed4 c = depthDelta * (_Color + depthDelta / 5.f);
		return c;
	}
		ENDCG
	}
	}
}
