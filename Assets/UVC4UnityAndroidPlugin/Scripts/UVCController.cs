#define ENABLE_LOG

using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_ANDROID && UNITY_2018_3_OR_NEWER
using UnityEngine.Android;
#endif

/*
 * AndroidでUVC機器からの映像をUnityのテクスチャとして取得するための
 * プラグイン(uvc-plugin-unity)へアクセスするためのヘルパークラスです。
 * 
 * プラグイン側では非UI Frgmentを使ってライフサイクルをコントロールしています。
 * Unityのメインアクティビティがフレームワーク版のActivityを使用しているため
 * プラグインでもフレームワーク版のFragmentを使用しています。
 * 
 * 現在の実装では接続されたUVC機器のうち１つだけしかハンドリングできません
 * ただしプラグイン自体は複数UVC機器に対応しているのでプラグインからの
 * イベント処理時に複数機器対応に変更すれば動作可能です。
 *
 * uvc-plugin-unity aar内で永続的パーミッションを保持するためのActivityを
 * AndroidManifest.xmlへ設定してあります。
 * もし永続的パーミッションを保持する必要がなければ、
 * 作成中のアプリのAndroidManifest.xmlへ
 * 次の行を追加してください。
 * 
 * <activity android:name="com.serenegiant.uvcplugin.UsbPermissionActivity" tools:node="remove"/>
 * 
 */

namespace Serenegiant.UVC {

	public class UVCController
	{
		private const string FQCN_PLUGIN = "com.serenegiant.uvcplugin.DeviceDetector";

		private MonoBehaviour parent;
		private GameObject target;
		private bool preferH264;
		private int defaultWidth;
		private int defaultHeight;
		/**
		 * プレビュー中のUVCカメラ識別子, レンダーイベント用
		 */
		private Int32 activeCameraId;

		private bool isPreviewing;

		private Texture previewTexture;


		private string attachedDeviceName;
		/**
		 * 接続中のUVC機器識別文字列
		 */
		public string AttachedDeviceName {
			get { return attachedDeviceName; }
		}

		private string activeDeviceName;
		/**
		 * 使用中のUVC機器識別文字列
		 */
		public string ActiveDeviceName {
			get { return activeDeviceName;  }
		}

		/**
		 * カメラをopenしているか
		 * 映像取得中かどうかはIsPreviewingを使うこと
		 */
		public bool IsOpen
		{
			get { return activeDeviceName != null; }
		}

		/**
		 * 映像取得中かどうか
		 */
		public bool IsPreviewing
		{
			get { return IsOpen && isPreviewing; }
		}

		//================================================================================
		/**
		 * コンストラクタ
		 * @param parent 親のスクリプト
		 * @param target  EventSystemによる関数呼び出しのターゲット
		 * @param width デフォルトの解像度(幅)
		 * @param height デフォルトの解像度(高さ)
		 * @param preferH264 H.264が使用可能な場合にMJPEGより優先して使用するかどうか
		 */
		public UVCController(MonoBehaviour parent, GameObject target, int width, int height, bool preferH264)
		{
			this.parent = parent;
			this.target = target;
			this.preferH264 = preferH264;
			defaultWidth = width;
			defaultHeight = height;
		}

		//================================================================================
		/**
		 * 映像取得用のTextureオブジェクトを取得する
		 * @return Textureオブジェクト, プレビュー中でなければnull
		 */
		public Texture GetTexture()
		{
			return previewTexture;
		}

		//================================================================================
		/**
		 * 指定したUVC機器をopenする
		 * @param deviceName UVC機器識別文字列
		 */
		public void Open(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"Open:{deviceName}");
#endif
			if (!String.IsNullOrEmpty(deviceName))
			{
				AndroidUtils.isPermissionRequesting = false;
				using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
				{
					activeCameraId = clazz.CallStatic<Int32>("openDevice",
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
			Console.WriteLine($"Close:{deviceName}");
#endif
			activeDeviceName = null;
			if (!String.IsNullOrEmpty(deviceName))
			{
				activeCameraId = 0;
				using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
				{
					clazz.CallStatic("closeDevice",
						AndroidUtils.GetCurrentActivity(), deviceName);
				}
			}
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine("Close:finished");
#endif
		}

		/**
		 * UVC機器/カメラからの映像受けとりを終了要求をする
		 * @param deviceName UVC機器識別文字列
		 */
		public void StopPreview(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"StopPreview:{deviceName}");
#endif
			parent.StopCoroutine(OnRender());
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
			Console.WriteLine($"OnEventAttach[{Time.frameCount}]:(" + args + ")");
#endif
			if (!String.IsNullOrEmpty(args))
			{   // argsはdeviceName
				attachedDeviceName = args;
				RequestUsbPermission(attachedDeviceName);
			}
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"OnEventAttach[{Time.frameCount}]:finished");
#endif
		}

