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

#ifndef AANDUSB_UNITY_UVC_HOLDER_H
#define AANDUSB_UNITY_UVC_HOLDER_H

// 標準ライブラリ
#include <memory>
#include <vector>
#include <GLES3/gl31.h>
// android
#include <media/NdkImage.h>	// AImage
// aandusb-native
#include "aandusb_native.h"
// unity plugin
#include "unity_defs.h"

//--------------------------------------------------------------------------------
// 外部クラスの前方宣言
namespace serenegiant {
namespace egl {
class EglImageWrapper;
}
namespace gl {
class GLRenderer;
class GLOffScreen;
}
namespace media {
class ImageReader;
class SurfaceTexture;
}
namespace vulkan {
class SVkContext;
class SVkTexture;
}
}
// 外部クラスの前方宣言ここまで
//--------------------------------------------------------------------------------

namespace serenegiant::unity {

//--------------------------------------------------------------------------------
/**
 * UVC機器1つ分の設定・映像取得/描画処理用のオブジェクトを保持するクラス
 */
class UnityUVCHolder {
private:
	const int32_t m_device_id;
	const int m_gl_version;
	usb_manager_t *m_manager;
	video_size_t m_current_size;
	std::vector<const video_size_t> m_supported_size;
	std::vector<uint64_t> m_supported_ctrls;
	/**
	 * 対応しているUVC設定機能一覧を更新する
	 */
	void update_supported_ctrls();
protected:
public:
	/**
	 * コンストラクタ
	 * @param manager
	 * @param device_id
	 * @param gl_version OpenGLのバージョン, OpenGL3.2なら320
	 * @param frame_type
	 * @param width
	 * @param height
	 */
	UnityUVCHolder(
		usb_manager_t *manager, const int32_t &device_id,
		const int &gl_version,
		const raw_frame_t &frame_type,
		const uint32_t &width, const uint32_t &height);
	/**
	 * デストラクタ
	 */
	virtual ~UnityUVCHolder() noexcept;

	[[nodiscard]]
	inline int gl_version() const { return m_gl_version; };

	/**
	 * 対応解像度一覧を取得
	 * @return
	 */
	[[nodiscard]]
	inline const std::vector<const video_size_t> &supported_size() const { return m_supported_size; };

	/**
	 * 対応しているUVC設定機能一覧を取得する
	 * @return
	 */
	[[nodiscard]]
	inline const std::vector<uint64_t> &supported_ctrls() const { return m_supported_ctrls; };

	[[nodiscard]]
	inline raw_frame_t frame_type() const { return (raw_frame_t)m_current_size.frame_type; };

	[[nodiscard]]
	inline uint32_t width() const { return m_current_size.width; };

	[[nodiscard]]
	inline uint32_t height() const { return m_current_size.height; };

	[[nodiscard]]
	bool is_running() const;

	int set_config(const int32_t &enabled, const bool &use_first_config);

	uint64_t get_ctrl_supports();

	uint64_t get_proc_supports();

	/**
	 * UVC設定機能の情報を取得
	 * @param info
	 * @return 0: 成功, 負: エラーコード
	 */
	int get_control_info(control_info_t &info) const;

	/**
	 * UVC設定機能へ値を適用
	 * @param type
	 * @param value
	 * @return 0: 成功, 負: エラーコード
	 */
	[[nodiscard]]
	int set_control_value(const uint64_t &type, const int32_t &value) const;

	/**
	 * UVC設定機能の現在の値を取得
	 * @param type
	 * @param value
	 * @return 0: 成功, 負: エラーコード
	 */
	int get_control_value(const uint64_t &type, int32_t &value) const;

	/**
	 * UVC機器からの映像を受け取るためのSurface(ANativeWindow*)をセット
	 * @param preview_window
	 * @param mvp_matrix モデルビュー変換行列、要素数16以上, nullptrなら単位行列をセットする
	 * @return
	 */
	int set_preview_surface(ANativeWindow *preview_window, const GLfloat *mvp_matrix = nullptr);

	/**
	 * モデルビュー変換行列を設定
	 * Surface(ANativeWindow)で映像を受け取るときのみ有効
	 * @param mvp_matrix モデルビュー変換行列、要素数16以上, nullptrなら単位行列をセットする
	 * @return
	 */
	int set_mvp_matrix(const GLfloat *mvp_matrix);

	int set_video_size(
		const raw_frame_t &frame_type,
		const uint32_t &width, const uint32_t  &height);

	const video_size_t &get_current_size();

	int get_supported_size(
		const int32_t &index, int32_t *num_supported,
		video_size_t *data) const;

	virtual int start(void *tex, const int32_t &tex_width, const int32_t &tex_height);

	virtual int stop();

	/**
	 * 描画処理実行
	 * イベントループから描画が必要になるたびに呼び出す
	 * 呼び出し元のスレッドでGL|ES/EGLコンテキストを保持していること
	 */
	virtual void on_draw() = 0;
};

typedef std::shared_ptr<UnityUVCHolder> UnityUVCHolderSp;
typedef std::unique_ptr<UnityUVCHolder> UnityUVCHolderUp;

//================================================================================
/**
 * GLESで描画する場合のUnityUVCHolder実装
 */
class UnityUVCHolderGLES : virtual public UnityUVCHolder {
private:
	/**
	 * GLES2/3の時はテクスチャID
	 * start - stop間でのみ有効
	 */
	GLuint m_tex_id_unity;
	std::unique_ptr<egl::EglImageWrapper> m_wrapper;
	std::unique_ptr<media::ImageReader> m_reader;
	std::unique_ptr<gl::GLOffScreen> m_offscreen;
	std::unique_ptr<gl::GLRenderer> m_gl_renderer;
	AImage *m_last_image;
	volatile bool m_first_frame_rendered;

