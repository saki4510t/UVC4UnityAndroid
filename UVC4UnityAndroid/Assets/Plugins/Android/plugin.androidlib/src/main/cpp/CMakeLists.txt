# For more information about using CMake with Android Studio, read the
# documentation: https://d.android.com/studio/projects/add-native-code.html

# Sets the minimum version of CMake required to build the native library.

cmake_minimum_required(VERSION 3.8)
project(unityuvcplugin)

set(CMAKE_VERBOSE_MAKEFILE on)
set(LIB_SRC_DIR ${CMAKE_CURRENT_SOURCE_DIR})

# C/C++用設定
set(c_cpp_flags "${c_cpp_flags} -DHAVE_PTHREADS -fPIC")
set(c_cpp_flags "${c_cpp_flags} -O3 -fstrict-aliasing")
set(c_cpp_flags "${c_cpp_flags} -Wno-parentheses -Wno-switch -Wno-extern-c-compat")
set(c_cpp_flags "${c_cpp_flags} -Wno-empty-body -Wno-deprecated-register -Wno-multichar")
set(c_cpp_flags "${c_cpp_flags} -Wreturn-type -Wno-incompatible-pointer-types -Wno-address-of-packed-member")
# public関数のみエクスポートする
set(export_flags "-Wl,--version-script,aandusb.map")
#set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} ${export_flags}")
#set(CMAKE_STATIC_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} ${export_flags}")

# C用の設定
set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} ${c_cpp_flags}")
#デバッグ用に警告を出す設定
#set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -Wall -Wreorder")

# C++用の設定
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} ${c_cpp_flags}")
# 例外を有効にする
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fexceptions")
# RTTI(実行時型情報)を有効にする
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -frtti")
#デバッグ用に警告を出す設定
#set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wall -Wreorder")

# OpenGL|ES3の静的/動的リンクサポート確認
message("ANDROID_PLATFORM_LEVEL=${ANDROID_PLATFORM_LEVEL}")
if (${ANDROID_PLATFORM_LEVEL} LESS 12)
    # OpenGL|ES2未対応
    message(FATAL_ERROR "OpenGL 2 is not supported before API level 11 \
        (currently using ${ANDROID_PLATFORM_LEVEL}).")
    return()
elseif (${ANDROID_PLATFORM_LEVEL} LESS 18)
    # GLES3の動的リンクを試みる
    message("ENABLE DYNAMIC_ES3")
    add_definitions("-DDYNAMIC_ES3")
    set(OPENGL_LIB GLESv2)
else ()
    # GLES3に対応している
    message("target platform supports GLES3")
    set(OPENGL_LIB GLESv3)
endif (${ANDROID_PLATFORM_LEVEL} LESS 12)

set(libs_platform
    EGL
    android
    mediandk
    log
)

#VulkanのAndroid拡張を有効にする
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -DVK_USE_PLATFORM_ANDROID_KHR")
#Vulkanのデバッグ用拡張を有効にする
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -DUSE_DEBUG_EXTENTIONS")

# プレビルドライブラリへのパスを生成
set(PREBUILD_LIB_PATH
    ${LIB_SRC_DIR}/libs/${ANDROID_ABI}
)

message("LIB_SRC_DIR=${LIB_SRC_DIR}")
message("PREBUILD_LIB_PATH=${PREBUILD_LIB_PATH}")

add_library(aandusb-native_static STATIC IMPORTED)
set_property(TARGET aandusb-native_static PROPERTY IMPORTED_LOCATION
    ${PREBUILD_LIB_PATH}/libaandusb-native_static.a
)

add_library(jpeg-turbo3x_static STATIC IMPORTED)
set_property(TARGET jpeg-turbo3x_static PROPERTY IMPORTED_LOCATION
    ${PREBUILD_LIB_PATH}/libjpeg-turbo3x_static.a
)

add_library(yuv1905_static STATIC IMPORTED)
set_property(TARGET yuv1905_static PROPERTY IMPORTED_LOCATION
    ${PREBUILD_LIB_PATH}/libyuv1905_static.a
)

add_library(unityuvcplugin SHARED
    _onload.cpp
    unity_uac_holder.cpp
    unity_uvc_holder.cpp
    unity_uvc_plugin.cpp
    unity_plugin_main.cpp
)

target_compile_definitions(unityuvcplugin PRIVATE
    AVOID_TABLES
    #ログ出力設定
    NDEBUG              # LOG_ALLを無効にする・assertを無効にする場合
    LOG_NDEBUG          # デバッグメッセージを出さないようにする時
#   USE_LOGALL          # define USE_LOGALL macro to enable all debug string
#   DISABLE_IMPORTGL    # when static link OpenGL|ES library
)

target_include_directories(unityuvcplugin PRIVATE
    include/
    include/aandusb/
)

# 共有ライブラリを16KB ELFアライメントでリンクする
target_link_options(unityuvcplugin PRIVATE
    "-Wl,--no-undefined-version"
    "-Wl,-z,max-page-size=16384"
)

# EGL GLESv3
target_link_libraries(unityuvcplugin PRIVATE
    ${OPENGL_LIB}
    ${libs_platform}
    aandusb-native_static
    jpeg-turbo3x_static
    yuv1905_static
)
