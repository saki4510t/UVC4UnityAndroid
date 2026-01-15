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

#ifndef EGLBASE_H_
#define EGLBASE_H_

#pragma interface

#define EGL_EGLEXT_PROTOTYPES
// 標準ライブラリ
#include <set>
#include <string>
#include <memory>
#include <EGL/egl.h>
#include <EGL/eglext.h>

#if defined(__ANDROID__)
#include <android/native_window.h>
#endif

// common
#if defined(__ANDROID__)
#include "common/hardware_buffer_stub.h"
#endif
#include "common/glutils.h"
#include "common/glsurface.h"

#undef EGLCHECK
#ifdef DEBUG_EGL_CHECK
	#define	EGLCHECK(OP) checkEglError(basename(__FILE__), __LINE__, __FUNCTION__, OP)
#else
	#define	EGLCHECK(OP)
#endif

namespace serenegiant::egl {

/**
 * eglGetErrorで取得したEGLのエラーをログに出力する
 * @param filename
 * @param line
 * @param func
 * @param op 実行した関数名
 */
void checkEglError(
	const char *filename, const int &line, const char *func,
	const char *op);

/**
 * EGLを使ってカレントスレッドにEGL/GLコンテキストを生成するクラス
 * EGL関係のヘルパー関数を含む
 */
class EGLBase {
friend class EglSync;
friend class GLSurface;
public:
	/**
	 * コンストラクタ
	 * @param client_version 2: OpenGL|ES2, 3:OpenGLES|3
	 * @param shared_context 共有コンテキスト, Nullable
	 * @param with_depth_buffer デプスバッファを使用するかどうか
	 * @param with_stencil_buffer ステンシルバッファを使用するかどうか
	 * @param isRecordable RECORDABLEフラグを漬けて初期化するかどうか
	 */
	EGLBase(const int & client_version,
		EGLBase *shared_context,
		const bool &with_depth_buffer,
		const bool &with_stencil_buffer,
		const bool &isRecordable);
	/**
	 * コンストラクタ
	 * @param client_version 2: OpenGL|ES2, 3:OpenGLES|3
	 * @param shared_context 共有コンテキスト, Nullable
	 */
	explicit EGLBase(const int &client_version, EGLBase *shared_context = nullptr);
	/**
	 * デストラクタ
	 */
	~EGLBase() noexcept;
	/**
	 * EGLレンダリングコンテキストを取得する
	 * @return
	 */
	[[nodiscard]]
	inline EGLContext context() const { return mEglContext; }
	[[nodiscard]]
	inline EGLDisplay display() const { return mEglDisplay; };
	/**
	 * GLES3を使用可能かどうか
	 * @return
	 */
	[[nodiscard]]
	inline bool isGLES3() const { return client_version >= 3; };
	/**
	 * OpenGL|ESのバージョンを取得
	 * @return
	 */
	[[nodiscard]]
	inline int glVersion() const { return client_version; };
	/**
	 * アタッチされているEGLSurfaceから切り離してデフォルトの描画先へ戻す
	 * @return
	 */
	int makeDefault();
	/**
	 * 文字列で指定したEGL拡張に対応しているかどうかを取得
	 * @param s
	 * @return
	 */
	bool hasEglExtension(const std::string &s);
	/**
	 * 文字列で指定したGL拡張に対応しているかどうかを取得
	 * @param s
	 * @return
	 */
	bool hasGLExtension(const std::string &s);
	/**
	 * eglWaitGLのシノニム
	 * eglWaitGL: コマンドキュー内のコマンドをすべて転送する, glFinish()と同様の効果
	 */
	static inline void waitGL() { eglWaitGL(); };
	/**
	 * eglWaitNativeのシノニム
	 * eglWaitNative: GPU側の描画処理が終了するまで実行をブロックする
	 */
	static inline void waitNative() { eglWaitNative(EGL_CORE_NATIVE_ENGINE); };
private:
	EGLDisplay mEglDisplay;
	EGLContext mEglContext;
	// GLコンテキスト保持にサーフェースが必要な場合の1x1のオフスクリーンサーフェース
	EGLSurface mEglSurface;
	EGLConfig mEglConfig;
	EGLint mMajor, mMinor;
	int client_version;
	bool mWithDepthBuffer;
	bool mWithStencilBuffer;
	bool mIsRecordable;

