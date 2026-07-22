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
#include <mutex>
#include <vector>
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
private:
	const bool m_use_pbo;
	bool m_initialized;
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
    // attribute変数のロケーション
	GLint maPositionLoc;		// 頂点情報配列のロケーション
	GLint maTextureCoordLoc;	// テクスチャ座標配列のロケーション
	/**
	 * 頂点座標・テクスチャ座標をセット
	 */
	void update_vertices();
protected:
	[[nodiscard]]
	inline GLuint shaderProgram() const { return mShaderProgram; };

	/**
	 * 初期化処理
	 * @param use_vbo 矩形描画時の頂点座標・テクスチャ座標にバッファオブジェクトを使うかどうか
	 */
	virtual void init(const bool &use_vbo);
	/**
	 * 描画の準備
	 * テクスチャサイズと映像サイズが同じ場合
	 * @param width
	 * @param height
	 * @param tex_matrix nullptrなら単位行列をセットする, nullptr以外は16個以上確保すること
	 * @param mvp_matrix nullptrなら単位行列をセットする, nullptr以外は16個以上確保すること
	 */
	virtual inline void prepare_draw(
		const uint32_t &width, const uint32_t &height,
		const GLfloat *tex_matrix, const GLfloat *mvp_matrix) {
		prepare_draw(width, height, width, height, tex_matrix, mvp_matrix);
	};
	/**
	 * 描画の準備
	 * テクスチャサイズと映像サイズが異なる同じ場合
	 * @param tex_width テクスチャサイズ(幅)
	 * @param tex_height テクスチャサイズ(高さ)
	 * @param width 映像サイズ(幅)
	 * @param height 映像サイズ(高さ)
	 * @param tex_matrix nullptrなら単位行列をセットする, nullptr以外は16個以上確保すること
	 * @param mvp_matrix nullptrなら単位行列をセットする, nullptr以外は16個以上確保すること
	 */
	virtual void prepare_draw(
		const uint32_t &tex_width, const uint32_t &tex_height,
		const uint32_t &width, const uint32_t &height,
		const GLfloat *tex_matrix, const GLfloat *mvp_matrix);
	/**
	 * 描画終了処理
	 */
	virtual void finish_draw();
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
	virtual ~GLRenderer() noexcept;
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

/**
 * ガンマ曲線をトーンカーブとする
 * @param gamma
 * @return
 */
int calc_gamma(std::vector<GLfloat> &curve, const float &gamma);
/**
 * コントラスト調整カーブをトーンカーブとする
 * @param strength -1.0〜+1.0, 0: 補正無し、負ならコントラスト抑制、正ならコントラスト向上
 * @return
 */
int calc_contrast(std::vector<GLfloat> &curve, const float &strength);
/**
 * シグモイドカーブをトーンカーブとする
 * @param k
 * @param threshold 0.0〜1.0
 * @return
 */
int calc_sigmoid(std::vector<GLfloat> &curve, const float &k, const float &threshold);

class GLRendererColorAdjust : public GLRenderer {
private:
#define NUM_TONE_STEP (65)
	mutable std::mutex m_mutex;
	GLint muColorMatrixLoc;		// 4x4色変換行列
	GLint muParamsLoc;			// 補正パラメータ
	// 4x4色変換行列
	GLfloat mColorMatrix[16];
	// 濃度補正に使うトーンカーブ
	std::vector<GLfloat> mToneCurve;
protected:
	/**
	 * 初期化処理
	 * @param use_vbo 矩形描画時の頂点座標・テクスチャ座標にバッファオブジェクトを使うかどうか
	 */
	void init(const bool &use_vbo) override;
	/**
	 * 描画の準備
	 * テクスチャサイズと映像サイズが異なる同じ場合
	 * @param tex_width テクスチャサイズ(幅)
	 * @param tex_height テクスチャサイズ(高さ)
	 * @param width 映像サイズ(幅)
	 * @param height 映像サイズ(高さ)
	 * @param tex_matrix nullptrなら単位行列をセットする, nullptr以外は16個以上確保すること
	 * @param mvp_matrix nullptrなら単位行列をセットする, nullptr以外は16個以上確保すること
	 */
	void prepare_draw(
		const uint32_t &tex_width, const uint32_t &tex_height,
		const uint32_t &width, const uint32_t &height,
		const GLfloat *tex_matrix, const GLfloat *mvp_matrix) override;
public:
	/**
	 * コンストラクタ
	 * 頂点シェーダーとフラグメントシェーダーは文字列で引き渡す
	 * @param pVertexSource
	 * @param pFragmentSource
	 * @param use_vbo 矩形描画時の頂点座標・テクスチャ座標にバッファオブジェクトを使うかどうか, デフォルトはfalse
	 */
	GLRendererColorAdjust(const char *pVertexSource, const char *pFragmentSource, const bool &use_vbo = false);
	/**
	 * デストラクタ
	 */
	~GLRendererColorAdjust() noexcept override;
	/**
	 * 色変換行列とトーンカーブをセット
	 * @param color_matrix nullptrまたは16要素以上を確保すること, nullptrなら単位行列にする
	 * @param curve トーンカーブ、NUM_TONE_STEP(65)要素未満なら0クリア
	 * @return
	 */
	int set_color_params(const GLfloat *color_matrix, const std::vector<GLfloat> &curve);
	/**
	 * 色変換行列をセット
	 * トーンカーブは変更しない
	 * @param color_matrix nullptrまたは16要素以上を確保すること, nullptrなら単位行列にする
	 * @return
	 */
	int set_color_matrix(const GLfloat *color_matrix);
};

