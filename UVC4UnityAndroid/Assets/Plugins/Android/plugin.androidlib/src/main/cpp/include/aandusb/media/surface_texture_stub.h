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

#ifndef AANDUSB_SURFACE_TEXTURE_STUB_H
#define AANDUSB_SURFACE_TEXTURE_STUB_H

#include <android/surface_texture_jni.h>
#include <android/surface_texture.h>

bool init_surface_texture_ndk();

#if (__ANDROID_API__ >= 28)
#define AASurfaceTexture_fromSurfaceTexture AASurfaceTexture_fromSurfaceTexture
#define AASurfaceTexture_release ASurfaceTexture_release
#define AASurfaceTexture_acquireANativeWindow ASurfaceTexture_acquireANativeWindow
#define AASurfaceTexture_attachToGLContext ASurfaceTexture_attachToGLContext
#define AASurfaceTexture_detachFromGLContext ASurfaceTexture_detachFromGLContext
#define AASurfaceTexture_updateTexImage ASurfaceTexture_updateTexImage
#define AASurfaceTexture_getTransformMatrix ASurfaceTexture_getTransformMatrix
#define AASurfaceTexture_getTimestamp ASurfaceTexture_getTimestamp
#else
//--------------------------------------------------------------------------------
// surface_texture_jni.h
/**
 * Get a reference to the native ASurfaceTexture from the corresponding java object.
 *
 * The caller must keep a reference to the Java SurfaceTexture during the lifetime of the returned
 * ASurfaceTexture. Failing to do so could result in the ASurfaceTexture to stop functioning
 * properly once the Java object gets finalized.
 * However, this will not result in program termination.
 *
 * Available since API level 28.
 *
 * \param env JNI environment
 * \param surfacetexture Instance of Java SurfaceTexture object
 * \return native ASurfaceTexture reference or nullptr if the java object is not a SurfaceTexture.
 *         The returned reference MUST BE released when it's no longer needed using
 *         ASurfaceTexture_release().
 */
using ASurfaceTexture_fromSurfaceTexturePtr =  ASurfaceTexture* (*)(JNIEnv* env, jobject surfacetexture);
extern ASurfaceTexture_fromSurfaceTexturePtr AASurfaceTexture_fromSurfaceTexture;

//--------------------------------------------------------------------------------
// surface_texture.h
/**
 * Release the reference to the native ASurfaceTexture acquired with
 * ASurfaceTexture_fromSurfaceTexture().
 * Failing to do so will result in leaked memory and graphic resources.
 *
 * Available since API level 28.
 *
 * \param st A ASurfaceTexture reference acquired with ASurfaceTexture_fromSurfaceTexture()
 */
using ASurfaceTexture_releasePtr = void (*)(ASurfaceTexture* st);
extern ASurfaceTexture_releasePtr AASurfaceTexture_release;

/**
 * Returns a reference to an ANativeWindow (i.e. the Producer) for this SurfaceTexture.
 * This is equivalent to Java's: Surface sur = new Surface(surfaceTexture);
 *
 * Available since API level 28.
 *
 * \param st A ASurfaceTexture reference acquired with ASurfaceTexture_fromSurfaceTexture()
 * @return A reference to an ANativeWindow. This reference MUST BE released when no longer needed
 * using ANativeWindow_release(). Failing to do so will result in leaked resources. nullptr is
 * returned if \p st is null or if it's not an instance of android.graphics.SurfaceTexture
 */
using ASurfaceTexture_acquireANativeWindowPtr = ANativeWindow* (*)(ASurfaceTexture* st);
extern ASurfaceTexture_acquireANativeWindowPtr AASurfaceTexture_acquireANativeWindow;

/**
 * Attach the SurfaceTexture to the OpenGL ES context that is current on the calling thread.  A
 * new OpenGL ES texture object is created and populated with the SurfaceTexture image frame
 * that was current at the time of the last call to {@link ASurfaceTexture_detachFromGLContext}.
 * This new texture is bound to the GL_TEXTURE_EXTERNAL_OES texture target.
 *
 * This can be used to access the SurfaceTexture image contents from multiple OpenGL ES
 * contexts.  Note, however, that the image contents are only accessible from one OpenGL ES
 * context at a time.
 *
 * Available since API level 28.
 *
 * \param st A ASurfaceTexture reference acquired with ASurfaceTexture_fromSurfaceTexture()
 * \param texName The name of the OpenGL ES texture that will be created.  This texture name
 * must be unusued in the OpenGL ES context that is current on the calling thread.
 * \return 0 on success, negative posix error code otherwise (see <errno.h>)
 */
