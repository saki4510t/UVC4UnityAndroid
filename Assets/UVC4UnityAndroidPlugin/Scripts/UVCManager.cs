#define ENABLE_LOG

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_ANDROID && UNITY_2018_3_OR_NEWER
using UnityEngine.Android;
#endif

using static Serenegiant.UVC.UVCEventHandler;

namespace Serenegiant.UVC {

	[RequireComponent(typeof(AndroidUtils))]
	public class UVCManager : MonoBehaviour
	{
		private const string TAG = "UVCManager#";
		private const string FQCN_PLUGIN = "com.serenegiant.uvcplugin.DeviceDetector";

		/**
		 * IUVCSelectorがセットされていないとき
		 * またはIUVCSelectorが解像度選択時にnullを
		 * 返したときのデフォルトの解像度(幅)
		 */
		public int DefaultWidth = 1280;
		/**
		 * IUVCSelectorがセットされていないとき
		 * またはIUVCSelectorが解像度選択時にnullを
		 * 返したときのデフォルトの解像度(高さ)
		 */
		public int DefaultHeight = 720;
		/**
		 * UVC機器とのネゴシエーション時に
		 * H.264を優先してネゴシエーションするかどうか
		 * Android実機のみ有効
		 * true:	H.264 > MJPEG > YUV
		 * false:	MJPEG > H.264 > YUV
		 */
		public bool PreferH264 = true;
		/**
		 * UVC機器が接続されたときのイベント
		 * @param UVCManager
		 * @param UVCDevice 接続されたUVC機器情報
		 * @return bool 接続されたUVC機器を使用するかどうか
		 */
		public IOnUVCAttachHandler OnAttachEventHandler;
		/**
		 * UVC機器が取り外されたときのイベント
		 * @param UVCManager
		 * @param UVCDevice 取り外されるUVC機器情報
		 */
		public IOnUVCDetachHandler OnDetachEventHandler;
		/**
		 * 解像度の選択処理
		 */
		public IOnUVCSelectSizeHandler OnUVCSelectSizeHandler;
		/**
		 * 映像描画処理
		 */
		[SerializeField, ComponentRestriction(typeof(IUVCDrawer))]
		public Component[] UVCDrawers;

		/**
		 * プラグインでのレンダーイベント取得用native(c/c++)関数
		 */
		[DllImport("uvc-plugin")]
		private static extern IntPtr GetRenderEventFunc();

		public class CameraInfo
		{
			internal readonly UVCDevice device;
			/**
			 * プレビュー中のUVCカメラ識別子, レンダーイベント用
			 */
			internal Int32 activeCameraId;
			internal Texture previewTexture;

			internal CameraInfo(UVCDevice device)
			{
				this.device = device;
			}

			/**
			 * 機器名を取得
			 */
			public string DeviceName
			{
				get { return device.deviceName;  }
			}

			/**
			 * ベンダーIDを取得
			 */
			public int Vid
			{
				get { return device.vid;  }
			}

			/**
			 * プロダクトIDを取得
			 */
			public int Pid
			{
				get { return device.pid;  }
			}

			/**
			 * カメラをopenしているか
			 * 映像取得中かどうかはIsPreviewingを使うこと
			 */
			public bool IsOpen
			{
				get { return (activeCameraId != 0); }
			}

			/**
			 * 映像取得中かどうか
			 */
			public bool IsPreviewing
			{
				get { return IsOpen && (previewTexture != null); }
			}

			/**
			 * 現在の解像度(幅)
			 * プレビュー中でなければ0
			 */
			public int CurrentWidth
			{
				get { return currentWidth;  }
			}

			/**
			 * 現在の解像度(高さ)
			 * プレビュー中でなければ0
			 */
			public int CurrentHeight
			{
				get { return currentHeight; }
			}

			private int currentWidth;
			private int currentHeight;
			/**
			 * 現在の解像度を変更
			 * @param width
			 * @param height
			 */
			internal void SetSize(int width, int height)
			{
				currentWidth = width;
				currentHeight = height;
			}
	
			/**
			 * レンダーイベント処理用
			 * コールーチンとして実行される
			 */
			internal IEnumerator OnRender()
			{
				var renderEventFunc = GetRenderEventFunc();
				for (; activeCameraId != 0; )
				{
					yield return new WaitForEndOfFrame();
					GL.IssuePluginEvent(renderEventFunc, activeCameraId);
				}
				yield break;
			}
		}

		/**
		 * ハンドリングしているカメラ情報を保持
		 * string(deviceName) - CameraInfo ペアを保持する
		 */
		private Dictionary<string, CameraInfo> cameraInfos = new Dictionary<string, CameraInfo>();

