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

#ifndef AANDUSB_UNITY_UVC_PLUGIN_H
#define AANDUSB_UNITY_UVC_PLUGIN_H

// 標準ライブラリ
#include <memory>
#include <mutex>
#include <unordered_map>
// native binding
#include "aandusb_native.h"
// unity plugin
#include "unity_defs.h"

//--------------------------------------------------------------------------------
// 外部クラスの前方宣言
namespace serenegiant::unity {
class UnityUVCHolder;
class UnityUACHolder;
}
// 外部クラスの前方宣言ここまで
//--------------------------------------------------------------------------------

namespace serenegiant::unity {

/**
 * 接続検出したUVC機器に対応するUnityUVCHolderのスマートポインタを保持するハッシュマップ
 * キーはUVC機器識別用のID、値はstd::shared_ptr<UnityUVCHolder>
 */
typedef std::unordered_map <int32_t, std::shared_ptr<UnityUVCHolder>> UVCHolderMap;
/**
 * 接続検出したUVC機器に対応するUnityUACHolderのスマートポインタを保持するハッシュマップ
 * キーはUVC機器識別用のID、値はstd::shared_ptr<UnityUACHolder>
 */
typedef std::unordered_map <int32_t, std::shared_ptr<UnityUACHolder>> UACHolderMap;
/**
 * USBの機器接続状態が変化したときのunity側のコールバック関数定義
 * @param callback_id Unity側のコールバック識別ID
 * @param connector usb::DeviceConnectorのポインタ
 * @param attached true: 接続した, false: 切断した
 */
using unity_on_device_changed = void (UNITY_INTERFACE_API *)(int32_t callback_id, int32_t device_id, bool attached);
/**
 * UACのデータを受信したときのUnity側コールバック関数定義
 * @param callback_id Unity側のコールバック識別ID
 * @param device_id UVC/UAC機器識別ID
 * @param data
 * @param data_len
 * @param pts_us
 */
using unity_on_uac_frame = void (UNITY_INTERFACE_API *)(int32_t callback_id, int32_t device_id, uint8_t *data, int32_t data_len, int64_t pts_us);

/**
 * USB機器接続時に呼び出すUnityのコールバック関数を保持するラッパークラス
 */
class UnityCallbackWrapper {
private:
	const int32_t callback_id;	// このidはコールバック識別ID
	unity_on_device_changed on_device_changed;
public:
	/**
	 * コンストラクタ
	 * @param callback_id コールバックID
	 * @param on_device_changed USB機器の接続状態が変化したときのコールバック関数
	 */
	UnityCallbackWrapper(int32_t &callback_id,
		unity_on_device_changed on_device_changed);
	/**
	 * USBの機器接続状態が変化したときのunity側のコールバック関数を呼び出す
	 * @param device_id
	 * @param attached
	 */
	void call(int32_t device_id, bool attached) const;
};
typedef std::unique_ptr<UnityCallbackWrapper> UnityCallbackWrapperUp;

/**
 * Unityからのレンダリングインベントを処理するためのヘルパークラス
 */
class UnityUVCPlugin {
	IUnityInterfaces *unity_interfaces;
	IUnityGraphics *unity_graphics;
	const UnityGfxRenderer unity_graphics_type;
	IUnityGraphicsVulkan *unity_graphics_vulkan;
	UnityVulkanInstance unity_vulkan_instance;
	mutable std::mutex m_lock;
	usb_manager_t *m_manager;
	UVCHolderMap uvc_holders;
	UACHolderMap uac_holders;
private:
	/**
	 * 指定したidに対応するUVCHolderSpを取得する
	 * 存在していない場合にcreate_if_absent=trueならUVCHolderSpを生成する
	 * @param device_id UVC機器識別用のID
	 * @param create_if_absent
	 * @return
	 */
	std::shared_ptr<UnityUVCHolder> get_uvc_holder_locked(const int32_t &device_id, const bool &create_if_absent);
	
	/**
	 * 指定したidに対応するUACHolderSpを取得する
	 * 存在していない場合にcreate_if_absent=trueならUVCHolderSpを生成する
	 * @param device_id UVC機器識別用のID
	 * @param create_if_absent
	 * @return
	 */
	std::shared_ptr<UnityUACHolder> get_uac_holder_locked(const int32_t &device_id, const bool &create_if_absent);

	/**
	 * UVC機器が接続された時の処理
	 * @param connector
	 */
	int add(const int32_t &device_id);

	/**
	 * UVC機器が取り外されたときの処理
	 * @param id
	 */
	void remove(const int32_t &device_id);
	/**
	 * USB機器が接続されたときのコールバック関数
	 * @param callback_args UVCMainへのポインタ
	 * @param device_id
	 */
	static void on_device_attach(usb_manager_t*, void *callback_args, int32_t device_id);
	/**
	 * USB機器が取り外されたときのコールバック関数
	 * @param callback_args UVCMainへのポインタ
	 * @param device_id
	 */
	static void on_device_detach(usb_manager_t*, void *callback_args, int32_t device_id);
protected:
public:
	/**
	 * コンストラクタ
	 * @param unity_interfaces
	 * @param unity_graphics
	 * @param unity_graphics_type
	 */
	UnityUVCPlugin(
		IUnityInterfaces *unity_interfaces,
		IUnityGraphics *unity_graphics,
		const UnityGfxRenderer &unity_graphics_type);

