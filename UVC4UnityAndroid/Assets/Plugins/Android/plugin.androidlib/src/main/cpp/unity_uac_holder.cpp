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

#define LOG_TAG "UnityUACHolder"

#if 1	// デバッグ情報を出さない時は1
	#ifndef LOG_NDEBUG
		#define	LOG_NDEBUG		// LOGV/LOGD/MARKを出力しない時
	#endif
	#undef USE_LOGALL			// 指定したLOGxだけを出力
#else
//	#define USE_LOGALL
	#undef LOG_NDEBUG
	#undef NDEBUG
#endif

// aandusb
#include "utilbase.h"
// unity plugin
#include "unity_uac_holder.h"

namespace serenegiant::unity {

/**
 * コンストラクタ
 */
/*public*/
UnityUACHolder::UnityUACHolder(usb_manager_t *manager, const int32_t &device_id)
:	m_manager(manager),
	m_device_id(device_id)
{
	ENTER();
	EXIT();
}

/**
 * デストラクタ
 */
/*public*/
UnityUACHolder::~UnityUACHolder() noexcept {
	ENTER();

	uac_stop(m_manager, m_device_id);

	EXIT();
}

/**
 * 音声取得中かどうかを取得
 * @return
 */
bool UnityUACHolder::is_running() const {
	ENTER();

	const auto state = uac_get_device_state(m_manager, m_device_id);

	RETURN(state > CONNECTED , bool);
}

/**
 * 音声取得開始
 * @param connector
 * @return
 */
int UnityUACHolder::start() {
	ENTER();
	RETURN(uac_start(m_manager, m_device_id), int);
}

/**
 * 音声取得終了
 * @return
 */
int UnityUACHolder::stop() {
	ENTER();
	RETURN(uac_stop(m_manager, m_device_id), int);
}

/**
 * 音声フレームをフレームキューから読み取る
 * @param data nullptrなら*lenにフレームデータのバイト数をセットするだけで実際の読み取りは行わない
 * @param data_len 音声フレームのバイト数
 * @param pts_us 音声データ受信時のシステムタイム[マイクロ秒]
 * @return
 */
int UnityUACHolder::get_uac_frame(uint8_t *data, uint32_t *data_len, int64_t *pts_us) {
//	ENTER();
	return uac_get_frame(m_manager, m_device_id, data, data_len, pts_us);
// RETURN(uac_get_frame(m_manager, m_device_id, data, &data_len, &pts_us), int);
}

/**
 * Unityへ引き渡す形式の音声取得設定を取得
 * @param info
 * @return
 */
/*public*/
int UnityUACHolder::get_uac_info(uac_info_t &info) {
	ENTER();
	RETURN(uac_get_info(m_manager, m_device_id, &info), int);
}

} // serenegiant::unity