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

#ifndef SERENEGIANT_SVK_TEXTURE_H
#define SERENEGIANT_SVK_TEXTURE_H

// 標準ライブラリ
#include <memory>

#if defined(__ANDROID__)
#include <android/asset_manager.h>
// common
#include "common/hardware_buffer_stub.h"
#endif	// __ANDROID__

// vulkan
#include "vulkan/svk_defs.h"
#include "vulkan/svk_image.h"	// SVkImage

//--------------------------------------------------------------------------------
// 外部クラスの前方宣言
namespace serenegiant::vulkan {
// 前方参照宣言
class SVkContext;
class SVkSampler;
}
// 外部クラスの前方宣言ここまで
//--------------------------------------------------------------------------------

namespace serenegiant::vulkan {

/**
 * SVkImageを継承してテクスチャとして扱うためのヘルパークラス
 */
class SVkTexture : public virtual SVkImage {
private:
protected:
public:
	/**
	 * VkTexture生成用のヘルパー関数
	 * サイズを指定してVulkanで確保したメモリーを使う
	 * @param vk_context
	 * @param width
	 * @param height
	 * @param usage
	 * @param required_props
	 * @return
	 */
	static std::shared_ptr<SVkTexture> create(
		const std::shared_ptr<SVkContext> &vk_context,
		const uint32_t &width, const uint32_t &height,
		const VkFormat &format, const VkImageUsageFlags &usage,
	  	const VkMemoryPropertyFlags &required_props,
		std::shared_ptr<SVkSampler> vk_sampler = nullptr);
	/**
	 * 既存のVkImage/VkDeviceMemoryをラップして新しいVkTextureを生成する
	 * @param width
	 * @param height
	 * @param vk_format
	 * @param layout
	 * @param vk_image
	 * @param vk_memory
	 * @param allocate_size
	 * @param aspect_mask
	 * @return
	 */
	static std::shared_ptr<SVkTexture> create(
		const std::shared_ptr<SVkContext> &vk_context,
		const uint32_t &width, const uint32_t &height,
		const VkFormat &vk_format, const VkImageLayout &layout,
		VkImage vk_image, VkDeviceMemory vk_memory,
		const VkDeviceSize &allocate_size,
		const VkImageAspectFlags &aspect_mask,
		std::shared_ptr<SVkSampler> vk_sampler = nullptr);
#if defined(__ANDROID__)
	/**
	 * VkTexture生成用のヘルパー関数
	 * Vulkanで確保したメモリーを使って指定したファイル名でアセットから映像を読み込む
	 * @param app
	 * @param vk_context
	 * @param file_path
	 * @param usage
	 * @param required_props
	 * @return
	 */
	static std::shared_ptr<SVkTexture> create_from_assets(
		AAssetManager *assetManager,
		const std::shared_ptr<SVkContext> &vk_context,
		const char *file_path,
		const VkImageUsageFlags &usage,
		const VkMemoryPropertyFlags &required_props);
	/**
	 * VkTexture生成用のヘルパー関数
	 * 外部で生成したAHardwareBufferをラップしてVulkanのテクスチャとして扱えるようにする
	 * @param vk_context
	 * @param buffer
	 * @param layout
	 * @param usage
	 * @param use_external_format
	 * @param sync_fd
	 * @return
	 */
	static std::shared_ptr<SVkTexture> create_from_AHardwareBuffer(
		const std::shared_ptr<SVkContext> &vk_context,
		AHardwareBuffer *buffer,
		const VkImageLayout &layout,
		const VkImageUsageFlags &usage,
		const bool &use_external_format = false,
		int sync_fd = -1);
	/**
	 * VkTexture生成用のヘルパー関数
	 * 内部で生成したAHardwareBufferをラップしてVulkanのテクスチャとして扱えるようにする
	 * @param vk_context
	 * @param width
	 * @param height
	 * @param format これはAHardwareBuffer生成時のフォーマット
	 * @param hb_usage AHardwareBufferのusageフラグ
	 * @param layout これはVkImage生成時のlayout
	 * @param vk_usage これはVkImage生成時のusage
	 * @param use_external_format
	 * @return
	 */
	static std::shared_ptr<SVkTexture> create_with_AHardwareBuffer(
		const std::shared_ptr<SVkContext> &vk_context,
		const uint32_t &width, const uint32_t &height,
		const AHardwareBufferFormat_t &format = AHARDWAREBUFFER_FORMAT_R8G8B8A8_UNORM,
		const uint64_t &hb_usage = SHARED_TEXTURE_DEFAULT_USAGE,
		const VkImageLayout &layout = VK_IMAGE_LAYOUT_GENERAL,
		const VkImageUsageFlags &vk_usage = SHARED_TEXTURE_DEFAULT_VK_USAGE,
		const bool &use_external_format = false);
#endif
	/**
	 * コンストラクタ
	 * @param vk_context
	 */
	explicit SVkTexture(std::shared_ptr<SVkContext> vk_context, std::shared_ptr<SVkSampler> vk_sampler = nullptr);
	/**
	 * デストラクタ
	 */
	~SVkTexture() noexcept override;

};

using SVkTextureSp = std::shared_ptr<SVkTexture>;
using SVkTextureUp = std::unique_ptr<SVkTexture>;

}	// namespace serenegiant::vulkan

#endif //SERENEGIANT_SVK_TEXTURE_H
