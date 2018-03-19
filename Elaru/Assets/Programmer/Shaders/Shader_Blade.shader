Shader "Custom/Shader_Blade" {
	Properties {
		_Color("Color", Color) = (1,1,1,1)
		_EdgeColor("Edge Color", Color) = (1,1,1,1)
		_EmissionTex("Emmision texture", 2D) = "white" {}
		_HexTex("Hex texture", 2D) = "white" {}
		[MaterialToggle] _InvertHex("Invert hex texture", Int) = 1
		_HexPanSpeed("Hex Pan Speed", Vector) = (0,0,0,0)
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_FlickerWidth("Flicker width", Float) = 5.0
		_FlickerSpeed("Flicker speed", Float) = 10
		_FlickerIntensity("Flicker intensity", Float) = 10
		_TranslucencyValue("Blade translucency", Range(0,1)) = 1.0
		[MaterialToggle] _InvertBladePanTwoSided("Invert flicker pan on one side", Int) = 1
	}
		SubShader{
			Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
			LOD 200
			Blend One One
			CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types
			#pragma surface surf Standard fullforwardshadows alpha

			// Use shader model 3.0 target, to get nicer looking lighting
			#pragma target 3.0

			sampler2D _EmissionTex;
		sampler2D _HexTex;

		struct Input {
			float2 uv_HexTex;
			float2 uv_EmissionTex;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		fixed4 _EdgeColor;
		float2 _HexPanSpeed;
		float _FlickerWidth;
		float _FlickerSpeed;
		float _FlickerIntensity;
		float _TranslucencyValue;
		bool _InvertHex;
		bool _InvertBladePanTwoSided;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			float2 hexUV = IN.uv_HexTex;
			hexUV.x -= frac(_Time * _HexPanSpeed.x);
			hexUV.y -= frac(_Time * _HexPanSpeed.y);
			float hexSample = tex2D(_HexTex, hexUV).r;
			if (_InvertHex)
				hexSample = 1 - hexSample;
			hexSample *= _Color.a;
			float4 hexColor = hexSample * _Color;

			float emissionSample = tex2D(_EmissionTex, IN.uv_EmissionTex).r;
			float4 emissionColor = emissionSample * _EdgeColor;
			hexColor *= 1 - emissionSample;

			float fSpeed = -_FlickerSpeed;
			if (_InvertBladePanTwoSided && IN.uv_EmissionTex.y > 0.07)
				fSpeed = -fSpeed;

			float flickerCenter = frac(_Time * fSpeed);
			float uvDiff = flickerCenter - IN.uv_EmissionTex.x;
			uvDiff *= flickerCenter - 1 - IN.uv_EmissionTex.x;
			uvDiff *= flickerCenter + 1 - IN.uv_EmissionTex.x;
			float gradient = pow(1 - abs(uvDiff), 1.f / _FlickerWidth);
			float4 flickerColor = (hexColor * gradient * _FlickerIntensity) * 1 - emissionSample;

			o.Albedo = hexColor + flickerColor;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Emission = emissionColor;
			o.Alpha = clamp(emissionSample + _TranslucencyValue, 0, 1);
		}
		ENDCG
	}
	FallBack "Diffuse"
}
