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

import static com.serenegiant.unity.utils.AndroidUtils.ARGS_KEY_CALLBACK_UNITY_OBJECT_NAME;
import static com.serenegiant.unity.utils.AndroidUtils.EVENT_ON_PAUSE;
import static com.serenegiant.unity.utils.AndroidUtils.EVENT_ON_PERMISSION_DENY;
import static com.serenegiant.unity.utils.AndroidUtils.EVENT_ON_PERMISSION_DENY_AND_NEVER_ASK_AGAIN;
import static com.serenegiant.unity.utils.AndroidUtils.EVENT_ON_PERMISSION_GRANT;
import static com.serenegiant.unity.utils.AndroidUtils.EVENT_ON_RESUME;
import static com.serenegiant.unity.utils.AndroidUtils.EVENT_ON_START;
import static com.serenegiant.unity.utils.AndroidUtils.EVENT_ON_STOP;
import static com.serenegiant.unity.utils.AndroidUtils.REQUEST_CODE;
import static com.serenegiant.unity.utils.AndroidUtils.hasPermission;

import android.Manifest;
import android.annotation.SuppressLint;
import android.app.Activity;
import android.app.Fragment;
import android.app.FragmentManager;
import android.content.pm.PackageManager;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.text.TextUtils;
import android.util.Log;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.annotation.StringRes;

import com.serenegiant.system.BuildCheck;
import com.serenegiant.system.PermissionUtils;
import com.serenegiant.unity.uvcplugin.R;
import com.unity3d.player.UnityPlayer;

/**
 * Androidのライフサイクルイベントとパーミッション処理を
 * ハンドリングするための非UIフラグメント
 */
