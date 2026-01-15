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

#ifndef AANDUSB_UNITY_UAC_HOLDER_H
#define AANDUSB_UNITY_UAC_HOLDER_H

// 標準ライブラリ
#include <memory>
// aandusb-native
#include "aandusb_native.h"
// unity plugin
#include "unity_defs.h"

namespace serenegiant::unity {

class UnityUACHolder {
private:
	const int32_t m_device_id;
	usb_manager_t *m_manager;
protected:
public:
	/**
	 * コンストラクタ
	 */
	explicit UnityUACHolder(usb_manager_t *manager, const int32_t &device_id);
	/**
	 * デストラクタ
	 */
	virtual ~UnityUACHolder() noexcept;

	/**
	 * 音声取得中かどうかを取得
	 * @return
	 */
	[[nodiscard]]
	bool is_running() const;

	/**
	 * 音声取得開始
	 * @param connector
	 * @return
	 */
	int start();
	/**
	 * 音声取得終了
	 * @return
	 */
	int stop();

	/**
	 * 音声フレームをフレームキューから読み取る
	 * @param data nullptrなら*lenにフレームデータのバイト数をセットするだけで実際の読み取りは行わない
	 * @param data_len 音声フレームのバイト数
	 * @param pts_us 音声データ受信時のシステムタイム[マイクロ秒]
	 * @return
	 */
	int get_uac_frame(uint8_t *data, uint32_t *data_len, int64_t *pts_us);

	/**
	 * Unityへ引き渡す形式の音声取得設定を取得
	 * @param info
	 * @return
	 */
	int get_uac_info(uac_info_t &info);
};

typedef std::shared_ptr<UnityUACHolder> UnityUACHolderSp;
typedef std::unique_ptr<UnityUACHolder> UnityUACHolderUp;

} // serenegiant::unity

#endif //AANDUSB_UNITY_UAC_HOLDER_H
