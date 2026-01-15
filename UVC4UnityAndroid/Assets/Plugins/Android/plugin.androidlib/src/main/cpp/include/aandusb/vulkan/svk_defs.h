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

#ifndef SERENEGIANT_SVK_DEFS_H
#define SERENEGIANT_SVK_DEFS_H

#if defined(__ANDROID__)
// common
#include "common/hardware_buffer_stub.h"
#endif	// __ANDROID__
// vulkan
#include "vulkan/vulkan_wrapper.h"

namespace serenegiant::vulkan {

// Vulkan call wrapper
#define CALL_VK(func) \
    { \
        auto _res = (func); \
        if (VK_SUCCESS != _res) { \
            LOGE("Vulkan error. %d", _res); \
            assert(false); \
        } \
    }

// A macro to check value is VK_SUCCESS
// Used also for non-vulkan functions but return VK_SUCCESS
#define VK_CHECK(x) CALL_VK(x)

// テクスチャとして使うときのVkImageのフォーマットのデフォルト
#define TEXTURE_FORMAT_DEFAULT (VK_FORMAT_R8G8B8A8_UNORM)
// テクスチャ用のディスクリプタセットの最大数のデフォルト
#define DEFAULT_MAX_NUM_TEXTURE_DESC (8)
// ユニフォームバッファ用のディスクリプタセットの最大数のデフォルト
#define DEFAULT_MAX_NUM_UNIFORM_DESC (8)

#if defined(__ANDROID__)
using AHardwareBufferFormat_t = enum AHardwareBuffer_Format;

/**
 * AHardwareBufferをVulkanのテクスチャとして使うときの
 * デフォルトのusageフラグ
 * テクスチャとして使う＆フレームバッファとして使う
 * CPUからはあまり読み書きしない
 */
#define SHARED_TEXTURE_DEFAULT_USAGE (AHARDWAREBUFFER_USAGE_GPU_SAMPLED_IMAGE \
	| AHARDWAREBUFFER_USAGE_GPU_FRAMEBUFFER \
	| AHARDWAREBUFFER_USAGE_CPU_WRITE_RARELY \
	| AHARDWAREBUFFER_USAGE_CPU_READ_RARELY \
	)

#endif // __ANDROID__

/**
 * Vulkanで共有テクスチャを使うときのデフォルトのusageフラグ
 */
#define SHARED_TEXTURE_DEFAULT_VK_USAGE (VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT \
	| VK_IMAGE_USAGE_SAMPLED_BIT  \
	| VK_IMAGE_USAGE_TRANSFER_SRC_BIT  \
	| VK_IMAGE_USAGE_TRANSFER_DST_BIT  \
	)

}	// namespace serenegiant::vulkan

#endif //SERENEGIANT_SVK_DEFS_H