public class AndroidUtilsFragment extends Fragment
	implements IAndroidUtils, MessageDialogAppFragment.MessageDialogListener {
	private static final boolean DEBUG = false;	// set false on production
	private static final String TAG = AndroidUtilsFragment.class.getSimpleName();

	/**
	 * イベント送信の遅延時間
	 */
	private static final long SEND_DELAY_MS = 300;
//--------------------------------------------------------------------------------
	/**
	 * AndroidUtilsインスタンスを取得する
	 * Unity側からの呼び出し時のヘルパーメソッド
	 * @param activity
	 * @return
	 * @throws IllegalStateException
	 */
	private static AndroidUtilsFragment requireUtils(@NonNull final Activity activity)
		throws IllegalStateException {

		final FragmentManager fm = activity.getFragmentManager();
		final Fragment detector = fm.findFragmentByTag(AndroidUtilsFragment.class.getName());
		if (!(detector instanceof AndroidUtilsFragment)) {
			throw new IllegalStateException();
		}
		return (AndroidUtilsFragment)detector;
	}

//--------------------------------------------------------------------------------
	/**
	 * UIスレッド上での処理用Handler
	 */
	private final Handler mUIHandler = new Handler(Looper.getMainLooper());
	/**
	 * Unityへコールバックメッセージを送るときのUnity側のオブジェクト名
	 * 正常に実行されていれば実行中は実際にはNullにはならない
	 */
	@Nullable
	private String mCallbackUnityObjName;

	/**
	 * デフォルトコンストラクタ
	 */
	public AndroidUtilsFragment() {
		super();
		// デフォルトコンストラクタが必要
		// Activity再生成時にもこのFragmentの再生成をしない
		setRetainInstance(true);
	}

	@Override
	public void onCreate(@Nullable final Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		if (DEBUG) Log.v(TAG, "onCreate:");
		Bundle args = savedInstanceState;
		if (args == null) {
			args = getArguments();
		}
		if (args != null) {
			mCallbackUnityObjName = args.getString(ARGS_KEY_CALLBACK_UNITY_OBJECT_NAME);
		}
		if (DEBUG) Log.v(TAG, "onCreate:mCallbackUnityObjName=" + mCallbackUnityObjName);
	}

	@Override
	public void onStart() {
		super.onStart();
		if (DEBUG) Log.v(TAG, "onStart:");
		mUIHandler.removeCallbacks(mSendOnStartTask);
		mUIHandler.removeCallbacks(mSendOnStopTask);
		mUIHandler.postDelayed(mSendOnStartTask, SEND_DELAY_MS);
	}

	@Override
	public void onResume() {
		super.onResume();
		if (DEBUG) Log.v(TAG, "onResume:");
		mUIHandler.removeCallbacks(mSendOnResumeTask);
		mUIHandler.removeCallbacks(mSendOnPauseTask);
		mUIHandler.postDelayed(mSendOnResumeTask, SEND_DELAY_MS);
	}

	@Override
	public void onSaveInstanceState(final Bundle outState) {
		super.onSaveInstanceState(outState);
		if (DEBUG) Log.v(TAG, "onSaveInstanceState:");
		final Bundle args = getArguments();
		if (args != null) {
			outState.putAll(args);
		}
	}

	@Override
	public void onPause() {
		if (DEBUG) Log.v(TAG, "onPause:");
		mUIHandler.removeCallbacks(mSendOnResumeTask);
		mUIHandler.removeCallbacks(mSendOnPauseTask);
		mUIHandler.postDelayed(mSendOnPauseTask, SEND_DELAY_MS);
		super.onPause();
	}

	@Override
	public void onStop() {
		if (DEBUG) Log.v(TAG, "onStop:");
		mUIHandler.removeCallbacks(mSendOnStartTask);
		mUIHandler.removeCallbacks(mSendOnStopTask);
		mUIHandler.postDelayed(mSendOnStopTask, SEND_DELAY_MS);
		super.onStop();
	}

	@Override
	public void onDestroy() {
		if (DEBUG) Log.v(TAG, "onDestroy:");
		mUIHandler.removeCallbacksAndMessages(null);
		super.onDestroy();
	}

//--------------------------------------------------------------------------------

	@Override
	public void onRequestPermissionsResult(final int requestCode,
		@NonNull final String[] permissions, @NonNull final int[] grantResults) {

		super.onRequestPermissionsResult(requestCode, permissions, grantResults);

		if (requestCode == REQUEST_CODE) {
			final int n = Math.min(permissions.length, grantResults.length);
			for (int i = 0; i < n; i++) {
				processPermission(permissions[i], grantResults[i]);
			}
		}
	}

//--------------------------------------------------------------------------------
	/**
	 * onStartイベントを遅延送信するためのRunnable
	 */
	private final Runnable mSendOnStartTask = new Runnable() {
		@Override
		public void run() {
			sendCallbackMessage(EVENT_ON_START, null);
		}
	};

	/**
	 * onResumeイベントを遅延送信するためのRunnable
	 */
	private final Runnable mSendOnResumeTask = new Runnable() {
		@Override
		public void run() {
			sendCallbackMessage(EVENT_ON_RESUME, null);
		}
	};

	/**
	 * onPauseイベントを遅延送信するためのRunnable
	 */
	private final Runnable mSendOnPauseTask = new Runnable() {
		@Override
		public void run() {
			sendCallbackMessage(EVENT_ON_PAUSE, null);
		}
	};

	/**
	 * onStopイベントを遅延送信するためのRunnable
	 */
	private final Runnable mSendOnStopTask = new Runnable() {
		@Override
		public void run() {
			sendCallbackMessage(EVENT_ON_STOP, null);
		}
	};

	/**
	 * パーミッション要求
	 * @param permission
	 * @param requestCode
	 * @throws IllegalStateException
	 */
	public void requestPermission(
		@NonNull final String permission, final int requestCode)
			throws IllegalStateException {
		requestPermissions(new String[] {permission}, requestCode);
	}

	/**
	 * パーミッション要求
	 * こっちはshouldShowRequestPermissionRationaleの処理とかも行う
	 * @param permission
	 * @param requestCode
	 * @throws IllegalStateException
	 */
	public void grantPermission(
		@NonNull final String permission, final int requestCode)
			throws IllegalStateException {

		final Activity activity = getActivity();
		if ((activity == null) || activity.isFinishing()) {
			throw new IllegalStateException();
		}
		if (!hasPermission(activity, permission)) {
			if (shouldShowRequestPermissionRationale(permission)) {
				showRationale(permission, requestCode);
			} else {
				requestPermission(permission, requestCode);
			}
		} else {
			processPermission(permission, PackageManager.PERMISSION_GRANTED);
		}
	}

	private void showRationale(@NonNull final String permission, final int requestCode) {
		@StringRes
		int idRequest = 0;
		switch (permission) {
		case Manifest.permission.CAMERA:
			idRequest = R.string.permission_camera_request_android9_uvc;
			break;
		case Manifest.permission.INTERNET:
			idRequest = R.string.permission_network_request;
			break;
		case Manifest.permission.RECORD_AUDIO:
			idRequest = R.string.permission_audio_recording_request;
			break;
		case Manifest.permission.WRITE_EXTERNAL_STORAGE:
		case Manifest.permission.READ_EXTERNAL_STORAGE:
			idRequest = R.string.permission_ext_storage_request;
			break;
		case Manifest.permission.ACCESS_COARSE_LOCATION:
		case Manifest.permission.ACCESS_FINE_LOCATION:
			idRequest = R.string.permission_location_request;
			break;
		case Manifest.permission.BLUETOOTH:
			idRequest = R.string.permission_location_request;	// 暫定
			break;
		default:
			break;
		}
		MessageDialogAppFragment.showDialog(this, requestCode,
			R.string.permission_title, idRequest,
			new String[] {permission});
	}

	@Override
	public void onMessageDialogResult(@NonNull final MessageDialogAppFragment dialog,
		final int requestCode, @NonNull final String[] permissions, final boolean result) {

		if (result) {
			// メッセージダイアログでOKを押された時はパーミッション要求する
			if (BuildCheck.isMarshmallow()) {
				requestPermissions(permissions, requestCode);
				return;
			}
		}
		// メッセージダイアログでキャンセルされた時とAndroid6でない時は自前でチェックして#checkPermissionResultを呼び出す
		for (final String permission : permissions) {
			processPermission(permission,
				PermissionUtils.checkSelfPermission(getActivity(), permission));
		}
	}

	@SuppressLint("NewApi")
	public void processPermission(@NonNull final String permission, final int grantResult) {
		if (grantResult == PackageManager.PERMISSION_GRANTED) {
			sendCallbackMessage(EVENT_ON_PERMISSION_GRANT, permission);
		} else if (!shouldShowRequestPermissionRationale(permission)) {
			sendCallbackMessage(EVENT_ON_PERMISSION_DENY_AND_NEVER_ASK_AGAIN, permission);
		} else {
			sendCallbackMessage(EVENT_ON_PERMISSION_DENY, permission);
		}
	}
//--------------------------------------------------------------------------------

	/**
	 * Unity側へコールバック用のメッセージを送信する
	 * @param event
	 * @param args
	 */
	private void sendCallbackMessage(@NonNull final String event, @Nullable final String args) {
		if (DEBUG) Log.v(TAG, "sendCallbackMessage:" + event + " to " + mCallbackUnityObjName);
		if (!TextUtils.isEmpty(mCallbackUnityObjName)) {
			try {
				UnityPlayer.UnitySendMessage(mCallbackUnityObjName, event, args != null ? args : "");
			} catch (final Exception e) {
				Log.w(TAG, e);
			}
		} else {
			Log.w(TAG, "sendCallbackMessage:invalid callback unity object name");
		}
	}
}
