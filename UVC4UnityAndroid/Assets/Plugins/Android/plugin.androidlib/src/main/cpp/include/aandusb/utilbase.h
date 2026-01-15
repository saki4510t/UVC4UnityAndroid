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

#ifndef UTILBASE_H_
#define UTILBASE_H_

#if defined(__ANDROID__)
#include <android/log.h>
#endif

#include <cstdio>
#include <cstdlib>

#include <unistd.h>
#include <libgen.h>
#include "localdefines.h"

#define		SAFE_FREE(p)				{ if ((p)) { free((p)); (p) = nullptr; } }
#define		SAFE_DELETE(p)				{ if ((p)) { delete (p); (p) = nullptr; } }
#define		SAFE_DELETE_ARRAY(p)		{ if ((p)) { delete [](p); (p) = nullptr; } }
#define		NUM_ARRAY_ELEMENTS(p)		(sizeof((p)) / sizeof(p[0]))

// コンパイル時アサートの定義
#ifdef NDEBUG
#define COMPILE_TIME_ASSERT_FUNCTION_SCOPE( expression )
#else // #ifdef NDEBUG
#define COMPILE_TIME_ASSERT_FUNCTION_SCOPE( expression )  typedef char DUMMY_ARRAY_FOR_COMPILE_ASSERT[ ( expression ) ? 1 : -1 ]
#endif // #ifdef NDEBUG

#if defined(__GNUC__)
// the macro for branch prediction optimization for gcc(-O2/-O3 required)
#define		CONDITION(cond)				((__builtin_expect((cond)!=0, 0)))
#define		LIKELY(x)					((__builtin_expect(!!(x), 1)))	// x is likely true
#define		UNLIKELY(x)					((__builtin_expect(!!(x), 0)))	// x is likely false
#else
#define		CONDITION(cond)				((cond))
#define		LIKELY(x)					((x))
#define		UNLIKELY(x)					((x))
#endif

// XXX assertはNDEBUGが定義されていたら引数を含めて丸ごと削除されてしまうので
//     関数実行を直接assertの引数にするとその関数はNDEBUGの時に実行されなくなるので注意
#include <cassert>
#define CHECK(CONDITION) { bool RES = (CONDITION); assert(RES); }
#define CHECK_EQ(X, Y) { bool RES = ((X) == (Y)); assert(RES); }
#define CHECK_NE(X, Y) { bool RES = ((X) != (Y)); assert(RES); }
#define CHECK_GE(X, Y) { bool RES = ((X) >= (Y)); assert(RES); }
#define CHECK_GT(X, Y) { bool RES = ((X) > (Y)); assert(RES); }
#define CHECK_LE(X, Y) { bool RES = ((X) <= (Y)); assert(RES); }
#define CHECK_LT(X, Y) { bool RES = ((X) < (Y)); assert(RES); }

#if defined(__ANDROID__)
#define LOG_VERBOSE ANDROID_LOG_VERBOSE
#define LOG_DEBUG ANDROID_LOG_DEBUG
#define LOG_INFO ANDROID_LOG_INFO
#define LOG_WARN ANDROID_LOG_WARN
#define LOG_ERROR ANDROID_LOG_ERROR
#define LOG_FATAL ANDROID_LOG_FATAL

#if defined(USE_LOGALL) && !defined(LOG_NDEBUG)
	#define LOG(LEVEL, TAG, FMT, ...) __android_log_print(LEVEL, TAG, FMT, ## __VA_ARGS__)
	#define LOGV(FMT, ...) __android_log_print(ANDROID_LOG_VERBOSE, LOG_TAG, "[%d*%s:%d:%s]:" FMT,	\
							gettid(), basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
	#define LOGD(FMT, ...) __android_log_print(ANDROID_LOG_DEBUG, LOG_TAG, "[%d*%s:%d:%s]:" FMT,	\
							gettid(), basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
	#define LOGI(FMT, ...) __android_log_print(ANDROID_LOG_INFO, LOG_TAG, "[%d*%s:%d:%s]:" FMT,	\
							gettid(), basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
	#define LOGW(FMT, ...) __android_log_print(ANDROID_LOG_WARN, LOG_TAG, "[%d*%s:%d:%s]:" FMT,	\
							gettid(), basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
	#define LOGE(FMT, ...) __android_log_print(ANDROID_LOG_ERROR, LOG_TAG, "[%d*%s:%d:%s]:" FMT,	\
							gettid(), basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
	#define LOGF(FMT, ...) __android_log_print(ANDROID_LOG_FATAL, LOG_TAG, "[%d*%s:%d:%s]:" FMT,	\
							gettid(), basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
	#define LOGV_IF(cond, ...) \
		( (CONDITION(cond)) \
			? LOGV(__VA_ARGS__) \
			: (0) )
	#define LOGD_IF(cond, ...) \
		( (CONDITION(cond)) \
			? LOGD(__VA_ARGS__) \
			: (0) )
	#define LOGI_IF(cond, ...) \
		( (CONDITION(cond)) \
			? LOGI(__VA_ARGS__) \
			: (0) )
	#define LOGW_IF(cond, ...) \
		( (CONDITION(cond)) \
			? LOGW(__VA_ARGS__) \
			: (0) )
	#define LOGE_IF(cond, ...) \
		( (CONDITION(cond)) \
			? LOGE(__VA_ARGS__) \
			: (0) )
	#define LOGF_IF(cond, ...) \
		( (CONDITION(cond)) \
			? LOGF(__VA_ARGS__) \
			: (0) )
