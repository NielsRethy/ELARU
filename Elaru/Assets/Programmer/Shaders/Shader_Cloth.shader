Shader "Custom/Shader_Cloth" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_AlphaTex ("Alpha texture", 2D) = "white" {}
		[MaterialToggle] _UseAlphaTexAlphaChannel("Alpha Is In Alpha Channel", Int) = 1
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
		Tags { "Queue" = "Transparent+1" "RenderType" = "Transparent" }
		LOD 200
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows alpha vertex:vert
			
		struct Input 
		{
			float2 uv_MainTex;
			float2 uv_AlphaTex;
			float2 uv_BumpMap;
		};

		sampler2D _MainTex;
		sampler2D _AlphaTex;
		sampler2D _BumpMap;
		bool _UseAlphaTexAlphaChannel;
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		float _Amplitude;
		float _WaveSpeed;
		float _TimeOffset;
		float _BumpMapStrength;
		float _CornerDistanceX;
		float _CornerDistanceY;
		bool _KeepCorners;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void vert(inout appdata_full v) 
		{
			//Get wave time
			float timeGen = _Time.x + _TimeOffset;

			//Calculate some random offset
			float tx = (sin(_Time.x * 20) + 1) / 2;
			float ty = (sin(_Time.x * 10 + 15) + 1) / 2;
			float change = sin(abs(.5 - v.vertex.x - tx) * (v.vertex.y + ty)) * sin(timeGen * _WaveSpeed);

			if (_KeepCorners)
			{
				//Multiply offset by 1 - distance from middlepoint
				float l = clamp(abs( sqrt((_CornerDistanceX * _CornerDistanceX) + (_CornerDistanceY * _CornerDistanceY)) - length(v.vertex.xy)), 0, 1);
				change *= l;
			}

			//Move vertex
			v.vertex.z += change * _Amplitude;
		}

		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			//Sample texture
			float3 c = tex2D (_MainTex, IN.uv_MainTex).rgb * _Color.rgb;
			fixed4 ac = tex2D(_MainTex, IN.uv_MainTex);

			//Set material properties
			o.Albedo = c;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = (_UseAlphaTexAlphaChannel ? ac.a : ac.r) * _Color.a;

			//Apply normal detail if necessary
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
