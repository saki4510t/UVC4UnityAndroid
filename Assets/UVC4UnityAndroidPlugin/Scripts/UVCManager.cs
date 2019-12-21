//#define ENABLE_LOG

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_ANDROID && UNITY_2018_3_OR_NEWER
using UnityEngine.Android;
#endif

namespace Serenegiant.UVC {

	public class UVCManager
	{
		private const string TAG = "UVCController#";
		private const string FQCN_PLUGIN = "com.serenegiant.uvcplugin.DeviceDetector";

		/**
		 * プラグインでのレンダーイベント取得用native(c/c++)関数
		 */
		[DllImport("uvc-plugin")]
		private static extern IntPtr GetRenderEventFunc();

		private class CameraInfo
		{
			/**
			 * プレビュー中のUVCカメラ識別子, レンダーイベント用
			 */
			public Int32 activeCameraId;
			public Texture previewTexture;

			/**
			 * レンダーイベント処理用
			 * コールーチンとして実行される
			 */
			public IEnumerator OnRender()
			{
				var renderEventFunc = GetRenderEventFunc();
				for (; ; )
				{
					yield return new WaitForEndOfFrame();
					GL.IssuePluginEvent(renderEventFunc, activeCameraId);
				}
			}
		}

		private readonly MonoBehaviour parent;
		private readonly GameObject target;
		private readonly bool preferH264;
		private readonly int defaultWidth;
		private readonly int defaultHeight;

		/**
		 * ハンドリングしているカメラ情報を保持
		 * string(deviceName) - CameraInfo ペアを保持する
		 */
		private Dictionary<string, CameraInfo> cameraInfos = new Dictionary<string, CameraInfo>();
	
		//================================================================================
		/**
		 * コンストラクタ
		 * @param parent 親のスクリプト
		 * @param target  EventSystemによる関数呼び出しのターゲット
		 * @param width デフォルトの解像度(幅)
		 * @param height デフォルトの解像度(高さ)
		 * @param preferH264 H.264が使用可能な場合にMJPEGより優先して使用するかどうか
		 */
		public UVCManager(MonoBehaviour parent, GameObject target, int width, int height, bool preferH264)
		{
			this.parent = parent;
			this.target = target;
			this.preferH264 = preferH264;
			defaultWidth = width;
			defaultHeight = height;
		}

		//================================================================================
		/**
		 * カメラをopenしているか
		 * 映像取得中かどうかはIsPreviewingを使うこと
		 */
		public bool IsOpen(string deviceName)
		{
			var info = Get(deviceName);
			return (info != null) && (info.activeCameraId != 0);
		}

		/**
		 * 映像取得中かどうか
		 */
		public bool IsPreviewing(string deviceName)
		{
			var info = Get(deviceName);
			return (info != null) && (info.activeCameraId != 0) && (info.previewTexture != null);
		}

		/**
		 * 映像取得用のTextureオブジェクトを取得する
		 * @return Textureオブジェクト, プレビュー中でなければnull
		 */
		public Texture GetTexture(string deviceName)
		{
			var info = Get(deviceName);
			return info != null ? info.previewTexture : null;
		}

		//================================================================================
		/**
		 * 指定したUVC機器をopenする
		 * @param deviceName UVC機器識別文字列
		 */
		public void Open(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}Open:{deviceName}");
#endif
			var info = Get(deviceName);
			if (info != null)
			{
				AndroidUtils.isPermissionRequesting = false;
				using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
				{
					info.activeCameraId = clazz.CallStatic<Int32>("openDevice",
						AndroidUtils.GetCurrentActivity(), deviceName,
						defaultWidth, defaultHeight, preferH264);
				}
			}
			else
			{
				throw new ArgumentException("device name is empty/null");
			}
		}

		/**
		 * 指定したUVC機器をcloseする
		 * @param deviceName UVC機器識別文字列
		 */
		public void Close(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}Close:{deviceName}");
#endif
			var info = Get(deviceName);
			if (info != null)
			{
				info.activeCameraId = 0;
				info.previewTexture = null;
			}
			if (!String.IsNullOrEmpty(deviceName))
			{
				using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
				{
					clazz.CallStatic("closeDevice",
						AndroidUtils.GetCurrentActivity(), deviceName);
				}
			}
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}Close:finished");
#endif
		}

		/**
		 * UVC機器/カメラからの映像受けとりを終了要求をする
		 * @param deviceName UVC機器識別文字列
		 */
		public void StopPreview(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}StopPreview:{deviceName}");
