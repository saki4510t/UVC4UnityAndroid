package com.serenegiant.usb;
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
import android.app.Fragment;
import android.content.Context;
import android.content.Intent;
import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbInterface;
import android.os.Bundle;
import android.os.Handler;
import android.util.Log;

import androidx.annotation.Keep;
import androidx.annotation.NonNull;

import com.serenegiant.system.PermissionUtils;
import com.serenegiant.utils.HandlerThreadHandler;

import java.io.IOException;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * USB関係のイベントの処理のためにContextが必要なので
 * DeviceDetectorのライフサイクルの処理も含めて
 * FragmentでDeviceDetectorを保持する
 * XXX 主にUnity/Flutterのプラグインやnative側だけで処理するアプリからの使用を想定する。
 *     UnityのUnityPlayerActivityはフレームワークのActivityでサポートパッケージや
 *     androidxのFragmentActivityではないためこのクラスでも旧来のフレームワークの
 *     Fragmentを使う。
 */
@Keep
public class DeviceDetectorFragment extends Fragment {
	private static final boolean DEBUG = false;	// set false on production
	private static final String TAG = DeviceDetectorFragment.class.getSimpleName();

	private static final String ARGS_DEVICE_FILTERS = "ARGS_DEVICE_FILTERS";

	static {
		NativeLibLoader.loadNative();
	}

	private final Object mSync = new Object();
	@NonNull
	private final DeviceDetector mDeviceDetector = DeviceDetector.createInstance();
	@NonNull
	private final Map<UsbDevice, UsbConnector> mConnectors = new HashMap<>();
	private USBMonitor mUSBMonitor;
	private Handler mAsyncHandler;

	@SuppressWarnings("deprecation")
	public DeviceDetectorFragment() {
		super();
		if (DEBUG) Log.v(TAG, "コンストラクタ:");
		// Activity再生成時にもこのFragmentの再生成をしない
		setRetainInstance(true);
	}

	@SuppressWarnings("deprecation")
	@Override
	public void onAttach(final Context context) {
		super.onAttach(context);
		if (DEBUG) Log.v(TAG, "onAttach:");
		synchronized (mSync) {
			mAsyncHandler = HandlerThreadHandler.createHandler(TAG);
		}
		mUSBMonitor = new USBMonitor(context, mOnDeviceConnectListener);
		final Bundle args = getArguments();
		if (args != null) {
			final List<DeviceFilter> filters = args.getParcelableArrayList(ARGS_DEVICE_FILTERS);
			if (filters != null) {
				mUSBMonitor.setDeviceFilter(filters);
			}
		}
		mDeviceDetector.add(mDeviceDetectorCallback);
	}

//	@Override
//	public void onCreate(@Nullable final Bundle savedInstanceState) {
//		super.onCreate(savedInstanceState);
//		if (DEBUG) Log.v(TAG, "onCreate:");
//	}

	@SuppressWarnings("deprecation")
	@Override
	public void onStart() {
		super.onStart();
		if (DEBUG) Log.v(TAG, "onStart:hasCameraPermission=" + PermissionUtils.hasCamera(getActivity()));
		if (mUSBMonitor != null) {
			if (DEBUG) Log.v(TAG, "onStart:register USBMonitor," + mUSBMonitor.getDeviceList() + "," + mUSBMonitor.getDeviceCount());
			mUSBMonitor.register();
		}
	}

//	@Override
//	public void onResume() {
//		super.onResume();
//		if (DEBUG) Log.v(TAG, "onResume:");
//	}

//	@Override
//	public void onPause() {
//		if (DEBUG) Log.v(TAG, "onPause:");
//		super.onPause();
//	}

	@SuppressWarnings("deprecation")
	@Override
	public void onStop() {
		if (DEBUG) Log.v(TAG, "onStop:");
		if (mUSBMonitor != null) {
			if (DEBUG) Log.v(TAG, "onStop:unregister USBMonitor");
			mUSBMonitor.unregister();
		}
		mDeviceDetector.clearAll();
		super.onStop();
	}

//	@Override
//	public void onDestroy() {
//		if (DEBUG) Log.v(TAG, "onDestroy:");
//		super.onDestroy();
//	}

	@SuppressWarnings("deprecation")
	@Override
	public void onDetach() {
		if (DEBUG) Log.v(TAG, "onDetach:");
		mDeviceDetector.remove(mDeviceDetectorCallback);
		if (mUSBMonitor != null) {
			mUSBMonitor.destroy();
			mUSBMonitor = null;
		}
		synchronized (mSync) {
			if (mAsyncHandler != null) {
				try {
					mAsyncHandler.removeCallbacksAndMessages(null);
					mAsyncHandler.getLooper().quit();
				} catch (final Exception e) {
					if (DEBUG) Log.w(TAG, e);
				}
				mAsyncHandler = null;
			}
		}
		super.onDetach();
	}

//--------------------------------------------------------------------------------
	/**
	 * native側へ登録する
	 * パーミッションを保持していること
	 * @param device
	 */
	private void addDevice(@NonNull final UsbDevice device) {
		if (DEBUG) Log.v(TAG, "addDevice:" + device.getDeviceName());
		if (mUSBMonitor.hasPermission(device)) {
			try {
				final UsbConnector connector
					= mUSBMonitor.openDevice(device);
				synchronized (mConnectors) {
					mConnectors.put(device, connector);
				}
				mDeviceDetector.add(device, connector.getFileDescriptor());
			} catch (final IOException e) {
				// ここに来るのはおかしい
				Log.w(TAG, e);
			}
		}
	}