#else
	#define LOG(LEVEL, TAG, FMT, ...) __android_log_print(LEVEL, TAG, FMT, ## __VA_ARGS__)
	#if defined(USE_LOGV) && !defined(LOG_NDEBUG)
		#define LOGV(FMT, ...) __android_log_print(ANDROID_LOG_VERBOSE, LOG_TAG, "[%d*%s:%d:%s]:" FMT,	\
         	 	 	 	 	 	 gettid(), basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
		#define LOGV_IF(cond, ...) \
			( (CONDITION(cond)) \
			? LOGV(__VA_ARGS__) \
			: (0) )
		#else
		#define LOGV(...)
		#define LOGV_IF(cond, ...)
	#endif
	#if defined(USE_LOGD) && !defined(LOG_NDEBUG)
		#define LOGD(FMT, ...) __android_log_print(ANDROID_LOG_DEBUG, LOG_TAG, "[%d*%s:%d:%s]:" FMT,	\
         	 	 	 	 	 	 gettid(), basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
		#define LOGD_IF(cond, ...) \
			( (CONDITION(cond)) \
			? LOGD(__VA_ARGS__) \
			: (0) )
	#else
		#define LOGD(...)
		#define LOGD_IF(cond, ...)
	#endif
	#if defined(USE_LOGI)
		#define LOGI(FMT, ...) __android_log_print(ANDROID_LOG_INFO, LOG_TAG, "[%d*%s:%d:%s]:" FMT,	\
         	 	 	 	 	 	 gettid(), basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
		#define LOGI_IF(cond, ...) \
			( (CONDITION(cond)) \
			? LOGI(__VA_ARGS__) \
			: (0) )
	#else
		#define LOGI(...)
		#define LOGI_IF(cond, ...)
	#endif
	#if defined(USE_LOGW)
		#define LOGW(FMT, ...) __android_log_print(ANDROID_LOG_WARN, LOG_TAG, "[%d*%s:%d:%s]:" FMT,	\
         	 	 	 	 	 	 gettid(), basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
		#define LOGW_IF(cond, ...) \
			( (CONDITION(cond)) \
			? LOGW(__VA_ARGS__) \
			: (0) )
	#else
		#define LOGW(...)
		#define LOGW_IF(cond, ...)
	#endif
	#if defined(USE_LOGE)
		#define LOGE(FMT, ...) __android_log_print(ANDROID_LOG_ERROR, LOG_TAG, "[%d*%s:%d:%s]:" FMT,	\
         	 	 	 	 	 	 gettid(), basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
		#define LOGE_IF(cond, ...) \
			( (CONDITION(cond)) \
			? LOGE(__VA_ARGS__) \
			: (0) )
	#else
		#define LOGE(...)
		#define LOGE_IF(cond, ...)
	#endif
	#if defined(USE_LOGF)
		#define LOGF(FMT, ...) __android_log_print(ANDROID_LOG_FATAL, LOG_TAG, "[%d*%s:%d:%s]:" FMT,	\
         	 	 	 	 	 	 gettid(), basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
		#define LOGF_IF(cond, ...) \
			( (CONDITION(cond)) \
			? LOGF(__VA_ARGS__) \
			: (0) )
	#else
		#define LOGF(...)
		#define LOGF_IF(cond, ...)
	#endif
#endif

