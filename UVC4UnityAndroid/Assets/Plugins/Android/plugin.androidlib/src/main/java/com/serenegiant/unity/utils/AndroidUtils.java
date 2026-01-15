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

import android.annotation.SuppressLint;
import android.app.Activity;
import android.app.Fragment;
import android.app.FragmentManager;
import android.app.FragmentTransaction;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.provider.Settings;

import androidx.annotation.Keep;
import androidx.annotation.NonNull;

import com.serenegiant.system.PermissionUtils;

import java.util.Random;

/**
 * Androidのライフサイクルイベントとパーミッション処理をハンドリングするための
 * ヘルパークラス
 * 実際の処理の大部分は非UIフラグメントのAndroidUtilsFragmentで行う
 */
@Keep
public final class AndroidUtils {
	private static final boolean DEBUG = false;	// set false on production
	private static final String TAG = AndroidUtils.class.getSimpleName();

	/**
	 * OnStartイベント
	 */
	static final String EVENT_ON_START = "OnStartEvent";
	/**
	 * onResumeイベント
	 */
	static final String EVENT_ON_RESUME = "OnResumeEvent";
	/**
	 * onPauseイベント
	 */
	static final String EVENT_ON_PAUSE = "OnPauseEvent";
	/**
	 * OnStartイベント
	 */
	static final String EVENT_ON_STOP = "OnStopEvent";

	static final String EVENT_ON_PERMISSION_GRANT = "OnPermissionGrant";
	static final String EVENT_ON_PERMISSION_DENY = "OnPermissionDeny";
	static final String EVENT_ON_PERMISSION_DENY_AND_NEVER_ASK_AGAIN = "OnPermissionDenyAndNeverAskAgain";
	/**
	 * UnityPlayer#UnitySendMessageで送信するコールバックの宛先
	 */
	static final String ARGS_KEY_CALLBACK_UNITY_OBJECT_NAME
		= "ARGS_KEY_CALLBACK_UNITY_OBJECT_NAME";

	static final int REQUEST_CODE = new Random().nextInt(0xffff);

//--------------------------------------------------------------------------------
	/**
	 * AndroidUtilsを初期化する
	 * Unity側から呼び出される
	 * @param activity
	 * @param callbackUnityObjName Unityへコールバックメッセージを送るときのUnity側のオブジェクト名
	 * @return
	 */
	public static void initialize(@NonNull final Activity activity,
		@NonNull final String callbackUnityObjName) {

		final FragmentManager fm = activity.getFragmentManager();
		Fragment utils = fm.findFragmentByTag(AndroidUtils.class.getName());
		if (!(utils instanceof IAndroidUtils)) {
			final FragmentTransaction ft = fm.beginTransaction();
			if (utils != null) {
				ft.remove(utils);
			}
			utils = new AndroidUtilsFragment();	// とりあえずフレームワーク版のみに対応
			// Unityへコールバックメッセージを送るときのUnity側のオブジェクト名Bundleに入れてて渡す
			final Bundle args = new Bundle();
			args.putString(ARGS_KEY_CALLBACK_UNITY_OBJECT_NAME, callbackUnityObjName);
			utils.setArguments(args);
			ft.add(utils, AndroidUtils.class.getName()).commit();
		}
	}

	/**
	 * パーミッションを保持しているかどうか取得
	 * @param activity
	 * @param permission
	 * @return
	 * @throws IllegalStateException
	 */
	public static boolean hasPermission(@NonNull final Activity activity,
		@NonNull final String permission) throws IllegalStateException {

		final IAndroidUtils utils = requireUtils((activity));
		return PermissionUtils.checkSelfPermission(activity, permission) == PackageManager.PERMISSION_GRANTED;
	}

	/**
	 * 許可ダイアログの再表示判定（永続的に不許可設定の場合、falseが返却される）
	 * @param activity
	 * @param permission
	 * @return
	 * @throws IllegalStateException
	 */
	public static boolean shouldShowRequestPermissionRationale(
		@NonNull final Activity activity,
		@NonNull final String permission) throws IllegalStateException {

		if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
			return activity.shouldShowRequestPermissionRationale(permission);
		} else {
			return true;
		}
	}

	/**
	 * パーミッションを要求
	 * @param activity
	 * @param permission
	 * @throws IllegalStateException
	 */
	@SuppressLint("NewApi")
	public static void requestPermission(@NonNull final Activity activity,
		@NonNull final String permission)
			throws IllegalStateException {

		final IAndroidUtils utils = requireUtils((activity));
		if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
			utils.requestPermission(permission, REQUEST_CODE);
		} else {
			utils.processPermission(permission, PackageManager.PERMISSION_GRANTED);
		}
	}

	/**
	 * パーミッション要求
	 * こっちはshouldShowRequestPermissionRationaleの処理とかも行う
	 * @param activity
	 * @param permission
	 * @throws IllegalStateException
	 */
	public static void grantPermission(@NonNull final Activity activity,
		@NonNull final String permission)
			throws IllegalStateException {

		final IAndroidUtils utils = requireUtils((activity));
		if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
			utils.grantPermission(permission, REQUEST_CODE);
		} else {
			utils.processPermission(permission, PackageManager.PERMISSION_GRANTED);
		}
	}

	/**
	 * このアプリ用のアプリ設定画面を表示する
	 * @param activity
	 */
	public static void showAppSettings(@NonNull final Activity activity) {
		final String uriString = "package:" + activity.getPackageName();
		Intent intent = new Intent(Settings.ACTION_APPLICATION_DETAILS_SETTINGS, Uri.parse(uriString));
		activity.startActivity(intent);
	}

//--------------------------------------------------------------------------------
	/**
	 * AndroidUtilsインスタンスを取得する
	 * Unity側からの呼び出し時のヘルパーメソッド
	 * @param activity
	 * @return
	 * @throws IllegalStateException
	 */
	private static IAndroidUtils requireUtils(@NonNull final Activity activity)
		throws IllegalStateException {

		final FragmentManager fm = activity.getFragmentManager();
		final Fragment utils = fm.findFragmentByTag(AndroidUtils.class.getName());
		if (!(utils instanceof IAndroidUtils)) {
			throw new IllegalStateException();
		}
		return (IAndroidUtils)utils;
	}

//--------------------------------------------------------------------------------
}
