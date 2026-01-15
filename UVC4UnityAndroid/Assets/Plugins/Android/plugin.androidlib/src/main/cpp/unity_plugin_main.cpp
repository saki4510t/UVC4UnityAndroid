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

#define LOG_TAG "UnityPluginMain"

#if 1	// デバッグ情報を出さない時は1
	#ifndef LOG_NDEBUG
		#define	LOG_NDEBUG		// LOGV/LOGD/MARKを出力しない時
	#endif
	#undef USE_LOGALL			// 指定したLOGxだけを出力
#else
	#define USE_LOGALL
	#undef LOG_NDEBUG
	#undef NDEBUG
#endif

#include <string>
#include <unordered_map>

// unity
#include "unity/IUnityInterface.h"
#include "unity/IUnityGraphics.h"
// aandusb
#include "utilbase.h"
// unity plugin
#include "unity_uvc_plugin.h"

namespace unity = serenegiant::unity;

//--------------------------------------------------------------------------------
// グルーバル変数定義
//--------------------------------------------------------------------------------
/**
 * DeviceConnectorのスマートポインタの参照渡しだとunityからうまくアクセスできないぽいので
 * DeviceConnectorのポインタとして渡すためのコールバック関数を保持するlist
 */
namespace serenegiant::unity {
	/**
	 * UVC機器接続時にUnityのコールバック関数を呼び出すためのラッパーを保持するハッシュマップ
	 */
	std::unordered_map<int32_t, UnityCallbackWrapperUp> callbacks;
	/**
	 * Unityからのレンダリングインベントを処理するためのUnityUVCPlugin
	 */
	UnityUVCPluginUp g_unity_plugin = nullptr;
}

//--------------------------------------------------------------------------------
// ローカル変数定義
//--------------------------------------------------------------------------------
static IUnityInterfaces *s_UnityInterfaces = nullptr;
static IUnityGraphics *s_UnityGraphics = nullptr;
static UnityGfxRenderer s_UnityGraphicsType = kUnityGfxRendererNull;

//--------------------------------------------------------------------------------
// 関数定義
//--------------------------------------------------------------------------------

/**
 * Unityのレンダーイベントで呼び出される関数の実体
 * @param eventID
 */
static void UNITY_INTERFACE_API OnRenderEvent(int eventID)
{
//	ENTER();

	if (unity::g_unity_plugin) {
		unity::g_unity_plugin->on_render_event(eventID);
	} else {
		LOGD("UnityUVCPlugin not exists!");
	}

//	EXIT();
}

/**
 * Unityのレンダーイベントで呼び出される関数の実体
 * @param eventID
 * @param data
 */
static void UNITY_INTERFACE_API OnRenderAndDataEvent(int eventID, void* data)
{
//	ENTER();

	if (unity::g_unity_plugin) {
		unity::g_unity_plugin->on_render_event(eventID, data);
	} else {
		LOGD("UnityUVCPlugin not exists!");
	}

//	EXIT();
}