	// API16だとeglPresentationTimeANDROIDは動的にリンクしないとビルドが通らない
	PFNEGLPRESENTATIONTIMEANDROIDPROC dynamicEglPresentationTimeANDROID;
	PFNEGLDUPNATIVEFENCEFDANDROIDPROC dynamicEglDupNativeFenceFDANDROID;
	// fence sync と reusable sync共通
	PFNEGLCREATESYNCKHRPROC dynamicEglCreateSyncKHR;
	PFNEGLDESTROYSYNCKHRPROC dynamicEglDestroySyncKHR;
	PFNEGLWAITSYNCKHRPROC dynamicEglWaitSyncKHR;	// これは呼び出し元スレッドをブロックせずに即座に返る
	PFNEGLCLIENTWAITSYNCKHRPROC dynamicEglClientWaitSyncKHR;
	PFNEGLGETSYNCATTRIBKHRPROC dynamicEglGetSyncAttribKHR;
	// これはreusable syncのみみたい
	PFNEGLSIGNALSYNCKHRPROC dynamicEglSignalSyncKHR;

	std::set<std::string> mEGLExtensions;
	std::set<std::string> mGLExtensions;

	/**
	 * EGLConfigを選択する
	 * @param client_version
	 * @param with_depth_buffer
	 * @param with_stencil_buffer
	 * @param isRecordable
	 * @return 正常に選択できれば0, それ以外ならエラー
	 */
	EGLint getConfig(const int &client_version,
		const bool &with_depth_buffer,
		const bool &with_stencil_buffer,
		const bool &isRecordable);
	/**
	 * EGLコンテキストを初期化する
	 * @param client_version
	 * @param shared_context
	 * @param with_depth_buffer
	 * @param with_stencil_buffer
	 * @param isRecordable
	 * @return 0:正常に初期家できた, それ以外:エラー
	 */
	int initEGLContext(const int &client_version,
		EGLContext shared_context,
		const bool &with_depth_buffer,
		const bool &with_stencil_buffer,
		const bool &isRecordable);
	/**
	 * EGLコンテキストを破棄する
	 */
	void releaseEGLContext();
#if defined(__ANDROID__)
	/**
	 * 与えられたSurfaceへOpenGL|ESで描画するためのEGLSurfaceを生成する
	 * @param window
	 * @param request_width
	 * @param request_height
	 * @return
	 */
	EGLSurface createWindowSurface(ANativeWindow *window,
		const int32_t &request_width, const int32_t &request_height);
#endif
	/**
	 * オフスクリーンへOpenGL|ESで描画するためのEGLSurfaceを生成する
	 * @param request_width
	 * @param request_height
	 * @return
	 */
	EGLSurface createOffscreenSurface(
	  	const int32_t &request_width, const int32_t &request_height);
	/**
	 * 指定したEGLSurfaceへ描画できるように切り替える
	 * @param surface
	 * @return
	 */
	int makeCurrent(EGLSurface surface);
	/**
	 * 指定したEGLSurfaceへ描画できるように切り替える
	 * 書き込みと読み込みを異なるEGLSurfaceで行う場合
	 * @param draw_surface
	 * @param read_surface
	 * @return
	 */
	int makeCurrent(EGLSurface draw_surface, EGLSurface read_surface);
	/**
	 * ダブルバッファリングのバッファを入れ替える(Surfaceへの転送される)
	 * @param surface
	 * @return
	 */
	int swap(EGLSurface surface);
	/**
	 * 可能であればptsをセットする
	 * @param pts_ns
	 * @return
	 */
	int setPresentationTime(EGLSurface surface,
		const khronos_stime_nanoseconds_t &pts_ns = 0);
	/**
	 * 指定したEGLSurfaceを破棄する
	 * @param surface
	 */
	void releaseSurface(EGLSurface surface);
	/**
	 * 指定したEGLSurfaceへ描画可能化かどうかを取得する
	 * @param surface
	 * @return
	 */
	bool isCurrent(EGLSurface surface);
};

//================================================================================

class GLSurface : virtual public IGLSurface {
private:
	EGLBase *mEgl;
#if defined(__ANDROID__)
	ANativeWindow *mWindow;
#endif
	EGLSurface mEglSurface;
	int32_t window_width, window_height;
public:
#if defined(__ANDROID__)
	/**
	 * 指定したSurfaceへOpenGL|ESで描画するためのEGLSurfaceを生成するコンストラクタ
	 * @param egl
	 * @param window
	 * @param width
	 * @param height
	 */
	GLSurface(EGLBase *egl, ANativeWindow *window,
		const int32_t &width = 0, const int32_t &height = 0);
#endif
	/**
	 * オフスクリーンへOpenGL|ESで描画するためのEGLSurfaceを生成するコンストラクタ
	 * @param egl
	 * @param width
	 * @param height
	 */
	GLSurface(EGLBase *egl, const int32_t &width, const int32_t &height);
	/**
	 * デストラクタ
	 */
	~GLSurface() noexcept override;
	/**
	 * このオブジェクトが保持しているEGLSurfaceへアタッチして描画できるようにする
	 * @return
	 */
	int bind() override;
	/**
	 * このオブジェクトが保持しているEGLSurfaceからデタッチする
	 * @return
	 */
	int unbind() override;
	/**
	 * ダブルバッファリングのバッファを入れ替える
	 * @return
	 */
	int swap();
	/**
	 * 可能であればptsをセットする
	 * @param pts_ns
	 * @return
	 */
	int setPresentationTime(const khronos_stime_nanoseconds_t &pts_ns);
	/**
	 * このオブジェクトが保持しているEGLSurfaceへ描画中かどうかを取得する
	 * @return
	 */
	bool isCurrent();
	/**
	 * glClear呼び出しのためのヘルパー関数
	 * @param color
	 * @param need_swap
	 * @return
	 */
	int clear(const int &color, const bool &need_swap = false);

