// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Copyright (c) 2015 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php
Shader "Theta/RealtimeEquirectangular1080p"
{
	Properties
	{
		[NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
		_UVOffset("UVOffset(Forward UV / Backward UV)", Vector) = (0.0, 0.0, 0.0, 0.0)

		[KeywordEnum(Theta S 1080p, Theta S, Theta, Insta360 Air)] _Mode("Mode", Int) = 0
	}

	SubShader
	{
		Tags{ "RenderType" = "Overlay" "Queue" = "Overlay" "ForceNoShadowCasting" = "True" }
//		Tags{ "RenderType" = "Opaque" "Queue" = "Geometry" "ForceNoShadowCasting" = "True" }

		ZTest Always
		Cull Off
		ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _MODE_THETA_S_1080P  _MODE_THETA_S _MODE_THETA _MODE_INSTA360_AIR

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
			float4 _UVOffset;

			#define ALPHA_RANGE 0.001
			#if defined(_MODE_THETA_S_1080P)
				#define _RADIUS 0.445
				#define _TEXTURE_Y_OFFSET 0
				#define _TEXTURE_Y_SCALE (640.0 / 720.0)
				#define _FORWARD_ROTATION_DEGREE 0
				#define _BACKWARD_ROTATION_DEGREE 0
			#elif defined(_MODE_THETA_S)
				#define _RADIUS 0.445
				#define _TEXTURE_Y_OFFSET ((720.0 - 640.0) / 720.0)
				#define _TEXTURE_Y_SCALE (640.0 / 720.0)
				#define _FORWARD_ROTATION_DEGREE 0
				#define _BACKWARD_ROTATION_DEGREE 0
			#elif defined(_MODE_INSTA360_AIR)
				#define _INSTA360_AIR_SENSOR_ROTATION_DEGREE  4.0
				#define _RADIUS 0.47
				#define _TEXTURE_Y_OFFSET 0
				#define _TEXTURE_Y_SCALE 1
				#define _FORWARD_ROTATION_DEGREE (-90 + _INSTA360_AIR_SENSOR_ROTATION_DEGREE)
				#define _BACKWARD_ROTATION_DEGREE (90 + _INSTA360_AIR_SENSOR_ROTATION_DEGREE)
			#else
				#define _RADIUS 0.445
				#define _TEXTURE_Y_OFFSET 0
				#define _TEXTURE_Y_SCALE 1
				#define _FORWARD_ROTATION_DEGREE 0
				#define _BACKWARD_ROTATION_DEGREE 0
			#endif

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			// ２次元変換行列 （３次元目は平行移動用に使う）
			float3x3 rotate_matrix_radian(float rot) {
				float sinX = sin(rot);
				float cosX = cos(rot);
				return float3x3(cosX, -sinX, 0, sinX, cosX, 0, 0, 0, 1);
			}

			// Scale/Rotate/Translate Matrix群
			float3x3 rotate_matrix_degree(float rot) {
				return rotate_matrix_radian(rot * UNITY_PI / 180.0);
			}
			float3x3 scale_matrix(float2 scale) {
				return float3x3(scale.x, 0, 0, 0, scale.y, 0, 0, 0, 1);
			}
			float3x3 scaleX_matrix(float x) {
				return float3x3(x, 0, 0, 0, 1, 0, 0, 0, 1);
			}
			float3x3 scaleY_matrix(float y) {
				return float3x3(1, 0, 0, 0, y, 0, 0, 0, 1);
			}
			float3x3 translate_matrix(float2 vec) {
				return float3x3(1, 0, 0, 0, 1, 0, vec.x, vec.y, 1);
			}
			float3x3 translateX_matrix(float x) {
				return float3x3(1, 0, 0, 0, 1, 0, x, 0, 1);
			}
			float3x3 translateY_matrix(float y) {
				return float3x3(1, 0, 0, 0, 1, 0, 0, y, 1);
			}

			float3x3 texture_matrix3() {
				float3x3 mat = float3x3(1, 0, 0, 0, 1, 0, 0, 0, 1);
				mat = mul(mat, scaleY_matrix(_TEXTURE_Y_SCALE));
				mat = mul(mat, translateY_matrix(_TEXTURE_Y_OFFSET));
				return mat;
			}

			// forward用変換行列
			// (0,0)を中心として半径1の範囲の座標、をテクスチャ座標に変換するmatrix
			// 計算量多いようだが、すべてコンパイル時に解決されるはず。
			float3x3 forward_matrix3() {
				float3x3 mat = float3x3(1, 0, 0, 0, 1, 0, 0, 0, 1);
				mat = mul(mat, rotate_matrix_degree(_FORWARD_ROTATION_DEGREE));
				mat = mul(mat, translate_matrix(float2(0.5, 0.5)));
				// 裏側なので逆方向にする
				mat = mul(mat, scaleX_matrix(-1));
				mat = mul(mat, translateX_matrix(1));
				// XのUV幅は半分なので x0.5
				mat = mul(mat, scaleX_matrix(0.5));
				// オフセット
				mat = mul(mat, translate_matrix(_UVOffset.yx));
				mat = mul(mat, texture_matrix3());
				return mat;
			}

			// backward用変換行列
			// (0,0)を中心として半径1の範囲の座標、をテクスチャ座標に変換するmatrix
			// 計算量多いようだが、すべてコンパイル時に解決されるはず。
			float3x3 backward_matrix3() {
				float3x3 mat = float3x3(1, 0, 0, 0, 1, 0, 0, 0, 1);
				mat = mul(mat, rotate_matrix_degree(_BACKWARD_ROTATION_DEGREE));
				mat = mul(mat, translate_matrix(float2(0.5, 0.5)));
				// 片目分のUV幅は半分なので x0.5
				mat = mul(mat, scaleX_matrix(0.5));
				// 右側なので +0.5
				mat = mul(mat, translateX_matrix(0.5));
				// Y逆方向
				mat = mul(mat, scaleY_matrix(-1));
				mat = mul(mat, translateY_matrix(1));
				// オフセット
				mat = mul(mat, translate_matrix(_UVOffset.wz));
				mat = mul(mat, texture_matrix3());
				return mat;
			}

			float2 convert_for_forward(float2 st) {
				return mul(float3(st.x, st.y, 1), forward_matrix3()).xy;
			}

			float2 convert_for_backward(float2 st) {
				return mul(float3(st.x, st.y, 1), backward_matrix3()).xy;
			}

			float4 frag(v2f i) : SV_Target
			{
//				float2 revUV = i.uv;
				float2 revUV = float2(i.uv.x, 1.0 - i.uv.y);	// THETAの画像そのままだと上下が入れ替わってしまうので対策
				#if defined(_DRAW_BOTH)
				if (i.uv.x <= 0.5) {
					revUV.x = 1.0 - revUV.x * 2.0;
				}
				else {
					revUV.x = 1.0 - (revUV.x - 0.5) * 2.0;
				}
				#endif

				revUV *= UNITY_PI;

				float3 p = float3(cos(revUV.x), cos(revUV.y), sin(revUV.x));
				p.xz *= sqrt(1.0 - p.y * p.y);

				float r = 1.0 - asin(p.z) / (UNITY_PI / 2.0);
				float2 st = float2(p.y, p.x);

				st *= r / sqrt(1.0 - p.z * p.z);
				st *= _RADIUS;

				// 前後のテクスチャの混合比
				float x = i.uv.x;
				float blend = 1.0;
				if ((x < 0.5 - ALPHA_RANGE) || (x > 0.5 + ALPHA_RANGE)) {
					blend = 0.0;
				}
				else if (x < 0.5 + ALPHA_RANGE) {
					blend = (x - 0.5) / (2.0 * ALPHA_RANGE);
				}
				else if (x > 0.5 - ALPHA_RANGE) {
					blend = 1.0 - ((x - 0.5) / (2.0 * ALPHA_RANGE));
				}

				// stは (0,0)を中心としたFisheye座標
				bool forward_or_back = (i.uv.x <= 0.5);

				float4 col;
				if (blend < 0.5 - ALPHA_RANGE) {
					st = convert_for_backward(st);
					#if !defined(SHADER_API_OPENGL)
					col = tex2Dlod(_MainTex, float4(st, 0.0, 0.0));
					#else // Memo: OpenGL not supported tex2Dlod.( Texture should be setting to generateMipMap = off. )
					col = tex2D(_MainTex, st);
					#endif
				} else if (blend > 0.5 + ALPHA_RANGE) {
					st = convert_for_forward(st);
					#if !defined(SHADER_API_OPENGL)
					col = tex2Dlod(_MainTex, float4(st, 0.0, 0.0));
					#else // Memo: OpenGL not supported tex2Dlod.( Texture should be setting to generateMipMap = off. )
					col = tex2D(_MainTex, st);
					#endif
				}
				else {
					float2 st_b = convert_for_backward(st);
					float2 st_f = convert_for_forward(st);
					#if !defined(SHADER_API_OPENGL)
						float4 col_b = tex2Dlod(_MainTex, float4(st_b, 0.0, 0.0));
						float4 col_f = tex2Dlod(_MainTex, float4(st_f, 0.0, 0.0));
					#else // Memo: OpenGL not supported tex2Dlod.( Texture should be setting to generateMipMap = off. )
						float4 col_b = tex2D(_MainTex, st_b);
						float4 col_f = tex2D(_MainTex, st_f);
					#endif
					col = lerp(col_b, col_f, blend);
				}

				return col;
			}
			ENDCG
		}
	}
}