		//================================================================================
		// UnityEngineからの呼び出し

		IEnumerator Start()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}Start:");
#endif
			yield return Initialize();
		}

#if (!NDEBUG && DEBUG && ENABLE_LOG)
		void OnApplicationFocus()
		{
			Console.WriteLine($"{TAG}OnApplicationFocus:");
		}
#endif

#if (!NDEBUG && DEBUG && ENABLE_LOG)
		void OnApplicationPause(bool pauseStatus)
		{
			Console.WriteLine($"{TAG}OnApplicationPause:{pauseStatus}");
		}
#endif

#if (!NDEBUG && DEBUG && ENABLE_LOG)
		void OnApplicationQuits()
		{
			Console.WriteLine($"{TAG}OnApplicationQuits:");
		}
#endif

		void OnDestroy()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnDestroy:");
#endif
			CloseAll();
		}

		//================================================================================
		/**
		 * 接続中のUVC機器一覧を取得
		 * @return 接続中のUVC機器一覧List
		 */
		public List<CameraInfo> GetAttachedDevices()
		{
			var result = new List<CameraInfo>(cameraInfos.Count);

			foreach (var info in cameraInfos.Values)
			{
				result.Add(info);
			}
	
			return result;
		}

		/**
		 * 対応解像度を取得
		 * @param camera 対応解像度を取得するUVC機器を指定
		 * @return 対応解像度 既にカメラが取り外されている/closeしているのであればnull
		 */
		public SupportedFormats GetSupportedVideoSize(CameraInfo camera)
		{
			var info = (camera != null) ? Get(camera.DeviceName) : null;
			if ((info != null) && info.IsOpen)
			{
				return GetSupportedVideoSize(info.DeviceName);
			} else
			{
				return null;
			}
		}

		/**
		 * 解像度を変更
		 * @param 解像度を変更するUVC機器を指定
		 * @param 変更する解像度を指定, nullならデフォルトに戻す
		 * @param 解像度が変更されたかどうか
		 */
		public bool SetVideoSize(CameraInfo camera, SupportedFormats.Size size)
		{
			var info = (camera != null) ? Get(camera.DeviceName) : null;
			var width = size != null ? size.Width : DefaultWidth;
			var height = size != null ? size.Height : DefaultHeight;
			if ((info != null) && info.IsPreviewing)
			{
				if ((width != info.CurrentWidth) || (height != info.CurrentHeight))
				{	// 解像度が変更になるとき
					StopPreview(info.DeviceName);
					StartPreview(info.DeviceName, width, height);
					return true;
				}
			}
			return false;
		}
	
		//================================================================================
		// Android固有の処理
		// Java側からのイベントコールバック

		/**
		 * UVC機器が接続された
		 * @param args UVC機器識別文字列
		 */
		void OnEventAttach(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnEventAttach[{Time.frameCount}]:(" + args + ")");
#endif
			if (!String.IsNullOrEmpty(args))
			{   // argsはdeviceName
				var info = CreateIfNotExist(args);
				if ((OnAttachEventHandler == null) 
					|| OnAttachEventHandler.OnUVCAttachEvent(this, info.device))
				{
					RequestUsbPermission(args);
				} else
				{
					Remove(args);
				}
			}
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnEventAttach[{Time.frameCount}]:finished");
#endif
		}

		/**
		 * UVC機器が取り外された
		 * @param args UVC機器識別文字列
		 */
		void OnEventDetach(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnEventDetach:({args})");
#endif
			var info = Get(args);
			if ((info != null) && (OnDetachEventHandler != null))
			{
				OnDetachEventHandler.OnUVCDetachEvent(this, info.device);
				Close(args);
				Remove(args);
			}
		}

		/**
		 * UVC機器へのアクセスのためのパーミッションを取得できた
		 * @param args UVC機器の識別文字列
		 */
		void OnEventPermission(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnEventPermission:({args})");
#endif
			if (!String.IsNullOrEmpty(args))
			{   // argsはdeviceName
				Open(args);
			}
		}

		/**
		 * UVC機器をオープンした
		 * @param args UVC機器の識別文字列
		 */
		void OnEventConnect(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnEventConnect:({args})");
#endif
		}

		/**
		 * UVC機器をクローズした
		 * @param args UVC機器の識別文字列
		 */
		void OnEventDisconnect(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnEventDisconnect:({args})");
#endif
			// このイベントはUnity側からclose要求を送ったとき以外でも発生するので
			// 念のためにCloseを呼んでおく
			Close(args);
		}

		/**
 * 映像を受け取れるようになった
 * @param args UVC機器の識別文字列
 */
		void OnEventReady(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnEventReady:({args})");
#endif
			StartPreview(args);
		}

		/**
		 * UVC機器からの映像取得を開始した
		 * @param args UVC機器の識別文字列
		 */
		void OnStartPreview(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnStartPreview:({args})");
#endif
			var info = Get(args);
			if ((info != null) && info.IsPreviewing && (UVCDrawers != null))
			{
				foreach (var drawer in UVCDrawers)
				{
					if ((drawer is IUVCDrawer) && (drawer as IUVCDrawer).CanDraw(this, info.device))
					{
						(drawer as IUVCDrawer).OnUVCStartEvent(this, info.device, info.previewTexture);
					}
				}
			}
		}

		/**
		 * UVC機器からの映像取得を終了した
		 * @param args UVC機器の識別文字列
		 */
		void OnStopPreview(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnStopPreview:({args})");
#endif
			var info = Get(args);
			if ((info != null) && info.IsPreviewing && (UVCDrawers != null))
			{
				info.SetSize(0, 0);
				foreach (var drawer in UVCDrawers)
				{
					if ((drawer is IUVCDrawer) && (drawer as IUVCDrawer).CanDraw(this, info.device))
					{
						(drawer as IUVCDrawer).OnUVCStopEvent(this, info.device);
					}
				}
			}
		}

		/**
		 * UVC機器からのステータスイベントを受信した
		 * @param args UVC機器識別文字列＋ステータス
		 */
		void OnReceiveStatus(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnReceiveStatus:({args})");
#endif
			// FIXME 未実装
		}

		/**
		 * UVC機器からのボタンイベントを受信した
		 * @param args UVC機器識別文字列＋ボタンイベント
		 */
		void OnButtonEvent(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnButtonEvent:({args})");
#endif
			// FIXME 未実装
		}

		/**
		 * onResumeイベント
		 */
		IEnumerator OnResumeEvent()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnResumeEvent:" +
				$"isPermissionRequesting={AndroidUtils.isPermissionRequesting}");
