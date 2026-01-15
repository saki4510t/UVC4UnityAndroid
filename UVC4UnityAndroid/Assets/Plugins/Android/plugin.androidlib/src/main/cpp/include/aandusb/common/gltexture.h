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

#ifndef GLTEXTURE_H_
#define GLTEXTURE_H_

// 標準ライブラリ
#include <memory>
// common
#include "common/glutils.h"
#if defined(__ANDROID__)
#include "common/hardware_buffer_stub.h"
#endif

namespace serenegiant::gl {

/**
 * OpenGL/OpenGL ESのテクスチャを扱うためのヘルパークラス
 */
class GLTexture {
private:
	/**
	 * 使用するテクスチャの種類GL_TEXTURE_2D/GL_TEXTURE_EXTERNAL_OES
	 */
	const GLuint TEX_TARGET;
	/**
	 * テクスチャユニット
	 */
	const GLenum TEX_UNIT;
	/**
	 * テクスチャの内部形式, デフォルトはGL_RGBA
	 * internal_pixel_formatとpixel_formatに使えるのはGL_ALPHA, GL_RGB, GL_RGBA, GL_LUMINANCE, and GL_LUMINANCE_ALPHAのみ
	 */
	const GLenum PIXEL_FORMAT_INTERNAL;
	/**
	 * テクスチャをセットするときの形式, デフォルトはGL_RGBA
	 * internal_pixel_formatとpixel_formatに使えるのはGL_ALPHA, GL_RGB, GL_RGBA, GL_LUMINANCE, and GL_LUMINANCE_ALPHAのみ
	 */
	const GLenum PIXEL_FORMAT;
	/**
	 * テクスチャのデータタイプ
	 * 使用できるのはGL_UNSIGNED_BYTE, GL_UNSIGNED_SHORT_5_6_5, GL_UNSIGNED_SHORT_4_4_4_4, and GL_UNSIGNED_SHORT_5_5_5_1のみ
	 */
	const GLenum DATA_TYPE;
	/**
	 * テクスチャのサイズ(幅)
	 */
	GLint mTexWidth;
	/**
	 * テクスチャのサイズ(高さ)
	 */
	GLint mTexHeight;
	/**
	 * テクスチャに貼り付けるイメージのサイズ(幅)
	 */
	const GLint mImageWidth;
	/**
	 * テクスチャに貼り付けるイメージのサイズ(高さ)
	 */
	const GLint mImageHeight;
	/**
	 * テクスチャ変換行列
	 */
	GLfloat mTexMatrix[16]{};
	/**
	 * テクスチャ名(テクスチャID)
	 */
	GLuint mTexId;
	/**
	 * このクラス内でテクスチャidを生成したかどうか
	 */
	bool is_own_tex;
	/**
	 * イメージサイズとテクスチャサイズが異なるかどうか
	 * (テクスチャサイズを2のべき乗サイズに拡張したかどうか)
	 */
	bool m_use_powered2;
	const GLsizeiptr image_size;
	/**
	 * PBO名（PBOのID）
	 */
	GLuint mPBO[2]{};
	int pbo_ix;
	GLsync pbo_sync;
	bool pbo_need_write;
#if defined(__ANDROID__)
	// 自前で生成したAHardwareBufferかどうか
	const bool own_hardware_buffer;
	AHardwareBuffer *graphicBuffer;
	EGLImageKHR eglImage;
	// API16だとeglGetNativeClientBufferANDROIDは動的にリンクしないとビルドが通らない
	PFNEGLGETNATIVECLIENTBUFFERANDROIDPROC dynamicEglGetNativeClientBufferANDROID;
#endif
protected:
	/**
	 * 初期化処理
	 * @param width テクスチャに割り当てる映像の幅
	 * @param height テクスチャに割り当てる映像の高さ
	 * @param use_pbo pboを使ってテクスチャの読み書きを行うかどうか
	 * @param use_powered2 true: テクスチャを2の乗数サイズにする
	 * @param try_hw_buf 可能であればhardware bufferを使う,use_pbo=trueなら無効
	 */
	void init(const GLint &width, const GLint &height,
		const bool &use_pbo,
		const bool &use_powered2,
		const bool &try_hw_buf);
	/**
	 * コンストラクタ
	 * internal_formatまたはformatのデフォルトはGL_RGBA,
	 * GL_RGBA以外にすると1バイトアライメントになるので効率が悪くなるので注意！
	 * internal_pixel_formatとpixel_formatに使えるのはGL_ALPHA, GL_RGB, GL_RGBA, GL_LUMINANCE, and GL_LUMINANCE_ALPHAのみ
	 * @param tex_target GL_TEXTURE_2D
	 * @param tex_unit テクスチャユニット GL_TEXTURE_0..GL_TEXTURE_31
	 * @param tex_id 外部で生成済みのテクスチャid, 0のときは内部で自動生成・破棄する
	 * @param width
	 * @param height
	 * @param use_pbo assignTextureでPBOを使うかどうか
	 * @param internal_pixel_format, テクスチャの内部フォーマット, デフォルトはGL_RGBA
	 * @param pixel_format, assignTextureするときのテクスチャのフォーマット, デフォルトはGL_RGBA
	 * @param data_type テクスチャのデータタイプ, デフォルトはGL_UNSIGNED_BYTE
	 * @param use_powered2 true: テクスチャを2の乗数サイズにする
	 * @param try_hw_buf 可能であればhardware bufferを使う,use_pbo=trueなら無効
	 */
	GLTexture(
		const GLenum &tex_target,
		const GLenum &tex_unit,
		const GLuint &tex_id,
		const GLint &width, const GLint &height,
		const bool &use_pbo,
		const GLenum &internal_pixel_format, const GLenum &pixel_format,
		const GLenum &data_type,
		const bool &use_powered2,
	  	const bool &try_hw_buf
#if defined(__ANDROID__)
		, AHardwareBuffer *a_hardware_buffer
#endif
	  	);
public:
	/**
	 * 外部で生成済みのテクスチャidをラップする
	 * internal_formatまたはformatのデフォルトはGL_RGBA,
	 * internal_pixel_formatとpixel_formatに使えるのはGL_ALPHA, GL_RGB, GL_RGBA, GL_LUMINANCE, and GL_LUMINANCE_ALPHAのみ
	 * GL_RGBA以外にすると1バイトアライメントになるので効率が悪くなるので注意！
	 * @param tex_target GL_TEXTURE_2D
	 * @param tex_unit テクスチャユニット GL_TEXTURE_0..GL_TEXTURE_31
	 * @param tex_id 外部で生成済みのテクスチャid, 0のときは内部で自動生成・破棄する
	 * @param width
	 * @param height
	 * @param use_pbo assignTextureでPBOを使うかどうか
	 * @param internal_pixel_format, テクスチャの内部フォーマット, デフォルトはGL_RGBA
	 * @param pixel_format, assignTextureするときのテクスチャのフォーマット, デフォルトはGL_RGBA
	 * @param data_type テクスチャのデータタイプ, デフォルトはGL_UNSIGNED_BYTE
	 * @param try_hw_buf 可能であればhardware bufferを使う,use_pbo=trueなら無効
	 * @return
	 * @param use_powered2 true: テクスチャを2の乗数サイズにする
	 */
	static GLTexture *wrap(
		const GLenum &tex_target,
		const GLenum &tex_unit,
		const GLuint &tex_id,
		const GLint &width, const GLint &height,
		const bool &use_pbo = false,
		const GLenum &internal_pixel_format = GL_RGBA, const GLenum &pixel_format = GL_RGBA,
		const GLenum &data_type = GL_UNSIGNED_BYTE,
		const bool &use_powered2 = true,
		const bool &try_hw_buf = false);

#if defined(__ANDROID__)
	/**
	 * 既存のAHardwareBufferをラップしてテクスチャとしてアクセスできるようにする
	 * @param graphicBuffer
	 * @param tex_target
	 * @param tex_unit
	 * @param width
	 * @param height
	 * @return
	 */
	static GLTexture *wrap(
		AHardwareBuffer *graphicBuffer,
		const GLenum &tex_target,
		const GLenum &tex_unit,
		const GLint &width, const GLint &height);
#endif