		/**
		 * UVC機器が取り外された
		 * @param args UVC機器識別文字列
		 */
		public void OnEventDetach(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"OnEventDetach:({args})");
#endif
			HandleDetach(args);
		}

		/**
		 * UVC機器からのステータスイベントを受信した
		 * @param args UVC機器識別文字列＋ステータス
		 */
		public void OnReceiveStatus(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"OnReceiveStatus:({args})");
#endif
		}

		/**
		 * UVC機器からのボタンイベントを受信した
		 * @param args UVC機器識別文字列＋ボタンイベント
		 */
		public void OnButtonEvent(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"OnButtonEvent:({args})");
#endif
		}

		/**
		 * onResumeイベント
		 */
		public void OnResumeEvent()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"OnResumeEvent:attachedDeviceName={attachedDeviceName},activeDeviceName={activeDeviceName}");
#endif
			if (!AndroidUtils.isPermissionRequesting
				&& !String.IsNullOrEmpty(attachedDeviceName)
				&& String.IsNullOrEmpty(activeDeviceName))
			{
				// アタッチされた機器があるけどオープンされていないとき
				RequestUsbPermission(attachedDeviceName);
			}
		}

		/**
		 * onPauseイベント
		 */
		public void OnPauseEvent()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine("OnPauseEvent:");
#endif
			Close(activeDeviceName);
		}

		//--------------------------------------------------------------------------------
		public IEnumerator Initialize()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine("Initialize:");
#endif
			if (AndroidUtils.CheckAndroidVersion(28))
			{
				yield return AndroidUtils.GrantCameraPermission((string permission, bool granted) =>
				{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
					Console.WriteLine($"OnPermission:{permission}={granted}");
#endif
					if (granted)
					{
						InitPlugin();
					}
					else
					{
						if (AndroidUtils.ShouldShowRequestPermissionRationale(AndroidUtils.PERMISSION_CAMERA))
						{
							// パーミッションを取得できなかった
							// FIXME 説明用のダイアログ等を表示しないといけない
						}
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
			Console.WriteLine("InitPlugin:");
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
			Console.WriteLine($"RequestUsbPermission[{Time.frameCount}]:({deviceName})");
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
			Console.WriteLine($"RequestUsbPermission[{Time.frameCount}]:finsihed");
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
			Console.WriteLine($"StartPreview:{deviceName}({width}x{height})");
#endif
			if (!IsPreviewing)
			{
				activeDeviceName = deviceName;

				if (!String.IsNullOrEmpty(deviceName))
				{
					isPreviewing = true;
					previewTexture = new Texture2D(
							width, height,
							TextureFormat.ARGB32,
							false, /* mipmap */
							true /* linear */);
					var nativeTexPtr = previewTexture.GetNativeTexturePtr();
#if (!NDEBUG && DEBUG && ENABLE_LOG)
					Console.WriteLine($"RequestStartPreview:tex={nativeTexPtr}");
#endif
					using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
					{
						clazz.CallStatic("setPreviewTexture",
							AndroidUtils.GetCurrentActivity(), deviceName,
							nativeTexPtr.ToInt32(),
							-1,	// PreviewMode, -1:自動選択(Open時に指定したPreferH264フラグが有効になる)
							width, height);
					}

					parent.StartCoroutine(OnRender());
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
			Console.WriteLine($"RequestStopPreviewUVC:{deviceName}");
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
		 * UVC機器が取り外されたときの処理
		 * @param deviceName UVC機器識別文字列
		 */
		private void HandleDetach(string deviceName)
		{
			Close(activeDeviceName);
			attachedDeviceName = null;
		}

		/**
		 * 指定したUVC機器の情報(今はvidとpid)をJSON文字列として取得する
		 * @param deviceName UVC機器識別文字列
		 */
		public UVCInfo GetInfo(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"GetInfo:{deviceName}");
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
			Console.WriteLine($"GetSupportedVideoSize:{deviceName}");
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

		//--------------------------------------------------------------------------------
		/**
		 * プラグインでのレンダーイベント取得用native(c/c++)関数
		 */
		[DllImport("uvc-plugin")]
		private static extern IntPtr GetRenderEventFunc();

		/**
		 * レンダーイベント処理用
		 * コールーチンとして実行される
		 */
		IEnumerator OnRender()
		{
			var renderEventFunc = GetRenderEventFunc();
			for (; ; )
			{
				yield return new WaitForEndOfFrame();
				GL.IssuePluginEvent(renderEventFunc, activeCameraId);
			}
		}


	} // UVCController

}   // namespace Serenegiant.UVC
