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
import android.app.FragmentManager;
import android.app.FragmentTransaction;
import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbInterface;
import android.os.Bundle;
import android.util.Log;

import androidx.annotation.Keep;
import androidx.annotation.NonNull;
import androidx.annotation.XmlRes;

import com.serenegiant.unity.uvcplugin.R;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.concurrent.CopyOnWriteArraySet;

/**
 * Java側でUSB関係のイベントを処理してnative側へ引き渡すための
 * ヘルパークラス
 */
@Keep
public final class DeviceDetector {
	private static final boolean DEBUG = false;	// set false on production
	private static final String TAG = DeviceDetector.class.getSimpleName();

	private static final String ARGS_DEVICE_FILTERS = "ARGS_DEVICE_FILTERS";

	/**
	 * DeviceDetectorをUVC機器用に初期化(非UI Fragmentを使ってシングルトン的にアクセスする)
	 * @param activity
	 */
	@Keep
	public static void initUVCDeviceDetector(
		@NonNull final Activity activity) {

		initDeviceDetector(activity, R.xml.device_filter_uvc);
	}

	/**
	 * DeviceDetectorを初期化(非UI Fragmentを使ってシングルトン的にアクセスする)
	 * @param activity
	 * @param filtersXml
	 * @return
	 */
	@SuppressWarnings("deprecation")
	@Keep
	public static void initDeviceDetector(
		@NonNull final Activity activity, @XmlRes final int filtersXml) {

		if (DEBUG) Log.v(TAG, "initDeviceDetector:");
		final FragmentManager fm = activity.getFragmentManager();
		Fragment detector = fm.findFragmentByTag(DeviceDetectorFragment.class.getName());
		if (!(detector instanceof DeviceDetectorFragment)) {
			final FragmentTransaction ft = fm.beginTransaction();
			if (detector != null) {
				ft.remove(detector);
			}
			detector = new DeviceDetectorFragment();
			final Bundle args = new Bundle();
			if (filtersXml != 0) {
				final ArrayList<DeviceFilter> filters
						= new ArrayList<>(DeviceFilter.getDeviceFilters(activity, filtersXml));
				args.putParcelableArrayList(ARGS_DEVICE_FILTERS, filters);
			}
			detector.setArguments(args);
			ft.add(detector, DeviceDetectorFragment.class.getName()).commit();
		}
	}

	/**
	 * DeviceDetectorを解放する
	 * @param activity
	 */
	@SuppressWarnings("deprecation")
	@Keep
	public static void releaseDeviceDetector(@NonNull final Activity activity) {
		if (DEBUG) Log.v(TAG, "releaseDeviceDetector:");
		final FragmentManager fm = activity.getFragmentManager();
		Fragment detector = fm.findFragmentByTag(DeviceDetectorFragment.class.getName());
		if (detector != null) {
			final FragmentTransaction ft = fm.beginTransaction();
			ft.remove(detector);
			ft.commit();
		}
	}

//--------------------------------------------------------------------------------
	/**
	 * native側からから呼び出されたときの通知用インターフェース
	 */
	@Keep
	public interface DeviceDetectorCallback {
		public void onRequestRefreshDevices();
		public boolean onRequestClaimInterfaces(
			@NonNull final UsbDevice device,
			@NonNull final List<UsbInterface> interfaces);
		public boolean onRequestReleaseInterfaces(
			@NonNull final UsbDevice device,
			@NonNull final List<UsbInterface> interfaces);
	}

	static {
		NativeLibLoader.loadNative();
	}

	/**
	 * DeviceDetectorインスタンスを生成する
	 * XXX DeviceDetectorを自前で生成することは基本的にないはず
	 *     代わりにinitDeviceDetector/initUVCDeviceDetectorを使って
	 *     非UIフラグメントで生成・保持させる
	 * @return
	 */
	static synchronized DeviceDetector createInstance() {
		return new DeviceDetector();
	}

//--------------------------------------------------------------------------------
	@NonNull
	private final Set<DeviceDetectorCallback> mCallbacks = new CopyOnWriteArraySet<>();
	@NonNull
	private final Map<String, UsbDevice> mDevices = new HashMap<>();

	/**
	 * コンストラクタ
	 */
	private DeviceDetector() {
		if (DEBUG) Log.v(TAG, "DeviceDetector:");
		nativeCreate();
	}

