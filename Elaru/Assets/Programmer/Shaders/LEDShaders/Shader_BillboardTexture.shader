Shader "Custom/Shader_BillboardTexture" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		[MaterialToggle] _UseEmmissive("Use Emmisive", Int) = 0
		_EmmisiveColor ("Emmisive Color", Color) = (1,1,1,1)
		_EmmissiveStrength ("Emmisive Strength", Float) = 1
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_MainTexPanSpeed("Texture Pan Speed", Vector) = (0,0,0,0)
		_LedTex ("LED texture", 2D) = "white" {}
		_PanSpeed("Flicker Pan Speed", Vector) = (0, 1, 0, 0)
		_PanWidth("Flicker Pan Width", Vector) = (0.5, 0.5, 0, 0)
		_PanIntensity("Flicker Pan Intensity", Vector) = (1,1,0,0)
		_PanTimeOffset("Flicker Pan Time Offset", Float) = 0.0
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_LedSize ("Led Size", Float) = 1.0	
		_TexCutOffValue ("Individual LED size", Range(0.0, 1.0)) = 0.1
	}

	SubShader {
		Tags { "Queue" = "Transparent" }
		LOD 200
		//Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _LedTex;
		sampler2D _PanTex;

		struct Input 
		{
			float2 uv_MainTex;
			float3 worldPos;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		bool _UseEmmissive;
		fixed4 _EmmisiveColor;
		float _EmmissiveStrength;
		float _LedSize;
		float2 _PanSpeed;
		float2 _PanWidth;
		float2 _PanIntensity;
		float _TexCutOffValue;
		float2 _MainTexPanSpeed;
		float _PanTimeOffset;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			//Pan main uv coordinates
			float2 panUV = IN.uv_MainTex;
			panUV.x -= frac((_Time + _PanTimeOffset) * _MainTexPanSpeed.x);
			panUV.y -= frac((_Time + _PanTimeOffset) * _MainTexPanSpeed.y);

			//Sample main texture
			fixed4 c = tex2D (_MainTex, panUV) * _Color;

			float ledSample = tex2D(_LedTex, IN.uv_MainTex * 1.f / _LedSize).r;
			ledSample = ledSample >= 1.f - _TexCutOffValue ? 1.f : 0.f;

			c *= ledSample;
			
			//Create frequency effect
			float gradient = 0;
			float flickerCenter = 0;
			float uvDiff = 0;
			float3 wp = mul((float3x3)unity_WorldToObject, float3(1,1,1));
			if (abs(_PanSpeed.x) > 0.f)
			{
				//Define current flicker center
				flickerCenter = frac((_Time + _PanTimeOffset + (wp.y)) * _PanSpeed.x);
				uvDiff = flickerCenter - IN.uv_MainTex.x;
				//Check for overflow
				uvDiff *= flickerCenter - 1 - IN.uv_MainTex.x;
				uvDiff *= flickerCenter + 1 - IN.uv_MainTex.x;
				//Adjust to width and intensity
				gradient += pow(1 - abs(uvDiff), 1.f / _PanWidth.x) * _PanIntensity.x;
			}
			if (abs(_PanSpeed.y) > 0.f)
			{
				//Define current flicker center
				flickerCenter = frac((_Time + +_PanTimeOffset) * _PanSpeed.y);
				uvDiff = flickerCenter - IN.uv_MainTex.y;
				//Check for overflow
				uvDiff *= flickerCenter - 1 - IN.uv_MainTex.y;
				uvDiff *= flickerCenter + 1 - IN.uv_MainTex.y;
				//Adjust to width and intensity
				gradient += pow(1 - abs(uvDiff), 1.f / _PanWidth.y) * _PanIntensity.y;
			}
			c += c * gradient * ledSample;

			//Create final color
			o.Albedo = c;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = 1;
			if (_UseEmmissive)
				o.Emission = c * _EmmisiveColor * _EmmissiveStrength;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
