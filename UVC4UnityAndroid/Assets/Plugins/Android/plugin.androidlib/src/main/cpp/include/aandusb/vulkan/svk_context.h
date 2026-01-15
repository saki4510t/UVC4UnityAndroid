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

#ifndef SERENEGIANT_SVK_CONTEXT_H
#define SERENEGIANT_SVK_CONTEXT_H

// 標準ライブラリ
#include <cassert>
#include <memory>
#include <optional>
#include <vector>
// vulkan
#include "vulkan/svk_defs.h"

//--------------------------------------------------------------------------------
// 外部クラスの前方宣言
class LayerAndExtensions;
namespace serenegiant::vulkan {
class VkYCbCrSampler;
}
// 外部クラスの前方宣言ここまで
//--------------------------------------------------------------------------------

namespace serenegiant::vulkan {

/**
 * Vulkanの描画環境を保持するためのヘルパークラス
 * VkInstance, VkPhysicalDevice等を保持してフェンスオブジェクトや
 * バッファオブジェクト、イメージオブジェクト等を生成するための
 * ヘルパー関数を提供する
 */
class SVkContext {
friend VkYCbCrSampler;
private:
	/**
	 * vulkan_wrapper.cpp/.hのInitVulkan関数呼び出しの結果を保持
	 *   Android  7 (API 24) Vulkan1.0対応必須
	 *   Android  9 (API 28) Vulkan1.1対応必須
	 *   Android 13 (API 33) Vulkan1.3対応必須
	 */
	const bool m_supported;
	const bool m_wrapped;	// Unity等の外部で初期化されたvulkan環境をラップする場合true
	bool m_initialized;
	uint32_t m_api_version;
	VkInstance m_vk_instance;
	VkPhysicalDevice m_vk_gpu;
	VkPhysicalDeviceMemoryProperties m_vk_gpu_memory_properties;
	VkPhysicalDeviceProperties m_vk_gpu_device_properties;
	VkDevice m_vk_device;
	uint32_t m_vk_queue_family_index;
	VkQueue m_vk_queue;
	VkCommandPool m_vk_cmd_pool;
	VkDescriptorPool m_vk_descriptor_pool;
	std::unique_ptr<LayerAndExtensions> m_vk_layer_util;
	std::shared_ptr<VkYCbCrSampler> m_vk_ycbcr_sampler;

	/**
	 * 初期化処理の共通部分
	 * @param appInfo
	 * @param enable_validation
	 * @param vk_instance VK_NULL_HANDLEなら内部で初期化する
	 * @param vk_gpu VK_NULL_HANDLEなら内部で初期化する
	 * @param vk_device VK_NULL_HANDLEなら内部で初期化する
	 * @param max_num_texture テクスチャ用ディスクリプタセットの最大個数(プール初期化時に使う)
	 * @param max_num_uniform_buffer ユニフォームバッファ用ディスクリプタセットの最大個数(プール初期化時に使う)
	 * @return
	 */
	int init(
		VkApplicationInfo &appInfo,
		const bool &enable_validation,
		VkInstance vk_instance,
		VkPhysicalDevice vk_gpu,
		VkDevice vk_device,
		const uint32_t &max_num_texture,
		const uint32_t &max_num_uniform_buffer);
protected:
public:
	/**
	 * コンストラクタ
	 * @param appInfo
	 * @param enable_validation
	 * @param max_num_texture テクスチャ用ディスクリプタセットの最大数(プール初期化時に使う)
	 * @param max_num_uniform_buffer ユニフォームバッファ用ディスクリプタセットの最大数(プール初期化時に使う)
	 *
	 */
	explicit SVkContext(
		VkApplicationInfo &appInfo,
		const bool &enable_validation = false,
		const uint32_t &max_num_texture = DEFAULT_MAX_NUM_TEXTURE_DESC,
		const uint32_t &max_num_uniform_buffer = DEFAULT_MAX_NUM_UNIFORM_DESC);
	/**
	 * Unity等の外部で初期化したVulkan環境のラップ用コンストラクタ
	 * @param instance
	 * @param gpu
	 * @param device
	 * @param max_num_texture テクスチャ用ディスクリプタセットの最大数(プール初期化時に使う)
	 * @param max_num_uniform_buffer ユニフォームバッファ用ディスクリプタセットの最大数(プール初期化時に使う)
	 */
	SVkContext(
		VkInstance vk_instance,
		VkPhysicalDevice vk_gpu,
		VkDevice vk_device,
		const uint32_t &max_num_texture = DEFAULT_MAX_NUM_TEXTURE_DESC,
		const uint32_t &max_num_uniform_buffer = DEFAULT_MAX_NUM_UNIFORM_DESC);
	/**
	 * デストラクタ
	 */
	~SVkContext() noexcept;