	/**
	 * 描画サイズを更新する
	 */
	void updateWindowSize();
	[[nodiscard]]
	inline int32_t width() const override { return window_width; };
	[[nodiscard]]
	inline int32_t height() const override { return window_height; };
};

using GLSurfaceSp = std::shared_ptr<GLSurface>;
using GLSurfaceUp = std::unique_ptr<GLSurface>;

//================================================================================
/**
 * 同期オブジェクトのラッパー
 *
 *  eglCreateSyncKHRで<type>として渡す値 and eglGetSyncAttribKHRで<type>として返ってくる値
 *  EGL_SYNC_FENCE_KHR					= 0x30F9
 *  EGL_SYNC_REUSABLE_KHR				= 0x30FA
 *
 *  eglGetSyncAttribKHRで<attribute>の値として渡す値
 *  EGL_SYNC_TYPE_KHR 					= 0x30F7
 *  EGL_SYNC_STATUS_KHR					= 0x30F1
 *  EGL_SYNC_CONDITION_KHR				= 0x30F8
 *
 *  eglGetSyncAttribKHRを<attribute>の値としてEGL_SYNC_STATUS_KHRを指定して呼び出したときに<value>に返ってくる値
 *  EGL_SIGNALED_KHR					= 0x30F2
 *  EGL_UNSIGNALED_KHR					= 0x30F3
 *
 * eglGetSyncAttribKHRを<attribute>の値としてEGL_SYNC_CONDITION_KHRを指定して呼び出したときに<value>に返ってくる値
 * EGL_SYNC_PRIOR_COMMANDS_COMPLETE_KHR	= 0x30F0
 *
 * eglClientWaitSyncKHRの<flags>へ指定する値
 * EGL_SYNC_FLUSH_COMMANDS_BIT_KHR		= 0x0001
 * eglClientWaitSyncKHRの<timeout>へ指定する値
 * EGL_FOREVER_KHR						= 0xFFFFFFFFFFFFFFFFl
 *
 * eglClientWaitSyncKHRから返ってくる値
 * EGL_TIMEOUT_EXPIRED_KHR				= 0x30F5
 * EGL_CONDITION_SATISFIED_KHR			= 0x30F6
 *
 * eglCreateSyncKHRでエラーの時に返す値
 * EGL_NO_SYNC_KHR						= 0
 */
class EglSync {
private:
	const EGLBase *m_egl;
	EGLSyncKHR m_sync;
protected:
public:
	explicit EglSync(const EGLBase *egl, int fence_fd = -1);
	~EglSync() noexcept;

