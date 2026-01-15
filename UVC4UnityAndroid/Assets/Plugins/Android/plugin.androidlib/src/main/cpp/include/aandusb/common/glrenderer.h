/*
 * aAndUsb
 * Copyright (c) 2014-2026 saki t_saki@serenegiant.com
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

#ifndef GLRENDERER_H_
#define GLRENDERER_H_

#pragma interface

// 標準ライブラリ
#include <memory>
// common
#include "common/glutils.h"

//--------------------------------------------------------------------------------
// 外部クラスの前方宣言
#if defined(__ANDROID__)
namespace serenegiant::egl {
class EglImageWrapper;
}
#endif
namespace serenegiant::gl {
class GLTexture;
}
// 外部クラスの前方宣言ここまで
//--------------------------------------------------------------------------------

namespace serenegiant::gl {

/**
 * 指定したイメージをview全面にOpenGL|ESで描画するクラス
 * すべての呼び出しはGLコンテキストを保持したスレッド上で行うこと
 */
class GLRenderer {
protected:
	GLuint mShaderProgram;
	GLuint mVertexShader;
	GLuint mFragmentShader;
	GLuint vbo[2]{0, 0};
    // uniform変数のロケーション
    GLint muTextureLoc;			// テクスチャ(テクスチャユニット番号)のロケーション
	GLint muTextureLoc2;		// テクスチャ(テクスチャユニット番号)のロケーション
	GLint muTextureLoc3;		// テクスチャ(テクスチャユニット番号)のロケーション
	GLint muMVPMatrixLoc;		// モデルビュー行列のロケーション
	GLint muTexMatrixLoc;		// テクスチャ行列のロケーション
	GLint muTextureSzLoc;		// テクスチャサイズ変数のロケーション
	GLint muFrameSzLoc;			// フレームサイズ変数のロケーション
	GLint muBrightnessLoc;		// 明るさのオフセット変数のロケーション
    // attribute変数のロケーション
	GLint maPositionLoc;		// 頂点情報配列のロケーション
	GLint maTextureCoordLoc;	// テクスチャ座標配列のロケーション
	//
	float mBrightness;			// 明るさのオフセット値
	/**
	 * 初期化処理
	 * @param use_vbo 矩形描画時の頂点座標・テクスチャ座標にバッファオブジェクトを使うかどうか
	 */
	void init(const bool &use_vbo);
	/**
	 * 頂点座標・テクスチャ座標をセット
	 */
	void update_vertices();
public:
	/**
	 * コンストラクタ
	 * 頂点シェーダーとフラグメントシェーダーは文字列で引き渡す
	 * @param pVertexSource
	 * @param pFragmentSource
	 * @param use_vbo 矩形描画時の頂点座標・テクスチャ座標にバッファオブジェクトを使うかどうか, デフォルトはfalse
	 */
	GLRenderer(const char *pVertexSource, const char *pFragmentSource, const bool &use_vbo = false);
	/**
	 * デストラクタ
	 */
	~GLRenderer() noexcept;
	/**
	 * 描画実行
	 * yuyvをrgbaに対応させる(2ピクセルの元データをテクスチャ1テクセルに代入する)時はview_widthを1/2にして呼び出すこと
	 * @param texture 描画するテクスチャ
	 * @param tex_matrix テクスチャ変換行列
	 * @param mvp_matrix モデルビュー変換行列
	 * @return
	 */
	int draw(GLTexture *texture, const GLfloat *tex_matrix = nullptr, const GLfloat *mvp_matrix = IDENTITY_MATRIX);
	/**
	 * 描画実効
	 * @param texture1
	 * @param texture2
	 * @param texture3
	 * @param mvp_matrix モデルビュー変換行列
	 * @return
	 */
	int draw(GLTexture *texture1, GLTexture *texture2, GLTexture *texture3 = nullptr, const GLfloat *mvp_matrix = IDENTITY_MATRIX);
#if defined(__ANDROID__)
	/**
	 * 描画実行
	 * yuyvをrgbaに対応させる(2ピクセルの元データをテクスチャ1テクセルに代入する)時はview_widthを1/2にして呼び出すこと
	 * @param texture 描画するテクスチャ
	 * @param tex_matrix テクスチャ変換行列
	 * @param mvp_matrix モデルビュー変換行列
	 * @return
	 */
	int draw(egl::EglImageWrapper *texture, const GLfloat *tex_matrix = nullptr, const GLfloat *mvp_matrix = IDENTITY_MATRIX);
#endif
};

using GLRendererSp = std::shared_ptr<GLRenderer>;
using GLRendererUp = std::unique_ptr<GLRenderer>;

}	// namespace serenegiant::gl

#endif /* GLRENDERER_H_ */