#endif
			if (!AndroidUtils.isPermissionRequesting
				&& AndroidUtils.CheckAndroidVersion(28)
				&& !AndroidUtils.HasPermission(AndroidUtils.PERMISSION_CAMERA))
			{
				yield return Initialize();
			}

			KeyValuePair<string, CameraInfo>? found = null;
			foreach (var elm in cameraInfos)
			{
				if (elm.Value.activeCameraId == 0)
				{   // アタッチされたけどオープンされていない機器があるとき
					found = elm;
					break;
				}
			}
			if (found != null)
			{	// アタッチされたけどオープンされていない機器があるとき
				var deviceName = found?.Key;
				if (!AndroidUtils.isPermissionRequesting)
				{ // パーミッション要求中ではないとき
					RequestUsbPermission(deviceName);
				}
				else if (HasUsbPermission(deviceName))
				{ // すでにパーミッションがあるとき
					AndroidUtils.isPermissionRequesting = false;
					OnEventPermission(deviceName);
				}
			}

			yield break;
		}

		/**
		 * onPauseイベント
		 */
		void OnPauseEvent()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnPauseEvent:");
#endif
			CloseAll();
		}

		//--------------------------------------------------------------------------------
		private IEnumerator Initialize()
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
			} else
			{
				InitPlugin();
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
			if (OnAttachEventHandler == null)
			{
				OnAttachEventHandler = GetComponent(typeof(IOnUVCAttachHandler)) as IOnUVCAttachHandler;
			}
			if (OnDetachEventHandler == null)
			{
				OnDetachEventHandler = GetComponent(typeof(IOnUVCDetachHandler)) as IOnUVCDetachHandler;
			}
			if ((UVCDrawers == null) || (UVCDrawers.Length == 0))
			{
				UVCDrawers = new Component[1];
			}
			var hasDrawer = false;
			foreach (var drawer in UVCDrawers)
			{
				if (drawer is IUVCDrawer)
				{
					hasDrawer = true;
					break;
				}
			}
			if (!hasDrawer)
			{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine($"{TAG}InitPlugin:has no IUVCDrawer, try to get from gameObject");
#endif
				UVCDrawers[0] = GetComponent(typeof(IUVCDrawer));
			}
			if (OnUVCSelectSizeHandler == null)
			{
				OnUVCSelectSizeHandler = GetComponent(typeof(IOnUVCSelectSizeHandler)) as IOnUVCSelectSizeHandler;
			}
			using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
			{
				clazz.CallStatic("initDeviceDetector",
					AndroidUtils.GetCurrentActivity(), gameObject.name);
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
					return clazz.CallStatic<bool>("hasPermission",
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
		 * 指定したUVC機器をopenする
		 * @param deviceName UVC機器識別文字列
		 */
		private void Open(string deviceName)
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
						DefaultWidth, DefaultHeight, PreferH264);
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
		private void Close(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}Close:{deviceName}");
#endif
			var info = Get(deviceName);
			if ((info != null) && (info.activeCameraId != 0))
			{
				info.SetSize(0, 0);
				info.activeCameraId = 0;
				info.previewTexture = null;
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
		 * OpenしているすべてのUVC機器をCloseする
		 */
		private void CloseAll()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}CloseAll:");
#endif
			List<string> keys = new List<string>(cameraInfos.Keys);
			foreach (var deviceName in keys)
			{
				Close(deviceName);
			}
		}

		/**
		 * UVC機器からの映像受け取り開始要求をする
		 * @param deviceName UVC機器識別文字列
		 */
		private void StartPreview(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}StartPreview:{deviceName}");
#endif
			var info = Get(deviceName);
			if ((info != null) && (info.activeCameraId != 0))
			{
				int width = DefaultWidth;
				int height = DefaultHeight;

				var supportedVideoSize = GetSupportedVideoSize(deviceName);
				if (supportedVideoSize == null)
				{
					throw new ArgumentException("fauled to get supported video size");
				}

				// 解像度の選択処理
				if (OnUVCSelectSizeHandler != null)
				{
					var size = OnUVCSelectSizeHandler.OnUVCSelectSize(this, info.device, supportedVideoSize);
#if (!NDEBUG && DEBUG && ENABLE_LOG)
					Console.WriteLine($"{TAG}StartPreview:selected={size}");
#endif
					if (size != null)
					{
						width = size.Width;
						height = size.Height;
					}
				}

				StartPreview(deviceName, width, height);
			}
		}

		/**
		 * UVC機器からの映像受け取り開始要求をする
		 * 通常はStartPreview(string deviceName)経由で呼び出す
		 * @param deviceName UVC機器識別文字列
		 * @param width
		 * @param height
		 */
		private void StartPreview(string deviceName, int width, int height)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}StartPreview:{deviceName}({width}x{height})");