	/**
	 * デストラクタ
	 * @throws Throwable
	 */
	@Override
	protected void finalize() throws Throwable {
		if (DEBUG) Log.v(TAG, "finalize:");
		try {
			release();
		} finally {
			super.finalize();
		}
	}

	private void release() {
		if (DEBUG) Log.v(TAG, "release:");
		nativeRelease();
	}

	/**
	 * UsbDeviceが検出＆パーミッション取得できたときの処理
	 * @param device 接続されたUSB機器を示すUsbDevice
	 * @param fileDescriptor USB機器へアクセスするためのファイルディスクリプタ
	 */
	public void add(@NonNull final UsbDevice device, final int fileDescriptor) {
		final String name = device.getDeviceName();
		if (DEBUG) Log.v(TAG, "add:" + name);
		mDevices.put(name, device);
		nativeAdd(name, fileDescriptor);
	}

	/**
	 * UsbDeviceが取り外されたときの処理
	 * @param device 取り外されたUSB機器を示すUsbDevice
	 */
	public void remove(@NonNull final UsbDevice device) {
		final String name = device.getDeviceName();
		if (DEBUG) Log.v(TAG, "remove:" + name);
		nativeRemove(name);
		mDevices.remove(name);
	}

	public void clearAll() {
		if (DEBUG) Log.v(TAG, "clearAll:");
		nativeClearAll();
	}

	public void add(@NonNull final DeviceDetectorCallback callback) {
		if (DEBUG) Log.v(TAG, "add:" + callback);
		mCallbacks.add(callback);
	}

	public void remove(@NonNull final DeviceDetectorCallback callback) {
		if (DEBUG) Log.v(TAG, "remove:" + callback);
		mCallbacks.remove(callback);
	}

	/**
	 * native側からの接続機器リフレッシュ要求
	 */
	/*CallByNative*/
	@Keep
	private void onRequestRefreshDevice() {
		if (DEBUG) Log.v(TAG, "onRequestRefreshDevice:");
		for (final DeviceDetectorCallback callback: mCallbacks) {
			callback.onRequestRefreshDevices();
		}
	}

	/**
	 * native側からのインターフェースclaim要求
	 * @param name
	 * @param clazz
	 * @param subClass
	 * @param protocol
	 */
	/*CallByNative*/
	@Keep
	private void onRequestClaimInterfaces(
		@NonNull final String name,
		final int clazz, final int subClass, final int protocol) {

		final UsbDevice device = mDevices.containsKey(name) ? mDevices.get(name) : null;
		if (DEBUG) Log.v(TAG, "onRequestClaimInterfaces:" + (device != null ? device.getDeviceName() : ""));
		if (device != null) {
			final List<UsbInterface> interfaces = UsbUtils.findInterfaces(device, clazz, subClass, protocol);
			for (final DeviceDetectorCallback callback: mCallbacks) {
				callback.onRequestClaimInterfaces(device, interfaces);
			}
		}
	}

	/**
	 * native側からのインターフェースrelease要求
	 * @param name
	 * @param clazz
	 * @param subClass
	 * @param protocol
	 */
	/*CallByNative*/
	@Keep
	private void onRequestReleaseInterfaces(
		@NonNull final String name,
		final int clazz, final int subClass, final int protocol) {
		final UsbDevice device = mDevices.containsKey(name) ? mDevices.get(name) : null;
		if (DEBUG) Log.v(TAG, "onRequestReleaseInterfaces:" + (device != null ? device.getDeviceName() : ""));
		if (device != null) {
			final List<UsbInterface> interfaces = UsbUtils.findInterfaces(device, clazz, subClass, protocol);
			for (final DeviceDetectorCallback callback: mCallbacks) {
				callback.onRequestReleaseInterfaces(device, interfaces);
			}
		}
	}

	/**
	 * nativeCreateはnative側へJava側のDeviceDetectorオブジェクトを引き渡すためstaticメソッドではない
	 * @return
	 */
	private native int nativeCreate();

	private static native int nativeRelease();
	private static native int nativeClearAll();
	private static native int nativeAdd(@NonNull final String name, final int fileDescriptor);
	private static native int nativeRemove(@NonNull final String name);
}
