Shader "Custom/Shield"
{
	Properties
	{
		_Color("Focefield Main Color", Color) = (0,0,0,0)
		_HexTex("Hexagon Shader", 2D) = "white" {}
		_EdgeFactor("EdgeFactor", float) = 0.5
		_PulseSpeed("Pulse speed", float) = 50.0
		_ScrollSpeed("Scroll speed", float) = 50.0
		_BaseHexStrength("Base Alpha Strenght", float) = .2
		_BaseHexPulseSpeed("Base Alpha Pulse Speed", float) = 100
		_BaseHexPulseIntensity("Base Alpha Pulse Intensity", float) = 1.0
		_ScrollHexStrenght("Scrolling Alpha Strenght", float) = 5.0
		_HexTurnSpeed("Alpha Turn Speed", float) = 5.0
		_OpenValue("OpenValue", float) = 0.0
		_ShieldTime("Shield Time", float) = 0.0
		_FadeValue("Fade Value", float) = 1.0
	}
		SubShader
	{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent"}
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
		float3 objectPos :TEXCOORD1;
	};

	float4 _Color;
	float _EdgeFactor;
	float _PulseSpeed;
	sampler2D _HexTex;
	float4 _HexTex_ST;
	float _ScrollSpeed;
	float _BaseHexStrength;
	float _ScrollHexStrenght;
	float _BaseHexPulseIntensity;
	float _HexTurnSpeed;
	float _BaseHexPulseSpeed;
	float _OpenValue;
	float _ShieldTime;
	float _FadeValue;

	v2f vert(appdata v)
	{
		v2f o;
		o.wVertex = UnityObjectToClipPos(v.vertex);
		o.uv = TRANSFORM_TEX(v.uv, _HexTex);
		o.color = v.color;
		o.objectPos = v.vertex.xyz;
		_OpenValue = 0;
		return o;
	}

	sampler2D _CameraDepthTexture;
	
	float triWave(float t, float offset, float yOffset)
	{
		return saturate(abs(frac(offset + t) * 2 - 1) + yOffset);
	}

	fixed4 frag(v2f i) : SV_Target
	{
		float2 screenPos = i.wVertex.xy;
		screenPos /= _ScreenParams.xy;

		float camDepth = LinearEyeDepth(tex2D(_CameraDepthTexture, screenPos).r);
		float ownDepth = LinearEyeDepth(i.wVertex.z);

		float depthDelta = camDepth - ownDepth;
		depthDelta = abs(depthDelta);
		depthDelta *= _EdgeFactor + (sin(_ShieldTime * _PulseSpeed) + 1) / 2.f;
		depthDelta = saturate(depthDelta);
		depthDelta = 1 - depthDelta;
		//depthDelta *= clamp(_OpenValue - 1.f, 0.0, 1.0);
		float northPole = (i.objectPos.y - .5f) / 2.f;
		float glow = max(depthDelta, northPole);
		fixed4 c = glow *(_Color + depthDelta / 5.f);

		float2 hexUv = i.uv;
		i.uv.x += _ShieldTime * _HexTurnSpeed;
		fixed4 hexes = tex2D(_HexTex, i.uv) * _Color;
		float baseHexSin = (sin(_ShieldTime * _BaseHexPulseSpeed) + 1) / 2.f;
		float bhs = _BaseHexStrength + _BaseHexStrength * baseHexSin * _BaseHexPulseIntensity;
		hexes *= (triWave(_ShieldTime.x * _ScrollSpeed, abs(i.objectPos.y), -0.7) * _ScrollHexStrenght) + bhs;
		fixed4 col = _Color * _Color.a + c + hexes;
		/*if (_OpenValue <= 1.0)
		{
			float spread = i.objectPos.y + -1 + _OpenValue;
			col *= clamp(spread, 0.0, 1.0);
		}*/
		return col * _FadeValue;
	}
		ENDCG
	}
	}
}