//================================================================================
// Unity側から呼び出される関数
//================================================================================
#ifdef __cplusplus
extern "C" {
#endif

/**
 * レンダーイベント処理用の関数をUnity側から取得するための関数
 * @return
 */
UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetRenderEventFunc()
{
	ENTER();
	RET(OnRenderEvent);
}

/**
 * レンダーイベント処理用の関数をUnity側から取得するための関数
 * @return
 */
UnityRenderingEventAndData UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetRenderEventAndDataFunc()
{
	ENTER();
	RET(OnRenderAndDataEvent);
}

/**
 * UVC pluginのnative側を初期化
 * Unity側から呼び出される
 * @param callback_id 同じidは登録できない
 * @param device_changed_callback
 */
void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API Register(
	int32_t callback_id,
	unity::unity_on_device_changed device_changed_callback) {
	ENTER();

	if (device_changed_callback && (unity::callbacks.find(callback_id) == unity::callbacks.end())) {
		if (unity::callbacks.empty() && !unity::g_unity_plugin) {
			MARK("UnityUVCPluginを生成");
			unity::g_unity_plugin = std::make_unique<unity::UnityUVCPlugin>(s_UnityInterfaces, s_UnityGraphics, s_UnityGraphicsType);
		}
		unity::callbacks.emplace(callback_id, std::make_unique<unity::UnityCallbackWrapper>(
			callback_id, device_changed_callback));
	}

	EXIT();
}

/**
 * UVC pluginのnative側を登録解除
 * Unity側から呼び出される
 * @param callback_id
 */
void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API Unregister(int32_t callback_id) {
	ENTER();

	unity::callbacks.erase(callback_id);
	if (unity::callbacks.empty() && unity::g_unity_plugin) {
		MARK("UnityUVCPluginを破棄");
		unity::g_unity_plugin->terminate_all();
		unity::g_unity_plugin.reset();
	}

	EXIT();
}

/**
 * bcdUSBを取得
 * @return
 */
uint16_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DeviceInfo_get_bcd_usb(int32_t device_id) {
	ENTER();

	uint16_t result = 0;
	if (unity::g_unity_plugin) {
		result = usb_get_bcd_usb(unity::g_unity_plugin->manager(), device_id);
	}

	RETURN(result, uint16_t);
}

/**
 * デバイスクラスを取得
 * @return
 */
uint8_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DeviceInfo_get_device_class(int32_t device_id) {
	ENTER();

	uint8_t result = 0;
	if (unity::g_unity_plugin) {
		result = usb_get_device_class(unity::g_unity_plugin->manager(), device_id);
	}

	RETURN(result, uint8_t);
}

/**
 * デバイスサブクラスを取得
 * @return
 */
uint8_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DeviceInfo_get_device_sub_class(int32_t device_id) {
	ENTER();

	uint8_t result = 0;
	if (unity::g_unity_plugin) {
		result = usb_get_device_sub_class(unity::g_unity_plugin->manager(), device_id);
	}

	RETURN(result, uint8_t);
}

/**
 * デバイスプロトコルを取得
 * @return
 */
uint8_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DeviceInfo_get_device_protocol(int32_t device_id) {
	ENTER();

	uint8_t result = 0;
	if (unity::g_unity_plugin) {
		result = usb_get_device_protocol(unity::g_unity_plugin->manager(), device_id);
	}

	RETURN(result, uint8_t);
}

/**
 * ベンダーIDを首都kする
 */
/*public*/
uint16_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DeviceInfo_get_vendor_id(int32_t device_id) {
	ENTER();

	uint16_t result = 0;
	if (unity::g_unity_plugin) {
		result = usb_get_vendor_id(unity::g_unity_plugin->manager(), device_id);
	}

	RETURN(result, uint16_t);
}

/**
 * プロダクトIDを取得する
 */
/*public*/
uint16_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DeviceInfo_get_product_id(int32_t device_id) {
	ENTER();

	uint16_t result = 0;
	if (unity::g_unity_plugin) {
		result = usb_get_product_id(unity::g_unity_plugin->manager(), device_id);
	}

	RETURN(result, uint16_t);
}

/**
 * 機器名取得する
 */
/*public*/
const char * UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DeviceInfo_get_name(int32_t device_id) {
	ENTER();

	size_t len = 0;
	std::string name_string;
	if (unity::g_unity_plugin) {
		auto r = usb_get_name(unity::g_unity_plugin->manager(), device_id, nullptr, &len);
		if (!r && (len > 0)) {
			char name[len + 1];
			if (!usb_get_name(unity::g_unity_plugin->manager(), device_id, name, &len)) {
				name[len] = 0;
				name_string = name;
			}
		}
	}

	RET(::strdup(name_string.c_str()));
}

/**
 * デバイスまたはインターフェースが指定した条件に一致するかどうかを確認
 * @param device
 * @param bClass     0xffなら常にマッチする(ワイルドカード)
 * @param bSubClass  0xffなら常にマッチする(ワイルドカード)
 * @param bProtocol  0xffなら常にマッチする(ワイルドカード)
 * @return 1: 一致した, 0: 一致しなかった
 */
/*public*/
int32_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DeviceInfo_match(
		int32_t device_id,
		uint8_t bClass, uint8_t bSubClass, uint8_t bProtocol) {
	ENTER();

	int32_t result = 0;
	if (unity::g_unity_plugin) {
		result = usb_match(unity::g_unity_plugin->manager(), device_id, bClass, bSubClass, bProtocol);
	}

	RETURN(result, int32_t);
}

/**
 * 機器との接続状態を取得する
 * @param device_id
 * @return
 */
int32_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetState(
	int32_t device_id) {

	ENTER();

	device_state_t result = DISCONNECTED;
	if (unity::g_unity_plugin) {
		result = unity::g_unity_plugin->get_device_state(device_id);
	}

	RETURN(result, device_state_t);
}

/**
 * UVC機器の基本設定をセット
 * @param device_id
 * @param enabled
 * @param use_first_config
 * @return
 */
int32_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API Config(
	int32_t device_id,
	int32_t enabled, int32_t use_first_config) {
	
	ENTER();

	int32_t  result = -1;
	if (unity::g_unity_plugin) {
		result = unity::g_unity_plugin->set_config(device_id, enabled, use_first_config);
	}

	RETURN(result, int32_t);
}

/**
 * 映像取得開始
 * レンダーコールバックを呼び出さないと実際には描画されない
 * @param device_id
 * @return
 */
int32_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API Start(int32_t device_id, void *tex, int32_t tex_width, int32_t tex_height) {
	ENTER();

	int32_t  result = -1;
	if (unity::g_unity_plugin) {
		result = unity::g_unity_plugin->start(device_id, tex, tex_width, tex_height);
	}

	RETURN(result, int32_t);
}

/**
 * 映像取得終了
 * @param device_id
 * @return
 */
int32_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API Stop(int32_t device_id) {
	ENTER();

	int32_t  result = -1;
	if (unity::g_unity_plugin) {
		result = unity::g_unity_plugin->stop(device_id);
	}

	RETURN(result, int32_t);
}

/**
 * 映像サイズ変更要求
 * @param device_id
 * @param width
 * @param height
 * @return
 */
int32_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API Resize(
	int32_t device_id,
	uint32_t frame_type,
	uint32_t width, uint32_t height) {

	ENTER();

	int32_t  result = -1;
	if (unity::g_unity_plugin) {
		result = unity::g_unity_plugin->resize(device_id, (raw_frame_t)frame_type, width, height);
	}

	RETURN(result, int32_t);
}

/**
 * コントロール機能でサポートしている機能を取得
 * @param device_id
 * @return
 */
uint64_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetCtrlSupports(int32_t device_id) {
	ENTER();

	uint64_t  result = 0;
	if (unity::g_unity_plugin) {
		result = unity::g_unity_plugin->get_ctrl_supports(device_id);
	}

	RETURN(result, int32_t);
}

/**
 * プロセッシングユニットでサポートしている機能を取得
 * @param device_id
 * @return
 */
uint64_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetProcSupports(int32_t device_id) {
	ENTER();

	uint64_t  result = 0;
	if (unity::g_unity_plugin) {
		result = unity::g_unity_plugin->get_proc_supports(device_id);
	}

	RETURN(result, int32_t);
}

/**
 * 指定した機能の設定情報を取得
 * @param device_id
 * @param value
 * @return
 */
int32_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetCtrlInfo(int32_t device_id, control_info_t *value) {
	ENTER();
	
	int32_t  result = -4;
	if (unity::g_unity_plugin) {
		result = unity::g_unity_plugin->get_control_info(device_id, *value);
	}
	
	RETURN(result, int32_t);
}

/**
 * 指定した機能の設定値を適用
 * @param device_id
 * @param type
 * @param value
 * @return
 */
int32_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetCtrlValue(int32_t device_id, uint64_t type, int32_t value) {
	ENTER();
	
	int32_t  result = -4;
	if (unity::g_unity_plugin) {
		result = unity::g_unity_plugin->set_control_value(device_id, type, value);
	}
	
	RETURN(result, int32_t);
}

/**
 * 指定した機能の設定値を取得
 * @param device_id
 * @param type
 * @param value
 * @return
 */
int32_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetCtrlValue(int32_t device_id, uint64_t type, int32_t *value) {
	ENTER();
	
	int32_t  result = -4;
	if (unity::g_unity_plugin) {
		result = unity::g_unity_plugin->get_control_value(device_id, type, *value);
	}
	
	RETURN(result, int32_t);
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
int32_t UNITY_INTERFACE_API GetSupportedSize(
	int32_t device_id, int32_t index, int32_t *num_supported, video_size_t *data) {

	ENTER();

	int32_t  result = -4;
	if (unity::g_unity_plugin) {
		result = unity::g_unity_plugin->get_supported_size(device_id, index, num_supported, data);
	}

	RETURN(result, int32_t);
}

//--------------------------------------------------------------------------------
/**
 * 音声取得開始
 * 音声データを受信するたびにRegister時に指定したuac_callbackが呼び出される
 * @param device_id
 * @return
 */
int32_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API StartUAC(int32_t device_id) {
	ENTER();
	
	int32_t  result = -1;
	if (unity::g_unity_plugin) {
		result = unity::g_unity_plugin->start_uac(device_id);
	}
	
	RETURN(result, int32_t);
}

/**
 * 音声取得終了
 * @param device_id
 * @return
 */
int32_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API StopUAC(int32_t device_id) {
	ENTER();
	
	int32_t  result = -1;
	if (unity::g_unity_plugin) {
		result = unity::g_unity_plugin->stop_uac(device_id);
	}
	
	RETURN(result, int32_t);
}

/**
 * 指定した機能の設定情報を取得
 * XXX StartUACを呼んだ後でないと正しい値が返らないので注意
 * @param device_id
 * @param value
 * @return
 */
int32_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetUACInfo(int32_t device_id, uac_info_t *value) {
	ENTER();
	
	int32_t  result = -4;
	if (unity::g_unity_plugin) {
		result = unity::g_unity_plugin->get_uac_info(device_id, *value);
	}
	
	RETURN(result, int32_t);
}

/**
 * 音声フレームをフレームキューから読み取る
 * @param device_id
 * @param data nullptrなら*lenにフレームデータのバイト数をセットするだけで実際の読み取りは行わない
 * @param data_len 音声フレームのバイト数
 * @param pts_us 音声データ受信時のシステムタイム[マイクロ秒]
 * @return
 */
int32_t UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetUACFrame(int32_t device_id, uint8_t *data, uint32_t *data_len, int64_t *pts_us) {
//	ENTER();
	
	int32_t  result = -4;
	if (unity::g_unity_plugin) {
		result = unity::g_unity_plugin->get_uac_frame(device_id, data, data_len, pts_us);
	}
	
	return result; // 	RETURN(result, int32_t);
}
//--------------------------------------------------------------------------------
// UnitySetInterfaces
// 先方参照宣言(Unityから呼び出されるグラフィックス関係のイベントコールバック関数)
static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType);

/**
 * Unity側からプラグインが読み込まれたときの処理
 * @param unityInterfaces
 */
void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces) {
	ENTER();

	s_UnityInterfaces = unityInterfaces;
	s_UnityGraphics = s_UnityInterfaces->Get<IUnityGraphics>();
	LOGD("RegisterDeviceEventCallback");
	s_UnityGraphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);
	// Run OnGraphicsDeviceEvent(initialize) manually on plugin load
	OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);

	EXIT();
}

