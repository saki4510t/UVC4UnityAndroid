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

#ifndef AANDUSB_SURFACE_TEXTURE_H_
#define AANDUSB_SURFACE_TEXTURE_H_

// 標準ライブラリ
#include <memory>
#include <GLES2/gl2.h>
#include <android/native_window_jni.h>
// common
#include "common/jni_utils.h"

namespace serenegiant::media {

/**
 * Java側のSurfaceTextureクラスへJNIを経由してアクセスするためのラッパークラス
 * NDK側でのSurfaceTextureはAPI>=28(surface_texture_stub.h/.cpp)を使う
 */
class SurfaceTexture {
private:
	const GLuint m_tex;
	const int32_t m_format;
	// Java側のSurfaceTextureオブジェクトのグローバル参照
	jobject m_surface_texture_obj;
	int32_t m_width;
	int32_t m_height;
	/**
	 * Java側のSurfaceTexture/SurfaceへアクセスするためのメソッドIDを初期化
	 */
	bool init_surface_texture(JNIEnv *env, jclass surface_texture_clazz);
protected:
	serenegiant::AutoJNIEnv auto_jni_env;
	ANativeWindow *m_native_window;
	/**
	 * コンストラクタ
	 * XXX 呼び出し時のスレッドにJNIEnvが無い場合はAutoJNIEnvによって自動的にアタッチされるので注意
	 *     コンストラクタを呼び出したスレッド以外でメンバー関数を呼び出すと正常に動作しないかも
	 * @param tex
	 * @param width
	 * @param height
	 * @param format AHardwareBuffer_Format_xxxのどれか
	 */
	SurfaceTexture(
		const GLuint &tex,
		const int32_t &width, const int32_t &height,
		const int32_t &format);

	inline jobject get_surface_texture_obj() { return m_surface_texture_obj; };
public:
	/**
	 * インスタンス生成用のヘルパークラス
	 * @param tex
	 * @param width
	 * @param height
	 * @param format
	 * @return
	 */
	static SurfaceTexture *create(
		const GLuint &tex,
		const int32_t &width, const int32_t &height,
		const int32_t &format);
	/**
	 * デストラクタ
	 */
	virtual ~SurfaceTexture() noexcept;
	/**
	 * Java側のSurfaceTexture/SurfaceTextureへアクセス可能かどうか
	 * @return
	 */
	[[nodiscard]]
	inline bool is_supported() const { return m_surface_texture_obj != nullptr; }
	/**
	 * バッファサイズをセット
	 * @param width
	 * @param height
	 */
	void setDefaultBufferSize(const int32_t &width, const int32_t &height);
	/**
	 * SurfaceTexture#updateTexImageを呼び出す
	 */
	virtual void updateTexImage() = 0;

	[[nodiscard]]
	inline GLuint tex() const { return m_tex; };

	[[nodiscard]]
	inline int32_t format() const { return m_format; };
	/**
	 * サイズ(幅)を取得
	 * @return
	 */
	[[nodiscard]]
	inline int32_t width() const { return m_width; };
	/**
	 * サイズ(高さ)を取得
	 * @return
	 */
	[[nodiscard]]
	inline int32_t height() const { return m_height; };
	/**
	 *
	 * @return
	 */
	[[nodiscard]]
	inline ANativeWindow *native_window() const { return m_native_window; };
};

using SurfaceTextureSp = std::shared_ptr<SurfaceTexture>;
using SurfaceTextureUp = std::unique_ptr<SurfaceTexture>;

}	// namespace serenegiant::media

#endif //AANDUSB_SURFACE_TEXTURE_H_