#endif
			var info = Get(deviceName);
			if (info != null)
			{   // 接続されているとき
				var supportedVideoSize = GetSupportedVideoSize(deviceName);
				if (supportedVideoSize == null)
				{
					throw new ArgumentException("fauled to get supported video size");
				}
				// 対応解像度のチェック
				if (supportedVideoSize.Find(width, height/*,minFps=0.1f, maxFps=121.0f*/) == null)
				{   // 指定した解像度に対応していない
#if (!NDEBUG && DEBUG && ENABLE_LOG)
					Console.WriteLine($"{TAG}StartPreview:{width}x{height} is NOT supported.");
					Console.WriteLine($"{TAG}Info={GetDevice(deviceName)}");
					Console.WriteLine($"{TAG}supportedVideoSize={supportedVideoSize}");
#endif
					throw new ArgumentOutOfRangeException($"{width}x{height} is NOT supported.");
				}

				if (info.IsOpen && !info.IsPreviewing)
				{   // openされているけど映像取得中ではないとき
					info.SetSize(width, height);
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
							-1, // PreviewMode, -1:自動選択(Open時に指定したPreferH264フラグが有効になる)
							width, height);
					}

					StartCoroutine(info.OnRender());
				}
			}
			else
			{
				throw new ArgumentException("device name is empty/null");
			}
		}

		/**
 * UVC機器/カメラからの映像受けとりを終了要求をする
 * @param deviceName UVC機器識別文字列
 */
		private void StopPreview(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}StopPreview:{deviceName}");
#endif
			var info = Get(deviceName);
			if (info != null)
			{
				info.SetSize(0, 0);
				StopCoroutine(info.OnRender());
				RequestStopPreview(deviceName);
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
		 * 指定したUVC機器の情報(今はvidとpid)をUVCDeviceとして取得する
		 * @param deviceName UVC機器識別文字列
		 */
		private UVCDevice GetDevice(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}GetDevice:{deviceName}");
#endif

			if (!String.IsNullOrEmpty(deviceName))
			{
				using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
				{
					return UVCDevice.Parse(deviceName,
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
		private SupportedFormats GetSupportedVideoSize(string deviceName)
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
				cameraInfos[deviceName] = new CameraInfo(GetDevice(deviceName));
			}
			return cameraInfos[deviceName];
		}

		/*Nullable*/
		private CameraInfo Get(string deviceName)
		{
			return !String.IsNullOrEmpty(deviceName) && cameraInfos.ContainsKey(deviceName) ? cameraInfos[deviceName] : null;
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
	

	} // UVCManager

}   // namespace Serenegiant.UVC