	[[nodiscard]]
	inline EGLSyncKHR sync() const { return m_sync; };

	/**
	 * 同期オブジェクトのファイルディスクリプタを複製する
	 * @return
	 */
	int dup();
	/**
	 * 同期オブジェクトがシグナル状態になるのを待機する
	 * @param timeout_ns 0なら同期オブジェクトの現在の状態をチェックするだけ,
	 *                   EGL_FOREVER_KHRなら無限待ち
	 * @return 0: シグナル状態になった, 1:タイムアウトした, それ以外(負数)：エラー
	 */
	int wait_sync(const EGLTimeKHR &timeout_ns = EGL_FOREVER_KHR);
	/**
	 * 同期オブジェクトをシグナル状態をセットする
	 * FIXME EGL_SYNC_REUSABLE_KHR以外でeglCreateSyncKHRを呼び出すとeglSignalSyncKHRがエラーになる
	 *       (EGL_SYNC_NATIVE_FENCE_ANDROIDでもだめ)
	 * @param reset デフォルトfalse, false: シグナル状態にする, true: シグナル状態を解除する
	 * @return
	 */
	int signal(const bool &reset = false);
};

//================================================================================
/**
 * レンダリングコンテキストの保存復帰を行うヘルパークラス
 * 生成時に保存、スコープを抜けて破棄されるときに復帰させる
 */
class EglContextSaver {
private:
	EGLContext context;
	EGLDisplay display;
	EGLSurface drawSurface;
	EGLSurface readSurface;
public:
	EglContextSaver();
	~EglContextSaver() noexcept;
};

//================================================================================
#if defined(__ANDROID__)
/**
 * AHardwareBufferとメモリーを共有するEGLImageKHRを生成してGL|ESのテクスチャとして
 * アクセスできるようにするためのヘルパークラス
 */
class EglImageWrapper : virtual public IGLSurface {
private:
	/**
	 * 使用するテクスチャの種類GL_TEXTURE_2D/GL_TEXTURE_EXTERNAL_OES
	 */
	const GLuint m_tex_target;
	/**
	 * テクスチャユニット
	 */
	const GLenum m_tex_unit;
	/**
	 * テクスチャIDを自前で生成したかどうか
	 * コンストラクタへ引き渡したテクスチャIDが0の時には内部で
	 * テクスチャIDを生成するのでtrue
	 */
	const bool own_tex_id;
	/**
	 * EglImageWrapperが利用可能かどうか
	 * AHardwareBuffer_xxxとeglGetNativeClientBufferANDROIDが使用可能な場合にtrueになる
	 */
	bool m_supported;
	/**
	 * テクスチャID
	 */
	GLuint m_tex_id;
	/**
	 * ラップしているAHardwareBufferオブジェクト
	 */
	AHardwareBuffer *m_buffer;
	/**
	 * AHardwareBufferとメモリーを共有してテクスチャとしてアクセスできるようにするための
	 * EGLImageKHRオブジェクト
	 */
	EGLImageKHR m_egl_image;
	/**
	 * AHardwareBufferをラップしている間のみ有効
	 * #wrap実行時にAAHardwareBuffer_describeで更新する
	 */
	AHardwareBuffer_Desc m_desc;
	/**
	 * テクスチャ変換行列
	 */
	GLfloat m_tex_matrix[16]{};
	/**
	 * API16だとeglGetNativeClientBufferANDROIDは動的にリンクしないとビルドが通らないので動的にリンク
	 */
	PFNEGLGETNATIVECLIENTBUFFERANDROIDPROC dynamicEglGetNativeClientBufferANDROID;
protected:
public:
	/**
	 * コンストラクタ
	 * @param tex_target
	 * param tex_unit
	 * @param tex_id GL_NO_TEXTURE(0)ならテクスチャIDを内部で自動生成する, デフォルトGL_NO_TEXTURE(0)
	 */
	EglImageWrapper(
		const GLenum &tex_target,
		const GLenum &tex_unit,
		const GLuint &tex_id = GL_NO_TEXTURE);
	/**
	 * デストラクタ
	 */
	~EglImageWrapper() noexcept override;

