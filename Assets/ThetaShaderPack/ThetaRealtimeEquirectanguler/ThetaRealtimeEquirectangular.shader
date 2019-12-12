// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Copyright (c) 2015 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php
Shader "Theta/RealtimeEquirectangular"
{
	Properties
	{
		[NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
		_Radius("Radius", Float) = 0.445 // Preset for Theta S
		_UVOffset("UVOffset(Forward UV / Backward UV)", Vector) = (0.0, 0.0, 0.0, 0.0)

		[KeywordEnum(Theta S, Theta)] _Mode("Mode", Int) = 0
		[KeywordEnum(Both, Forward, Backward)] _Draw("Draw", Int) = 0
	}

	SubShader
	{
		Tags{ "RenderType" = "Overlay" "Queue" = "Overlay" "ForceNoShadowCasting" = "True" }

		ZTest Always
		Cull Off
		ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _MODE_THETA_S _MODE_THETA
			#pragma multi_compile _DRAW_BOTH _DRAW_FORWARD _DRAW_BACKWARD

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
			float _Radius;
			float4 _UVOffset;

			// Supports 1280x720 or 1920x1080
			#define _THETA_S_Y_OFST		((720.0 - 640.0) / 720.0)
			#define _THETA_S_Y_SCALE	(640.0 / 720.0)

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			half4 frag(v2f i) : SV_Target
			{
				float2 revUV = i.uv;
				#if defined(_DRAW_BOTH)
				if (i.uv.x <= 0.5) {
					revUV.x = revUV.x * 2.0;
				} else {
					revUV.x = (revUV.x - 0.5) * 2.0;
				}
				#endif

				revUV *= UNITY_PI;

				float3 p = float3(cos(revUV.x), cos(revUV.y), sin(revUV.x));
				p.xz *= sqrt(1.0 - p.y * p.y);

				float r = 1.0 - asin(p.z) / (UNITY_PI / 2.0);
				float2 st = float2(p.y, p.x);

				st *= r / sqrt(1.0 - p.z * p.z);
				st *= _Radius;
				st += 0.5;

				#ifdef _DRAW_BACKWARD
				st.x *= 0.5;
				st.x += 0.5;
				st.y = 1.0 - st.y;
				st.xy += _UVOffset.wz;
				#elif _DRAW_FORWARD
				st.x = 1.0 - st.x;
				st.x *= 0.5;
				st.xy += _UVOffset.yx;
				#else
				if (i.uv.x <= 0.5) {
					st.x *= 0.5;
					st.x += 0.5;
					st.y = 1.0 - st.y;
					st.xy += _UVOffset.wz;
				} else {
					st.x = 1.0 - st.x;
					st.x *= 0.5;
					st.xy += _UVOffset.yx;
				}
				#endif

				#if defined(_MODE_THETA_S)
				st.y = st.y * _THETA_S_Y_SCALE + _THETA_S_Y_OFST;
				#endif

				#if defined(_DRAW_BOTH)
				#if !defined(SHADER_API_OPENGL)
				half4 col = tex2Dlod(_MainTex, float4(st,0.0,0.0));
				#else // Memo: OpenGL not supported tex2Dlod.( Texture should be setting to generateMipMap = off. )
				half4 col = tex2D(_MainTex, st);
				#endif
				#else
				half4 col = tex2D(_MainTex, st);
				#endif
				return col;
			}
			ENDCG
		}
	}
}