#endif
			var info = Get(deviceName);
			if (info != null)
			{
				parent.StopCoroutine(info.OnRender());
			}
			RequestStopPreview(deviceName);
		}

		//================================================================================
		// Android固有の処理
		// Java側からのイベントコールバック

		/**
		 * UVC機器が接続された
		 * @param args UVC機器識別文字列
		 */
		public void OnEventAttach(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnEventAttach[{Time.frameCount}]:(" + args + ")");
#endif
			if (!String.IsNullOrEmpty(args))
			{   // argsはdeviceName
				var inf = CreateIfNotExist(args);
				RequestUsbPermission(args);
			}
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnEventAttach[{Time.frameCount}]:finished");
#endif
		}

		/**
		 * UVC機器が取り外された
		 * @param args UVC機器識別文字列
		 */
		public void OnEventDetach(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnEventDetach:({args})");
#endif
			Close(args);
			Remove(args);
		}

		/**
		 * UVC機器からのステータスイベントを受信した
		 * @param args UVC機器識別文字列＋ステータス
		 */
		public void OnReceiveStatus(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnReceiveStatus:({args})");
#endif
		}

		/**
		 * UVC機器からのボタンイベントを受信した
		 * @param args UVC機器識別文字列＋ボタンイベント
		 */
		public void OnButtonEvent(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnButtonEvent:({args})");
#endif
		}

		/**
		 * onResumeイベント
		 */
		public IEnumerator OnResumeEvent()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnResumeEvent:attachedDeviceName={attachedDeviceName}" +
				$",activeDeviceName={activeDeviceName}" +
				$",isPermissionRequesting={AndroidUtils.isPermissionRequesting}");
#endif
			if (!AndroidUtils.isPermissionRequesting
				&& AndroidUtils.CheckAndroidVersion(28)
				&& !AndroidUtils.HasPermission(AndroidUtils.PERMISSION_CAMERA))

			{
				yield return Initialize();
			}

			if (!AndroidUtils.isPermissionRequesting)
			{	// パーミッション要求中ではないとき
				foreach (var elm in cameraInfos)
				{
					if (elm.Value.activeCameraId == 0)
					{	// アタッチされた機器があるけどオープンされていないとき
						RequestUsbPermission(elm.Key);
						break;
					}
				}
			}

			yield break;
		}

		/**
		 * onPauseイベント
		 */
		public void OnPauseEvent()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnPauseEvent:");
#endif
//			Close(activeDeviceName);	// CameraDrawerからCloseを呼ぶので不要
		}

		//--------------------------------------------------------------------------------
		public IEnumerator Initialize()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}Initialize:");
#endif
			if (AndroidUtils.CheckAndroidVersion(28))
			{
				yield return AndroidUtils.GrantCameraPermission((string permission, AndroidUtils.PermissionGrantResult result) =>
				{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
					Console.WriteLine($"{TAG}OnPermission:{permission}={result}");
#endif
					switch (result)
					{
						case AndroidUtils.PermissionGrantResult.PERMISSION_GRANT:
							InitPlugin();
							break;
						case AndroidUtils.PermissionGrantResult.PERMISSION_DENY:
							if (AndroidUtils.ShouldShowRequestPermissionRationale(AndroidUtils.PERMISSION_CAMERA))
							{
								// パーミッションを取得できなかった
								// FIXME 説明用のダイアログ等を表示しないといけない
							}
							break;
						case AndroidUtils.PermissionGrantResult.PERMISSION_DENY_AND_NEVER_ASK_AGAIN:
							break;
					}
				});
			}

			yield break;
		}

		// uvc-plugin-unityへの処理要求
		/**
		 * プラグインを初期化
		 */
		private void InitPlugin()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}InitPlugin:");
#endif
			using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
			{
				clazz.CallStatic("initDeviceDetector",
					AndroidUtils.GetCurrentActivity(), target.name);
			}
		}

		/**
		 * 指定したUSB機器をアクセスするパーミッションを持っているかどうかを取得
		 * @param deviceName UVC機器識別文字列
		 */
		private bool HasUsbPermission(string deviceName)
		{
			if (!String.IsNullOrEmpty(deviceName))
			{
				using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
				{
					return clazz.CallStatic<bool>("hasUsbPermission",
						AndroidUtils.GetCurrentActivity(), deviceName);
				}
			}
			else
			{
				return false;
			}
		}

		/**
		 * USB機器アクセスのパーミッション要求
		 * @param deviceName UVC機器識別文字列
		 */
		private void RequestUsbPermission(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}RequestUsbPermission[{Time.frameCount}]:({deviceName})");