	/**
	 * 映像表示に必要なリソースを開放する
	 */
	void internal_stop();
	/**
	 * ImageReaderが映像を受け取ったときのコールバック関数
	 * @param reader
	 */
	void on_image_available(media::ImageReader &reader);
public:
	/**
	 * コンストラクタ
	 * @param manager
	 * @param device_id
	 * @param gl_version OpenGLのバージョン, OpenGL3.2なら320
	 * @param frame_type
	 * @param width
	 * @param height
	 */
	explicit UnityUVCHolderGLES(
		usb_manager_t *manager, const int32_t &device_id,
		const int &gl_version,
		const raw_frame_t &frame_type = RAW_FRAME_MJPEG,
		const uint32_t &width = 640, const uint32_t &height = 480);

	/**
	 * デストラクタ
	 */
	~UnityUVCHolderGLES() override;

	[[nodiscard]]
	inline GLuint tex_id() const { return m_tex_id_unity; };

	/**
	 * 映像取得を開始
	 * Unity側のレンダーイベントに応じてon_drawを呼び出さないと描画されない
	 * @param tex UnityのGetNativeTexturePtrが返した値(void *だけどGL|ESの時はテクスチャID)
	 * @param tex_width
	 * @param tex_height
	 * @return
	 */
	int start(void *tex, const int32_t &tex_width, const int32_t &tex_height) override;

	/**
	 * 映像取得を終了
	 * @return
	 */
	int stop() override;

	/**
	 * 描画実行
	 * Unityのレンダリングコンテキスト内でレンダリングイベントから呼ばれる
	 */
	void on_draw() override;
};

//================================================================================
/**
 * Vulkanで描画する場合のUnityUVCHolder実装
 * 現在の実装では、
 * ・GLWorkerを使ってEGL|GLコンテキストを生成
 * ・ハードウエアバッファを使ってEGL|GLテクスチャとVulkanのテクスチャを共有
 * ・ワーカースレッド上でUVC機器からの映像をGLESのテクスチャへオフスクリーン描画
 * ・UnityのレンダリングイベントでVulkan側の共有テクスチャをUnityのテクスチャへコピー
 */
class UnityUVCHolderVulkan : virtual public UnityUVCHolder {
private:
	/**
	 * なんかわからんけどGetNativeTexturePtrが返した整数値
	 * start - stop間でのみ有効
	 */
	void *m_tex_id_unity;
	IUnityGraphicsVulkan *m_unity_graphics_vulkan;
	UnityVulkanInstance m_unity_vulkan_instance;
	std::shared_ptr<vulkan::SVkContext> m_svk_context;
	std::unique_ptr<media::ImageReader> m_reader;
	volatile bool m_first_frame_rendered;

	/**
	 * ImageReaderから映像を取得できないときにVulkanのテクスチャへ何もコピーしないと
	 * 透明になってチラつくので最後の映像を保持してコピーできるようにする
	 */
	std::shared_ptr<vulkan::SVkTexture> m_vk_last_texture;
	AImage *m_last_image;
	/**
	 * ImageReaderが映像を受け取ったときのコールバック関数
	 * @param reader
	 */
	void on_image_available(media::ImageReader &reader);

	/**
	 * 映像表示に必要なリソースを開放する
	 */
	void internal_stop();
public:
	/**
	 * コンストラクタ
	 * @param manager
	 * @param device_id
	 * @param gl_version OpenGLのバージョン, OpenGL3.2なら320
	 * @param unity_graphics_vulkan
	 * @param unity_vulkan_instance
	 * @param frame_type
	 * @param width
	 * @param height
	 */
	UnityUVCHolderVulkan(
		usb_manager_t *manager, const int32_t &device_id,
		const int &gl_version,
		IUnityGraphicsVulkan *unity_graphics_vulkan,
		const UnityVulkanInstance &unity_vulkan_instance,
		const raw_frame_t &frame_type = RAW_FRAME_MJPEG,
		const uint32_t &width = 640, const uint32_t &height = 480);

	/**
	 * デストラクタ
	 */
	~UnityUVCHolderVulkan() override;

	[[nodiscard]]
	inline void *tex_id() const { return m_tex_id_unity; };

	/**
	 * 描画処理を実行可能かどうかを取得する
	 * @return
	 */
	[[nodiscard]]
	inline bool is_ready() const {
		return is_running() && m_reader && m_first_frame_rendered;
	}

	/**
	 * 映像取得を開始
	 * Unity側のレンダーイベントに応じてon_drawを呼び出さないと描画されない
	 * @param tex UnityのGetNativeTexturePtrが返した値
	 * @param tex_width
	 * @param tex_height
	 * @return
	 */
	int start(void *tex, const int32_t &tex_width, const int32_t &tex_height) override;

	/**
	 * 映像取得を終了
	 * @return
	 */
	int stop() override;

	/**
	 * 描画処理実行
	 * UVCHolderBaseの純粋仮想関数を実装しているけどこのクラスでは使わない
	 */
	void on_draw() override;
};

} // namespace serenegiant::unity
#endif //AANDUSB_UNITY_UVC_HOLDER_H