	/**
	 * 実行環境でVulkanに対応しているかどうかを取得
	 * (vulkan_wrapper.cpp/.hのInitVulkan関数呼び出しの結果)
	 * @return
	 */
	[[nodiscard]]
	inline bool is_supported() const { return m_supported; };
	/**
	 * 実行環境がVulkanに対応していて正常に初期化できたかどうかを取得
	 * @return
	 */
	[[nodiscard]]
	inline bool is_initialized() const { return m_supported && m_initialized; };
	/**
	 * Vulkanに対応していて初期化できた場合にVulkanのAPIバージョンを取得する
	 * 結果はVK_API_VERSION_1_1などの定数で比較するか、VK_VERSION_MAJOR,
	 * VK_VERSION_MINOR, VK_VERSION_PATCHマクロでメジャーバージョン,
	 * マイナーバージョン, パッチバージョンに展開できる
	 * @return
	 */
	[[nodiscard]]
	inline uint32_t api_version() const { return m_api_version; };

	inline VkInstance &instance() { return m_vk_instance; };
	inline VkPhysicalDevice &gpu() { return m_vk_gpu; };
	[[nodiscard]]
	inline const VkPhysicalDeviceMemoryProperties &gpu_memory_properties() const { return m_vk_gpu_memory_properties; };
	[[nodiscard]]
	inline const VkPhysicalDeviceProperties &gpu_device_properties() const { return m_vk_gpu_device_properties; };
	inline VkDevice &device() { return m_vk_device; };
	[[nodiscard]]
	inline const uint32_t &queue_family_index() const { return m_vk_queue_family_index; };
	inline VkQueue &queue() { return m_vk_queue; };
	inline VkCommandPool &cmd_pool() { return m_vk_cmd_pool; };
	inline VkDescriptorPool &descriptor_pool() { return m_vk_descriptor_pool; };

