Shader "Custom/Shader_ClothNoAlpha" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap("Bump map texture", 2D) = "white" {}
		_BumpMapStrength("Bump map strength", Float) = 0
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_Amplitude("Amplitude", Float) = 1.0
		_WaveSpeed("Cloth wave speed", Float) = 40.0
		_TimeOffset("Wave generation time offset", Float) = 0.0
		[MaterialToggle] _KeepCorners("Keep Corners", Int) = 0
		_CornerDistanceX("Corner distance X", Float) = 10.0
		_CornerDistanceY("Corner distance Y", Float) = 10.0
	}
	SubShader {
		Tags { "Queue" = "Transparent" }
		LOD 200
		Cull Off
		//ZWrite On
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows  vertex:vert
			
		struct Input 
		{
			float2 uv_MainTex;
			float2 uv_BumpMap;
		};

		sampler2D _MainTex;
		sampler2D _BumpMap;
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		float _Amplitude;
		float _WaveSpeed;
		float _BumpMapStrength;
		float _CornerDistanceX;
		float _CornerDistanceY;
		bool _KeepCorners;

		 #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			float _TimeOffset;
		UNITY_INSTANCING_BUFFER_END(Props)

		void vert(inout appdata_full v) 
		{
			// Do whatever you want with the "vertex" property of v here
			float3 worldPos = mul((float3x3)unity_ObjectToWorld, float4(0,0,0,1));

			float offset = abs(.5 - v.vertex.x) * v.vertex.y;
			float timeGen = _Time + _TimeOffset;

			float tx = (sin(_Time * 20) + 1) / 2;
			float ty = (sin(_Time * 10 + 15) + 1) / 2;
			float change = sin(abs(.5 - v.vertex.x - tx) * (v.vertex.y + ty)) * sin(timeGen * _WaveSpeed);
			if (_KeepCorners)
			{
				float l = clamp(abs( sqrt((_CornerDistanceX * _CornerDistanceX) + (_CornerDistanceY * _CornerDistanceY)) - length(v.vertex.xy)), 0, 1);
				change *= l;
			}
			v.vertex.z += change * _Amplitude;
		}

		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			// Albedo comes from a texture tinted by color
			float3 c = tex2D (_MainTex, IN.uv_MainTex).rgb * _Color.rgb;
			fixed4 ac = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			if (abs(_BumpMapStrength) > 0.f)
			{
				float3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
				normal.rg *= _BumpMapStrength;
				o.Normal = normal;
			}
		}
		ENDCG
	}
	FallBack "Diffuse"
}