	/**
	 * native側から登録解除する
	 * @param device
	 */
	private void removeDevice(@NonNull final UsbDevice device) {
		if (DEBUG) Log.v(TAG, "removeDevice:" + device.getDeviceName());
		mDeviceDetector.remove(device);
		synchronized (mConnectors) {
			if (mConnectors.containsKey(device)) {
				final UsbConnector removed = mConnectors.remove(device);
				if (removed != null) {
					removed.close();
				}
			}
		}
	}

	@SuppressWarnings("deprecation")
	private void bringToForeground() {
		final Activity activity = getActivity();
		if ((activity != null) && !activity.isFinishing()) {
//			final Intent intent = activity.getPackageManager().getLaunchIntentForPackage(activity.getPackageName());
			final Intent intent = new Intent(activity, activity.getClass())
				.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
			activity.startActivity(intent);
		}
	}

	private final USBMonitor.Callback
		mOnDeviceConnectListener = new USBMonitor.Callback() {
		@Override
		public void onAttach(@NonNull final UsbDevice device) {
			if (DEBUG) Log.v(TAG, "Callback#onAttach:" + device.getDeviceName());
			if (mUSBMonitor.hasPermission(device)) {
				// すでにパーミッションを保持しているとき
				addDevice(device);
			} else {
				// パーミッションを保持していないとき
				mUSBMonitor.requestPermission(device);
			}
		}

		@Override
		public void onPermission(@NonNull final UsbDevice device) {
			if (DEBUG) Log.v(TAG, "Callback#onPermission:" + device.getDeviceName());
			addDevice(device);
			// システムダイアログが表示されている状態でアプリ上に表示されているパーミッションダイアログで許可すると
			// システムダイアログが表示されたままになるのでアプリをフォアグラウンドへ移動させる
			bringToForeground();
		}

		@Override
		public void onConnected(@NonNull final UsbDevice device,
			@NonNull final UsbConnector connector) {
			if (DEBUG) Log.v(TAG, "Callback#onConnected:" + device.getDeviceName());
		}

		@Override
		public void onDisconnect(@NonNull final UsbDevice device) {
			if (DEBUG) Log.v(TAG, "Callback#onDisconnect:" + device.getDeviceName());
		}

		@Override
		public void onDetach(@NonNull final UsbDevice device) {
			if (DEBUG) Log.v(TAG, "Callback#onDetach:" + device.getDeviceName());
			removeDevice(device);
		}

		@Override
		public void onCancel(@NonNull final UsbDevice device) {
			if (DEBUG) Log.v(TAG, "Callback#onCancel:" + device.getDeviceName());
		}

		@Override
		public void onError(final UsbDevice device, @NonNull final Throwable t) {
			Log.w(TAG, "Callback#onError:", t);
		}
	};

	private final DeviceDetector.DeviceDetectorCallback mDeviceDetectorCallback
		= new DeviceDetector.DeviceDetectorCallback() {
		@Override
		public void onRequestRefreshDevices() {
			if (DEBUG) Log.v(TAG, "onRequestRefreshDevices:");
			// native側からの接続機器一覧更新要求
			synchronized (mSync) {
				if (mAsyncHandler != null) {
					mAsyncHandler.post(new Runnable() {
						@Override
						public void run() {
							mDeviceDetector.clearAll();
							if (mUSBMonitor != null && mUSBMonitor.isRegistered()) {
								mUSBMonitor.refreshDevices();
							}
						}
					});
				}
			}
		}

		@Override
		public boolean onRequestClaimInterfaces(
			@NonNull final UsbDevice device, @NonNull final List<UsbInterface> interfaces) {

			if (DEBUG) Log.v(TAG, "onRequestClaimInterfaces:" + device.getDeviceName());
			boolean result = false;

			synchronized (mConnectors) {
				if (mConnectors.containsKey(device)) {
					final UsbConnector connector = mConnectors.get(device);
					if (connector != null) {
						for (final UsbInterface intf: interfaces) {
							connector.claimInterface(intf);
						}
						result = true;
					}
				}
			}

			return result;
		}

		@Override
		public boolean onRequestReleaseInterfaces(
			@NonNull final UsbDevice device, @NonNull final List<UsbInterface> interfaces) {

			if (DEBUG) Log.v(TAG, "onRequestReleaseInterfaces:" + device.getDeviceName());
			boolean result = false;

			synchronized (mConnectors) {
				if (mConnectors.containsKey(device)) {
					final UsbConnector connector = mConnectors.get(device);
					if (connector != null) {
						for (final UsbInterface intf: interfaces) {
							connector.releaseInterface(intf);
						}
						result = true;
					}
				}
			}

			return result;
		}
	};
}
