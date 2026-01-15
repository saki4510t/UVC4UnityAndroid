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

#ifndef GLOFFSCREEN_H_
#define GLOFFSCREEN_H_

#pragma interface

// 標準ライブラリ
#include <memory>
// common
#include "common/glsurface.h"
#include "common/glutils.h"

//--------------------------------------------------------------------------------
// 外部クラスの前方宣言
namespace serenegiant::gl {
class GLTexture;
class GLRenderer;
}
// 外部クラスの前方宣言ここまで
//--------------------------------------------------------------------------------

namespace serenegiant::gl {

/**
 * フレームバッファオブジェクト(FBO)を使ってオフスクリーン描画するためのクラス
 * バックバッファとしてテスクチャを使用する
 */
class GLOffScreen : virtual public egl::IGLSurface {
private:
	const int32_t mWidth;
	const int32_t mHeight;
	const bool m_own_texture;
	GLuint mFrameBufferObj;
	GLuint mDepthBufferObj;
	/**
	 * バックバッファとして使用するテクスチャオブジェクト
	 * FIXME スマートポインターに変える
	 */
	GLTexture *mFBOTexture;
	/**
	 * モデルビュー変換行列
	 */
	float mMvpMatrix[16]{};

	/**
	 * コンストラクタ
	 * @param texture GLTextureインスタンス FIXME スマートポインターに変える
	 * @param use_depth_buffer
	 * @param own_texture GLOffScreen内で生成したGLTextureかどうか
	 */
	GLOffScreen(GLTexture *texture,
		const bool &use_depth_buffer,
		const bool &own_texture);
public:
	/**
	 * コンストラクタ
	 * @param texture 既存のGLTextureインスタンス　FIXME スマートポインターに変える
	 * 			このオブジェクトはデストラクタで破棄されないので注意
	 * @param use_depth_buffer
	 */
	explicit GLOffScreen(GLTexture *texture,
		const bool &use_depth_buffer = false);
	/**
	 * コンストラクタ
	 * @param tex_unit テクスチャユニット GL_TEXTURE_0..GL_TEXTURE_31
	 * @param width
	 * @param height
	 * @param use_depth_buffer
	 */
	GLOffScreen(const GLenum &tex_unit,
		const GLint &width, const GLint &height,
		const bool &use_depth_buffer = false);
	/**
	 * デストラクタ
	 */
	~GLOffScreen() noexcept override;
	/**
	 * オフスクリーンへの描画に切り替える
	 * Viewportも変更になるので必要であればunbind後にViewportの設定をすること
	 * @return
	 */
	int bind() override;
	/**
	 * デフォルトのレンダリングバッファへ戻す
	 * @return
	 */
	int unbind() override;
	/**
	 * オフスクリーンテクスチャを指定したGLRendererで描画する(ヘルパーメソッド)
	 * @param renderer
	 * @return
	 */
	int draw(GLRenderer *renderer);
	/**
	 * バックバッファとして使用しているテクスチャオブジェクトを取得
	 * @return
	 */
	inline GLTexture *getOffscreen() { return mFBOTexture; }
	/**
	 * モデルビュー変換行列を取得
	 * @return
	 */
	[[nodiscard]]
	inline const GLfloat *getMatrix() const { return mMvpMatrix; }
	/**
	 * モデルビュー変換行列を設定
	 * @param mvp_matrix 要素数16以上
	 * @return
	 */
	int set_mvp_matrix(const GLfloat *mvp_matrix, const int &offset = 0);

	[[nodiscard]]
	inline int32_t width() const override { return mWidth; };
	[[nodiscard]]
	inline int32_t height() const override { return mHeight; };
};

using GLOffScreenSp = std::shared_ptr<GLOffScreen>;
using GLOffScreenUp = std::unique_ptr<GLOffScreen>;

}	// namespace serenegiant::gl

#endif /* GLOFFSCREEN_H_ */
