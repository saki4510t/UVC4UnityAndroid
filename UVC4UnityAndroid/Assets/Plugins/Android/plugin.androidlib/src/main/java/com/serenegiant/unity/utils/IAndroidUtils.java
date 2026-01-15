package com.serenegiant.unity.utils;
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

import androidx.annotation.Keep;
import androidx.annotation.NonNull;

interface IAndroidUtils {
	public void requestPermission(
		@NonNull final String permission, final int requestCode)
			throws IllegalStateException;

	public void grantPermission(
		@NonNull final String permission, final int requestCode)
			throws IllegalStateException;

	public void processPermission(@NonNull final String permission, final int grantResult);
}
