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

#ifndef JNI_UTILS_H_
#define JNI_UTILS_H_

#include <string>
#include <jni.h>
#include <android/api-level.h>
#include "localdefines.h"

namespace serenegiant {

// Utility functions
/**
 * 実行している端末のandroid.os.Build$VERSION.SDK_INTを取得する
 * @param env
 * @return 正常に実行できれば端末のVERSION.SDK_INT、正常に取得できなければ0
 */
jint getSDKVersionInt(JNIEnv *env);

/**
 * 引数がnullptrまたはグローバル参照であればそのまま返す
 * グローバル参照でなければグローバル参照を生成して返す
 * @param obj
 * @return
 */
jobject ensureGlobalRef(jobject obj);

/**
 * return whether or not the specified field is null
 * @param env
 * @param java_obj
 * @param field_name
 * @param field_type
 * @return return true if the field does not exist or the field value is null or the filed type is wrong
 */
bool isNullField(JNIEnv *env, jobject java_obj, const char *field_name, const char *field_type);

//--------------------------------------------------------------------------------
/**
 * return a field value as boolean
 * @param env
 * @param java_obj
 * @return	return the value, return false(0) if the field does not exist
 */
bool getField(JNIEnv *env, jobject java_obj, const char *field_name, const bool &default_value = false);
bool __getField(JNIEnv *env, jobject java_obj, jclass clazz, const char *field_name, const bool &default_value = false);

inline bool getField_bool(JNIEnv *env, jobject java_obj, const char *field_name, const bool &default_value = false) {
	return getField(env, java_obj, field_name, default_value);
}

inline jint __getField_bool(JNIEnv *env, jobject java_obj, jclass clazz, const char *field_name, const bool &default_value = false) {
	return __getField(env, java_obj, clazz, field_name, default_value);
}

bool setField(JNIEnv *env, jobject java_obj, const char *field_name, const bool &val);
bool __setField(JNIEnv *env, jobject java_obj, jclass clazz, const char *field_name, const bool &val);

inline bool setField_bool(JNIEnv *env, jobject java_obj, const char *field_name, const bool &val) {
	return setField(env, java_obj, field_name, val);
}

inline bool __setField_bool(JNIEnv *env, jobject java_obj, jclass clazz, const char *field_name, const bool &val) {
	return __setField(env, java_obj, clazz , field_name, val);
}

bool getStaticField(JNIEnv *env, jclass clazz, const char *field_name, const bool &default_value = false);
bool getStaticField(JNIEnv *env, jobject java_obj, const char *field_name, const bool &default_value = false);
bool setStaticField(JNIEnv *env, jobject java_obj, const char *field_name, const bool &val);


inline bool getStaticField_bool(JNIEnv *env, jclass clazz, const char *field_name, const bool &default_value = false) {
	return getStaticField(env, clazz, field_name, default_value);
}

inline bool getStaticField_bool(JNIEnv *env, jobject java_obj, const char *field_name, const bool &default_value = false) {
	return getStaticField(env, java_obj, field_name, default_value);
}

inline bool setStaticField_bool(JNIEnv *env, jobject java_obj, const char *field_name, const bool &val) {
	return setStaticField(env, java_obj, field_name, val);
}

//--------------------------------------------------------------------------------
/**
 * get int field that has specified name from specified Java object
 * @param env
 * @param java_obj
 * @return return the value, return 0 if the field does not exist
 */
jint getField(JNIEnv *env, jobject java_obj, const char *field_name, const jint &default_value = 0);
jint __getField(JNIEnv *env, jobject java_obj, jclass clazz, const char *field_name, const jint &default_value = 0);

inline jint getField_int(JNIEnv *env, jobject java_obj, const char *field_name, const jint &default_value = 0) {
	return getField(env, java_obj, field_name, default_value);
}

inline jint __getField_int(JNIEnv *env, jobject java_obj, jclass clazz, const char *field_name, const jint &default_value = 0) {
	return __getField(env, java_obj, clazz, field_name, default_value);
}

/**
 * set the value to int field of specified Java object
 * @param env
 * @param java_obj
 * @param field_name
 * @params val
 */
jint setField(JNIEnv *env, jobject java_obj, const char *field_name, const jint &val);
jint __setField(JNIEnv *env, jobject java_obj, jclass clazz, const char *field_name, const jint &val);

inline jint setField_int(JNIEnv *env, jobject java_obj, const char *field_name, const jint &val) {
	return setField(env, java_obj, field_name, val);
}

inline jint __setField_int(JNIEnv *env, jobject java_obj, jclass clazz, const char *field_name, const jint &val) {
	return __setField(env, java_obj, clazz, field_name, val);
}

/**
 * return the static int field value
 * @param env
 * @param java_obj
 * @return return the value, return 0 if the field does not exist
 */
jint getStaticField(JNIEnv *env, jobject java_obj, const char *field_name, const jint &default_value = 0);

inline jint getStaticField_int(JNIEnv *env, jobject java_obj, const char *field_name, const jint &default_value = 0) {
	return getStaticField(env, java_obj, field_name, default_value);
}

/**
 * set specified value into the static int field
 * @param env
 * @param java_obj
 * @param field_name
 * @params val
 */
jint setStaticField(JNIEnv *env, jobject java_obj, const char *field_name, const jint &val);

inline jint setStaticField_int(JNIEnv *env, jobject java_obj, const char *field_name, const jint &val) {
	return setStaticField(env, java_obj, field_name, val);
}

//--------------------------------------------------------------------------------
/**
 * get long field that has specified name from specified Java object
 * @param env
 * @param java_obj
 * @return return the value, return 0 if the field does not exist
 */
jlong getField(JNIEnv* env, jobject java_obj, const char* field_name, const jlong &default_value = 0);
jlong __getField(JNIEnv* env, jobject java_obj, jclass clazz, const char* field_name, const jlong &default_value = 0);

inline jlong getField_long(JNIEnv* env, jobject java_obj, const char* field_name, const jlong default_value = 0) {
	return getField(env, java_obj, field_name, default_value);
}

inline jlong __getField_long(JNIEnv* env, jobject java_obj, jclass clazz, const char* field_name, const jlong &default_value = 0) {
	return __getField(env, java_obj, clazz, field_name, default_value);
}

/**
 * set the value into the long field
 * @param env
 * @param java_obj
 * @param field_name
 * @params val
 */
jlong setField(JNIEnv* env, jobject java_obj, const char* field_name, const jlong &val);
jlong __setField(JNIEnv* env, jobject java_obj, jclass clazz, const char* field_name, const jlong &val);

inline jlong setField_long(JNIEnv* env, jobject java_obj, const char* field_name, const jlong &val) {
	return setField(env, java_obj, field_name, val);
}

inline jlong __setField_long(JNIEnv* env, jobject java_obj, jclass clazz, const char* field_name, const jlong &val) {
	return __setField(env, java_obj, clazz, field_name, val);
}

//--------------------------------------------------------------------------------
/**
 * get static float field that has specified name from specified Java object
 * @param env
 * @param java_obj
 * @param field_name
 * @return return the value, return 0 if the field does not exist
 */
jfloat getStaticField(JNIEnv *env, jobject java_obj, const char *field_name, const jfloat &default_value = 0.0f);

inline jfloat getStaticField_float(JNIEnv *env, jobject java_obj, const char *field_name, const jfloat &default_value = 0.0f) {
	return getStaticField(env, java_obj, field_name, default_value);
}

/**
 * 指定したJavaオブジェクトの指定した名前のstatic float型のフィールド値を取得
 * @param env
 * @param java_obj
 * @param field_name
 */
jfloat setStaticField(JNIEnv *env, jobject java_obj, const char *field_name, const jfloat &val);

inline jfloat setStaticField_float(JNIEnv *env, jobject java_obj, const char *field_name, const jfloat &val) {
	return setStaticField(env, java_obj, field_name, val);
}

/**
 * get the value of float field that has specified name from specified Java object
 * @param env
 * @param java_obj
 * @param field_name
 * @return return the value, return 0 if the field does not exist
 */
jfloat getField(JNIEnv *env, jobject java_obj, const char *field_name, const jfloat &default_value = 0.0f);
jfloat __getField(JNIEnv *env, jobject java_obj, jclass clazz, const char *field_name, const jfloat &default_value = 0.0f);

inline jfloat getField_float(JNIEnv *env, jobject java_obj, const char *field_name, const jfloat &default_value = 0.0f) {
	return getField(env, java_obj, field_name, default_value);
}

inline jfloat __getField_float(JNIEnv *env, jobject java_obj, jclass clazz, const char *field_name, const jfloat &default_value = 0.0f) {
	return __getField(env, java_obj, clazz, field_name, default_value);
}

/**
 * set float value into the specific Java object & field
 * @param env
 * @param java_obj
 * @param field_name
 * @params val
 */
jfloat setField(JNIEnv* env, jobject java_obj, const char* field_name, const jfloat &val);
jfloat __setField(JNIEnv *env, jobject java_obj, jclass clazz, const char *field_name, const jfloat &val);

inline jfloat setField_float(JNIEnv* env, jobject java_obj, const char* field_name, const jfloat &val) {
	return setField(env, java_obj, field_name, val);
}

inline jfloat __setField_float(JNIEnv *env, jobject java_obj, jclass clazz, const char *field_name, const jfloat &val) {
	return __setField(env, java_obj, clazz, field_name, val);
}

/**
 * return specific Java object and its field value as a native pointer
 * @return pointer value
 */
inline ID_TYPE getField_NativeObj(JNIEnv *env, jobject java_obj, const char *field_name, const char *field_type, const ID_TYPE &default_value = 0) {
	return reinterpret_cast<ID_TYPE>(getField(env, java_obj, field_name, default_value));
}

/**
 * return jobject filed that is specified type from specified field.
 * you should check the field exist and is not null with #isNullField
 * before you call this function.
 * @param env
 * @param java_obj
 * @param field_name
 * @param field_type
 * @return jobject
 */
jobject getField(JNIEnv *env, jobject java_obj, const char *field_name, const char *obj_type, const jobject &default_value = nullptr);

inline jobject getField_obj(JNIEnv *env, jobject java_obj, const char *field_name, const char *obj_type, const jobject &default_value = nullptr) {
	return getField(env, java_obj, field_name, obj_type, default_value);
}

/**
 * return id from specified field name and type
 * @param env
 * @param java_obj
 * @param field_name
 * @param field_type
 * @return ID_TYPE
 */
inline ID_TYPE getField_obj_id(JNIEnv *env, jobject java_obj, const char *field_name, const char *obj_type, const ID_TYPE &default_value = 0) {
	return reinterpret_cast<ID_TYPE>(getField(env, java_obj, field_name, default_value));
};

/**
 * @param env: this param should not be null
 * @param java_obj: this param should not be null
 * @return
 */
inline void *getField_nativePtr(JNIEnv *env, jobject java_obj, const char *field_name) {
//	LOGV("get_nativeRec:");
	return reinterpret_cast<void *>(getField_long(env, java_obj, field_name));
}

/**
 * @param env: this param should not be null
 * @param java_obj: this param should not be null
 */
inline void setField_nativePtr(JNIEnv *env, jobject java_obj, const char *field_name, void *ptr) {
//	LOGV("get_nativeRec:");
	setField_long(env, java_obj, field_name, reinterpret_cast<ID_TYPE>(ptr));
}

jobject getStaticField(JNIEnv *env, jobject java_obj, const char *field_name, const char *field_type, const jobject &default_value = nullptr);

inline jobject getStaticField_obj(JNIEnv *env, jobject java_obj, const char *field_name, const char *field_type, const jobject &default_value = nullptr) {
	return getStaticField(env, java_obj, field_name, field_type, default_value);
}

int prepareBytebufferId(JNIEnv *env, jclass &byteBufClass, jmethodID &byteBufArrayID);
jlong getByteBuffer(JNIEnv *env, jobject byte_buffer_obj, void *dst_buf, const size_t &_offset, const size_t &_size, jclass byteBufClass, jmethodID byteBufArrayID);
jlong setByteBuffer(JNIEnv *env, jobject byte_buffer_obj, void *src_buf, const size_t &_offset, const size_t &_size, jclass byteBufClass, jmethodID byteBufArrayID);
/**
 * jstringからstd::stringを取得
 * @param env
 * @param src_jstr
 * @return
 */
std::string to_string(JNIEnv *env, jstring src_jstr);

jint registerNativeMethods(JNIEnv *env, const char *class_name, JNINativeMethod *methods, int num_methods);
void setVM(JavaVM *);
JavaVM *getVM();
JNIEnv *getEnv(bool &attached, const char *name = nullptr);
int DetachCurrentThread(JNIEnv *env);

/**
 * 実行中スレッドのJavaVMへのアタッチ/デタッチを自動的に行ない
 * JNIEnv *を取得できるようにするためのヘルパークラス
 * インスタンスが有効な間JNIEnvが有効
 */
class AutoJNIEnv {
private:
	bool attached;
	JNIEnv *env;
public:
	explicit AutoJNIEnv(const char *name = nullptr);
	~AutoJNIEnv() noexcept;
	inline JNIEnv *get() { return env; };
};

}	// namespace serenegiant

#endif /* JNI_UTILS_H_ */
