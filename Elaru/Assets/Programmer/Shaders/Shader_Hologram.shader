Shader "Custom/Shader_Hologram"
{
	Properties
	{
		//Fresnel
		_Fresnel("Fresnel", Color) = (1,1,1,1)
		//Glow (de tiling glow over de mesh)
		_GlowTiling("Glow Tiling", Range(0.01, 1.0)) = 0.05
		_GlowSpeed("Glow Speed", Range(-10.0, 10.0)) = 1.0
		//Glitch (das die random movement van de "Mesh")
		_GlitchSpeed("Glitch Speed", Range(0, 50)) = 1.0
		_GlitchIntensity("Glitch Intensity", Float) = 0
		//Flicker
		_FlickerTex("Flicker Texture", 2D) = "white" {}
		_FlickerSpeed("Flicker Speed", Range(0.01, 100)) = 1.0

		_BaseAlpha("Base alpha", Range(0,1)) = 0
	}
		SubShader
		{
			Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
			Blend SrcAlpha OneMinusSrcAlpha
			LOD 100
			ColorMask RGB
			Cull Back

			Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			
			//dit is voor de UnityObjectToWorldNormal
			#include "UnityCG.cginc"


			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 worldVertex : TEXCOORD1;
				float3 viewDir : TEXCOORD2;
				float3 worldNormal : NORMAL;
			};

			sampler2D _FlickerTex;
			float4 _MainColor;
			float4 _Fresnel;
			float _GlitchSpeed;
			float _GlitchIntensity;
			float _GlowTiling;
			float _GlowSpeed;
			float _FlickerSpeed;
			float _BaseAlpha;

			v2f vert(appdata v)
			{
				//vertex
				v2f output;

				//Glitch van de mesh
				v.vertex.x += _GlitchIntensity * (step(0.5, sin(_Time.y * 2.0 + v.vertex.y * 1.0)) * step(0.99, sin(_Time.y*_GlitchSpeed * 0.5)));
				output.vertex = UnityObjectToClipPos(v.vertex);

				//Set de overige vertex
				output.worldVertex = mul(unity_ObjectToWorld, v.vertex);
				output.worldNormal = UnityObjectToWorldNormal(v.normal);
				output.viewDir = normalize(UnityWorldSpaceViewDir(output.worldVertex.xyz));

				//Return vertex
				return output;
			}


			fixed4 frag(v2f i) : SV_Target
			{
				//Calculate gow
				float glow = frac(i.worldVertex.y * _GlowTiling - _Time.x * _GlowSpeed);
				//Calculate flicker
				fixed4 flicker = tex2D(_FlickerTex, _Time * _FlickerSpeed);
				//Rim
				half rim = 1.0 - saturate(dot(i.viewDir, i.worldNormal));
				//Fresnel
				fixed4 col = (glow*0.35) + _Fresnel;
				col.a = clamp((rim /*+ glow*/) * flicker * _Fresnel.a, _BaseAlpha, 1);

				//Return color
				return col;
			}
			ENDCG
		}
	}
}