#endif
			if (!String.IsNullOrEmpty(deviceName))
			{
				AndroidUtils.isPermissionRequesting = true;

				using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
				{
					clazz.CallStatic("requestPermission",
						AndroidUtils.GetCurrentActivity(), deviceName);
				}
			}
			else
			{
				throw new ArgumentException("device name is empty/null");
			}
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}RequestUsbPermission[{Time.frameCount}]:finsihed");
#endif
		}

		/**
		 * UVC機器からの映像受け取り開始要求をする
		 * この関数では指定したサイズに対応しているかどうかのチェックをしないので
		 * 呼び出し元でチェックすること
		 * 通常はStartPreview(string deviceName)経由で呼び出す
		 * @param deviceName UVC機器識別文字列
		 * @param width
		 * @param height
		 */
		public void StartPreview(string deviceName, int width, int height)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}StartPreview:{deviceName}({width}x{height})");
#endif
			if (!IsPreviewing(deviceName))
			{
				var info = Get(deviceName);
				if (info != null)
				{
					info.previewTexture = new Texture2D(
							width, height,
							TextureFormat.ARGB32,
							false, /* mipmap */
							true /* linear */);
					var nativeTexPtr = info.previewTexture.GetNativeTexturePtr();
#if (!NDEBUG && DEBUG && ENABLE_LOG)
					Console.WriteLine($"{TAG}RequestStartPreview:tex={nativeTexPtr}");
#endif
					using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
					{
						clazz.CallStatic("setPreviewTexture",
							AndroidUtils.GetCurrentActivity(), deviceName,
							nativeTexPtr.ToInt32(),
							-1,	// PreviewMode, -1:自動選択(Open時に指定したPreferH264フラグが有効になる)
							width, height);
					}

					parent.StartCoroutine(info.OnRender());
				}
				else
				{
					throw new ArgumentException("device name is empty/null");
				}
			}
		}

		/**
		 * UVC機器からの映像受けとりを終了要求をする
		 * @param deviceName UVC機器識別文字列
		 */
		private void RequestStopPreview(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}RequestStopPreviewUVC:{deviceName}");
#endif
			if (!String.IsNullOrEmpty(deviceName))
			{
				using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
				{
					clazz.CallStatic("stopPreview",
						AndroidUtils.GetCurrentActivity(), deviceName);
				}
			}
		}

		/**
		 * 指定したUVC機器の情報(今はvidとpid)をJSON文字列として取得する
		 * @param deviceName UVC機器識別文字列
		 */
		public UVCInfo GetInfo(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}GetInfo:{deviceName}");
#endif

			if (!String.IsNullOrEmpty(deviceName))
			{
				using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
				{
					return UVCInfo.Parse(
						clazz.CallStatic<string>("getInfo",
							AndroidUtils.GetCurrentActivity(), deviceName));
				}
			}
			else
			{
				throw new ArgumentException("device name is empty/null");
			}

		}

		/**
		 * 指定したUVC機器の対応解像度を取得する
		 * @param deviceName UVC機器識別文字列
		 */
		public SupportedFormats GetSupportedVideoSize(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}GetSupportedVideoSize:{deviceName}");
#endif

			if (!String.IsNullOrEmpty(deviceName))
			{
				using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
				{
					return SupportedFormats.Parse(
						clazz.CallStatic<string>("getSupportedVideoSize",
							AndroidUtils.GetCurrentActivity(), deviceName));
				}
			}
			else
			{
				throw new ArgumentException("device name is empty/null");
			}
		}

		/*NonNull*/
		private CameraInfo CreateIfNotExist(string deviceName)
		{
			if (!cameraInfos.ContainsKey(deviceName))
			{
				cameraInfos[deviceName] = new CameraInfo();
			}
			return cameraInfos[deviceName];
		}

		/*Nullable*/
		private CameraInfo Get(string deviceName)
		{
			return cameraInfos.ContainsKey(deviceName) ? cameraInfos[deviceName] : null;
		}

		/*Nullable*/
		private CameraInfo Remove(string deviceName)
		{
			CameraInfo info = null;

			if (cameraInfos.ContainsKey(deviceName))
			{
				info = cameraInfos[deviceName];
				cameraInfos.Remove(deviceName);
			}
	
			return info;
		}
	

	} // UVCController

}   // namespace Serenegiant.UVC
