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

#define LOG_TAG "UnityUVCPlugin"

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

// aandusb
#include "utilbase.h"
// unity plugin
#include "unity_uvc_holder.h"
#include "unity_uac_holder.h"
#include "unity_uvc_plugin.h"

namespace serenegiant::unity {

extern UnityUVCPlugin *g_unity_plugin;
extern std::unordered_map<int32_t, unity::UnityCallbackWrapperUp> callbacks;

/**
 * コンストラクタ
 * @param callback_id コールバックID
 * @param on_device_changed USB機器の接続状態が変化したときのコールバック関数
 */
UnityCallbackWrapper::UnityCallbackWrapper(int32_t &callback_id,
	unity_on_device_changed on_device_changed)
:	callback_id(callback_id),
	on_device_changed(on_device_changed)
{
	ENTER();
	EXIT();
}

/**
 * USBの機器接続状態が変化したときのunity側のコールバック関数を呼び出す
 * @param connector
 * @param attached
 */
void UnityCallbackWrapper::call(int32_t device_id, bool attached) const {
	ENTER();

	if (on_device_changed) {
		on_device_changed(callback_id, device_id, attached);
	}

	EXIT();
}

//==========
/**
 * コンストラクタ
 * @param unity_interfaces
 * @param unity_graphics
 * @param unity_graphics_type
 */
/*public*/
UnityUVCPlugin::UnityUVCPlugin(
	IUnityInterfaces *unity_interfaces,
	IUnityGraphics *unity_graphics,
 	const UnityGfxRenderer &unity_graphics_type)
:	unity_interfaces(unity_interfaces),
	unity_graphics(unity_graphics),
	unity_graphics_type(unity_graphics_type),
	unity_graphics_vulkan(nullptr),
	unity_vulkan_instance(),
	m_manager(nullptr),
	uvc_holders(), uac_holders()
{
	ENTER();

	LOGD("unity_graphics_type=%d", unity_graphics_type);
	if (unity_graphics_type == kUnityGfxRendererVulkan) {
		LOGD("prepare for vulkan");
		unity_graphics_vulkan = unity_interfaces->Get<IUnityGraphicsVulkan>();
		if (unity_graphics_vulkan) {
			unity_vulkan_instance = unity_graphics_vulkan->Instance();
		}
	}

	m_manager = manager_init(this, on_device_attach, on_device_detach);

	EXIT();
}

/**
 * デストラクタ
 */
/*public*/
UnityUVCPlugin::~UnityUVCPlugin() noexcept {
	ENTER();

	if (m_manager) {
		manager_release(m_manager);
		m_manager = nullptr;
	}

	EXIT();
}

/**
 * 指定したIDに対応するUVC機器が接続されていて利用可能かどうか
 * @param id
 * @return
 */
/*public*/
bool UnityUVCPlugin::is_available(const int32_t &device_id) {
	ENTER();

	bool result = false;
	UnityUVCHolderSp holder = nullptr;
	if (m_lock.try_lock()) {
		holder = get_uvc_holder_locked(device_id, false);
		m_lock.unlock();
	}
	result = holder != nullptr;

	RETURN(result, bool);
}

/**
 * 機器との接続状態を取得
 * @return
 */
/*public*/
device_state_t UnityUVCPlugin::get_device_state(const int32_t &device_id) {
	ENTER();

	device_state result = DISCONNECTED;
	UnityUVCHolderSp holder = nullptr;
	if (m_lock.try_lock()) {
		holder = get_uvc_holder_locked(device_id, false);
		m_lock.unlock();
	}
	result = holder && holder->is_running() ? STREAMING : CONNECTED;

	RETURN(result, device_state);
}

/**
 * UVC機器の基本設定をセット
 * @param device_id
 * @param enabled
 * @param use_first_config
 * @return
 */
/*public*/
int UnityUVCPlugin::set_config(
	const int32_t &device_id,
	const int32_t &enabled, const bool &use_first_config) {

	ENTER();

	int result = -4;
	if (LIKELY(device_id)) {
		UnityUVCHolderSp holder = nullptr;
		if (m_lock.try_lock()) {
			holder = get_uvc_holder_locked(device_id, false);
			m_lock.unlock();
		}
		if (holder) {
			result = holder->set_config(enabled, use_first_config);
		} else {
			LOGD("UnityUVCHolder not found! id=%d", device_id);
		}
	} else {
		LOGW("failed to get DeviceConnector,id=%d", device_id);
	}

	RETURN(result, int);
}

/**
 * UVC機器からの映像サイズの変更要求
 * @param device_id UVC機器識別用のID
 * @param width 映像の幅
 * @param height 映像の高さ
 * @return
 */
/*public*/
int UnityUVCPlugin::resize(const int32_t &device_id,
	const raw_frame_t &frame_type,
	const uint32_t &width, const uint32_t &height) {

	ENTER();

	int result = -4;
	UnityUVCHolderSp holder = nullptr;
	if (m_lock.try_lock()) {
		holder = get_uvc_holder_locked(device_id, false);
		m_lock.unlock();
	}
	if (holder) {
		result = holder->set_video_size(frame_type, width, height);
	} else {
		LOGD("UnityUVCHolder not found! id=%d", device_id);
	}

	RETURN(result, int);
}

/**
 * 映像取得開始
 * レンダーコールバックを呼び出さないと実際には描画されない
 * @param device_id UVC機器識別用のID
 * @param tex
 * @param tex_width
 * @param tex_height
 * @return
 */
/*public*/
int UnityUVCPlugin::start(
	const int32_t &device_id,
	void *tex, const int32_t &tex_width, const int32_t &tex_height) {
	ENTER();
	
	int result = -1;
	UnityUVCHolderSp holder = nullptr;
	if (m_lock.try_lock()) {
		holder = get_uvc_holder_locked(device_id, false);
		m_lock.unlock();
	}
	if (LIKELY(holder)) {
		result = holder->start(tex, tex_width, tex_height);
	} else {
		LOGD("UnityUVCHolder not found! id=%d", device_id);
	}

	RETURN(result, int);
}

/**
 * 映像取得終了
 * @param device_id UVC機器識別用のID
 * @return
 */
/*public*/
int UnityUVCPlugin::stop(const int32_t &device_id) {
	ENTER();

	if (LIKELY(device_id)) {
		UnityUVCHolderSp holder = nullptr;
		if (m_lock.try_lock()) {
			holder = get_uvc_holder_locked(device_id, false);
			m_lock.unlock();
		}
		if (holder) {
			holder->stop();
		} else {
			LOGD("UnityUVCHolder not found! id=%d", device_id);
		}
	}

	RETURN(0, int);
}

/**
 * Unityからのレンダリング要求時の描画処理を行う
 * 今は第2引数は使っていない
 * @param device_id UVC機器識別用のID
 * @param
 */
/*public*/
void UnityUVCPlugin::on_render_event(const int &device_id, void *) {
//	ENTER();

	if (LIKELY(device_id)) {
		UnityUVCHolderSp holder = nullptr;
		if (m_lock.try_lock()) {
			holder = get_uvc_holder_locked(device_id, false);
			m_lock.unlock();
		}
		if (LIKELY(holder)) {
			holder->on_draw();
		} else {
			LOGD("UnityUVCHolder not found! id=%d", device_id);
		}
	}

//	EXIT();
}

/**
 * コントロール機能でサポートしている機能を取得
 * @param device_id
 * @return
 */
/*public*/
uint64_t UnityUVCPlugin::get_ctrl_supports(const int &device_id) {
	ENTER();

	if (LIKELY(device_id)) {
		UnityUVCHolderSp holder = nullptr;
		if (m_lock.try_lock()) {
			holder = get_uvc_holder_locked(device_id, false);
			m_lock.unlock();
		}
		if (holder) {
			return holder->get_ctrl_supports();
		} else {
			LOGD("UnityUVCHolder not found! id=%d", device_id);
		}
	}

	RETURN(0, uint64_t);
}

/**
 * プロセッシングユニットでサポートしている機能を取得
 * @param device_id
 * @return
 */
/*public*/
uint64_t UnityUVCPlugin::get_proc_supports(const int &device_id) {
	ENTER();

	if (LIKELY(device_id)) {
		UnityUVCHolderSp holder = nullptr;
		if (m_lock.try_lock()) {
			holder = get_uvc_holder_locked(device_id, false);
			m_lock.unlock();
		}
		if (holder) {
			return holder->get_proc_supports();
		} else {
			LOGD("UnityUVCHolder not found! id=%d", device_id);
		}
	}

	RETURN(0, uint64_t);
}

/**
 * native側でUVC設定機能へアクセスするときのヘルパー関数
 * 主にUnityやFlutterからのアクセスを想定
 * @param device_id
 * @param info
 * @return 0: 成功, 負: エラーコード
 */
/*public*/
int UnityUVCPlugin::get_control_info(const int &device_id, control_info_t &info) {
	ENTER();

	int result = -4;
	if (LIKELY(device_id)) {
		UnityUVCHolderSp holder = nullptr;
		if (m_lock.try_lock()) {
			holder = get_uvc_holder_locked(device_id, false);
			m_lock.unlock();
		}
		if (holder) {
			result = holder->get_control_info(info);
		} else {
			LOGD("UnityUVCHolder not found! id=%d", device_id);
		}
	}

	RETURN(result, int);
}

/**
 * native側でUVC設定機能へアクセスするときのヘルパー関数
 * 主にUnityやFlutterからのアクセスを想定
 * @param device_id
 * @param type
 * @param value
 * @return 0: 成功, 負: エラーコード
 */
/*public*/
int UnityUVCPlugin::set_control_value(const int &device_id, const uint64_t &type, const int32_t &value) {
	ENTER();

	int result = -4;
	if (LIKELY(device_id)) {
		UnityUVCHolderSp holder = nullptr;
		if (m_lock.try_lock()) {
			holder = get_uvc_holder_locked(device_id, false);
			m_lock.unlock();
		}
		if (holder) {
			result = holder->set_control_value(type, value);
		} else {
			LOGD("UnityUVCHolder not found! id=%d", device_id);
		}
	}

	RETURN(result, int);
}

/**
 * native側でUVC設定機能へアクセスするときのヘルパー関数
 * 主にUnityやFlutterからのアクセスを想定
 * @param device_id
 * @param type
 * @param value
 * @return 0: 成功, 負: エラーコード
 */
/*public*/
int UnityUVCPlugin::get_control_value(const int &device_id, const uint64_t &type, int32_t &value) {
	ENTER();
	
	int result = -4;
	if (LIKELY(device_id)) {
		UnityUVCHolderSp holder = nullptr;
		if (m_lock.try_lock()) {
			holder = get_uvc_holder_locked(device_id, false);
			m_lock.unlock();
		}
		if (holder) {
			result = holder->get_control_value(type, value);
		} else {
			LOGD("UnityUVCHolder not found! id=%d", device_id);
		}
	}
	
	RETURN(result, int);
}

/**
 * native側でUVC映像サイズ設定へアクセスするときのヘルパー関数
 * 主にUnityやFlutterからのアクセスを想定
 * @param device_id
 * @param index 映像サイズ設定のインデックス
 * @param num_supported 対応している映像サイズ設定の数を入れるuint32_tへのポインタ
 * @param data 映像サイズ設定を書き込むためのunity_video_size_t構造体へのポインタ
 * @return 0: 成功, 負: エラーコード
 */
int UnityUVCPlugin::get_supported_size(
	const int &device_id,
	const int32_t &index, int32_t *num_supported,
	video_size_t *data) {

	ENTER();

	int result = -4;
	UnityUVCHolderSp holder = nullptr;
	if (m_lock.try_lock()) {
		holder = get_uvc_holder_locked(device_id, false);
		m_lock.unlock();
	}
	if (holder) {
		result = holder->get_supported_size(index, num_supported, data);
	} else {
		LOGD("UnityUVCHolder not found! id=%d", device_id);
	}

	RETURN(result, int);
}

//--------------------------------------------------------------------------------
/**
 * 音声取得開始
 * レンダーコールバックを呼び出さないと実際には描画されない
 * @param device_id UVC機器識別用のID
 * @return
 */
/*public*/
int UnityUVCPlugin::start_uac(const int32_t &device_id) {
	ENTER();
	
	int result = -1;
	UnityUACHolderSp holder = nullptr;
	if (m_lock.try_lock()) {
		holder = get_uac_holder_locked(device_id, false);
		m_lock.unlock();
	}
	if (holder && !holder->is_running()) {
		// まだ音声取得開始していないとき
		result = holder->start();
	}

	RETURN(result, int);
}

/**
 * 音声取得終了
 * @param device_id UVC機器識別用のID
 * @return
 */
/*public*/
int UnityUVCPlugin::stop_uac(const int32_t &device_id) {
	ENTER();

	if (LIKELY(device_id)) {
		UnityUACHolderSp holder = nullptr;
		if (m_lock.try_lock()) {
			holder = get_uac_holder_locked(device_id, false);
			m_lock.unlock();
		}
		if (holder) {
			holder->stop();
		}
	}
	
	RETURN(0, int);
}

/**
 * Unityへ引き渡す形式の音声取得設定を取得
 * @param device_id UVC機器識別用のID
 * @param info
 * @return
 */
/*public*/
int UnityUVCPlugin::get_uac_info(const int32_t &device_id, uac_info_t &info) {
	ENTER();
	
	int result = -4;
	if (LIKELY(device_id)) {
		UnityUACHolderSp holder = nullptr;
		if (m_lock.try_lock()) {
			holder = get_uac_holder_locked(device_id, false);
			m_lock.unlock();
		}
		if (holder) {
			result = holder->get_uac_info(info);
		} else {
			LOGD("UACHolderUnity not found! id=%d", device_id);
		}
	}
	
	RETURN(result, int);
}

/**
 * 音声フレームをフレームキューから読み取る
 * @param device_id UVC機器識別用のID
 * @param data nullptrなら*lenにフレームデータのバイト数をセットするだけで実際の読み取りは行わない
 * @param data_len 音声フレームのバイト数
 * @param pts_us 音声データ受信時のシステムタイム[マイクロ秒]
 * @return
 */
/*public*/
int UnityUVCPlugin::get_uac_frame(const int32_t &device_id, uint8_t *data, uint32_t *data_len, int64_t *pts_us) {
//	ENTER();
	
	int result = -4;
	if (LIKELY(device_id)) {
		UnityUACHolderSp holder = nullptr;
		if (m_lock.try_lock()) {
			holder = get_uac_holder_locked(device_id, false);
			m_lock.unlock();
		}
		if (holder) {
			result = holder->get_uac_frame(data, data_len, pts_us);
		} else {
			LOGD("UACHolderUnity not found! id=%d", device_id);
		}
	}
	
	return result; // RETURN(result, int);
}

//--------------------------------------------------------------------------------
/**
 * 使用中のＵＶＣ機器があれば終了させる
 */
/*public*/
void UnityUVCPlugin::terminate_all() {
	ENTER();

	std::lock_guard<std::mutex> _lock(m_lock);

	// UVC機器を停止
	for (const auto& iter: uvc_holders) {
		LOGD("stop&remove,%d", iter.first);
		auto holder = iter.second;
		if (holder) {
			holder->stop();
			holder.reset();
		}
	}
	uvc_holders.clear();
	// UAC機器を停止
	for (const auto& iter: uac_holders) {
		LOGD("stop&remove,%d", iter.first);
		auto holder = iter.second;
		if (holder) {
			holder->stop();
			holder.reset();
		}
	}
	uac_holders.clear();

	EXIT();
}

/**
 * 指定したidに対応するUVCHolderSpを取得する
 * 存在していない場合にcreate_if_absent=trueならUVCHolderSpを生成する
 * @param device_id UVC機器識別用のID
 * @param create_if_absent
 * @return
 */
/*private*/
UnityUVCHolderSp UnityUVCPlugin::get_uvc_holder_locked(const int32_t &device_id, const bool &create_if_absent) {
//	ENTER();

	UnityUVCHolderSp result = nullptr;
	auto iter = uvc_holders.find(device_id);
	if (iter != uvc_holders.end()) {
		result = iter->second;
	}
	if (!result && create_if_absent) {
		switch (unity_graphics_type) {
			case kUnityGfxRendererOpenGLES20:
			case kUnityGfxRendererOpenGLES30:
				LOGI("create UnityUVCHolderGLES,id=%d,unity_graphics_type=%d", device_id, unity_graphics_type);
				result = std::make_shared<UnityUVCHolderGLES>(m_manager, device_id, unity_graphics_type == kUnityGfxRendererOpenGLES30 ? 300 : 200);
				break;
			case kUnityGfxRendererVulkan:
				if (unity_graphics_vulkan) {
					LOGI("create UnityUVCHolderVulkan,id=%d,unity_graphics_type=%d", device_id, unity_graphics_type);
					result = std::make_shared<UnityUVCHolderVulkan>(m_manager, device_id, 300, unity_graphics_vulkan, unity_vulkan_instance);
				}
				break;
			default:
				break;
		}
		if (result) {
			LOGD("push %p to uvc_holders", result.get());
			uvc_holders[device_id] = result;
		} else {
			LOGW("failed to create UnityUVCHolder for unsupported unity_graphics_type,%d", unity_graphics_type);
		}
	}

	return result; // RET(result);
}

/**
 * 指定したidに対応するUACHolderSpを取得する
 * 存在していない場合にcreate_if_absent=trueならUVCHolderSpを生成する
 * @param device_id UVC機器識別用のID
 * @param create_if_absent
 * @return
 */
/*private*/
UnityUACHolderSp UnityUVCPlugin::get_uac_holder_locked(const int32_t &device_id, const bool &create_if_absent) {
//	ENTER();

	UnityUACHolderSp result;
	auto iter = uac_holders.find(device_id);
	if (iter != uac_holders.end()) {
		result = iter->second;
	} else if (create_if_absent) {
		result = std::make_shared<UnityUACHolder>(m_manager, device_id);
		uac_holders[device_id] = result;
	}

	return result; // RET(result);
}

/**
 * UVC機器が接続された時の処理
 * @param connector
 */
/*private*/
int UnityUVCPlugin::add(const int32_t &device_id) {
	ENTER();

	int32_t result = -1;
	{
		std::lock_guard<std::mutex> lock(m_lock);
		auto uvc_holder = get_uvc_holder_locked(device_id, true);
		if (uvc_holder) {
			get_uac_holder_locked(device_id, true);
			result = 0;
		}
	}

	if (!result) {
		MARK("num callbacks=%" FMT_SIZE_T, callbacks.size());
		for (const auto &callback: callbacks) {
			if (callback.second) {
				try {
					// Unity側のコールバックを呼び出す
					callback.second->call(device_id, true);
				} catch (...) {
					LOGE("Exception occurred on calling 'on_device_changed'");
				}
			}
		}
	}

	RETURN(result, int32_t);
}

/**
 * UVC機器が取り外されたときの処理
 * @param id
 */
/*private*/
void UnityUVCPlugin::remove(const int32_t &device_id) {
	ENTER();

	MARK("num callbacks=%" FMT_SIZE_T, callbacks.size());
	for (const auto &callback: callbacks) {
		if (callback.second) {
			try {
				// Unity側のコールバックを呼び出す
				callback.second->call(device_id, false);
			} catch (...) {
				LOGE("Exception occurred on calling 'on_device_changed'");
			}
		}
	}

	UnityUVCHolderSp uvc_removed;
	UnityUACHolderSp uac_removed;
	{
		std::lock_guard<std::mutex> _lock(m_lock);
		auto uvc_itr = uvc_holders.find(device_id);
		if (uvc_itr != uvc_holders.end()) {
			uvc_removed = uvc_itr->second;
			uvc_holders.erase(device_id);
		}
		auto uac_itr = uac_holders.find(device_id);
		if (uac_itr != uac_holders.end()) {
			uac_removed = uac_itr->second;
			uac_holders.erase(device_id);
		}
	}
	if (uvc_removed) {
		LOGD("remove uvc %d", id);
		uvc_removed->stop();
	}
	if (uac_removed) {
		LOGD("remove uac %d", id);
		uac_removed->stop();
	}
	LOGD("remove: finished");

	EXIT();
}

/**
 * USB機器が接続されたときのコールバック関数
 * @param callback_args UVCMainへのポインタ
 * @param device_id
 */
/*private,static*/
void UnityUVCPlugin::on_device_attach(usb_manager_t*, void *callback_args, int32_t device_id) {
	ENTER();

	auto plugin = reinterpret_cast<UnityUVCPlugin *>(callback_args);
	if (plugin) {
		plugin->add(device_id);
	}

	EXIT();
}

/**
 * USB機器が取り外されたときのコールバック関数
 * @param callback_args UVCMainへのポインタ
 * @param device_id
 */
void UnityUVCPlugin::on_device_detach(usb_manager_t*, void *callback_args, int32_t device_id) {
	ENTER();

	auto plugin = reinterpret_cast<UnityUVCPlugin *>(callback_args);
	if (plugin) {
		plugin->remove(device_id);
	}

	EXIT();
}

}	// namespace serenegiant::unity
