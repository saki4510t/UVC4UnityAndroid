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

/*
 * ビルド時の最小APIレベルが26未満の場合にも実行する端末がAPI>=26であれば
 * hardware buffer APIが使えるはずなのでlibnativewindow.soへ
 * 動的リンクを試みるヘルパー関数
 * 動的リンクした関数はhardware buffer APIオリジナルの関数名の前にAを
 * プレフィックスした関数となる
 * 例)AHardwareBuffer_allocateの代わりにAAHardwareBuffer_allocateを使う
 */
#ifndef AANDUSB_HARDWARE_BUFFER_STUB_H
#define AANDUSB_HARDWARE_BUFFER_STUB_H

bool init_hardware_buffer();
bool is_hardware_buffer_supported_v29();

#include <android/hardware_buffer.h>

/**
 * Buffer pixel formats.
 * ここの定数はNDKに含まれない非公開(VNDK)の値なので使えるかどうかはわからない
 * グラフィックHALの定数に対応している値らしい
 */
enum {
    /* for future proofing, keep these in sync with system/graphics-base.h */
    /* same as HAL_PIXEL_FORMAT_BGRA_8888 */
    AHARDWAREBUFFER_FORMAT_B8G8R8A8_UNORM           = 5,
    /* same as HAL_PIXEL_FORMAT_YV12 */
    AHARDWAREBUFFER_FORMAT_YV12                     = 0x32315659,
    /* same as HAL_PIXEL_FORMAT_Y8 */
    AHARDWAREBUFFER_FORMAT_Y8                       = 0x20203859,
    /* same as HAL_PIXEL_FORMAT_Y16 */
    AHARDWAREBUFFER_FORMAT_Y16                      = 0x20363159,
    /* same as HAL_PIXEL_FORMAT_RAW16 */
    AHARDWAREBUFFER_FORMAT_RAW16                    = 0x20,
    /* same as HAL_PIXEL_FORMAT_RAW10 */
    AHARDWAREBUFFER_FORMAT_RAW10                    = 0x25,
    /* same as HAL_PIXEL_FORMAT_RAW12 */
    AHARDWAREBUFFER_FORMAT_RAW12                    = 0x26,
    /* same as HAL_PIXEL_FORMAT_RAW_OPAQUE */
    AHARDWAREBUFFER_FORMAT_RAW_OPAQUE               = 0x24,
    /* same as HAL_PIXEL_FORMAT_IMPLEMENTATION_DEFINED */
    AHARDWAREBUFFER_FORMAT_IMPLEMENTATION_DEFINED   = 0x22,
    /* same as HAL_PIXEL_FORMAT_YCBCR_422_SP */
    AHARDWAREBUFFER_FORMAT_YCbCr_422_SP             = 0x10,
    /* same as HAL_PIXEL_FORMAT_YCRCB_420_SP */
    AHARDWAREBUFFER_FORMAT_YCrCb_420_SP             = 0x11,
    /* same as HAL_PIXEL_FORMAT_YCBCR_422_I */
    AHARDWAREBUFFER_FORMAT_YCbCr_422_I              = 0x14,
};

#if !defined(DYNAMIC_HARDWARE_BUFFER_NDK)
// libnativewindow.soへリンクするとき
#define AAHardwareBuffer_allocate AHardwareBuffer_allocate
#define AAHardwareBuffer_acquire AHardwareBuffer_acquire
#define AAHardwareBuffer_release AHardwareBuffer_release
#define AAHardwareBuffer_describe AHardwareBuffer_describe
#define AAHardwareBuffer_lock AHardwareBuffer_lock
#define AAHardwareBuffer_unlock AHardwareBuffer_unlock
#define AAHardwareBuffer_sendHandleToUnixSocket AHardwareBuffer_sendHandleToUnixSocket
#define AAHardwareBuffer_recvHandleFromUnixSocket AHardwareBuffer_recvHandleFromUnixSocket
#define AAHardwareBuffer_lockPlanes AHardwareBuffer_lockPlanes
#define AAHardwareBuffer_isSupported AHardwareBuffer_isSupported
#define AAHardwareBuffer_lockAndGetInfo AHardwareBuffer_lockAndGetInfo
#else
// libnativewindow.soへ動的リンクを試みる場合

#include <cinttypes>

#include <sys/cdefs.h>

#include <android/rect.h>

__BEGIN_DECLS

/**
 * Allocates a buffer that matches the passed AHardwareBuffer_Desc.
 *
 * If allocation succeeds, the buffer can be used according to the
 * usage flags specified in its description. If a buffer is used in ways
 * not compatible with its usage flags, the results are undefined and
 * may include program termination.
 *
 * Available since API level 26.
 *
 * \return 0 on success, or an error number of the allocation fails for
 * any reason. The returned buffer has a reference count of 1.
 */
using AHardwareBuffer_allocate_ptr = int (*)(
	const AHardwareBuffer_Desc* desc,
	AHardwareBuffer** outBuffer);
