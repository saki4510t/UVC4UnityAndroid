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
// common
#include "common/_onload.h"
#include "common/jni_utils.h"

extern bool init_hardware_buffer();
extern bool init_image_decoder();
extern bool init_image_reader();
extern bool init_media_ndk();
extern bool init_media_muxer_ndk();
extern bool init_surface_texture_ndk();
extern bool init_camera_ndk();
extern int InitVulkan();
extern int register_device_detector(JNIEnv *env);

namespace sere = serenegiant;

jint JNI_OnLoad(JavaVM *vm, void *) {
    ENTER();

    JNIEnv *env;
    if (vm->GetEnv(reinterpret_cast<void **>(&env), JNI_VERSION_1_6) != JNI_OK) {
        return JNI_ERR;
    }

	init_hardware_buffer();
	init_image_decoder();
	init_image_reader();
	init_media_ndk();
	init_media_muxer_ndk();
	init_surface_texture_ndk();
	init_camera_ndk();
	InitVulkan();
	int result = register_device_detector(env);
	env->ExceptionClear();
	CHECK(!result);

	sere::setVM(vm);

    LOGD("JNI_OnLoad:finished:result=%d", result);
    RETURN(JNI_VERSION_1_6, jint);
}
