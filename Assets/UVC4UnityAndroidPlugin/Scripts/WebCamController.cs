using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Serenegiant
{

	public class WebCamController
	{

		private string activeDeviceName;
		private WebCamTexture webCameraTexure;

		public void Initialize(string deviceKeyword)
		{
			// FIXME 未実装
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine("InitializeWebCam:");
#endif
			// 使用可能なWebカメラを探す
			var devices = WebCamTexture.devices;
			if (devices != null)
			{
				foreach (var cam in devices)
				{
					if (MatchWebCame(cam, deviceKeyword))
					{   // 最初に見つかったものを使う
						activeDeviceName = cam.name;
						break;
					}
				}
			}
			if (activeDeviceName != null)
			{	// 見つかったときはopenする
				Open(activeDeviceName);
			}
		}

		public void Open(string deviceName)
		{
			WebCamDevice found = new WebCamDevice();
			if (FindWebCam(deviceName, ref found))
			{
				// FIXME 今は解像度は適当
				StartPreview(deviceName, 1280, 720);
			}
		}

		public void Close(string deviceName)
		{
			StopPreview(deviceName);
			activeDeviceName = null;
		}

		/**
		 * カメラからの映像取得開始
		 * @param deviceName
		 * @param width
		 * @param height
		 */
		public void StartPreview(string deviceName, int width, int height)
		{
			WebCamDevice found = new WebCamDevice();
			if (FindWebCam(deviceName, ref found))
			{
				RequestStartPreview(found, width, height);
			}
		}

		public void StopPreview(string deviceName)
		{
			if (webCameraTexure != null)
			{
//				HandleOnStopPreview(deviceName);
				webCameraTexure.Stop();
				webCameraTexure = null;
			}
		}

		/**
		 * 映像受け取り用のTextureを返す
		 * @return プレビュー中でなければnull
		 */
		public Texture GetTexture()
		{
			return webCameraTexure;
		}

		//--------------------------------------------------------------------------------

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

		/**
		 * カメラからの映像取得開始
		 * @param device
		 * @param width
		 * @param height
		 */
		private void RequestStartPreview(WebCamDevice device, int width, int height)
		{
			if (webCameraTexure == null)
			{
				webCameraTexure = new WebCamTexture(device.name, width, height);
//				HandleOnStartPreview(webCameraTexure);
			}
			if ((webCameraTexure != null) && !webCameraTexure.isPlaying)
			{
				webCameraTexure.Play();
			}
		}
	}

} // namespace Serenegiant