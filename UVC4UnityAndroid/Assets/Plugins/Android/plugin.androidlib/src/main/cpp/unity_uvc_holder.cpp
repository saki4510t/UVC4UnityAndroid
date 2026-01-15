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

#define LOG_TAG "UnityUVCHolder"

#if 1	// デバッグ情報を出さない時は1
	#ifndef LOG_NDEBUG
		#define	LOG_NDEBUG		// LOGV/LOGD/MARKを出力しない時
	#endif
	#undef USE_LOGALL			// 指定したLOGxだけを出力
#else
//	#define USE_LOGALL
	#define USE_LOGD
	#undef LOG_NDEBUG
	#undef NDEBUG
#endif

#define COUNT_FRAMES (0)
#define MEAS_TIME (0)				// 1フレーム当たりの描画時間を測定する時1

// aandusb
#include "utilbase.h"
// common
#include "common/eglbase.h"
#include "common/gloffscreen.h"
#include "common/glrenderer.h"
#include "common/gltexture.h"
// gl
#include "gl/texture_vsh.h"
#include "gl/rgba_fsh.h"
// media
#include "media/image_reader.h"
#include "media/surface_texture.h"
#include "media/surface_texture_stub.h"
// vulkan
#include "vulkan/svk_context.h"
#include "vulkan/svk_texture.h"
#include "vulkan/svk_utils.h"
// unity plugin
#include "unity_uvc_holder.h"


//--------------------------------------------------------------------------------
#if MEAS_TIME
#define MEAS_TIME_INIT	static nsecs_t _meas_time_ = 0;\
	static nsecs_t _init_time_ = systemTime();\
	static int _meas_count_ = 0;
#define MEAS_TIME_START	const nsecs_t _meas_t_ = systemTime();
#define MEAS_TIME_STOP \
	_meas_time_ += (systemTime() - _meas_t_); \
	_meas_count_++; \
	if (UNLIKELY((_meas_count_ % 100) == 0)) { \
		const float d = _meas_time_ / (1000000.f * _meas_count_); \
		const float fps = _meas_count_ * 1000000000.f / (systemTime() - _init_time_); \
		LOGI("draw time=%5.2f[msec]/fps=%5.2f", d, fps); \
	}
#define MEAS_RESET \
	_meas_count_ = 0; \
	_meas_time_ = 0; \
	_init_time_ = systemTime();
#else
#define MEAS_TIME_INIT
#define MEAS_TIME_START
#define MEAS_TIME_STOP
#define MEAS_RESET
#endif

