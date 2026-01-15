package com.serenegiant.unity.uvcplugin;
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

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

import androidx.annotation.Keep;
import androidx.annotation.Nullable;

import com.serenegiant.usb.NativeLibLoader;

/**
 * Android用にビルドしたUnityアプリのメインアクティビティ(UnityPlayerActivity)へ
 * USBのパーマネントパーミッションアクセス用の設定を追加するのは面倒なので
 * Android OS側からパーミッションを付与されたときに起動される専用Activityを作成した。
 * Unityのメインアクティビティを起動してUsbPermissionActivity自体はすぐに終了する
 */
@Keep
public class UsbPermissionActivity extends Activity {
	private static final boolean DEBUG = false;	// set false on production
	private static final String TAG = UsbPermissionActivity.class.getSimpleName();

	static {
		NativeLibLoader.loadNative();
	}

	@Override
	protected void onCreate(@Nullable final Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		try {
			// ランチャーアクティビティ(たぶんUnityPlayerActivityのはず)の起動を試みる
			final Intent intent = getPackageManager().getLaunchIntentForPackage(getPackageName());
			if (intent != null) {
				startActivity(intent);
			}
		} catch (final Exception e) {
			Log.w(TAG, e);
		}
		finish();
	}
}