using ASurfaceTexture_attachToGLContextPtr = int (*)(ASurfaceTexture* st, uint32_t texName);
extern ASurfaceTexture_attachToGLContextPtr AASurfaceTexture_attachToGLContext;

/**
 * Detach the SurfaceTexture from the OpenGL ES context that owns the OpenGL ES texture object.
 * This call must be made with the OpenGL ES context current on the calling thread.  The OpenGL
 * ES texture object will be deleted as a result of this call.  After calling this method all
 * calls to {@link ASurfaceTexture_updateTexImage} will fail until a successful call to
 * {@link ASurfaceTexture_attachToGLContext} is made.
 *
 * This can be used to access the SurfaceTexture image contents from multiple OpenGL ES
 * contexts.  Note, however, that the image contents are only accessible from one OpenGL ES
 * context at a time.
 *
 * Available since API level 28.
 *
 * \param st A ASurfaceTexture reference acquired with ASurfaceTexture_fromSurfaceTexture()
 * \return 0 on success, negative posix error code otherwise (see <errno.h>)
 */
using ASurfaceTexture_detachFromGLContextPtr = int (*)(ASurfaceTexture* st);
extern ASurfaceTexture_detachFromGLContextPtr AASurfaceTexture_detachFromGLContext;

/**
 * Update the texture image to the most recent frame from the image stream.  This may only be
 * called while the OpenGL ES context that owns the texture is current on the calling thread.
 * It will implicitly bind its texture to the GL_TEXTURE_EXTERNAL_OES texture target.
 *
 * Available since API level 28.
 *
 * \param st A ASurfaceTexture reference acquired with ASurfaceTexture_fromSurfaceTexture()
 * \return 0 on success, negative posix error code otherwise (see <errno.h>)
 */
using ASurfaceTexture_updateTexImagePtr = int (*)(ASurfaceTexture* st);
extern ASurfaceTexture_updateTexImagePtr AASurfaceTexture_updateTexImage;

/**
 * Retrieve the 4x4 texture coordinate transform matrix associated with the texture image set by
 * the most recent call to updateTexImage.
 *
 * This transform matrix maps 2D homogeneous texture coordinates of the form (s, t, 0, 1) with s
 * and t in the inclusive range [0, 1] to the texture coordinate that should be used to sample
 * that location from the texture.  Sampling the texture outside of the range of this transform
 * is undefined.
 *
 * The matrix is stored in column-major order so that it may be passed directly to OpenGL ES via
 * the glLoadMatrixf or glUniformMatrix4fv functions.
 *
 * Available since API level 28.
 *
 * \param st A ASurfaceTexture reference acquired with ASurfaceTexture_fromSurfaceTexture()
 * \param mtx the array into which the 4x4 matrix will be stored.  The array must have exactly
 *     16 elements.
 */
using ASurfaceTexture_getTransformMatrixPtr = void (*)(ASurfaceTexture* st, float mtx[16]);
extern ASurfaceTexture_getTransformMatrixPtr AASurfaceTexture_getTransformMatrix;

/**
 * Retrieve the timestamp associated with the texture image set by the most recent call to
 * updateTexImage.
 *
 * This timestamp is in nanoseconds, and is normally monotonically increasing. The timestamp
 * should be unaffected by time-of-day adjustments, and for a camera should be strictly
 * monotonic but for a MediaPlayer may be reset when the position is set.  The
 * specific meaning and zero point of the timestamp depends on the source providing images to
 * the SurfaceTexture. Unless otherwise specified by the image source, timestamps cannot
 * generally be compared across SurfaceTexture instances, or across multiple program
 * invocations. It is mostly useful for determining time offsets between subsequent frames.
 *
 * For EGL/Vulkan producers, this timestamp is the desired present time set with the
 * EGL_ANDROID_presentation_time or VK_GOOGLE_display_timing extensions
 *
 * Available since API level 28.
 *
 * \param st A ASurfaceTexture reference acquired with ASurfaceTexture_fromSurfaceTexture()
 */
using ASurfaceTexture_getTimestampPtr = int64_t (*)(ASurfaceTexture* st);
extern ASurfaceTexture_getTimestampPtr AASurfaceTexture_getTimestamp;

#endif

#endif //AANDUSB_SURFACE_TEXTURE_STUB_H
