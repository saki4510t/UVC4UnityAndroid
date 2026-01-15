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

#ifndef AANDUSB_TEXTURE_VSH_H
#define AANDUSB_TEXTURE_VSH_H

// 矩形にテクスチャを貼り付けるための頂点シェーダー

#if defined(__ANDROID__)
constexpr const char *texture_gl2_vsh =
R"SHADER(#version 100
attribute vec4 aPosition;
attribute vec4 aTextureCoord;
varying vec2 vTextureCoord;
uniform mat4 uMVPMatrix;
uniform mat4 uTexMatrix;

void main() {
	vTextureCoord = (uTexMatrix * aTextureCoord).xy;
	gl_Position = uMVPMatrix * aPosition;
}
)SHADER";

constexpr const char *texture_gl3_vsh =
R"SHADER(#version 300 es
in vec4 aPosition;
in vec4 aTextureCoord;
out vec2 vTextureCoord;
uniform mat4 uMVPMatrix;
uniform mat4 uTexMatrix;

void main() {
	vTextureCoord = (uTexMatrix * aTextureCoord).xy;
	gl_Position = uMVPMatrix * aPosition;
}
)SHADER";
#else	// #if defined(__ANDROID__)
constexpr const char *texture_gl2_vsh =
R"SHADER(#version 100
attribute vec4 aPosition;
attribute vec4 aTextureCoord;
varying vec2 vTextureCoord;
uniform mat4 uMVPMatrix;
uniform mat4 uTexMatrix;

void main() {
	vTextureCoord = (uTexMatrix * aTextureCoord).xy;
	gl_Position = uMVPMatrix * aPosition;
}
)SHADER";

constexpr const char *texture_gl3_vsh =
R"SHADER(#version 330
in vec4 aPosition;
in vec4 aTextureCoord;
out vec2 vTextureCoord;
uniform mat4 uMVPMatrix;
uniform mat4 uTexMatrix;

void main() {
	vTextureCoord = (uTexMatrix * aTextureCoord).xy;
	gl_Position = uMVPMatrix * aPosition;
}
)SHADER";
#endif

#endif //AANDUSB_TEXTURE_VSH_H