	/**
	 * デストラクタ
	 */
	~UnityUVCPlugin() noexcept;

	usb_manager_t *manager() const { return m_manager; };

	/**
	 * 使用中のＵＶＣ機器があれば終了させる
	 */
	void terminate_all();

	/**
	 * 指定したIDに対応するUVC機器が接続されていて利用可能かどうか
	 * @param id
	 * @return
	 */
	bool is_available(const int32_t &device_id);
	/**
	 * 機器との接続状態を取得
	 * @return
	 */
	device_state_t get_device_state(const int32_t &device_id);
	/**
	 * UVC機器の基本設定をセット
	 * @param device_id
	 * @param enabled
	 * @param use_first_config
	 * @return
	 */
	int set_config(const int32_t &device_id, const int32_t &enabled, const bool &use_first_config);

	/**
	 * UVC機器からの映像サイズの変更要求
	 * @param device_id UVC機器識別用のID
	 * @param width
	 * @param height
	 * @return
	 */
	int resize(const int32_t &device_id,
		const raw_frame_t &frame_type,
		const uint32_t &width, const uint32_t &height);

	/**
	 * 映像取得開始
	 * レンダーコールバックを呼び出さないと実際には描画されない
	 * @param device_id UVC機器識別用のID
	 * @param tex テクスチャ名
	 * @param tex_width
	 * @param tex_height
	 * @return
	 */
	int start(
		const int32_t &device_id,
		void *tex, const int32_t &tex_width, const int32_t &tex_height);

	/**
	 * 映像取得終了
	 * @param device_id UVC機器識別用のID
	 * @return
	 */
	int stop(const int32_t &device_id);

	/**
	 * Unityからのレンダリング要求時の描画処理を行う
	 * @param device_id UVC機器識別用のID
	 * @param args
	 */
	void on_render_event(const int &device_id, void *args = nullptr);

	/**
	 * コントロール機能でサポートしている機能を取得
	 * @param device_id
	 * @return
	 */
	uint64_t get_ctrl_supports(const int &device_id);

	/**
	 * プロセッシングユニットでサポートしている機能を取得
	 * @param device_id
	 * @return
	 */
	uint64_t get_proc_supports(const int &device_id);
	/**
	 * native側でUVC設定機能へアクセスするときのヘルパー関数
	 * 主にUnityやFlutterからのアクセスを想定
	 * @param device_id
	 * @param info
	 * @return 0: 成功, 負: エラーコード
	 */
	int get_control_info(const int &device_id, control_info_t &info);
	/**
	 * native側でUVC設定機能へアクセスするときのヘルパー関数
	 * 主にUnityやFlutterからのアクセスを想定
	 * @param device_id
	 * @param type
	 * @param value
	 * @return 0: 成功, 負: エラーコード
	 */
	int set_control_value(const int &device_id, const uint64_t &type, const int32_t &value);
	/**
	 * native側でUVC設定機能へアクセスするときのヘルパー関数
	 * 主にUnityやFlutterからのアクセスを想定
	 * @param device_id
	 * @param type
	 * @param value
	 * @return 0: 成功, 負: エラーコード
	 */
	int get_control_value(const int &device_id, const uint64_t &type, int32_t &value);
	/**
	 * native側でUVC映像サイズ設定へアクセスするときのヘルパー関数
	 * 主にUnityやFlutterからのアクセスを想定
	 * @param device_id
	 * @param index 映像サイズ設定のインデックス
	 * @param num_supported 対応している映像サイズ設定の数を入れるuint32_tへのポインタ
	 * @param data 映像サイズ設定を書き込むためのunity_video_size_t構造体へのポインタ
	 * @return 0: 成功, 負: エラーコード
	 */
	int get_supported_size(const int &device_id, const int32_t &index, int32_t *num_supported, video_size_t *data);

	//--------------------------------------------------------------------------------
	/**
	 * 音声取得開始
	 * レンダーコールバックを呼び出さないと実際には描画されない
	 * @param device_id UVC機器識別用のID
	 * @return
	 */
	int start_uac(const int32_t &device_id);
	
	/**
	 * 音声取得終了
	 * @param device_id UVC機器識別用のID
	 * @return
	 */
	int stop_uac(const int32_t &device_id);
	/**
	 * Unityへ引き渡す形式の音声取得設定を取得
	 * @param device_id UVC機器識別用のID
	 * @param info
	 * @return
	 */
	int get_uac_info(const int32_t &device_id, uac_info_t &info);
	/**
	 * 音声フレームをフレームキューから読み取る
	 * @param device_id UVC機器識別用のID
	 * @param data nullptrなら*lenにフレームデータのバイト数をセットするだけで実際の読み取りは行わない
	 * @param data_len 音声フレームのバイト数
	 * @param pts_us 音声データ受信時のシステムタイム[マイクロ秒]
	 * @return
	 */
	int get_uac_frame(const int32_t &device_id, uint8_t *data, uint32_t *data_len, int64_t *pts_us);
};

typedef std::unique_ptr<UnityUVCPlugin> UnityUVCPluginUp;

}	// namespace serenegiant::unity

#endif //AANDUSB_UNITY_UVC_PLUGIN_H
