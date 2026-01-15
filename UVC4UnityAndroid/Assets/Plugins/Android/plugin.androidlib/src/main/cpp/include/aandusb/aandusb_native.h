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

#ifndef AANDUSB_NATIVE_BINDING_H_
#define AANDUSB_NATIVE_BINDING_H_

#include <android/native_window.h>

#ifdef __cplusplus
#include <cstdint>
extern "C" {
#else
#include <stdint.h>
#endif

//--------------------------------------------------------------------------------
// native Cバインディング/型定義
//--------------------------------------------------------------------------------
typedef struct manager usb_manager_t;	// UsbManager
/**
 * USB機器が接続されたときのコールバック関数
 */
typedef void (*on_device_attach_t)(usb_manager_t*, void *callback_args, int32_t device_id);
/**
 * USB機器が取り外されたときのコールバック関数
 */
typedef void (*on_device_detach_t)(usb_manager_t*, void *callback_args, int32_t device_id);

/**
 * UVC機器との接続状態
 */
typedef enum device_state {
	/**
	 * プラグインが初期化されていない
	 */
	UNINITIALIZED = -1,
	/**
	 * 指定した機器が接続されていない
	 */
	DISCONNECTED = 0,
	/**
	 * 指定した機器が接続されている(映像/音声取得中ではない)
	 */
	CONNECTED = 1,
	/**
	 * 指定した機器が接続されており映像取得中
	 */
	STREAMING = 2,
} device_state_t;

//--------------------------------------------------------------------------------
// native Cバインディング/USB
//--------------------------------------------------------------------------------
/**
 * USBデバイスマネジャーを初期化
 * 返り値のポインターはrelease_managerを呼びまで有効
 * null以外のポインターが返った場合は必ずrelease_managerを呼び出すこと
 * @return USBデバイスマネージャーを示すポインター
 */
usb_manager_t *manager_init(void *args, on_device_attach_t, on_device_detach_t);

/**
 * USBデバイスマネジャーを破棄
 * @param manager USBデバイスマネージャーを示すポインター
 */
void manager_release(usb_manager_t *manager);

/**
 * デバイスまたはインターフェースが指定した条件に一致するかどうかを確認
 * @param manager
 * @param device_id
 * @param bClass     0xffなら常にマッチする(ワイルドカード)
 * @param bSubClass  0xffなら常にマッチする(ワイルドカード)
 * @param bProtocol  0xffなら常にマッチする(ワイルドカード)
 * @return 1: 一致した, 0: 一致しなかった
 */
int32_t usb_match(usb_manager_t *manager, int32_t device_id, uint8_t bClass, uint8_t bSubClass, uint8_t bProtocol);

/**
 * bcdUSBを取得
 * @param manager USBデバイスマネージャーを示すポインター
 * @param device USB機器を指定するポインター
 * @return bcdUSBまたは0(deviceで指定したUSB機器が存在しない場合)
 */
uint16_t usb_get_bcd_usb(usb_manager_t *manager, int32_t device_id);

/**
 * デバイスクラスを取得
 * @param manager USBデバイスマネージャーを示すポインター
 * @param device USB機器を指定するポインター
 * @return デバイスクラスまたは0(deviceで指定したUSB機器が存在しない場合)
 */
uint8_t usb_get_device_class(usb_manager_t *manager, int32_t device_id);

/**
 * デバイスサブクラスを取得
 * @param manager USBデバイスマネージャーを示すポインター
 * @param device USB機器を指定するポインター
 * @return デバイスサブクラスまたは0(deviceで指定したUSB機器が存在しない場合)
 */
uint8_t usb_get_device_sub_class(usb_manager_t *manager, int32_t device_id);

/**
 * デバイスプロトコルを取得
 * @param manager USBデバイスマネージャーを示すポインター
 * @param device USB機器を指定するポインター
 * @return デバイスプロトコルまたは0(deviceで指定したUSB機器が存在しない場合)
 */
uint8_t usb_get_device_protocol(usb_manager_t *manager, int32_t device_id);

/**
 * ベンダーIDを取得する
 * @param manager USBデバイスマネージャーを示すポインター
 * @param device USB機器を指定するポインター
 * @return ベンダーIDまたは0(deviceで指定したUSB機器が存在しない場合)
 */
uint16_t usb_get_vendor_id(usb_manager_t *manager, int32_t device_id);

/**
 * プロダクトIDを取得する
 * @param manager USBデバイスマネージャーを示すポインター
 * @param device USB機器を指定するポインター
 * @return プロダクトIDまたは0(deviceで指定したUSB機器が存在しない場合)
 */
uint16_t usb_get_product_id(usb_manager_t *manager, int32_t device_id);

/**
 * 機器名文字列を取得する
 * @param manager USBデバイスマネージャーを示すポインター
 * @param device USB機器を指定するポインター
 * @param buffer 機器名文字列を受け取るバッファ
 * @param buf_size 機器名文字列を受け取るバッファのサイズ, null以外なら機器名文字列の長さをセットする
 * @return 0: 正常終了, 負数: エラー
 */
int usb_get_name(usb_manager_t *manager, int32_t device_id, char *, size_t *);

//--------------------------------------------------------------------------------
// native Cバインディング/UVC
//--------------------------------------------------------------------------------
/**
 * UVC機器のコントロール機能の情報を取得するための構造体
 */
typedef struct _control_info {
public:
	uint64_t type;			// UVCコントロールの種類(CTRL_XXXまたはPU_XXX)
	int32_t initialized;	// 初期化済みかどうか
	int32_t has_min_max;	// 最大最小値を持つかどうか
	int32_t def;			// デフォルト値
	int32_t current;		// 現在値
	int32_t res;			// 分解能
	int32_t min;			// 最小値
	int32_t max;			// 最大値
} __attribute__((__packed__)) control_info_t;

/**
 * 映像サイズ設定をやりとりするための構造体定義
 */
typedef struct video_size {
public:
	uint32_t frame_type;
	/**
	 * フレームインデックス
	 */
	int32_t frame_index;
	/**
	 * 映像幅[ピクセル数]
	 */
	uint32_t width;
	/**
	 * 映像高さ[ピクセル数]
	 */
	uint32_t height;
	/**
	 * フレームレートのタイプ
	 * 0: min/max/stepの3つのuint32_tで指定
	 * 正数: フレームインターバルデータの個数
	 */
	int32_t frame_interval_type;
	/**
	 * フレームインターバルデータ
	 */
	uint32_t *frame_intervals;
	/**
	 * フレームインターバルデータの個数
	 */
	int32_t num_frame_intervals;
	/**
	 * フレームレート
	 */
	float *fps;
	/**
	 * フレームレートの個数
	 */
	int32_t num_fps;
} __attribute__((__packed__)) video_size_t;

/**
 * 映像フォーマット
 * 一般的にUVC機器が対応しているのはYUYV(YUY2)とMJPEGのみ、一部UVC機器ではH264にも対応
 * #uvc_resizeで指定できるのはUVC機器が対応している映像フォーマットのみ
 * #uvc_get_frameにはUVC機器が対応していない映像フォーマットも指定できるが
 * その場合には内部で変換処理が実行される(ただしMJPEGやH264へは変換できない)
 */
typedef enum raw_frame {
	RAW_FRAME_UNKNOWN				= 0,
	RAW_FRAME_UNCOMPRESSED_YUYV		= 0x00010005,
	RAW_FRAME_UNCOMPRESSED_NV21		= 0x00050005,
	RAW_FRAME_UNCOMPRESSED_NV12		= 0x000b0005,
	RAW_FRAME_UNCOMPRESSED_RGB565	= 0x000d0005,
	RAW_FRAME_UNCOMPRESSED_RGBX		= 0x00100005,
	RAW_FRAME_MJPEG					= 0x00000007,	// #uvc_get_frameの変換先映像フォーマットとしては無効
	RAW_FRAME_H264					= 0x00000014,	// #uvc_get_frameの変換先映像フォーマットとしては無効
} raw_frame_t;

// Camera Terminal DescriptorのbmControlsフィールドのビットマスク
#define	CTRL_SCANNING		(0x00000001)	// D0:  Scanning Mode
#define	CTRL_AE				(0x00000002)	// D1:  Auto-Exposure Mode
#define	CTRL_AE_PRIORITY	(0x00000004)	// D2:  Auto-Exposure Priority
#define	CTRL_AE_ABS			(0x00000008)	// D3:  Exposure Time (Absolute)
#define CTRL_FOCUS_ABS    	(0x00000020)	// D5:  Focus (Absolute)
#define CTRL_IRIS_ABS		(0x00000080)	// D7:  Iris (Absolute)
#define	CTRL_ZOOM_ABS		(0x00000200)	// D9:  Zoom (Absolute)
#define	CTRL_PAN_ABS		(0x01000800)	// D11: PanTilt (Absolute)
#define	CTRL_TILT_ABS		(0x02000800)	// D11: PanTilt (Absolute)
#define CTRL_ROLL_ABS		(0x00002000)	// D13: Roll (Absolute)
#define CTRL_FOCUS_AUTO		(0x00020000)	// D17: Focus, Auto
#define CTRL_PRIVACY		(0x00040000)	// D18: Privacy

// Processing Unit DescriptorのbmControlsフィールドのビットマスク
#define PU_BRIGHTNESS		(0x00000001)	// D0: Brightness
#define PU_CONTRAST			(0x00000002)	// D1: Contrast
#define PU_HUE				(0x00000004)	// D2: Hue
#define	PU_SATURATION		(0x00000008)	// D3: Saturation
#define PU_SHARPNESS		(0x00000010)	// D4: Sharpness
#define PU_GAMMA			(0x00000020)	// D5: Gamma
#define	PU_WB_TEMP			(0x00000040)	// D6: White Balance Temperature
#define	PU_WB_COMPO			(0x00000080)	// D7: White Balance Component
#define	PU_BACKLIGHT		(0x00000100)	// D8: Backlight Compensation
#define PU_GAIN				(0x00000200)	// D9: Gain
#define PU_POWER_LF			(0x00000400)	// D10: Power Line Frequency
#define PU_HUE_AUTO			(0x00000800)	// D11: Hue, Auto
#define PU_WB_TEMP_AUTO		(0x00001000)	// D12: White Balance Temperature, Auto
#define PU_WB_COMPO_AUTO	(0x00002000)	// D13: White Balance Component, Auto
#define PU_CONTRAST_AUTO	(0x00040000)	// D18: Contrast, Auto

#define PU_MASK				(0x80000000)

/**
 * 機器との接続状態を取得
 * @return
 */
device_state_t uvc_get_device_state(usb_manager_t *manager, int32_t device_id);

/**
 * UVC機器の基本設定をセット
 * @param device_id
 * @param enabled
 * @param use_first_config
 * @return
 */
int uvc_set_config(
	usb_manager_t *manager, int32_t device_id,
	int32_t enabled, uint8_t use_first_config);

/**
 * UVC機器からの映像サイズの変更要求
 * @param device_id UVC機器識別用のID
 * @param width
 * @param height
 * @return
 */
int uvc_resize(
	usb_manager_t *manager, int32_t device_id,
	uint32_t frame_type,
	uint32_t width, uint32_t height);

/**
 * 映像取得開始
 * @param device_id UVC機器識別用のID
 * @return
 */
int uvc_start(usb_manager_t *manager, int32_t device_id);

/**
 * 映像取得終了
 * @param device_id UVC機器識別用のID
 * @return
 */
int uvc_stop(usb_manager_t *manager, int32_t device_id);

/**
 * コントロール機能でサポートしている機能を取得
 * @param device_id
 * @return
 */
uint64_t uvc_get_ctrl_supports(usb_manager_t *manager, int32_t device_id);

/**
 * プロセッシングユニットでサポートしている機能を取得
 * @param device_id
 * @return
 */
uint64_t uvc_get_proc_supports(usb_manager_t *manager, int32_t device_id);

/**
 * native側でUVC設定機能へアクセスするときのヘルパー関数
 * @param device_id
 * @param info
 * @return 0: 成功, 負: エラーコード
 */
int uvc_get_control_info(
	usb_manager_t *manager, int32_t device_id,
	control_info_t *info);

/**
 * native側でUVC設定機能へアクセスするときのヘルパー関数
 * @param device_id
 * @param type
 * @param value
 * @return 0: 成功, 負: エラーコード
 */
int uvc_set_control_value(
	usb_manager_t *manager, int32_t device_id,
	uint64_t type, int32_t value);

/**
 * native側でUVC設定機能へアクセスするときのヘルパー関数
 * @param device_id
 * @param type
 * @param value
 * @return 0: 成功, 負: エラーコード
 */
int uvc_get_control_value(
	usb_manager_t *manager, int32_t device_id,
	uint64_t type, int32_t *value);

/**
 * native側でUVC映像サイズ設定へアクセスするときのヘルパー関数
 * @param device_id
 * @param index 映像サイズ設定のインデックス
 * @param num_supported 対応している映像サイズ設定の数を入れるuint32_tへのポインタ
 * @param size 映像サイズ設定を書き込むためのvideo_size_t構造体へのポインタ
 * @return 0: 成功, 負: エラーコード
 */
int uvc_get_supported_size(
	usb_manager_t *manager, int32_t device_id,
	int32_t index, int32_t *num_supported, video_size_t *size);

/**
 * 現在選択されている映像サイズを取得
 * @param manager
 * @param device_id
 * @param size
 * @return
 */
int uvc_get_current_size(
	usb_manager_t *manager, int32_t device_id,
	video_size_t *size);

/**
 * UVC機器からの映像を取得する
 * #uvc_set_surfaceと同時使用しないこと
 * @param manager
 * @param device_id
 * @param frame_type 呼び出し時には変換先映像フォーマット(nullptrまたはRAW_FRAME_UNKNOWNなら変換しない)
 *                   nullptrでなければ関数実行後に実際の映像フォーマットがセットされる
 * @param width
 * @param height
 * @param data 映像データバッファ
 * @param data_len 呼び出し時には映像データバッファのサイズ、nullptrでなければ関数実行後に実際の映像データサイズがセットされる
 * @param pts_us
 * @param flags
 * @return
 */
int uvc_get_frame(
	usb_manager_t *manager, int32_t device_id,
	uint32_t *frame_type, uint32_t *width, uint32_t *height,
	uint8_t *data, uint32_t *data_len,
	int64_t *pts_us, uint32_t *flags);

/**
 * UVC機器からの映像を描画するSurface(ANativeWindow)をセットする
 * #uvc_get_frameと同時使用しないこと
 * @param manager
 * @param device_id
 * @param surface
 * @param mvp_matrix 要素数16以上, nullptrなら単位行列をセットする
 * @return
 */
int uvc_set_surface(
	usb_manager_t *manager, int32_t device_id,
	ANativeWindow *surface, float *mvp_matrix);

/**
 * モデルビュー変換行列を設定
 * Surface(ANativeWindow)で映像を受け取るときのみ有効
 * @param manager
 * @param device_id
 * @param mvp_matrix 要素数16以上, nullptrなら単位行列をセットする
 * @return
 */
int uvc_set_mvp_matrix(
	usb_manager_t *manager, int32_t device_id,
	float *mvp_matrix);

//--------------------------------------------------------------------------------
// native Cバインディング/UAC
//--------------------------------------------------------------------------------
typedef struct _uac_info {
	int32_t device_id;
	int32_t channels;
	int32_t resolution;
	uint32_t sampling_freq;
	int32_t packet_bytes;
} __attribute__((__packed__)) uac_info_t;

/**
 * 機器との接続状態を取得
 * @return
 */
device_state_t uac_get_device_state(usb_manager_t *manager, int32_t device_id);

/**
 * 音声取得開始
 * uac_get_frameを呼び出さないと音声取得できない
 * @param device_id UVC機器識別用のID
 * @return
 */
int uac_start(usb_manager_t *manager, int32_t device_id);

/**
 * 音声取得終了
 * @param device_id UVC機器識別用のID
 * @return
 */
int uac_stop(usb_manager_t *manager, int32_t device_id);

/**
 * 音声取得設定を取得
 * @param device_id UVC機器識別用のID
 * @param info
 * @return
 */
int uac_get_info(
	usb_manager_t *manager, int32_t device_id,
	uac_info_t *info);

/**
 * 音声フレームをフレームキューから読み取る
 * @param device_id UVC機器識別用のID
 * @param data nullptrなら*lenにフレームデータのバイト数をセットするだけで実際の読み取りは行わない
 * @param data_len 音声フレームのバイト数
 * @param pts_us 音声データ受信時のシステムタイム[マイクロ秒]
 * @return
 */
int uac_get_frame(
	usb_manager_t *manager, int32_t device_id,
	uint8_t *data, uint32_t *data_len, int64_t *pts_us);

#ifdef __cplusplus
};
#endif

#endif //AANDUSB_NATIVE_BINDING_H_
