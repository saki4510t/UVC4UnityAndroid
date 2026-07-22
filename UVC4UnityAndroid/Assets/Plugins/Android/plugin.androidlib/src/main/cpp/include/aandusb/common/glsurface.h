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

#ifndef AANDUSB_GL_SURFACE_H_
#define AANDUSB_GL_SURFACE_H_

namespace serenegiant::egl {

//================================================================================
/**
 * egl::EglSurface, egl::EglImageWrapper等のOpenGLGLESのSurface系クラスが実装する関数を
 * 定義するための純粋仮想クラス
 */
class IGLSurface {
public:
	IGLSurface() = default;
	virtual ~IGLSurface() = default;
	/**
	 * このオブジェクトが保持しているSurfaceへアタッチして描画できるようにする
	 * @return
	 */
	virtual int bind() = 0;
	/**
	 * このオブジェクトが保持しているSurfaceへアタッチして描画できるようにする
	 * @return
	 */
	virtual int unbind() = 0;
	/**
	 * このオブジェクトが保持しているSurfaceへアタッチして描画できるようにする
	 * #bindのシノニム
	 * #bindの別名
	 * @return
	 */
	inline int make_current() { return bind(); };
	/**
	 * このオブジェクトが保持しているSurfaceからデタッチする
	 * #unbindのシノニム
	 * @return
	 */
	inline int make_default() { return unbind(); };

	[[nodiscard]]
	virtual int32_t width() const = 0;
	[[nodiscard]]
	virtual int32_t height() const = 0;
};

}

#endif //AANDUSB_GL_SURFACE_H_
