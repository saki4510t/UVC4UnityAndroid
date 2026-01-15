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

#ifndef SERENEGIANT_SVK_IMAGE_H
#define SERENEGIANT_SVK_IMAGE_H

// 標準ライブラリ
#include <memory>
#if defined(__ANDROID__)
#include <android/hardware_buffer.h>
#endif
// vulkan
#include "vulkan/svk_defs.h"

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
 * VkImageと関係するリソースのラッパークラス
 */
class SVkImage {
private:
	std::shared_ptr<SVkContext> m_vk_context;
	VkMemoryPropertyFlags m_memory_properties;
	uint32_t m_width;
	uint32_t m_height;
	VkDeviceSize m_allocate_bytes;
	VkFormat m_vk_image_format;
	VkImageLayout m_vk_image_layout;
	VkImage m_vk_image;
	VkDeviceMemory m_vk_memory;
	VkImageView m_vk_image_view;
	VkSemaphore m_vk_semaphore;
	std::shared_ptr<SVkSampler> m_vk_sampler;
#if defined(__ANDROID__)
	AHardwareBuffer *m_buffer;
#endif
	volatile bool m_mapped;
	void *m_mapped_ptr;
protected:
	bool m_own_image;	// 自分でアロケートしたVkImage/VkDeviceMemoryかどうか、trueならデストラクタで破棄する
public:
	/**
	 * コンストラクタ
	 * @param vk_context
	 */
	explicit SVkImage(std::shared_ptr<SVkContext> vk_context, std::shared_ptr<SVkSampler> vk_sampler = nullptr);
	/**
	 * デストラクタ
	 */
	virtual ~SVkImage() noexcept;

	/**
	 * Vulkanのコンテキストを取得
	 * @return
	 */
	inline std::shared_ptr<SVkContext> context() { return m_vk_context; };
	/**
	 * VkImageの幅を取得
	 * @return
	 */
	[[nodiscard]]
	inline uint32_t width() const { return m_width; };
	/**
	 * VkImageの高さを取得
	 * @return
	 */
	[[nodiscard]]
	inline uint32_t height() const { return m_height; };
	/**
	 * VkFormatを取得
	 * @return
	 */
	[[nodiscard]]
	inline VkFormat format() const { return m_vk_image_format; }
	/**
	 * VkImageLayoutを取得
	 * @return
	 */
	[[nodiscard]]
	inline VkImageLayout layout() const { return m_vk_image_layout; };
	/**
	 * 割り当てたメモリーサイズを取得
	 * @return
	 */
	[[nodiscard]]
	inline VkDeviceSize allocate_bytes() const { return m_allocate_bytes; };
	/**
	 * VkImageを取得
	 * @return
	 */
	inline VkImage &image() { return m_vk_image; };
	/**
	 * VkDeviceMemoryを取得
	 * @return
	 */
	inline VkDeviceMemory &memory() { return m_vk_memory; };
	/**
	 * VkISamplerSpを取得
	 * @return
	 */
	inline std::shared_ptr<SVkSampler> &sampler() { return m_vk_sampler; };
	/**
	 * VkSamplerを取得
	 * @return
	 */
	[[nodiscard]]
	VkSampler vk_sampler() const;
	/**
	 * VkImageViewを取得
	 * @return
	 */
	inline VkImageView &view() { return m_vk_image_view; };
#if defined(__ANDROID__)
	/**
	 * 内包するAHardwareBufferへのポインタを取得
	 * ハードウエアバッファーを使っていない場合はnullptrが返る
	 * @return
	 */
	inline AHardwareBuffer *buffer() { return m_buffer; };
#endif

	/**
	 * 既存のVkImage/VkDeviceMemoryからの初期化用
	 * VkImageViewを生成する
	 * @param width
	 * @param height
	 * @param vk_format
	 * @param vk_layout
	 * @param vk_image
	 * @param vk_memory
	 * @param allocate_size
	 * @return
	 */
	virtual VkResult init(
		const uint32_t &width, const uint32_t &height,
		const VkFormat &vk_format, const VkImageLayout &layout,
		VkImage vk_image, VkDeviceMemory vk_memory,
		const VkDeviceSize &allocate_size,
		const VkImageAspectFlags &aspect_mask);
	/**
	 * 内部でVkImage/VkDeviceMemoryを生成してSVkImageを初期化
	 * @param width
	 * @param height
	 * @param vk_format
	 * @param vk_image_tiling
	 * @param vk_image_usage
	 * @param memory_properties
	 * @param aspect_mask
	 * @return
	 */
	VkResult init(
		const uint32_t &width, const uint32_t &height,
		const VkFormat &vk_format,
		const VkImageTiling &vk_image_tiling,
		const VkImageUsageFlags &vk_image_usage,
		const VkMemoryPropertyFlags &memory_properties,
		const VkImageAspectFlags &aspect_mask);
#if defined(__ANDROID__)
	/**
	 * ハードウエアバッファーからの初期化用
	 * @param buffer
	 * @param usage
	 * @param aspect_mask
	 * @param use_external_format
	 * @param sync_fd
	 * @return
	 */
	VkResult init(AHardwareBuffer *buffer,
		const VkImageUsageFlags &usage,
		const VkImageAspectFlags &aspect_mask,
		const bool &use_external_format, int sync_fd);
#endif

	/**
	 * 内部で保持しているVulkanのオブジェクトを解放する
	 */
	void release();
	/**
	 * 指定したレイアウトに変更する
	 * @param layout
	 * @return
	 */
	VkResult transition_layout(const VkImageLayout &layout);
	/**
	 * CPU側からアクセスできるようにマップしてポインタを返す(マップ中でなければvkMapMemoryを呼び出す)
	 * nullptr以外が返った場合は必ずunmapを呼び出すこと
	 * @param offset	デフォルトは0
	 * @param size		デフォルトは0で割り当てたメモリー全てをマップする
	 * @param flags 	デフォルトは0
	 * @return
	 */
	void *map(
		const VkDeviceSize &offset = 0,
		const VkDeviceSize &size = 0,
		const VkMemoryMapFlags &flags = 0);
	/**
	 * マップを解除(マップ中ならvkUnmapMemoryを呼び出す)
	 * @param flush flushを自動的に呼び出すかどうか、デフォルトはfalse
	 */
	void unmap(const bool &flush = false);
	/**
	 * 指定したメモリーを無効化する
	 * @param offset 無効化する範囲の先頭インデックス, デフォルト0
	 * @param size 無効化するバイト数, 0ならすべてのバッファを無効化, デフォルト0
	 */
	void invalidate(const VkDeviceSize &offset = 0, const VkDeviceSize &size = 0);
	/**
	 * メモリーをフラッシュ(CPU側とGPU側を同期)
	 * @param offset フラッシュする範囲の先頭インデックス, デフォルト0
	 * @param size フラッシュするバイト数, 0ならすべてのバッファをフラッシュ, デフォルト0
	 */
	void flush(const VkDeviceSize &offset = 0, const VkDeviceSize &size = 0);
};

using SVkImageSp = std::shared_ptr<SVkImage>;
using SVkImageUp = std::unique_ptr<SVkImage>;

} // namespace serenegiant::vulkan

#endif //SERENEGIANT_SVK_IMAGE_H