	/**
	 * 対応しているかどうか
	 * @return
	 */
	[[nodiscard]]
	inline bool is_supported() const { return m_supported; };
	/**
	 * AHardwareBufferをラップしていて使用可能かどうか
	 * AHardwareBuffer_xxxとeglGetNativeClientBufferANDROIDが使用可能な場合にtrueを返す
	 * @return
	 */
	[[nodiscard]]
	inline bool is_wrapped() const { return m_buffer && m_egl_image; };
	/**
	 * テクスチャターゲットを取得
	 * @return
	 */
	[[nodiscard]]
	inline GLenum tex_target() const { return m_tex_target; };
	/**
	 * テクスチャユニットを取得
	 * @return
	 */
	[[nodiscard]]
	inline GLenum tex_unit() const { return m_tex_unit; };
	/**
	 * テクスチャIDを取得
	 * @return
	 */
	[[nodiscard]]
	inline GLuint tex_id() const { return m_tex_id; };
	/**
	 * #wrap実行時にAAHardwareBuffer_describeで取得した
	 * AHardwareBuffer_Desc構造体を返す
	 * @return
	 */
	[[nodiscard]]
	inline const AHardwareBuffer_Desc &description() const { return m_desc; };
	/**
	 * AHardwareBufferのフォーマットを取得
	 * AHardwareBufferをラップしている間のみ有効
	 * #wrap実行時にAAHardwareBuffer_describeで取得した
	 * AHardwareBuffer_Desc構造体のformatフィールドを返す
	 * @return
	 */
	[[nodiscard]]
	inline uint32_t format() const { return m_desc.format; };
	/**
	 * テクスチャサイズ(幅)を取得
	 * AHardwareBufferをラップしている間のみ有効
	 * #wrap実行時にAAHardwareBuffer_describeで取得した
	 * AHardwareBuffer_Desc構造体のwidthフィールドを返す
	 * @return
	 */
	[[nodiscard]]
	inline int32_t width() const override { return (int32_t)m_desc.width; };
	/**
	 * テクスチャサイズ(高さ)を取得
	 * AHardwareBufferをラップしている間のみ有効
	 * #wrap実行時にAAHardwareBuffer_describeで取得した
	 * AHardwareBuffer_Desc構造体のheightフィールドを返す
	 * @return
	 */
	[[nodiscard]]
	inline int32_t height() const override { return  (int32_t)m_desc.height; };
	/**
	 * ストライドを取得
	 * #wrap実行時にAAHardwareBuffer_describeで取得した
	 * AHardwareBuffer_Desc構造体のstrideフィールドを返す
	 * @return
	 */
	[[nodiscard]]
	inline uint32_t stride() const { return m_desc.stride; };
	/**
	 * テクスチャ変換行列を取得
	 * widthとstrideに応じて幅方向のスケーリングをした変換行列を返す
	 * @return
	 */
	[[nodiscard]]
	inline const GLfloat *tex_matrix() const { return m_tex_matrix; }
	
	/**
	 * AHardwareBufferとメモリーを共有するEGLImageKHRを生成して
	 * テクスチャとして利用できるようにする、
	 * 正常にラップできるとテクスチャにbindした状態で返る
	 * @param buffer
	 * @return 0: 正常終了, それ以外: エラー
	 */
	int wrap(AHardwareBuffer *buffer);
	/**
	 * wrapで生成したEGLImageKHRを解放、AHardwareBufferの参照も解放する
	 * @return
	 */
	int unwrap();

	/**
	 * テクスチャをバインド
	 * @return
	 */
	int bind() override;
	/**
	 * テクスチャのバインドを解除
	 * @return
	 */
	int unbind() override;
};
#endif

using EGLBaseSp = std::shared_ptr<EGLBase>;
using EGLBaseUp = std::unique_ptr<EGLBase>;
using GLSurfaceSp = std::shared_ptr<GLSurface>;
using GLSurfaceUp = std::unique_ptr<GLSurface>;
using EglSyncSp = std::shared_ptr<EglSync>;
using EglSyncUp = std::unique_ptr<EglSync>;
#if defined(__ANDROID__)
using EglImageWrapperSp = std::shared_ptr<EglImageWrapper>;
using EglImageWrapperUp = std::unique_ptr<EglImageWrapper>;
#endif

}	// namespace serenegiant::egl

#endif /* EGLBASE_H_ */
