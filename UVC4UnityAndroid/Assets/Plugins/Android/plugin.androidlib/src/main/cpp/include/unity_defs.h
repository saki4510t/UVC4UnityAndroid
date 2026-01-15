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

#ifndef AANDUSB_UNITY_DEFS_H
#define AANDUSB_UNITY_DEFS_H

#include <cstdint>
// vulkan
#include "vulkan/svk_defs.h"
// unity
#include "unity/IUnityInterface.h"
#include "unity/IUnityGraphics.h"
#include "unity/IUnityGraphicsVulkan.h" // このヘッダーはVulkan関係のヘッダーよりも後にインクルードしないとだめ(redefinitionのエラーになる)

#endif //AANDUSB_UNITY_DEFS_H