#ifndef		LOG_ALWAYS_FATAL_IF
#define		LOG_ALWAYS_FATAL_IF(cond, ...) \
				( (CONDITION(cond)) \
				? ((void)__android_log_assert(#cond, LOG_TAG, ## __VA_ARGS__)) \
				: (void)0 )
#endif

#ifndef		LOG_ALWAYS_FATAL
	#define		LOG_ALWAYS_FATAL(...) \
					( ((void)__android_log_assert(nullptr, LOG_TAG, ## __VA_ARGS__)) )
#endif

#ifndef		LOG_ASSERT
	#define		LOG_ASSERT(cond, ...) LOG_FATAL_IF(!(cond), ## __VA_ARGS__)
#endif

#ifdef LOG_NDEBUG
	
	#ifndef		LOG_FATAL_IF
		#define		LOG_FATAL_IF(cond, ...) ((void)0)
	#endif
	#ifndef		LOG_FATAL
		#define		LOG_FATAL(...) ((void)0)
	#endif
	
	#else
	
	#ifndef		LOG_FATAL_IF
		#define		LOG_FATAL_IF(cond, ...) LOG_ALWAYS_FATAL_IF(cond, ## __VA_ARGS__)
	#endif
	#ifndef		LOG_FATAL
		#define		LOG_FATAL(...) LOG_ALWAYS_FATAL(__VA_ARGS__)
	#endif

#endif // #ifdef LOG_NDEBUG

#if (defined(USE_LOGALL) || defined(USE_LOGI)) && !defined(LOG_NDEBUG)
	#define MARK(FMT, ...) __android_log_print(ANDROID_LOG_INFO, LOG_TAG, "[%d*%s:%d:%s]:" FMT,	\
						gettid(), basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
#else
	#define		MARK(...)
#endif

#else	// #ifdef __ANDROID__
#define LOG_VERBOSE "[V/"
#define LOG_DEBUG "[D/"
#define LOG_INFO "[I/"
#define LOG_WARN "[W/"
#define LOG_ERROR "[E/"
#define LOG_FATAL "[F/"

#if defined(USE_LOGALL) && !defined(LOG_NDEBUG)
	#define LOG(LEVEL, TAG, FMT, ...) fprintf(stderr, LEVEL TAG, FMT "\n", ## __VA_ARGS__)
	#define LOGV(FMT, ...) fprintf(stderr, "[V/" LOG_TAG ":%s:%d:%s]:" FMT "\n",	\
							basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
	#define LOGD(FMT, ...) fprintf(stderr, "[D/" LOG_TAG ":%s:%d:%s]:" FMT "\n",	\
							basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
	#define LOGI(FMT, ...) fprintf(stderr, "[I/" LOG_TAG ":%s:%d:%s]:" FMT "\n",	\
							basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
	#define LOGW(FMT, ...) fprintf(stderr, "[W/" LOG_TAG ":%s:%d:%s]:" FMT "\n",	\
							basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
	#define LOGE(FMT, ...) fprintf(stderr, "[E/" LOG_TAG ":%s:%d:%s]:" FMT "\n",	\
							basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
	#define LOGF(FMT, ...) fprintf(stderr, "[F/" LOG_TAG ":%s:%d:%s]:" FMT "\n",	\
							basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
	#define LOGV_IF(cond, ...) \
		( (CONDITION(cond)) \
			? LOGV(__VA_ARGS__) \
			: (0) )
	#define LOGD_IF(cond, ...) \
		( (CONDITION(cond)) \
			? LOGD(__VA_ARGS__) \
			: (0) )
	#define LOGI_IF(cond, ...) \
		( (CONDITION(cond)) \
			? LOGI(__VA_ARGS__) \
			: (0) )
	#define LOGW_IF(cond, ...) \
		( (CONDITION(cond)) \
			? LOGW(__VA_ARGS__) \
			: (0) )
	#define LOGE_IF(cond, ...) \
		( (CONDITION(cond)) \
			? LOGE(__VA_ARGS__) \
			: (0) )
	#define LOGF_IF(cond, ...) \
		( (CONDITION(cond)) \
			? LOGF(__VA_ARGS__) \
			: (0) )
#else
	#define LOG(LEVEL, TAG, FMT, ...) fprintf(stderr, LEVEL TAG, FMT "\n", ## __VA_ARGS__)
	#if defined(USE_LOGV) && !defined(LOG_NDEBUG)
		#define LOGV(FMT, ...) fprintf(stderr, "[V/" LOG_TAG ":%s:%d:%s]:" FMT "\n",	\
							basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
		#define LOGV_IF(cond, ...) \
			( (CONDITION(cond)) \
			? LOGV(__VA_ARGS__) \
			: (0) )
		#else
		#define LOGV(...)
		#define LOGV_IF(cond, ...)
	#endif
	#if defined(USE_LOGD) && !defined(LOG_NDEBUG)
		#define LOGD(FMT, ...) fprintf(stderr, "[D/" LOG_TAG ":%s:%d:%s]:" FMT "\n",	\
							basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
		#define LOGD_IF(cond, ...) \
			( (CONDITION(cond)) \
			? LOGD(__VA_ARGS__) \
			: (0) )
	#else
		#define LOGD(...)
		#define LOGD_IF(cond, ...)
	#endif
	#if defined(USE_LOGI)
		#define LOGI(FMT, ...) fprintf(stderr, "[I/" LOG_TAG ":%s:%d:%s]:" FMT "\n",	\
							basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
		#define LOGI_IF(cond, ...) \
			( (CONDITION(cond)) \
			? LOGI(__VA_ARGS__) \
			: (0) )
	#else
		#define LOGI(...)
		#define LOGI_IF(cond, ...)
	#endif
	#if defined(USE_LOGW)
		#define LOGW(FMT, ...) fprintf(stderr, "[W/" LOG_TAG ":%s:%d:%s]:" FMT "\n",	\
							basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
		#define LOGW_IF(cond, ...) \
			( (CONDITION(cond)) \
			? LOGW(__VA_ARGS__) \
			: (0) )
	#else
		#define LOGW(...)
		#define LOGW_IF(cond, ...)
	#endif
	#if defined(USE_LOGE)
		#define LOGE(FMT, ...) fprintf(stderr, "[E/" LOG_TAG ":%s:%d:%s]:" FMT "\n",	\
							basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
		#define LOGE_IF(cond, ...) \
			( (CONDITION(cond)) \
			? LOGE(__VA_ARGS__) \
			: (0) )
	#else
		#define LOGE(...)
		#define LOGE_IF(cond, ...)
	#endif
	#if defined(USE_LOGF)
#define LOGF(FMT, ...) fprintf(stderr, "[F/" LOG_TAG ":%s:%d:%s]:" FMT,	\
							basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
		#define LOGF_IF(cond, ...) \
			( (CONDITION(cond)) \
			? LOGF(__VA_ARGS__) \
			: (0) )
	#else
		#define LOGF(...)
		#define LOGF_IF(cond, ...)
	#endif
#endif

#ifndef		LOG_ALWAYS_FATAL_IF
#define		LOG_ALWAYS_FATAL_IF(cond, ...) \
				( (CONDITION(cond)) \
				? ((void)__android_log_assert(#cond, LOG_TAG, ## __VA_ARGS__)) \
				: (void)0 )
#endif

#ifndef		LOG_ALWAYS_FATAL
#define		LOG_ALWAYS_FATAL(...) \
				( ((void)__android_log_assert(nullptr, LOG_TAG, ## __VA_ARGS__)) )
#endif

#ifndef		LOG_ASSERT
#define		LOG_ASSERT(cond, ...) LOG_FATAL_IF(!(cond), ## __VA_ARGS__)
#endif

#ifdef LOG_NDEBUG

	#ifndef		LOG_FATAL_IF
		#define		LOG_FATAL_IF(cond, ...) ((void)0)
	#endif
	#ifndef		LOG_FATAL
		#define		LOG_FATAL(...) ((void)0)
	#endif

#else

	#ifndef		LOG_FATAL_IF
		#define		LOG_FATAL_IF(cond, ...) LOG_ALWAYS_FATAL_IF(cond, ## __VA_ARGS__)
	#endif
	#ifndef		LOG_FATAL
		#define		LOG_FATAL(...) LOG_ALWAYS_FATAL(__VA_ARGS__)
	#endif

#endif // #ifdef LOG_NDEBUG

#if (defined(USE_LOGALL) || defined(USE_LOGI)) && !defined(LOG_NDEBUG)
	#define MARK(FMT, ...) fprintf(stderr, "[M/" LOG_TAG ":%s:%d:%s]:" FMT "\n",	\
							basename(__FILE__), __LINE__, __FUNCTION__, ## __VA_ARGS__)
#else
	#define		MARK(...)
#endif

#endif	// #else #ifdef __ANDROID__

#define		ENTER()				LOGV("begin")
#ifdef __LP64__
	#define		RETURN(code,type)	{type RESULT = (code); LOGV("end (%ld)", (long)RESULT); return RESULT;}
#else
	#define		RETURN(code,type)	{type RESULT = code; LOGV("end (%d)", (int)RESULT); return RESULT;}
#endif
#define		RET(code)			{LOGV("end"); return (code);}
#define		EXIT()				{LOGV("end"); return;}
#define		PRE_EXIT()			LOGV("end")


#define LITERAL_TO_STRING_INTERNAL(x)    #x
#define LITERAL_TO_STRING(x) LITERAL_TO_STRING_INTERNAL(x)

#define TRESPASS() \
		LOG_ALWAYS_FATAL(                                       \
			__FILE__ ":" LITERAL_TO_STRING(__LINE__)            \
			" Should not be here.");

#define SIGN(a) ((a) > 0 ? 1 : ((a) < 0 ? -1 : 0))
//#define SIGN(a) (((a) > 0) - ((a) < 0))

#ifndef CLAMP
#define CLAMP(val, min, max) ((val) < (min) ? (min) : ((val) > (max) ? (max) : (val)))
#endif

#ifndef MIN
#define MIN(a, b)	((a) < (b) ? (a) : (b))
#endif

#ifndef MAX
#define MAX(a, b)	((a) > (b) ? (a) : (b))
#endif

#endif /* UTILBASE_H_ */
