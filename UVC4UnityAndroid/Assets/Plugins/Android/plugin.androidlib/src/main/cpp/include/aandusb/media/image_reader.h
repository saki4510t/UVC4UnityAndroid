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

#ifndef AANDUSB_IMAGE_READER_H
#define AANDUSB_IMAGE_READER_H

#include <functional>
#include <memory>
#include <mutex>

#include <android/native_window.h>
#include "media/image_reader_stub.h"

namespace serenegiant::media {

/**
 * ImageReaderのデフォルトのバッファの数
 */
#define IMAGE_READER_DEFAULT_BUF_NUM (2)

/**
 * ImageReaderのデフォルトの用途フラグ
 * ・GPUのテクスチャとして使う
 * ・CPUからは頻繁に読み込む
 * ・CPUからはほとんど書き込まない
 */
#define IMAGE_READER_DEFAULT_USAGE \
	(AHARDWAREBUFFER_USAGE_GPU_SAMPLED_IMAGE \
	| AHARDWAREBUFFER_USAGE_CPU_READ_OFTEN \
	| AHARDWAREBUFFER_USAGE_CPU_WRITE_RARELY)

class ImageReader;
/**
 * ImageReaderが映像を受け取ったときのコールバック
 */
using on_image_available_t = std::function<void(ImageReader &reader)>;

/**
 * AImageReaderのラッパークラス
 * AImageReader自体はAPI>=24だけどAAImageReader_newWithUsageで
 * 用途フラグをを指定したいのでAPI>=26
 */
class ImageReader {
private:
	/**
	 * ImageReaderに対応しているかどうか
	 * 実行環境がAPI>=26なら対応しているはず
	 */
	const bool m_supported;
	/**
	 * バッファの数
	 */
	const int m_buf_num;
	/**
	 * 映像サイズ(幅)
	 */
	const int32_t m_width;
	/**
	 * 映像サイズ(高さ)
	 */
	const int32_t m_height;
	/**
	 * コールバックのファンクショナルの排他制御用ミューテックス
	 */
	mutable std::mutex callback_mutex;
	/**
	 * AImageReaderインスタンス
	 */
	AImageReader *m_reader;
	/**
	 * 取得可能なバッファの数
	 */
	uint8_t m_available_count;
	/**
	 * コールバック関数
	 */
	on_image_available_t m_on_image_available;

	/**
	 * AImageReaderからのコールバック関数
	 * @param context ImageReaderへのポインタ
	 * @param reader
	 */
	static void on_image_available_func(void *context, AImageReader *reader);
	/**
	 * AImageReaderからのコールバック関数が呼ばれたときの実際の処理
	 * @param reader
	 */
	void on_image_available(AImageReader *reader);
	/**
	 * render_imageの下請け, m_rotation=0の時
	 * @param buf
	 * @param image
	 * @return
	 */
	static int render_image0(ANativeWindow_Buffer *buf, AImage *image);
	/**
	 * render_imageの下請け, m_rotation=90の時
	 * @param buf
	 * @param image
	 * @return
	 */
	static int render_image90(ANativeWindow_Buffer *buf, AImage *image);
	/**
	 * render_imageの下請け, m_rotation=180の時
	 * @param buf
	 * @param image
	 * @return
	 */
	static int render_image180(ANativeWindow_Buffer *buf, AImage *image);
	/**
	 * render_imageの下請け, m_rotation=270の時
	 * @param buf
	 * @param image
	 * @return
	 */
	static int render_image270(ANativeWindow_Buffer *buf, AImage *image);
protected:
public:
	/**
	 * コンストラクタ
	 * @param width
	 * @param height
	 * @param format
	 * @param usage デフォルトはIMAGE_READER_DEFAULT_USAGE
	 * @param buf_num デフォルトはIMAGE_READER_DEFAULT_BUF_NUM(2)
	 */
	ImageReader(
		const int32_t &width, const int32_t &height,
		const enum AIMAGE_FORMATS &format,
		const uint64_t &usage = IMAGE_READER_DEFAULT_USAGE,
		const int &buf_num = IMAGE_READER_DEFAULT_BUF_NUM);
	/**
	 * デストラクタ
	 */
	~ImageReader() noexcept;

	/**
	 * AImageReader_xxxに対応しているかどうかを取得
	 * falseお場合このクラスでは何もしない
	 * @return
	 */
	inline bool is_supported() const { return m_supported && (m_reader != nullptr); };
	/**
	 * 受け取る映像サイズ(幅)を取得
	 * @return
	 */
	inline int32_t width() const { return m_width; };
	/**
	 * 受け取る映像サイズ(高さ)を取得
	 * @return
	 */
	inline int32_t height() const { return m_height; };
	/**
	 * 映像受け取り用のANativeWindows*を取得
	 * @return
	 */
	ANativeWindow *native_window();
	/**
	 * 読み取った映像を取得
	 * @return
	 */
	AImage *get_next_image();
	/**
	 * 読み取った映像のうち最新の物を取得、古い物は破棄される
	 * @return
	 */
	AImage *get_latest_image();
	/**
	 * 現在取得できる最大の映像数を取得する
	 * @return
	 */
	int32_t get_max_image();
	/**
	 * 取得可能な映像が存在しているかどうかを取得
	 * @return
	 */
	inline uint8_t has_image() const { return m_available_count; };
	/**
	 * 映像受け取り時のコールバックを設定
	 * @param on_image_available
	 * @return
	 */
	int set_on_image_available(const on_image_available_t &on_image_available);
	/**
	 *　受け取った映像を破棄する
	 * @param image
	 */
	static void delete_image(AImage *image);

	/**
	 * 描画用のヘルパー関数
	 * set_rotationで設定した回転角を反映して映像をANativeWindow_Bufferへ書き出す
	 * AImageのYUV420spをRGBA8888/RGBX8888へ変換する
	 * @param buf
	 * @param image
	 * @return
	 */
	static int render_image(ANativeWindow_Buffer *buf, AImage *image, const int &rotation);
};

using ImageReaderSp = std::shared_ptr<ImageReader>;
using ImageReaderUp = std::unique_ptr<ImageReader>;

}	// namespace serenegiant::media

#endif //AANDUSB_IMAGE_READER_H
