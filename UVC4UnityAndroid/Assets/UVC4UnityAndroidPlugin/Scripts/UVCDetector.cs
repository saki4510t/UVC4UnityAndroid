//#define ENABLE_LOG
/*
 * Copyright (c) 2014 - 2026 t_saki@serenegiant.com 
 */
using AOT;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Serenegiant.UVC
{
	/**
	 * UVC機器の接続・切断イベント用インターフェース
	 */
	public interface IUVCDetectHandler
	{
		/**
		 * UVC機器が接続された
		 * @param device 接続されたUVC機器情報
		 * @return 現在は返り値を使わない
		 */
		bool OnUVCAttachEvent(UVCDevice device);
		/**
		 * UVC機器が取り外された
		 * @param device 取り外されたUVC機器情報
		 */
		void OnUVCDetachEvent(UVCDevice device);
	}

	/**
	 * UVC機器の接続・切断イベントをハンドリングするためのヘルパークラス
	 */
	[RequireComponent(typeof(AndroidUtils))]
	public class UVCDetector
	{
		private const string TAG = "UVCDetector#";
		private const string FQCN_DETECTOR = "com.serenegiant.usb.DeviceDetector";

		/**
		 * コールバックインターフェース
		 */
		private readonly List<IUVCDetectHandler> detectHandlers = new List<IUVCDetectHandler>();
		/**
		 * 初期化済みかどうか
		 */
		private bool isInitialized;
		/**
		 * 端末に接続されたUVC機器の状態が変化した時のイベントコールバックを受け取るデリゲーター
		 */
		private DetectEventManager.OnDeviceChangedFunc callback;
		/**
		 * 端末に接続されたUVC機器リスト
		 */
		private List<UVCDevice> attachedDevices = new List<UVCDevice>();

		/**
		 * コンストラクタ
		 */
		public UVCDetector()
		{
			isInitialized = false;
			Initialize();
		}
	
		/**
		 * 初期化
		 */
		public void Initialize()
		{
			if (!isInitialized)
			{
				// UVC機器接続・切断イベントを受け取れるように登録
				callback = DetectEventManager.Add(this);

				// aandusbのDeviceDetectorを読み込み要求
				using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_DETECTOR))
				{
					clazz.CallStatic("initUVCDeviceDetector",
						AndroidUtils.GetCurrentActivity());
				}
				isInitialized = true;
			}
		}

		/**
		 * リソースを解放
		 */
		public void Release()
		{
			if (isInitialized)
			{
				DetectEventManager.Remove(this);
				isInitialized = false;
			}
		}

		/**
		 * UVC機器接続・切断イベント用のインターフェースを登録する
		 * @param handler
		 */
		public void Register(IUVCDetectHandler handler)
		{
			detectHandlers.Add(handler);
		}

		/**
		 * UVC機器接続・切断イベント用のインターフェースの登録を解除する
		 * @param handler
		 */
		public void Unregister(IUVCDetectHandler handler)
		{
			detectHandlers.Remove(handler);
		}

		/**
		 * 接続されているUVCDevice一覧を取得する
		 */
		public IReadOnlyList<UVCDevice> GetAttached()
		{
			return attachedDevices.AsReadOnly();
		}

		//--------------------------------------------------------------------------------
		// UVC機器接続状態が変化したときのプラグインからのコールバック関数
		//--------------------------------------------------------------------------------
		internal void OnDeviceChanged(Int32 deviceId, bool attached)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
            Console.WriteLine($"{TAG}OnDeviceChangedInternal:id={deviceId},attached={attached}");
#endif
			if (attached)
			{
				UVCDevice device = new UVCDevice(deviceId);
#if (!NDEBUG && DEBUG && ENABLE_LOG)
                Console.WriteLine($"{TAG}OnDeviceChangedInternal:device={device.ToString()}");
#endif
				foreach(IUVCDetectHandler handler in detectHandlers)
				{
					handler.OnUVCAttachEvent(device);
				}
				// 元々はIUVCDetectHandler#OnUVCAttachEventがtrueを返したときだけ
				// attachedDevicesへ追加していたけどUVCManagerとUVCDetectorを分割して
				// 後からUVCManager/UVCDrawerから使うかもしれないので常に
				// attachedDevicesへ追加する
				attachedDevices.Add(device);
			}
			else
			{
				var found = attachedDevices.Find(item =>
				{
					return item != null && item.id == deviceId;
				});
				if (found != null)
				{
					foreach (IUVCDetectHandler handler in detectHandlers)
					{
						handler.OnUVCDetachEvent(found);
					}
					attachedDevices.Remove(found);
				}
			}
		}

		/**
		 * IL2Cppだとc/c++からのコールバックにつかうデリゲーターをマーシャリングできないので
		 * staticなクラス・関数で処理をしないといけない。
		 * だだしそれだと呼び出し元のオブジェクトの関数を呼び出せないのでマネージャークラスを作成
		 * とりあえずはUVCDetectorだけを受け付けるのでインターフェースにはしていない
		 */
		internal static class DetectEventManager
		{
			//コールバック関数の型を宣言
			[UnmanagedFunctionPointer(CallingConvention.StdCall)]
			public delegate void OnDeviceChangedFunc(Int32 id, Int32 deviceId, bool attached);

			/**
			 * プラグインのnative側登録関数
			 */
			[DllImport("unityuvcplugin")]
			private static extern IntPtr Register(Int32 id, OnDeviceChangedFunc deviceChanged);
			/**
			 * プラグインのnative側登録解除関数
			 */
			[DllImport("unityuvcplugin")]
			private static extern IntPtr Unregister(Int32 id);

			private static Dictionary<Int32, UVCDetector> sDetectors = new Dictionary<Int32, UVCDetector>();

			/**
			 * 指定したUVCDetectorを接続機器変化コールバックに追加
			 */
			public static OnDeviceChangedFunc Add(UVCDetector detector)
			{
				Int32 id = detector.GetHashCode();
				OnDeviceChangedFunc onDeviceChanged = new OnDeviceChangedFunc(OnDeviceChanged);
				sDetectors.Add(id, detector);
				Register(id, onDeviceChanged);
				return onDeviceChanged;
			}

			/**
			 * 指定したUVCDetectorを接続機器変化コールバックから削除
			 */
			public static void Remove(UVCDetector detector)
			{
				Int32 id = detector.GetHashCode();
				Unregister(id);
				sDetectors.Remove(id);
			}

			/**
			 * nativeプラグインから呼び出されるコールバック関数
			 */
			[MonoPInvokeCallback(typeof(OnDeviceChangedFunc))]
			public static void OnDeviceChanged(Int32 id, Int32 deviceId, bool attached)
			{
				var detectotor = sDetectors.ContainsKey(id) ? sDetectors[id] : null;
				if (detectotor != null)
				{
					detectotor.OnDeviceChanged(deviceId, attached);
				}
			}

		} // DetectEventManager

	} // UVCDetector


} // namespace Serenegiant.UVC
