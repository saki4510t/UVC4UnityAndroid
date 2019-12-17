#define ENABLE_LOG

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

using Serenegiant.UVC;

namespace Serenegiant
{

	/**
	 * PCの場合はUSB接続のカメラハンドリング用ヘルパークラス
	 * Androidの場合は内蔵カメラのハンドリング用ヘルパークラス
	 */
	public class WebCamController
	{
		private MonoBehaviour parent;
		private GameObject target;
		private int defaultWidth;
		private int defaultHeight;
		private string deviceKeyword;
		private WebCamTexture webCameraTexure;

		private string attachedDeviceName;
		/**
		 * 接続中のカメラ識別文字列
		 */
		public string AttachedDeviceName
		{
			get { return attachedDeviceName; }
		}

		private string activeDeviceName;
		/**
		 * 使用中のカメラ識別文字列
		 */
		public string ActiveDeviceName
		{
			get { return activeDeviceName; }
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
			get { return IsOpen && (webCameraTexure != null); }
		}

		//================================================================================
		/**
		 * コンストラクタ
		 * @param parent 親のスクリプト
		 * @param target  EventSystemによる関数呼び出しのターゲット
		 * @param width デフォルトの解像度(幅)
		 * @param height デフォルトの解像度(高さ)
		 * @param deviceKeyword カメラ選択用のキーワード, nullのときは最初に見つかったカメラを使う
		 */
		public WebCamController(MonoBehaviour parent, GameObject target,
			int width, int height,
			string deviceKeyword)
		{
			this.parent = parent;
			this.target = target;
			this.defaultWidth = width;
			this.defaultHeight = height;
			this.deviceKeyword = deviceKeyword;
		}

		//--------------------------------------------------------------------------------
		/**
 * onResumeイベント
 */
		public IEnumerator OnResumeEvent()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"OnResumeEvent:attachedDeviceName={attachedDeviceName}" +
				$",activeDeviceName={activeDeviceName}" +
				$",isPermissionRequesting={AndroidUtils.isPermissionRequesting}");
#endif
			if (!AndroidUtils.isPermissionRequesting)
			{
				yield return Initialize();
			}
			yield break;
		}

		/**
		 * onPauseイベント
		 */
		public void OnPauseEvent()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine("OnPauseEvent:");
#endif
//			Close(activeDeviceName);	// CameraDrawerからCloseを呼ぶので不要
		}

		//--------------------------------------------------------------------------------
		/**
		 * 初期化実行
		 */
		public IEnumerator Initialize()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"Initialize:({deviceKeyword})");
#endif
#if UNITY_ANDROID
			yield return AndroidUtils.GrantCameraPermission((string permission, AndroidUtils.PermissionGrantResult result) =>
			{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine($"OnPermission:{permission}={result}");
#endif
				switch (result)
				{
					case AndroidUtils.PermissionGrantResult.PERMISSION_GRANT:
						FindCamera(deviceKeyword);
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
#else
			FindCamera(deviceKeyword);
#endif
			yield break;
		}

		/**
		 * 指定したカメラをOpenする
		 * @param deviceName カメラの識別文字列
		 */
		public void Open(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"Open:({deviceName})");
#endif
			var found = new WebCamDevice();
			if (FindWebCam(deviceName, ref found))
			{
				activeDeviceName = deviceName;
				// カメラとの接続通知
				ExecuteEvents.Execute<IUVCEventHandler>(
					target: target,
					eventData: null,
					functor: (recieveTarget, eventData) => recieveTarget.OnEventConnect(deviceName));
				// カメラからの映像取得の準備完了通知
				ExecuteEvents.Execute<IUVCEventHandler>(
					target: target,
					eventData: null,
					functor: (recieveTarget, eventData) => recieveTarget.OnEventReady(deviceName));
			}
		}

		/**
		 * 指定したカメラを閉じる
		 * @param deviceName カメラの識別文字列
		 */
		public void Close(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"Close:({deviceName})");
#endif
			StopPreview(deviceName);
			activeDeviceName = null;
			// カメラ切断通知
			ExecuteEvents.Execute<IUVCEventHandler>(
				target: target,
				eventData: null,
				functor: (recieveTarget, eventData) => recieveTarget.OnEventDisconnect(deviceName));
		}

		/**
		 * カメラからの映像取得開始
		 * @param deviceName カメラの識別文字列
		 * @param width
		 * @param height
		 */
		public void StartPreview(string deviceName, int width, int height)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"StartPreview:{deviceName}({width}x{height})");