/**
 * Unity側からプラグインの読み込みが解除されたときの処理
 */
void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload() {
	unity::callbacks.clear();
	if (s_UnityGraphics) {
		LOGD("UnregisterDeviceEventCallback");
		s_UnityGraphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
		s_UnityGraphics = nullptr;
	}
	s_UnityInterfaces = nullptr;
}

//--------------------------------------------------------------------------------
/**
 * GraphicsDeviceEvent
 * Unityから呼び出されるグラフィックス関係のイベントコールバック関数
 * UnityPluginLoadが呼び出されたときに登録、
 * UnityPluginUnloadが呼び出されたときに登録解除する
 * @param eventType
 */
static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType) {
	ENTER();

	LOGD("eventType=%d", eventType);
	switch (eventType) {
	case kUnityGfxDeviceEventInitialize:
		// Create graphics API implementation upon initialization
		s_UnityGraphicsType = s_UnityGraphics->GetRenderer();
		MARK("initialize,graphics type=%d", s_UnityGraphicsType);
		switch (s_UnityGraphicsType) {
		case kUnityGfxRendererD3D11:		// 2, // Direct3D 11
			LOGD("kUnityGfxRendererD3D11"); break;
		case kUnityGfxRendererNull:			// 4, // "null" device (used in batch mode)
			LOGD("kUnityGfxRendererNull"); break;
		case kUnityGfxRendererOpenGLES20:	// 8, // OpenGL ES 2.0
			LOGD("kUnityGfxRendererOpenGLES20"); break;
		case kUnityGfxRendererOpenGLES30:	// 11, // OpenGL ES 3.0
			LOGD("kUnityGfxRendererOpenGLES30"); break;
		case kUnityGfxRendererPS4:			// 13, // PlayStation 4
			LOGD("kUnityGfxRendererPS4"); break;
		case kUnityGfxRendererXboxOne:		// 14, // Xbox One
			LOGD("kUnityGfxRendererXboxOne"); break;
		case kUnityGfxRendererMetal:		// 16, // iOS Metal
			LOGD("kUnityGfxRendererMetal"); break;
		case kUnityGfxRendererOpenGLCore:	// 17, // OpenGL core
			LOGD("kUnityGfxRendererOpenGLCore"); break;
		case kUnityGfxRendererD3D12:		// 18, // Direct3D 12
			LOGD("kUnityGfxRendererD3D12"); break;
		case kUnityGfxRendererVulkan:		// 21, // Vulkan
			LOGD("kUnityGfxRendererVulkan"); break;
		case kUnityGfxRendererNvn:			// 22, // Nintendo Switch NVN API
			LOGD("kUnityGfxRendererNvn"); break;
		case kUnityGfxRendererXboxOneD3D12:	// 23  // MS XboxOne Direct3D 12
			LOGD("kUnityGfxRendererXboxOneD3D12"); break;
		}
		break;
	case kUnityGfxDeviceEventShutdown:
		// Cleanup graphics API implementation upon shutdown
		MARK("shutdown");
		s_UnityGraphicsType = kUnityGfxRendererNull;
		break;
	case kUnityGfxDeviceEventBeforeReset:
	case kUnityGfxDeviceEventAfterReset:
	default:
		break;
	}

	EXIT();
}

#ifdef __cplusplus
}
#endif