	/**
	 * このコンテキストのVkDeviceに紐付いたフェンスオブジェクトを生成する
	 * @param fence
	 * @param flags
	 * @return
	 */
	bool create_fence(VkFence &fence,
		const VkFenceCreateFlags &flags = 0) const;
	/**
	 * このコンテキストのVkDeviceに紐付いたセマフォを生成する
	 * @param semaphore
	 * @param flags
	 * @return
	 */
	bool create_semaphore(VkSemaphore &semaphore,
		const VkSemaphoreCreateFlags &flags = 0) const;
	/**
	 * OpenGL|ESのセマフォをインポートする
	 * @param semaphore
	 * @param sync_fd
	 * @return
	 */
	bool create_semaphore_from_egl(VkSemaphore &semaphore, const int &sync_fd) const;
	/**
	 * 指定したmemoryTypeBitsとpropertiesに一致するメモリータイプを取得する
	 * @param memory_type_bits
	 * @param req_properties
	 * @return
	 */
	[[nodiscard]]
	std::optional<uint32_t> find_memory_type(
	  	const uint32_t &memory_type_bits,
	  	const VkMemoryPropertyFlags &req_properties) const;
	/**
	 * デプスバッファ用に対応するVkFormatを取得する
	 * @return
	 */
	[[nodiscard]]
	std::optional<VkFormat> find_depth_format() const;
	/**
	 * デプス&ステンシルバッファ用に対応するVkFormatを取得する
	 * @return
	 */
	[[nodiscard]]
	std::optional<VkFormat> find_depth_stencil_format() const;
	/**
	 * このコンテキストのVkDeviceに紐付いたメモリーを確保する
	 * @param memoryProperties
	 * @param memory
	 * @return
	 */
	VkDeviceSize allocate_memory(
		const VkMemoryRequirements &memoryRequirements,
		const VkMemoryPropertyFlags &memoryProperties,
		VkDeviceMemory &memory);
	/**
	 * このコンテキストのVkDeviceに紐付いたメモリー・バッファを生成
	 * @param bytes
	 * @param data nullptrでなければvkMapMemory/vkUnmapMemoryとmemcpyでコピーする
	 * @param bufferUsage
	 * @param memoryProperties
	 * @param buffer
	 * @param memory
	 * @return 成功したときは確保したメモリーサイズ、失敗したときは0
	 */
	VkDeviceSize create_buffer(
		const size_t &bytes, const void *data,
		const VkBufferUsageFlags &bufferUsage, const VkMemoryPropertyFlags &memoryProperties,
		VkBuffer &buffer, VkDeviceMemory &memory);
	/**
	 * このコンテキストのVkDeviceに紐付いたメモリー・イメージを生成
	 * @param width
	 * @param height
	 * @param tiling
	 * @param usage
	 * @param initialLayout
	 * @param memoryProperties
	 * @param image
	 * @param memory
	 * @return 成功したときは確保したメモリーサイズ、失敗したときは0
	 */
	VkDeviceSize create_image(
		const uint32_t &width, const uint32_t &height,
		const VkFormat &format,
		const VkImageTiling &tiling, const VkImageUsageFlags &usage,
		const VkImageLayout &initialLayout,
		const VkMemoryPropertyFlags &memoryProperties,
		VkImage &image, VkDeviceMemory &memory);

	/**
	 * コマンドバッファを生成
	 * @param cmd_buf_count 生成するコマンドバッファの数
	 * @param cmd_buffer
	 * @return
	 */
	bool create_cmd_buffer(
		const uint32_t &cmd_buf_count,
		VkCommandBuffer *cmd_buffer) const;

	/**
	 * 1回だけ実行するためのコマンドバッファーを生成して実行開始するためのヘルパー関数
	 * @return
	 */
	[[nodiscard]]
	VkCommandBuffer begin_onetime_cmd() const;
	/**
	 * begin_onetime_cmdで生成したワンタイムのコマンドバッファを
	 * 実行終了＆GPUへ処理依頼＆終了待ち＆コマンドバッファ破棄をするためのヘルパー関数
	 * @param cmd_buffer
	 * @return
	 */
	bool end_and_submit_onetime_cmd(VkCommandBuffer &cmd_buffer) const;
	/**
	 * begin_onetime_cmdで生成したワンタイムのコマンドバッファを
	 * 実行終了＆GPUへ処理依頼＆終了待ち＆コマンドバッファ破棄をするためのヘルパー関数
	 * @param cmd_buffer
	 * @param wait_semaphores
	 * @param signal_semaphores
	 * @return
	 */
	bool end_and_submit_onetime_cmd(
		VkCommandBuffer &cmd_buffer,
		const std::vector<VkSemaphore> &wait_semaphores,
		const std::vector<VkSemaphore> &signal_semaphores) const;

	/**
	 * このコンテキストに紐付いたImmutableなYCｂCrサンプラーを取得する(シングルトンアクセスする)
	 * @return
	 */
	[[nodiscard]]
	VkSampler get_ycbcr_sampler() const;
};

using SVkContextSp = std::shared_ptr<SVkContext>;
using SVkContextUp = std::unique_ptr<SVkContext>;

}	// namespace serenegiant::vulkan

#endif //SERENEGIANT_SVK_CONTEXT_H
