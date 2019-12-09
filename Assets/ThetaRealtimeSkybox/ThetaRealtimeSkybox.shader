// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Copyright (c) 2015 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php
Shader "Theta/RealtimeSkybox" {
Properties {
	[NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
	_Tint("Tint Color", Color) = (.5, .5, .5, .5)
	[Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
	_Rotation ("Rotation", Range(0, 360)) = 0

	_Radius("Radius", Float) = 0.445 // Preset for Theta S
	_UVOffset("UVOffset(Forward UV / Backward UV)", Vector) = (0.0, 0.0, 0.0, 0.0)

	[KeywordEnum(Theta S, Theta)] _Mode("Mode", Int) = 0
}

SubShader {
	Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
	Cull Off ZWrite Off

	Pass {
		
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile _MODE_THETA_S _MODE_THETA

		#include "UnityCG.cginc"

		sampler2D _MainTex;
		half4 _Tex_HDR;
		half4 _Tint;
		half _Exposure;
		float _Rotation;
		float _Radius;
		float4 _UVOffset;

		// Supports 1280x720 or 1920x1080
		#define _THETA_S_Y_OFST		((720.0 - 640.0) / 720.0)
		#define _THETA_S_Y_SCALE	(640.0 / 720.0)

		//#define _ENABLE_HDR

		float4 RotateAroundYInDegrees (float4 vertex, float degrees)
		{
			float alpha = degrees * UNITY_PI / 180.0;
			float sina, cosa;
			sincos(alpha, sina, cosa);
			float2x2 m = float2x2(cosa, -sina, sina, cosa);
			return float4(mul(m, vertex.xz), vertex.yw).xzyw;
		}
		
		struct appdata_t {
			float4 vertex : POSITION;
		};

		struct v2f {
			float4 vertex : SV_POSITION;
			float3 texcoord : TEXCOORD0;
		};

		v2f vert (appdata_t v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(RotateAroundYInDegrees(v.vertex, _Rotation + 90.0));
			o.texcoord = v.vertex.xyz;
			return o;
		}

		half4 frag (v2f i) : SV_Target
		{
			float3 p = i.texcoord;
			if (i.texcoord.z < 0.0) {
				p = -p;
			}

			float r = 1.0 - asin(p.z) / (UNITY_PI / 2.0);
			float2 st = float2(-p.y, -p.x);

			st *= r / sqrt(1.0 - p.z * p.z);
			st *= _Radius;
			st += 0.5;

			if (i.texcoord.z < 0.0) {
				st.x *= 0.5;
				st.x += 0.5;
				st.xy += _UVOffset.wz;
			} else {
				st.x *= 0.5;
				st.y = 1.0 - st.y;
				st.xy += _UVOffset.yx;
			}

			#if defined(_MODE_THETA_S)
			st.y = st.y * _THETA_S_Y_SCALE + _THETA_S_Y_OFST;
			#endif

			#if !defined(SHADER_API_OPENGL)
			half4 tex = tex2Dlod(_MainTex, float4(st, 0.0, 0.0));
			#else // Memo: OpenGL not supported tex2Dlod.( Texture should be setting to generateMipMap = off. )
			half4 tex = tex2D(_MainTex, st);
			#endif

			#if defined(_ENABLE_HDR)
			half3 c = DecodeHDR (tex, _Tex_HDR);
			#else
			half3 c = tex.rgb;
			#endif
			c = c * _Tint.rgb * unity_ColorSpaceDouble.rgb;
			c *= _Exposure;
			return half4(c, 1.0);
		}
		ENDCG 
	}
} 	

Fallback Off

}
