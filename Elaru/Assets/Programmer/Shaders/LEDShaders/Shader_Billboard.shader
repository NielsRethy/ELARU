Shader "Custom/Shader_Billboard" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		[MaterialToggle] _UseEmmissive("Use Emmisive", Float) = 0
		_EmmisiveColor ("Emmisive Color", Color) = (1,1,1,1)
		_EmmissiveStrength ("Emmisive Strength", Float) = 1
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_AlphaTex ("Alpha map", 2D) = "white" {}
		_PanTex ("Pan texture", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_LedSize ("Led Size", Float) = 1.0
		_LedCountHorizontal ("Led Count horizontal", Int) = 10
		_LedCountVertical ("Led Count vertical", Int) = 10		
		_PanSpeed ("Pan Speed", Float) = 1.0
	}
	SubShader {
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 200
		Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows alpha

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _AlphaTex;
		sampler2D _PanTex;

		struct Input 
		{
			float2 uv_MainTex;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		float _UseEmmissive;
		fixed4 _EmmisiveColor;
		float _EmmissiveStrength;
		float _LedSize;
		int _LedCountHorizontal;
		int _LedCountVertical;
		float _PanSpeed;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			//Create LED effect
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			fixed4 alphaSample = tex2D(_AlphaTex, IN.uv_MainTex);
			if (alphaSample.r < 1e-5)
				discard;
			float2 centerUV = IN.uv_MainTex ;
			float2 uv = centerUV;
			uv.x -= fmod(uv.x, 1.f / _LedCountHorizontal);
			uv.y -= fmod(uv.y, 1.f / _LedCountVertical);

			uv.x += .5 / _LedCountHorizontal;
			uv.y += .5 / _LedCountVertical;

			//Return black when not on LED
			if (length(IN.uv_MainTex - uv) > _LedSize)
				c = 0;
			
			//Create frequency effect
			if (abs(_PanSpeed) > 0.f)
			{
				float2 panUV = IN.uv_MainTex;
				panUV.y = IN.uv_MainTex.y - frac(_Time * _PanSpeed);
				float4 panColor = tex2D (_PanTex, panUV);
				c += c * panColor;
			}

			o.Albedo = c;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = alphaSample.r;
			if (_UseEmmissive)
			{
				o.Emission = c * _EmmisiveColor * _EmmissiveStrength * alphaSample.r;
			}
		}
		ENDCG
	}
	FallBack "Diffuse"
}