namespace serenegiant::unity {

/**
 * コンストラクタ
 * @param gl_version OpenGLのバージョン, OpenGL3.2なら320
 * @param frame_type
 * @param width
 * @param height
 */
UnityUVCHolder::UnityUVCHolder(
	usb_manager_t *manager, const int32_t &device_id,
	const int &gl_version,
	const raw_frame_t &frame_type,
	const uint32_t &width, const uint32_t &height)
:	m_manager(manager),
	m_device_id(device_id),
	m_gl_version(gl_version),
	m_current_size(),
	m_supported_size(),
	m_supported_ctrls()
{
	ENTER();

	uvc_resize(m_manager, m_device_id, frame_type, width, height);

	// 対応解像度一覧を取得する
	int32_t num_supported = 0;
	auto r = uvc_get_supported_size(manager, m_device_id, 0, &num_supported, nullptr);
	if (!r && num_supported) {
		video_size_t size;
		for (int32_t i = 0; i < num_supported; i++) {
			r = uvc_get_supported_size(manager, m_device_id, i, &num_supported, &size);
			if (!r) {
				m_supported_size.push_back(size);
				LOGD("video_size_t(type=0x%08x,ix=%d,%dx%d)", size.frame_type, size.frame_index, size.width, size.height);
			} else {
				LOGE("Failed to get supported size,err=%d", r);
				break;
			}
		}
	} else {
		LOGE("Failed to get supported size,err=%d", r);
	}
	LOGD("num_supported=%d,added=%" FMT_SIZE_T, num_supported, m_supported_size.size());

	get_current_size();
	update_supported_ctrls();

	EXIT();
}

/**
 * デストラクタ
 */
UnityUVCHolder::~UnityUVCHolder() noexcept {
	ENTER();

	uvc_stop(m_manager, m_device_id);

	EXIT();
}

bool UnityUVCHolder::is_running() const {
	ENTER();

	const auto state = uvc_get_device_state(m_manager, m_device_id);

	RETURN(state > CONNECTED , bool);
}

int UnityUVCHolder::set_config(const int32_t &enabled, const bool &use_first_config) {
	ENTER();
	RETURN(uvc_set_config(m_manager, m_device_id, enabled, use_first_config), int);
}

uint64_t UnityUVCHolder::get_ctrl_supports() {
	ENTER();
	RETURN(uvc_get_ctrl_supports(m_manager, m_device_id), uint64_t);
}

uint64_t UnityUVCHolder::get_proc_supports() {
	ENTER();
	RETURN(uvc_get_proc_supports(m_manager, m_device_id), uint64_t);
}

/**
 * UVC設定機能の情報を取得
 * @param info
 * @return 0: 成功, 負: エラーコード
 */
int UnityUVCHolder::get_control_info(control_info_t &info) const {
	ENTER();
	RETURN(uvc_get_control_info(m_manager, m_device_id, &info), int);
}

/**
 * UVC設定機能へ値を適用
 * @param type
 * @param value
 * @return 0: 成功, 負: エラーコード
 */
int UnityUVCHolder::set_control_value(const uint64_t &type, const int32_t &value) const {
	ENTER();
	RETURN(uvc_set_control_value(m_manager, m_device_id, type, value), int);
}

/**
 * UVC設定機能の現在の値を取得
 * @param type
 * @param value
 * @return 0: 成功, 負: エラーコード
 */
int UnityUVCHolder::get_control_value(const uint64_t &type, int32_t &value) const {
	ENTER();
	RETURN(uvc_get_control_value(m_manager, m_device_id, type, &value), int);
}

/**
 * UVC機器からの映像を受け取るためのSurface(ANativeWindow*)をセット
 * @param preview_window
 * @param mvp_matrix モデルビュー変換行列、要素数16以上, nullptrなら単位行列をセットする
 * @return
 */
int UnityUVCHolder::set_preview_surface(ANativeWindow *preview_window, const GLfloat *mvp_matrix) {
	ENTER();
	RETURN(uvc_set_surface(m_manager, m_device_id, preview_window, const_cast<float *>(mvp_matrix)), int);
}

/**
 * モデルビュー変換行列を設定
 * Surface(ANativeWindow)で映像を受け取るときのみ有効
 * @param mvp_matrix モデルビュー変換行列、要素数16以上, nullptrなら単位行列をセットする
 * @return
 */
int UnityUVCHolder::set_mvp_matrix(const GLfloat *mvp_matrix) {
	ENTER();
	RETURN(uvc_set_mvp_matrix(m_manager, m_device_id, const_cast<float *>(mvp_matrix)), int);
}

int UnityUVCHolder::set_video_size(
	const raw_frame_t &frame_type,
	const uint32_t &width, const uint32_t  &height) {

	ENTER();
	auto r = uvc_resize(m_manager, m_device_id, frame_type, width, height);
	get_current_size();
	RETURN(r, int);
}

const video_size_t &UnityUVCHolder::get_current_size() {
	ENTER();

	const auto r = uvc_get_current_size(m_manager, m_device_id, &m_current_size);
	if (UNLIKELY(r)) {
		LOGD("uvc_get_current_size failed, err=%d", r);
	}

	RET(m_current_size);
}

int UnityUVCHolder::get_supported_size(
	const int32_t &index, int32_t *num_supported,
	video_size_t *data) const {

	ENTER();
	const auto &supported = supported_size();
	const auto num = static_cast<int32_t>(supported.size());
	if (num_supported) {
		*num_supported = num;
	}
	int result = 1;
	if ((index >= 0) && (index < num)) {
		if (data) {
			*data = supported[index];
		}
		result = 0;
	}

	RETURN(result, int);
}

int UnityUVCHolder::start(void *tex, const int32_t &tex_width, const int32_t &tex_height) {
	ENTER();
	RETURN(uvc_start(m_manager, m_device_id), int);
}

int UnityUVCHolder::stop() {
	ENTER();
	RETURN(uvc_stop(m_manager, m_device_id), int);
}

/**
 * 対応しているUVC設定機能一覧を更新する
 */
/*private*/
void UnityUVCHolder::update_supported_ctrls() {
	ENTER();

	const auto ctrls = uvc_get_ctrl_supports(m_manager, m_device_id);
	const auto procs = uvc_get_proc_supports(m_manager, m_device_id);

	m_supported_ctrls.clear();
	for (int i = 0; i < 32; i++) {
		const uint64_t type = 0x00000001llu << i;
		if ((ctrls & type) == type) {
			if (type == (CTRL_PAN_ABS & 0x00ffffff)) {
				// PAN/TILTだけ特別扱いが必要(UVC規格場はPANとTILT2つ合わせて１つの設定項目)
				m_supported_ctrls.push_back(CTRL_PAN_ABS);
				m_supported_ctrls.push_back(CTRL_TILT_ABS);
			} else {
				m_supported_ctrls.push_back(type);
			}
		}
		if ((procs && type) == type) {
			m_supported_ctrls.push_back(type | PU_MASK);
		}
	}
	// 昇順にソート
	std::sort(m_supported_ctrls.begin(), m_supported_ctrls.end());

	EXIT();
}

//================================================================================
/**
 * コンストラクタ
 * @param manager
 * @param device_id
 * @param gl_version
 * @param frame_type
 * @param width
 * @param height
 */
/*public*/
UnityUVCHolderGLES::UnityUVCHolderGLES(
	usb_manager_t *manager, const int32_t &device_id,
	const int &gl_version,
	const raw_frame_t &frame_type,
	const uint32_t &width, const uint32_t &height)
:	UnityUVCHolder(manager, device_id, gl_version, frame_type, width, height),
	m_tex_id_unity(0),
	m_wrapper(nullptr),
	m_reader(nullptr),
	m_last_image(nullptr),
	m_gl_renderer(nullptr),
	m_offscreen(nullptr),
	m_first_frame_rendered(false)
{

	ENTER();
	EXIT();
}
/**
 * デストラクタ
 */
/*public*/
UnityUVCHolderGLES::~UnityUVCHolderGLES() {
	ENTER();

	internal_stop();

	EXIT();
}

/**
 * 映像取得を開始
 * Unity側のレンダーイベントに応じてon_drawを呼び出さないと描画されない
 * @param tex UnityのGetNativeTexturePtrが返した値(void *だけどGL|ESの時はテクスチャID)
 * @param tex_width
 * @param tex_height
 * @return
 */
int UnityUVCHolderGLES::start(
	void *tex,
	const int32_t &tex_width, const int32_t &tex_height) {

	ENTER();

	int result = 0;
	if (!is_running()) {
		m_tex_id_unity = (GLuint)(size_t)tex;
		LOGD("tex=%d", m_tex_id_unity);
		m_first_frame_rendered = false;
		result = UnityUVCHolder::start(tex, tex_width, tex_height);
		if (!m_reader) {
			LOGD("create ImageReader");
			m_reader = std::make_unique<media::ImageReader>(
				width(), height(),
				AIMAGE_FORMAT_RGBA_8888,
				AHARDWAREBUFFER_USAGE_GPU_SAMPLED_IMAGE,
				4);
			// 初回映像取得を検知できるようにコールバックをセット
			m_reader->set_on_image_available([this](media::ImageReader &reader){
				this->on_image_available(reader);
			});
			LOGD("set preview surface with mvp matrix");
			GLfloat matrix[16];
			gl::setIdentityMatrix(matrix, 0);
			set_preview_surface(m_reader->native_window(), matrix);
		}
	}

	RETURN(result, int);
}

/**
 * 映像取得を終了
 * @return
 */
/*public*/
int UnityUVCHolderGLES::stop() {
	ENTER();

	UnityUVCHolder::stop();
	internal_stop();

	RETURN(0, int);
}

/**
 * 描画実行
 * Unityのレンダリングコンテキスト内でレンダリングイベントから呼ばれる
 */
/*public*/
void UnityUVCHolderGLES::on_draw() {
//	ENTER();

	if (LIKELY(is_running())) {
		egl::EglContextSaver saver; // レンダリングコンテキストを自動保存自動復帰
		// ImageReaderを使って映像を受け取るとき, API>=26
		auto tex = tex_id();
		if (UNLIKELY(!m_offscreen)) {
			MARK("create GLOffScreen");
			// FBOを使ってUnityからのテクスチャをバックバッファとしたオフスクリーンを生成する
			m_offscreen = std::make_unique<gl::GLOffScreen>(gl::GLTexture::wrap(
				GL_TEXTURE_2D, GL_TEXTURE1, tex,
				(int32_t)width(), (int32_t)height()));
		}
		if (UNLIKELY(!m_wrapper)) {
			m_wrapper = std::make_unique<egl::EglImageWrapper>(
				GL_TEXTURE_EXTERNAL_OES, GL_TEXTURE1);
		}
		if (UNLIKELY(!m_gl_renderer)) {
			if (gl_version() >= 300) {
				MARK("create GLRenderer GLES3");
				m_gl_renderer = std::make_unique<gl::GLRenderer>(
					texture_gl3_vsh, rgba_gl3_ext_fsh,
					true);
			} else {
				MARK("create GLRenderer GLES2");
				m_gl_renderer = std::make_unique<gl::GLRenderer>(
					texture_gl2_vsh, rgba_gl2_ext_fsh,
					true);
			}
		}

		auto image = m_reader->get_latest_image();
		if (image) {
			AHardwareBuffer *buffer = nullptr;
			media_status_t status = AAImage_getHardwareBuffer(image, &buffer);    // API>=26
			if (LIKELY((status == AMEDIA_OK) && buffer)) {
				AAHardwareBuffer_acquire(buffer);    // API>=26
				{    // AHardwareBuffer -> EglImageWrapperでラップ
					m_wrapper->unwrap();
					if (m_last_image) {
						m_reader->delete_image(m_last_image);
					}
					m_last_image = image;
					m_wrapper->wrap(buffer);    // これはテクスチャにbindした状態になる
					m_offscreen->bind();
					{
						// テクスチャをバックバッファとしたオフスクリーンへEglImageWrapperをGLESで描画
						m_gl_renderer->draw(m_wrapper.get(), m_wrapper->tex_matrix());
					}
					m_offscreen->unbind();
				}
				AAHardwareBuffer_release(buffer);			// API>=26
				// ここではimageを解放しない
			} else {
				m_reader->delete_image(image);
				LOGW("AAImage_getHardwareBuffer failed,%d", status);
			}
		}
	} else {
		LOGW("No source pipeline");
	}

	//	EXIT();
}

//--------------------------------------------------------------------------------
/*private*/
void UnityUVCHolderGLES::internal_stop() {
	ENTER();

	m_gl_renderer.reset();
	if (m_last_image) {
		AAImage_delete(m_last_image);
		m_last_image = nullptr;
	}
	if (m_offscreen) {
		MARK("release offscreen");
		// XXX GLOffScreenのデストラクタで外部GLTextureを破棄しないように変更したので
		//     ラップしたGLTextureを自前で破棄する
		auto *texture = m_offscreen ? m_offscreen->getOffscreen() : nullptr;
		SAFE_DELETE(texture);
		m_offscreen.reset();
	}

	EXIT();
}

/*private*/
void UnityUVCHolderGLES::on_image_available(media::ImageReader &reader) {
	ENTER();

	static int cnt = 0;
	MARK("on_image_available:%d", ++cnt);
	// 2回目以降は呼び出されないようにコールバックをクリア
	reader.set_on_image_available(nullptr);
	m_first_frame_rendered = true;

	EXIT();
}

//================================================================================
/**
 * コンストラクタ
 * @param manager
 * @param device_id
 * @param unity_graphics_vulkan
 * @param unity_vulkan_instance
 * @param frame_type
 * @param width
 * @param height
 */
/*public*/
UnityUVCHolderVulkan::UnityUVCHolderVulkan(
	usb_manager_t *manager, const int32_t &device_id,
	const int &gl_version,
	IUnityGraphicsVulkan *unity_graphics_vulkan,
	const UnityVulkanInstance &unity_vulkan_instance,
	const raw_frame_t &frame_type,
	const uint32_t &width, const uint32_t &height)
:	UnityUVCHolder(manager, device_id, gl_version, frame_type, width, height),
	m_unity_graphics_vulkan(unity_graphics_vulkan),
	m_unity_vulkan_instance(unity_vulkan_instance),
	m_tex_id_unity(nullptr),
	m_reader(nullptr),
	m_vk_last_texture(nullptr),
	m_last_image(nullptr),
	m_first_frame_rendered(false)
{
	ENTER();
	EXIT();
}
/**
 * デストラクタ
 */
/*public*/
UnityUVCHolderVulkan::~UnityUVCHolderVulkan() {
	ENTER();

	internal_stop();

	EXIT();
}

/**
 * 映像取得を開始
 * Unity側のレンダーイベントに応じてon_drawを呼び出さないと描画されない
 * @param context
 * @param connector
 * @param tex UnityのGetNativeTexturePtrが返した値(void *だけどGL|ESの時はテクスチャID)
 * @param tex_width
 * @param tex_height
 * @return
 */
/*virtual,public*/
int UnityUVCHolderVulkan::start(
	void *tex,
	const int32_t &tex_width, const int32_t &tex_height) {
	ENTER();

	auto result = UnityUVCHolder::start(tex, tex_width, tex_height);
	// Vulkan関係のテクスチャを初期化
	if (!m_svk_context) {
		m_tex_id_unity = tex;
		m_first_frame_rendered = false;
		LOGD("wrap SVkContext");
		auto vk_instance = m_unity_vulkan_instance.instance;
		auto vk_physical_device = m_unity_vulkan_instance.physicalDevice;
		auto vk_device = m_unity_vulkan_instance.device;
		m_svk_context = std::make_shared<vulkan::SVkContext>(vk_instance, vk_physical_device, vk_device);
	}
	if (!m_reader) {
		LOGD("create ImageReader");
		m_reader = std::make_unique<media::ImageReader>(
			width(), height(),
			AIMAGE_FORMAT_RGBA_8888,
			AHARDWAREBUFFER_USAGE_GPU_SAMPLED_IMAGE,
			4);
		// 初回映像取得を検知できるようにコールバックをセット
		m_reader->set_on_image_available([this](media::ImageReader &reader){
			this->on_image_available(reader);
		});
		LOGD("set preview surface with mvp matrix");
		GLfloat matrix[16];
		gl::setIdentityMatrix(matrix, 0);
		gl::setMirror(matrix, 0, 2); // 上下反転
		set_preview_surface(m_reader->native_window(), matrix);
	}

	RETURN(result, int);
}

/**
 * 映像取得を終了
 * @return
 */
/*public*/
int UnityUVCHolderVulkan::stop() {
	ENTER();

	auto result = UnityUVCHolder::stop();
	internal_stop();

	RETURN(result, int);
}

/**
 * 描画実行
 * Unityのレンダリングコンテキスト内でレンダリングイベントから呼ばれる
 */
/*public*/
void UnityUVCHolderVulkan::on_draw() {
//	ENTER();

	if (!m_svk_context) {
		LOGD("SvkContext is null");
		return;
	}

	UnityVulkanRecordingState recording_state {};
	if (!m_unity_graphics_vulkan->CommandRecordingState(&recording_state, kUnityVulkanGraphicsQueueAccess_DontCare)) {
		LOGD("Failed to get recording state from IUnityGraphicsVulkan::CommandRecordingState");
		return;
	}

	UnityVulkanImage unity_vk_image {};
	if (!m_unity_graphics_vulkan->AccessTexture(
		tex_id(), UnityVulkanWholeImage,
		VK_IMAGE_LAYOUT_GENERAL, VK_PIPELINE_STAGE_TRANSFER_BIT, VK_ACCESS_TRANSFER_READ_BIT,
		kUnityVulkanResourceAccess_PipelineBarrier, &unity_vk_image)) {
		LOGD("Failed to get UnityVulkanImage from IUnityGraphicsVulkan::AccessTexture");
		return;
	}
	if (!unity_vk_image.image) {
		LOGD("Failed to get VkImage from UnityVulkanImage");
		return;
	}

	if (LIKELY(is_ready())) {
		MEAS_TIME_INIT
		MEAS_TIME_START

		const VkImageCopy bltInfo {
			.srcSubresource {
				.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT,
				.mipLevel = 0,
				.baseArrayLayer = 0,
				.layerCount = 1,
			},
			.srcOffset { .x = 0, .y = 0, .z = 0, },
			.dstSubresource {
				.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT,
				.mipLevel = 0,
				.baseArrayLayer = 0,
				.layerCount = 1,
			},
			.dstOffset {.x = 0, .y = 0, .z = 0},
			.extent {
				.width = width(),
				.height = height(),
				.depth = 1,
			},
		};

		bool rendered = false;
		auto image = m_reader->get_latest_image();
		if (image) {
			AHardwareBuffer *buffer = nullptr;
			auto status = AAImage_getHardwareBuffer(image, &buffer);	// API>=26
			if (LIKELY((status == AMEDIA_OK) && buffer)) {
				if (m_last_image) {
					// 1つ前のAImage*があれば解放する
					AAImage_delete(m_last_image);
				}
				m_last_image = image;
				if (m_vk_last_texture) {
					// 前回の映像が残っていれば削除する
					m_vk_last_texture.reset();
				}
				AAHardwareBuffer_acquire(buffer);	// API>=26
				{
					// AHardwareBuffer -> SVkTexture -> ワンタイムコマンドバッファ生成 -> vkCmdCopyImageでテクスチャへコピーする
					AHardwareBuffer_Desc desc;
					AAHardwareBuffer_describe(buffer, &desc);
					m_vk_last_texture= vulkan::SVkTexture::create_from_AHardwareBuffer(
						m_svk_context, buffer,
						VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
						VK_IMAGE_USAGE_TRANSFER_SRC_BIT);
					// 表示用にUnity側のテクスチャへコピーする
					vkCmdCopyImage(recording_state.commandBuffer,
						m_vk_last_texture->image(), VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
						unity_vk_image.image, VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
						1, &bltInfo);
					rendered = true;
				}
				AAHardwareBuffer_release(buffer);			// API>=26
				// ここではimageを解放しない
			} else {
				m_reader->delete_image(image);
			}
		}
		if (!rendered && m_vk_last_texture) {
			// ImageReaderから映像を取得できなかったが前回の映像が残っていればUnity側のテクスチャへコピーする
			vkCmdCopyImage(recording_state.commandBuffer,
				m_vk_last_texture->image(), VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
				unity_vk_image.image, VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
				1, &bltInfo);
		}

		MEAS_TIME_STOP
#if COUNT_FRAMES
		static int cnt = 0;
		if ((++cnt %100) == 0) {
			LOGI("cnt=%d", cnt);
		}
#endif
#ifndef LOG_NDEBUG
	} else {
		LOGD("Not ready");
#endif
	}

//	EXIT();
}

/*private*/
void UnityUVCHolderVulkan::on_image_available(media::ImageReader &reader) {
	ENTER();

	static int cnt = 0;
	MARK("on_image_available:%d", ++cnt);
	// 2回目以降は呼び出されないようにコールバックをクリア
	reader.set_on_image_available(nullptr);
	m_first_frame_rendered = true;

	EXIT();
}

/**
 * 映像表示に必要なリソースを開放する
 */
/*private*/
void UnityUVCHolderVulkan::internal_stop() {
	ENTER();

	if (m_vk_last_texture) {
		LOGD("release last image texture");
		m_vk_last_texture.reset();
	}
	if (m_last_image) {
		LOGD("release last image");
		AAImage_delete(m_last_image);
	}
	m_last_image = nullptr;
	if (m_reader) {
		LOGD("release image reader");
		m_reader.reset();
	}
	if (m_svk_context) {
		LOGD("release wrapped SVkContext");
		m_svk_context.reset();
	}

	EXIT();
}

} // namespace serenegiant::unity
