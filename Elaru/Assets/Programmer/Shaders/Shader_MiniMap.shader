Shader "Unlit/Shader_MiniMap"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_PlayerIcon("Player Icon", 2D) = "white"{}
		_MainQuestTex("Main quest indicator", 2D) = "white"{}
		_SideQuestTex("Side quest indicator", 2D) = "white"{}
		_ArrowTex("Arrow texture", 2D) = "white"{}
		_Offset ("Offset", Vector) = (0,0,0,0)
		_PlayerIconOffset ("Player icon offset", Vector) = (0,0,0,0)
		_MainQuestOffset ("Main quest offset", Vector) = (0,0,0,0)
		_SideQuestOffset ("Side quest offset", Vector) = (0,0,0,0)
		_ArrowEdgeOffset ("Arrow offset from edge", Float) = .25
		_Scale ("Scale", Float) = 1.0
		_PlayerIconScale ("Player Icon Scale", Float) = 1.0
		_QuestScale ("Quest Icon Scale", Float) = 1.0
		_ArrowScale ("Arrow Icon Scale", Float) = 1.0
		_MainQuestActive("Main quest active", Int) = 0
		_SideQuestActive("Side quest active", Int) = 0
		_ShowPlayer("Show player on map", Int) = 1
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		LOD 100

		Blend SrcAlpha OneMinusSrcAlpha

		//Render pass for base map
		Pass
		{
			 CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			float2 _Offset;
			float _Scale;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				
				float2 uvc = TRANSFORM_TEX(v.uv, _MainTex);
				float2 center = float2(.5f, .5f);
				uvc = (uvc - center) / _Scale + center;
				uvc += _Offset - .5f;
                o.uv = uvc;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
		}
		
		//Render pass for icons
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
				float2 uvPlayer : TEXCOORD0;
                float2 uvMain : TEXCOORD1;
                float2 uvSide : TEXCOORD2;
                float2 uvArrow : TEXCOORD3;
                float2 uvOffArrow : TEXCOORD4;
                float4 vertex : SV_POSITION;
				float2 useArrow : COLOR;
            };
			
			sampler2D _PlayerIcon;
			sampler2D _MainQuestTex;
			sampler2D _SideQuestTex;
			sampler2D _ArrowTex;
            float4 _MainTex_ST;
			float2 _Offset;
			float2 _PlayerIconOffset;
			float2 _MainQuestOffset;
			float2 _SideQuestOffset;
			float _Scale;
			float _PlayerIconScale;
			float _QuestScale;
			int _MainQuestActive;
			int _SideQuestActive;
			float _ArrowScale;
			float _ArrowEdgeOffset;
			bool _ShowPlayer;

			inline float angleBetween(fixed4 colour, fixed4 original) 
			{
				return acos(dot(colour, original)/(length(colour)*length(original)));
			}
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.useArrow.r = 0; 
				o.useArrow.g = 0;

				float2 uvc = TRANSFORM_TEX(v.uv, _MainTex);
				float2 center = float2(.5f, .5f);

				//Set player icon on map
				uvc = (uvc - center) / _PlayerIconScale + center;
				uvc -= _PlayerIconOffset;
				o.uvPlayer = uvc;

				uvc = TRANSFORM_TEX(v.uv, _MainTex);

				//Main quest uv		
				uvc -= (_MainQuestOffset - _Offset) * _Scale;
				uvc = (uvc - center) / _QuestScale + center;
				
                o.uvMain = uvc;
				o.uvArrow = 0;

				//Side quest uv
				uvc = TRANSFORM_TEX(v.uv, _MainTex);
				
				uvc -= (_SideQuestOffset - _Offset) * _Scale;
				uvc = (uvc - center) / _QuestScale + center;
				
                o.uvSide = TRANSFORM_TEX(uvc, _MainTex);

				float angle = 0;
				float2 arrowOffset = float2(0,0);

				//Check for arrows if quest off map
				//=====MAIN QUEST ARROW=====
				float2 mainDiff = abs(_MainQuestOffset - _Offset);
				if (mainDiff.x > mainDiff.y)
				{
					//out on the right
					if (_MainQuestOffset.x > _Offset.x && _MainQuestOffset.x - _Offset.x > .5f / _Scale)
					{
						o.useArrow.r = 1;	
						arrowOffset.x = _ArrowEdgeOffset;
						arrowOffset.y = clamp((-_MainQuestOffset.y + _Offset.y) * _Scale, _ArrowEdgeOffset, -_ArrowEdgeOffset);
					}
					//out on the left
					else if (_MainQuestOffset.x < _Offset.x && _Offset.x - _MainQuestOffset.x > .5f / _Scale)
					{
						o.useArrow.r = 1;
						angle = 3.14f;
						arrowOffset.x = _ArrowEdgeOffset;					
						arrowOffset.y = clamp((_MainQuestOffset.y - _Offset.y) * _Scale, _ArrowEdgeOffset, -_ArrowEdgeOffset);
					}
				}
				//out up
				else
				{
					if (_MainQuestOffset.y > _Offset.y && _MainQuestOffset.y - _Offset.y > .5f / _Scale)
					{
						o.useArrow.r = 1;
						angle = 3.14f / 2.f;
						arrowOffset.x = _ArrowEdgeOffset;
						arrowOffset.y = clamp((_MainQuestOffset.x - _Offset.x) * _Scale, _ArrowEdgeOffset, -_ArrowEdgeOffset);
					}
					//out down
					else if (_MainQuestOffset.y < _Offset.y && _Offset.y - _MainQuestOffset.y > .5f / _Scale)
					{
						o.useArrow.r = 1;
						angle = 3.14f * 1.5f;
						arrowOffset.x = _ArrowEdgeOffset;
						arrowOffset.y = clamp((-_MainQuestOffset.x + _Offset.x) * _Scale, _ArrowEdgeOffset, -_ArrowEdgeOffset);
					}
				}

				if (o.useArrow.r > 0)
				{
					//Rotate arrow and decide position
					float sinX = sin (angle);
					float cosX = cos (angle);
					float2 centerUV = v.uv - .5f;
					float2x2 rotationMatrix = float2x2( cosX, -sinX, sinX, cosX);
					rotationMatrix *= .5f;
					rotationMatrix += .5f;
					rotationMatrix = rotationMatrix * 2 - 1;
					centerUV = mul(centerUV, rotationMatrix);
					centerUV += .5f;
					centerUV = (centerUV - center) / _ArrowScale + center;
					centerUV += arrowOffset / _ArrowScale;
					o.uvArrow = centerUV;
				}


				//=====SIDE QUEST ARROW=====
				float2 sideDiff = abs(_SideQuestOffset - _Offset);
				if (sideDiff.x > sideDiff.y)
				{
					//out on the right
					if (_SideQuestOffset.x > _Offset.x && _SideQuestOffset.x - _Offset.x > .5f / _Scale)
					{
						o.useArrow.g = 1;	
						arrowOffset.x = _ArrowEdgeOffset;
						arrowOffset.y = clamp((-_SideQuestOffset.y + _Offset.y) * _Scale, _ArrowEdgeOffset, -_ArrowEdgeOffset);
					}
					//out on the left
					else if (_SideQuestOffset.x < _Offset.x && _Offset.x - _SideQuestOffset.x > .5f / _Scale)
					{
						o.useArrow.g = 1;
						angle = 3.14f;
						arrowOffset.x = _ArrowEdgeOffset;					
						arrowOffset.y = clamp((_SideQuestOffset.y - _Offset.y) * _Scale, _ArrowEdgeOffset, -_ArrowEdgeOffset);
					}
				}
				//out up
				else
				{
					if (_SideQuestOffset.y > _Offset.y && _SideQuestOffset.y - _Offset.y > .5f / _Scale)
					{
						o.useArrow.g = 1;
						angle = 3.14f / 2.f;
						arrowOffset.x = _ArrowEdgeOffset;
						arrowOffset.y = clamp((_SideQuestOffset.x - _Offset.x) * _Scale, _ArrowEdgeOffset, -_ArrowEdgeOffset);
					}
					//out down
					else if (_SideQuestOffset.y < _Offset.y && _Offset.y - _SideQuestOffset.y > .5f / _Scale)
					{
						o.useArrow.g = 1;
						angle = 3.14f * 1.5f;
						arrowOffset.x = _ArrowEdgeOffset;
						arrowOffset.y = clamp((-_SideQuestOffset.x + _Offset.x) * _Scale, _ArrowEdgeOffset, -_ArrowEdgeOffset);
					}
				}

				if (o.useArrow.g > 0)
				{
					//Rotate arrow and decide position
					float sinX = sin (angle);
					float cosX = cos (angle);
					float2 centerUV = v.uv - .5f;
					float2x2 rotationMatrix = float2x2( cosX, -sinX, sinX, cosX);
					rotationMatrix *= .5f;
					rotationMatrix += .5f;
					rotationMatrix = rotationMatrix * 2 - 1;
					centerUV = mul(centerUV, rotationMatrix);
					centerUV += .5f;
					centerUV = (centerUV - center) / _ArrowScale + center;
					centerUV += arrowOffset / _ArrowScale;
					o.uvOffArrow = centerUV;
				}

                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
				//Sample player icon
				fixed4 playerCol = tex2D(_PlayerIcon, i.uvPlayer);

				//Sample main quest
                fixed4 mainCol = tex2D(_MainQuestTex, i.uvMain);
				mainCol *= _MainQuestActive;

				//Sample side quest
                fixed4 sideCol = tex2D(_SideQuestTex, i.uvSide);
				sideCol *= _SideQuestActive;

				//Compose final pixel coloer
				fixed4 final = _ShowPlayer ? playerCol : fixed4(0,0,0,0);
				if (final.a < .1)
					final = mainCol;
				if (final.a < .1f)
					final = sideCol;

				//Show arrows if quests are off map
				if (_MainQuestActive > 0 && i.useArrow.r > 0 && final.a < .1f)
				{
					fixed4 arrow = tex2D(_ArrowTex, i.uvArrow);
					final = arrow;
				}
				if (_SideQuestActive > 0 && i.useArrow.g > 0 && final.a < .1f)
				{
					fixed4 arrow = tex2D(_ArrowTex, i.uvOffArrow);
					final = arrow;
				}
                return final;
            }
			ENDCG
		}
	}
}
