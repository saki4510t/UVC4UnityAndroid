// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Copyright (c) 2015 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php
Shader "360Camera/RealtimeEquirectangular"
{
	Properties
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[NoScaleOffset] _FrontTex ("Front [+Z]", 2D) = "grey" {}
		[NoScaleOffset] _BackTex ("Back [-Z]", 2D) = "grey" {}
		[NoScaleOffset] _LeftTex ("Left [+X]", 2D) = "grey" {}
		[NoScaleOffset] _RightTex ("Right [-X]", 2D) = "grey" {}
		[NoScaleOffset] _UpTex ("Up [+Y]", 2D) = "grey" {}
		[NoScaleOffset] _DownTex ("Down [-Y]", 2D) = "grey" {}
	}
	
	SubShader
	{
		Tags{ "RenderType" = "Overlay" "Queue" = "Overlay+1" "ForceNoShadowCasting" = "True" }

		ZTest Always
		Cull Off
		ZWrite Off

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

			float4 _Color;
			float _Cutoff;
			sampler2D _FrontTex;
			sampler2D _BackTex;
			sampler2D _LeftTex;
			sampler2D _RightTex;
			sampler2D _UpTex;
			sampler2D _DownTex;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			#define _SIN45 (0.70710678118)
			#define _DEG2RAD (UNITY_PI / 180.0)
			
			//#define _COL_DEBUG

			inline float2 _ToUV( float2 st )
			{
				st += 0.5;
				st.y = 1.0 - st.y; // RenderTexture.
				return st;
			}
			
			half4 frag (v2f i) : SV_Target
			{
				float2 revUV = i.uv;
				revUV *= float2(2.0 * UNITY_PI, UNITY_PI);
				
				float3 p = float3(cos(revUV.x), cos(revUV.y), sin(revUV.x));
				p.xz *= sqrt(1.0 - p.y * p.y);

				half4 col;

				float2 vec_yx = normalize(float2(p.y, p.x));
				float2 vec_yz = normalize(float2(p.y, p.z));
				
				if( vec_yx.x >= _SIN45 && vec_yz.x >= _SIN45 ) {
					float2 st = float2(p.z, -p.x) / (abs(p.y) * 2.0);
					col = tex2D(_UpTex, _ToUV(st));
					#ifdef _COL_DEBUG
					col = fixed4( 0.0, 1.0, 0.0, 1.0 );
					#endif
				} else if( vec_yx.x <= -_SIN45 && vec_yz.x <= -_SIN45 ) {
					float2 st = float2(p.z, p.x) / (abs(p.y) * 2.0);
					col = tex2D(_DownTex, _ToUV(st));
					#ifdef _COL_DEBUG
					col = fixed4( 1.0, 0.0, 0.0, 1.0 );
					#endif
				} else {
					float2 vec_zx = normalize( float2( p.z, p.x ) );
					if( vec_zx.y >= _SIN45 ) { // Back
						float2 st = float2(-p.z, -p.y) / (abs(p.x) * 2.0);
						col = tex2D(_BackTex, _ToUV(st));
						#ifdef _COL_DEBUG
						col = fixed4( 1.0, 1.0, 0.0, 1.0 );
						#endif
					} else if( vec_zx.y <= -_SIN45 ) { // Front
						float2 st = float2(p.z, -p.y) / (abs(p.x) * 2.0);
						col = tex2D(_FrontTex, _ToUV(st));
						#ifdef _COL_DEBUG
						col = fixed4( 0.0, 1.0, 1.0, 1.0 );
						#endif
					} else if( vec_zx.x >= 0.0 ) { // Right
						float2 st = float2(p.x, -p.y) / (abs(p.z) * 2.0);
						col = tex2D(_RightTex, _ToUV(st));
						#ifdef _COL_DEBUG
						col = fixed4( 0.25, 0.25, 0.25, 1.0 );
						#endif
					} else { // Left
						float2 st = float2(-p.x, -p.y) / (abs(p.z) * 2.0);
						col = tex2D(_LeftTex, _ToUV(st));
						#ifdef _COL_DEBUG
						col = fixed4( 0.75, 0.75, 0.75, 1.0 );
						#endif
					}
				}
				
				col *= _Color;
				clip( col.a - _Cutoff );
				return col;
			}
			ENDCG
		}
	}
}