extern AHardwareBuffer_allocate_ptr AAHardwareBuffer_allocate;


/**
 * Acquire a reference on the given AHardwareBuffer object.
 *
 * This prevents the object from being deleted until the last reference
 * is removed.
 *
 * Available since API level 26.
 */
using AHardwareBuffer_acquire_ptr = void (*)(AHardwareBuffer* buffer);
extern AHardwareBuffer_acquire_ptr AAHardwareBuffer_acquire;

/**
 * Remove a reference that was previously acquired with
 * AHardwareBuffer_acquire() or AHardwareBuffer_allocate().
 *
 * Available since API level 26.
 */
using AHardwareBuffer_release_ptr = void (*)(AHardwareBuffer* buffer);
extern AHardwareBuffer_release_ptr AAHardwareBuffer_release;

/**
 * Return a description of the AHardwareBuffer in the passed
 * AHardwareBuffer_Desc struct.
 *
 * Available since API level 26.
 */
using AHardwareBuffer_describe_ptr = void (*)(
	const AHardwareBuffer* buffer,
	AHardwareBuffer_Desc* outDesc);
extern AHardwareBuffer_describe_ptr AAHardwareBuffer_describe;

/**
 * Lock the AHardwareBuffer for direct CPU access.
 *
 * This function can lock the buffer for either reading or writing.
 * It may block if the hardware needs to finish rendering, if CPU caches
 * need to be synchronized, or possibly for other implementation-
 * specific reasons.
 *
 * The passed AHardwareBuffer must have one layer, otherwise the call
 * will fail.
 *
 * If \a fence is not negative, it specifies a fence file descriptor on
 * which to wait before locking the buffer. If it's negative, the caller
 * is responsible for ensuring that writes to the buffer have completed
 * before calling this function.  Using this parameter is more efficient
 * than waiting on the fence and then calling this function.
 *
 * The \a usage parameter may only specify AHARDWAREBUFFER_USAGE_CPU_*.
 * If set, then outVirtualAddress is filled with the address of the
 * buffer in virtual memory. The flags must also be compatible with
 * usage flags specified at buffer creation: if a read flag is passed,
 * the buffer must have been created with
 * AHARDWAREBUFFER_USAGE_CPU_READ_RARELY or
 * AHARDWAREBUFFER_USAGE_CPU_READ_OFTEN. If a write flag is passed, it
 * must have been created with AHARDWAREBUFFER_USAGE_CPU_WRITE_RARELY or
 * AHARDWAREBUFFER_USAGE_CPU_WRITE_OFTEN.
 *
 * If \a rect is not NULL, the caller promises to modify only data in
 * the area specified by rect. If rect is NULL, the caller may modify
 * the contents of the entire buffer. The content of the buffer outside
 * of the specified rect is NOT modified by this call.
 *
 * It is legal for several different threads to lock a buffer for read
 * access; none of the threads are blocked.
 *
 * Locking a buffer simultaneously for write or read/write is undefined,
 * but will neither terminate the process nor block the caller.
 * AHardwareBuffer_lock may return an error or leave the buffer's
 * content in an indeterminate state.
 *
 * If the buffer has AHARDWAREBUFFER_FORMAT_BLOB, it is legal lock it
 * for reading and writing in multiple threads and/or processes
 * simultaneously, and the contents of the buffer behave like shared
 * memory.
 *
 * Available since API level 26.
 *
 * \return 0 on success. -EINVAL if \a buffer is NULL, the usage flags
 * are not a combination of AHARDWAREBUFFER_USAGE_CPU_*, or the buffer
 * has more than one layer. Error number if the lock fails for any other
 * reason.
 */
using AHardwareBuffer_lock_ptr = int (*)(
	AHardwareBuffer* buffer, uint64_t usage,
	int32_t fence, const ARect* rect, void** outVirtualAddress);
extern AHardwareBuffer_lock_ptr AAHardwareBuffer_lock;

/**
 * Lock a potentially multi-planar AHardwareBuffer for direct CPU access.
 *
 * This function is similar to AHardwareBuffer_lock, but can lock multi-planar
 * formats. The locked planes are returned in the \a outPlanes argument. Note,
 * that multi-planar should not be confused with multi-layer images, which this
 * locking function does not support.
 *
 * YUV formats are always represented by three separate planes of data, one for
 * each color plane. The order of planes in the array is guaranteed such that
 * plane #0 is always Y, plane #1 is always U (Cb), and plane #2 is always V
 * (Cr). All other formats are represented by a single plane.
 *
 * Additional information always accompanies the buffers, describing the row
 * stride and the pixel stride for each plane.
 *
 * In case the buffer cannot be locked, \a outPlanes will contain zero planes.
 *
 * See the AHardwareBuffer_lock documentation for all other locking semantics.
 *
 * Available since API level 29.
 *
 * \return 0 on success. -EINVAL if \a buffer is NULL, the usage flags
 * are not a combination of AHARDWAREBUFFER_USAGE_CPU_*, or the buffer
 * has more than one layer. Error number if the lock fails for any other
 * reason.
 */
