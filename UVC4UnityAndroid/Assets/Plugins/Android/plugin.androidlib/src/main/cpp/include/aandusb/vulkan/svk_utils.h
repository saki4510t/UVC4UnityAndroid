/*
 * Copyright (C) 2017 The Android Open Source Project
 * Copyright (c) 2022-2026 saki t_saki@serenegiant.com
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */

#ifndef SERENEGIANT_SVK_UTILS_H
#define SERENEGIANT_SVK_UTILS_H

// 標準ライブラリ
#include <memory>
#include <vector>
// vulkan
#include "vulkan/svk_defs.h"

#if defined(__ANDROID__)
// android
#include <android/asset_manager.h>
#endif

//--------------------------------------------------------------------------------
// 外部クラスの前方宣言
namespace serenegiant::vulkan {
// 前方参照宣言
class SVkContext;
class SVkTexture;
}
// 外部クラスの前方宣言ここまで
//--------------------------------------------------------------------------------

namespace serenegiant::vulkan {

using VkFrameBufferAttachment = struct VkFrameBufferAttachment {
	VkImage image = VK_NULL_HANDLE;
	VkDeviceMemory memory = VK_NULL_HANDLE;
	VkImageView view = VK_NULL_HANDLE;
};

extern PFN_vkBindImageMemory2 vkBindImageMemory2;
extern PFN_vkGetImageMemoryRequirements2 vkGetImageMemoryRequirements2;
extern PFN_vkCreateSamplerYcbcrConversion vkCreateSamplerYcbcrConversion;
extern PFN_vkDestroySamplerYcbcrConversion vkDestroySamplerYcbcrConversion;
extern PFN_vkImportSemaphoreFdKHR vkImportSemaphoreFd;
extern PFN_vkGetSemaphoreFdKHR vkGetSemaphoreFdKHR;
extern PFN_vkGetAndroidHardwareBufferPropertiesANDROID vkGetAndroidHardwareBufferProperties;
//extern PFN_vkGetPhysicalDeviceFeatures2 vkGetPhysicalDeviceFeatures2;

/*
 * build_shader_from_assets()
 *   Create a Vulkan shader module from the given glsl shader file
 *   Input shader is compiled with shaderc (https://github.com/google/shaderc)
 *   prebuilt binary on github (https://github.com/ggfan/shaderc/release)
 *
 *   The pre-built shaderc lib is packed as CDep format of:
 *      https://github.com/google/cdep
 *   Refer to full documentation from the above homepage
 *
 *   feedback for CDep is very welcome to the https://github.com/google/cdep
 * Input:
 *     appInfo:   android_app, from which get AAssertManager*
 *     filePaht:  shader file full name with path inside APK/assets
 *     type:      borrowed VK's shader type to indicate which glsl shader it is
 *     vkDevice:  Vulkan logical device
 * Output:
 *     result_shader:  built shader module return to caller
 * Return:
 *     VK_SUCCESS: shader module is at result_shader
 *     Others:  error happened, no shader module created at all
 */

#if defined(__ANDROID__)
VkResult build_shader_from_assets(
	AAssetManager* assetManager,
    const char* filePath,
	VkDevice vkDevice,
	const VkShaderStageFlagBits &type,
    VkShaderModule &result_shader);
#endif

VkResult build_shader_from_file(
	const char* filePath,
	VkDevice vkDevice,
	const VkShaderStageFlagBits &type,
	VkShaderModule &result_shader);

/**
 * コンパイル済みのSPVシェーダーでVkShaderModuleを初期化する
 * @param vkDevice
 * @param spv_code_size
 * @param spv_code_bytes
 * @param result_shader
 * @return
 */
VkResult build_shader_from_spv(
	VkDevice vkDevice,
	const size_t &spv_code_size,
	const uint32_t *spv_code_bytes,
	VkShaderModule &result_shader);

#if defined(ENABLE_SHADERRC)
/**
 * シェーダーを生成
 * @param vkDevice
 * @param shader_source
 * @param type
 * @param result_shader
 * @return
 */
VkResult build_shader_from_glsl(
	VkDevice vkDevice,
	const std::vector<char> &shader_source,
	VkShaderStageFlagBits type,
	VkShaderModule &result_shader);
#endif

/**
 * レイアウト変換のvkCmdPipelineBarrierをコマンドバッファへ追加
 * @param cmd
 * @param image
 * @param old_layout
 * @param new_layout
 * @param old_access_flags
 * @param new_access_flags
 * @param old_stage_flags
 * @param new_stage_flags
 * @return
 */
VkImageLayout record_layout_transition_barrier(
	VkCommandBuffer &vk_cmd_buffer, VkImage &vk_image,
	const VkImageLayout &old_layout,
	const VkImageLayout &new_layout,
	const VkAccessFlags &old_access_flags,
	const VkAccessFlags &new_access_flags,
	const VkPipelineStageFlags &old_stage_flags,
	const VkPipelineStageFlags &new_stage_flags);

/**
 * レイアウト変換のvkCmdPipelineBarrierをコマンドバッファへ追加
 * @param cmd
 * @param image
 * @param old_layout
 * @param new_layout
 * @param preserve_data
 * @return
 */
VkImageLayout record_layout_transition_barrier(
	VkCommandBuffer &vk_cmd_buffer, VkImage &image,
	VkImageLayout old_layout, VkImageLayout new_layout,
	const bool &preserve_data = true);

VkImageLayout transition_layout(
	const std::shared_ptr<SVkContext> &svk_context,
	VkImage &vk_image,
	VkImageLayout old_layout, VkImageLayout new_layout);

void setImageLayout(
	VkCommandBuffer vk_cmd_buffer, VkImage &vk_image,
	VkImageLayout old_layout, VkImageLayout new_layout,
	VkPipelineStageFlags srcStages,
	VkPipelineStageFlags destStages);

/**
 * VkTextureを別のVkTextureへコピーする
 * @param svk_context
 * @param width
 * @param height
 * @param src
 * @param dst
 */
void copy_texture(
	const std::shared_ptr<SVkContext> &svk_context,
	const uint32_t &width, const uint32_t &height,
	std::shared_ptr<SVkTexture> &src, std::shared_ptr<SVkTexture> &dst);

}	// namespace serenegiant::vulkan

#endif // SERENEGIANT_SVK_UTILS_H