using GLRendererColorAdjustSp = std::shared_ptr<GLRendererColorAdjust>;
using GLRendererColorAdjustUp = std::unique_ptr<GLRendererColorAdjust>;

/**
 * 3x3カーネル関数を適用するGLRenderer実装
 */
class GLRenderer3x3Kernel : public GLRenderer {
private:
#define KERNEL_SIZE3x3 (9)
	mutable std::mutex m_mutex;
	GLint muKernel;				// 3x3カーネル関数のユニフォーム変数のロケーション
	GLint muTexOffset;			// 隣接テクセル座標オフセット用ユニフォーム変数のロケーション
	GLint muColorAdjust;		// 色調整用ユニフォーム変数のロケーション
	// 3x3カーネル関数、mat3
	GLfloat mKernel[KERNEL_SIZE3x3];
	// 隣接テクセル座標オフセット, vec2なので2倍確保
	GLfloat mTexOffset[KERNEL_SIZE3x3 * 2];
	// 色調整用
	GLfloat mColorAdjust;
	// 隣接テクセル座標オフセットの計算に使うテクスチャのサイズ
	uint32_t m_text_width;
	uint32_t m_tex_height;
protected:
	/**
	 * 初期化処理
	 * @param use_vbo 矩形描画時の頂点座標・テクスチャ座標にバッファオブジェクトを使うかどうか
	 */
	void init(const bool &use_vbo) override;
	/**
	 * 描画の準備
	 * テクスチャサイズと映像サイズが異なる同じ場合
	 * @param tex_width テクスチャサイズ(幅)
	 * @param tex_height テクスチャサイズ(高さ)
	 * @param width 映像サイズ(幅)
	 * @param height 映像サイズ(高さ)
	 * @param tex_matrix nullptrなら単位行列をセットする, nullptr以外は16個以上確保すること
	 * @param mvp_matrix nullptrなら単位行列をセットする, nullptr以外は16個以上確保すること
	 */
	void prepare_draw(
		const uint32_t &tex_width, const uint32_t &tex_height,
		const uint32_t &width, const uint32_t &height,
		const GLfloat *tex_matrix, const GLfloat *mvp_matrix) override;
public:
	/**
	 * コンストラクタ
	 * 頂点シェーダーとフラグメントシェーダーは文字列で引き渡す
	 * @param pVertexSource
	 * @param pFragmentSource
	 * @param use_vbo 矩形描画時の頂点座標・テクスチャ座標にバッファオブジェクトを使うかどうか, デフォルトはfalse
	 */
	GLRenderer3x3Kernel(const char *pVertexSource, const char *pFragmentSource, const bool &use_vbo = false);
	/**
	 * デストラクタ
	 */
	~GLRenderer3x3Kernel() noexcept override;
	/**
	 * 3x3カーネル関数と調整用変数をセット
	 * @param kernel_matrix nullptrまたは9要素以上を確保すること, nullptrなら中央だけ1.0fにして周辺からのサンプリングをしない
	 * @param params
	 * @return
	 */
	int set_kernel(const GLfloat *kernel_matrix = nullptr, const GLfloat &color_adjust = 0.0f);
	/**
	 * 隣接テクセル座標オフセットを計算
	 * @param width
	 * @param height
	 * @return
	 */
	int set_tex_size(const uint32_t &width, const uint32_t &height);
};

using GLRenderer3x3KernelSp = std::shared_ptr<GLRenderer3x3Kernel>;
using GLRenderer3x3KernelUp = std::unique_ptr<GLRenderer3x3Kernel>;

}	// namespace serenegiant::gl

#endif /* GLRENDERER_H_ */