using AHardwareBuffer_lockPlanes_ptr = int (*)(
	AHardwareBuffer* buffer, uint64_t usage,
	int32_t fence, const ARect* rect, AHardwareBuffer_Planes* outPlanes);
extern AHardwareBuffer_lockPlanes_ptr AAHardwareBuffer_lockPlanes;

/**
 * Unlock the AHardwareBuffer from direct CPU access.
 *
 * Must be called after all changes to the buffer are completed by the
 * caller.  If \a fence is NULL, the function will block until all work
 * is completed.  Otherwise, \a fence will be set either to a valid file
 * descriptor or to -1.  The file descriptor will become signaled once
 * the unlocking is complete and buffer contents are updated.
 * The caller is responsible for closing the file descriptor once it's
 * no longer needed.  The value -1 indicates that unlocking has already
 * completed before the function returned and no further operations are
 * necessary.
 *
 * Available since API level 26.
 *
 * \return 0 on success. -EINVAL if \a buffer is NULL. Error number if
 * the unlock fails for any reason.
 */
using AHardwareBuffer_unlock_ptr = int (*)(AHardwareBuffer* buffer, int32_t* fence);
extern AHardwareBuffer_unlock_ptr AAHardwareBuffer_unlock;

/**
 * Send the AHardwareBuffer to an AF_UNIX socket.
 *
 * Available since API level 26.
 *
 * \return 0 on success, -EINVAL if \a buffer is NULL, or an error
 * number if the operation fails for any reason.
 */
using AHardwareBuffer_sendHandleToUnixSocket_ptr = int (*)(const AHardwareBuffer* buffer, int socketFd);
extern AHardwareBuffer_sendHandleToUnixSocket_ptr AAHardwareBuffer_sendHandleToUnixSocket;

/**
 * Receive an AHardwareBuffer from an AF_UNIX socket.
 *
 * Available since API level 26.
 *
 * \return 0 on success, -EINVAL if \a outBuffer is NULL, or an error
 * number if the operation fails for any reason.
 */
using AHardwareBuffer_recvHandleFromUnixSocket_ptr = int (*)(int socketFd, AHardwareBuffer** outBuffer);
extern AHardwareBuffer_recvHandleFromUnixSocket_ptr AAHardwareBuffer_recvHandleFromUnixSocket;

/**
 * Test whether the given format and usage flag combination is
 * allocatable.
 *
 * If this function returns true, it means that a buffer with the given
 * description can be allocated on this implementation, unless resource
 * exhaustion occurs. If this function returns false, it means that the
 * allocation of the given description will never succeed.
 *
 * The return value of this function may depend on all fields in the
 * description, except stride, which is always ignored. For example,
 * some implementations have implementation-defined limits on texture
 * size and layer count.
 *
 * Available since API level 29.
 *
 * \return 1 if the format and usage flag combination is allocatable,
 *     0 otherwise.
 */
using AHardwareBuffer_isSupported_ptr = int (*)(const AHardwareBuffer_Desc* desc);
extern AHardwareBuffer_isSupported_ptr AAHardwareBuffer_isSupported;

/**
 * Lock an AHardwareBuffer for direct CPU access.
 *
 * This function is the same as the above lock function, but passes back
 * additional information about the bytes per pixel and the bytes per stride
 * of the locked buffer.  If the bytes per pixel or bytes per stride are unknown
 * or variable, or if the underlying mapper implementation does not support returning
 * additional information, then this call will fail with INVALID_OPERATION
 *
 * Available since API level 29.
 */
using AHardwareBuffer_lockAndGetInfo_ptr = int (*)(
	AHardwareBuffer* buffer, uint64_t usage,
	int32_t fence, const ARect* rect, void** outVirtualAddress,
	int32_t* outBytesPerPixel, int32_t* outBytesPerStride);
extern AHardwareBuffer_lockAndGetInfo_ptr AAHardwareBuffer_lockAndGetInfo;

__END_DECLS
#endif  // #if __ANDROID_API__ >= 26 #else

/*
 * ハードウエアバッファー情報をlogCatへ出力するヘルパー関数
 */
void dump(const AHardwareBuffer_Desc &desc, const AHardwareBuffer_Planes &planes);
/**
 * AAHardwareBuffer_allocate呼び出し用のヘルパー関数
 * @param width
 * @param height
 * @param format enum AHardwareBuffer_Format
 * @param usage AHARDWAREBUFFER_USAGE_XXX
 * @return
 */
AHardwareBuffer *allocate_hardware_buffer(
  const uint32_t &width, const uint32_t &height,
  const uint32_t &format,
  const uint64_t &usage);

#endif //AANDUSB_HARDWARE_BUFFER_STUB_H