#endif
			WebCamDevice found = new WebCamDevice();
			if (FindWebCam(deviceName, ref found))
			{
				if (webCameraTexure == null)
				{
					webCameraTexure = new WebCamTexture(found.name, width, height);
				}
				if ((webCameraTexure != null) && !webCameraTexure.isPlaying)
				{
					webCameraTexure.Play();
				}
				// 映像取得開始通知
				ExecuteEvents.Execute<IUVCEventHandler>(
					target: target,
					eventData: null,
					functor: (recieveTarget, eventData) => recieveTarget.OnStartPreview(activeDeviceName));
			}
		}

		/**
		 * カメラからの映像取得を終了する
		 * @param deviceName カメラの識別文字列
		 */
		public void StopPreview(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"StopPreview:({deviceName})");
#endif
			if (webCameraTexure != null)
			{
				webCameraTexure.Stop();
				webCameraTexure = null;
				// 映像取得終了通知
				ExecuteEvents.Execute<IUVCEventHandler>(
					target: target,
					eventData: null,
					functor: (recieveTarget, eventData) => recieveTarget.OnStopPreview(activeDeviceName));
			}
		}

		/**
		 * 映像取得用のTextureオブジェクトを取得する
		 * @return Textureオブジェクト, プレビュー中でなければnull
		 */
		public Texture GetTexture()
		{
			return webCameraTexure;
		}

		/**
		 * カメラ情報を取得
		 * @param deviceName カメラの識別文字列
		 */
		public UVCInfo GetInfo(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"GetInfo:({deviceName})");
#endif
			return new UVCInfo();
		}

		/**
		 * 指定したカメラの対応解像度を取得する
		 * @param deviceName カメラの識別文字列
		 */
		public SupportedFormats GetSupportedVideoSize(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"GetSupportedVideoSize:({deviceName})");
#endif
			WebCamDevice found = new WebCamDevice();
			if (FindWebCam(deviceName, ref found))
			{
				return SupportedFormats.Parse(found);
			}
			return null;
		}

		//--------------------------------------------------------------------------------
		/**
		 * 使用可能なWebカメラを探す
		 */
		private void FindCamera(string deviceKeyword)
		{
			var devices = WebCamTexture.devices;
			if (devices != null)
			{
				foreach (var cam in devices)
				{
					if (MatchWebCame(cam, deviceKeyword))
					{   // 最初に見つかったものを使う
						attachedDeviceName = cam.name;
						break;
					}
				}
			}
			if (attachedDeviceName != null)
			{   // 使用可能なカメラが見つかったとき
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine($"Initialize:found={activeDeviceName}");
#endif
				// パーミッションを取得通知
				ExecuteEvents.Execute<IUVCEventHandler>(
					target: target,
					eventData: null,
					functor: (recieveTarget, eventData) => recieveTarget.OnEventPermission(attachedDeviceName));
			}
		}


		/**
		 * WebCamDevice選択関数
		 * WebCameraDeviceKeywordがnull
		 * またはWebCameraDeviceKeywordと一致するか
		 * またはWebCameraDeviceKeywordをnameに含む場合trueを返す
		 * @param webCam
		 */
		private bool MatchWebCame(WebCamDevice webCam/*NonNull*/, string deviceKeyword)
		{
			return (String.IsNullOrEmpty(deviceKeyword)
				|| (webCam.name == deviceKeyword)
				|| webCam.name.Contains(deviceKeyword));
		}

		/**
		 * 指定したデバイス名を持つWebCamDeviceを探す
		 * @param deviceName
		 * @param found 見つかったWebCamDevice
		 * @return true: 見つかった, false: 見つからなかった
		 */
		private bool FindWebCam(string deviceName, ref WebCamDevice found)
		{
			var devices = WebCamTexture.devices;
			if (devices != null)
			{
				foreach (var device in devices)
				{
					if (device.name == deviceName)
					{
						found = device;
						return true;
					}
				}
			}
			return false;
		}

	} // WebCamController

} // namespace Serenegiant