	/**
	 * コンストラクタ
	 * テクスチャidは内部で自動生成・破棄する
	 * internal_formatまたはformatのデフォルトはGL_RGBA,
	 * internal_pixel_formatとpixel_formatに使えるのはGL_ALPHA, GL_RGB, GL_RGBA, GL_LUMINANCE, and GL_LUMINANCE_ALPHAのみ
	 * GL_RGBA以外にすると1バイトアライメントになるので効率が悪くなるので注意！
	 * @param tex_target GL_TEXTURE_2D
	 * @param tex_unit テクスチャユニット GL_TEXTURE_0..GL_TEXTURE_31
	 * @param width
	 * @param height
	 * @param use_pbo assignTextureでPBOを使うかどうか
	 * @param internal_pixel_format, テクスチャの内部フォーマット, デフォルトはGL_RGBA
	 * @param pixel_format, assignTextureするときのテクスチャのフォーマット, デフォルトはGL_RGBA
	 * @param data_type テクスチャのデータタイプ, デフォルトはGL_UNSIGNED_BYTE
	 * @param use_powered2 true: テクスチャを2の乗数サイズにする
	 * @param try_hw_buf 可能であればhardware bufferを使う,use_pbo=trueなら無効
	 */
	GLTexture(
		const GLenum &tex_target,
		const GLenum & tex_unit,
		const GLint &width, const GLint &height,
		const bool &use_pbo = false,
		const GLenum &internal_pixel_format = GL_RGBA, const GLenum &pixel_format = GL_RGBA,
		const GLenum &data_type = GL_UNSIGNED_BYTE,
		const bool &use_powered2 = true,
		const bool &try_hw_buf = false);
	/**
	 * デストラクタ
	 */
	virtual ~GLTexture() noexcept;
	/**
	 * テキスチャへイメージを書き込む
	 * @param src イメージデータ, コンストラクタで引き渡したフォーマットに合わせること
	 * @return
	 */
	int assignTexture(const uint8_t *src);
	/**
	 * PBOへの書き込み処理が完了していればテクスチャへ反映させる
	 * @param timeout 最大待ち時間［ナノ秒］
	 */
	void refresh(const GLuint64 &timeout = 0);
	/**
	 * テクスチャをバインド
	 * @return
	 */
	int bind();
	/**
	 * テクスチャのバインドを解除
	 * @return
	 */
	int unbind();
	/**
	 * テクスチャの補間方法をセット
	 * min_filter/max_filterはそれぞれGL_NEARESTかGL_LINEAR
	 * GL_LINEARにすると補間なし
	 * @param min_filter
	 * @param max_filter
	 * @return
	 */
	int setFilter(const GLint &min_filter, const GLint &max_filter);
	/**
	 * テクスチャターゲットを取得
	 * @return
	 */
	[[nodiscard]]
	inline GLuint getTexTarget() const { return TEX_TARGET; }
	/**
	 * テクスチャユニットを取得
	 * @return
	 */
	[[nodiscard]]
	inline GLuint  getTexUnit() const { return TEX_UNIT; };
	/**
	 * テクスチャidを取得
	 * @return
	 */
	[[nodiscard]]
	inline GLuint getTexture() const { return mTexId; }
	/**
	 * テクスチャ変換行列を取得
	 * @return
	 */
	[[nodiscard]]
	inline const GLfloat *getTexMatrix() const { return mTexMatrix; }
	/**
	 * テクスチャの幅を取得
	 * 内部でテクスチャidを生成した場合は2のべき乗にしているので実イメージの幅と異なる可能性がある
	 * @return
	 */
	[[nodiscard]]
	inline int getTexWidth() const { return mTexWidth; }
	/**
	 * テクスチャの高さを取得
	 * 内部でテクスチャidを生成した場合は2のべき乗にしているので実イメージの高さと異なる可能性がある
	 * @return
	 */
	[[nodiscard]]
	inline int getTexHeight() const { return mTexHeight; }
	/**
	 * 実イメージ幅を取得
	 * 内部でテクスチャidを生成した場合は2のべき乗にしているので実テクスチャの幅と異なる可能性がある
	 * @return
	 */
	[[nodiscard]]
	inline GLint getImageWidth() const { return mImageWidth; }
	/**
	 * 実イメージ高さを取得
	 * 内部でテクスチャidを生成した場合は2のべき乗にしているので実テクスチャの高さと異なる可能性がある
	 * @return
	 */
	[[nodiscard]]
	inline GLint getImageHeight() const { return mImageHeight; }
	/**
	 * @brief テクスチャの幅に対して実イメージが占める割合/実イメージの左下UV座標
	 *		  デフォルトだとテクスチャサイズを2のべき乗に繰り上げるのでイメージサイズより
	          テクスチャのほうが大きい可能性があるため左下UV座標を調整するためのヘルパー関数
	 * 
	 * @return GLfloat 
	 */
	[[nodiscard]]
	inline GLfloat getTexScaleX() const { return mImageWidth / (GLfloat)mTexWidth; };
	/**
	 * @brief テクスチャの高さに対して実イメージが占める割合/実イメージの左下UV座標
	 *		  デフォルトだとテクスチャサイズを2のべき乗に繰り上げるのでイメージサイズより
	          テクスチャのほうが大きい可能性があるため左下UV座標を調整するためのヘルパー関数
	 * 
	 * @return GLfloat 
	 */
	[[nodiscard]]
	inline GLfloat getTexScaleY() const { return mImageHeight / (GLfloat)mTexHeight; };
#if defined(__ANDROID__)
	// 自前で生成したAHardwareBufferかどうか
	[[nodiscard]]
	inline bool is_own_hardware_buffer() const { return own_hardware_buffer; };
	inline AHardwareBuffer *buffer() { return graphicBuffer; };
#endif
};

using GLTextureSp = std::shared_ptr<GLTexture>;
using GLTextureUp = std::unique_ptr<GLTexture>;

}	// namespace serenegiant::gl

#endif /* GLTEXTURE_H_ */
