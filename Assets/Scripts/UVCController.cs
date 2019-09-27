using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class UVCController : MonoBehaviour
{
	private const string FQCN_UNITY_PLAYER = "com.unity3d.player.UnityPlayer";
	private const string FQCN_PLUGIN = "com.serenegiant.uvcplugin.DeviceDetector";
	private const int DEFAULT_WIDTH = 1280;
	private const int DEFAULT_HEIGHT = 720;

	/**
	 * 使用中のUVCカメラ識別文字列
	 */
	private string activeDeviceName;
	/**
	 * プレビュー中のUVCカメラ識別子, レンダーイベント用
	 */
	private Int32 activeCameraId;

	// Start is called before the first frame update
	void Start()
    {
		InitPlugin();
	}

	void OnDestroy()
	{
		CloseCamera(activeDeviceName);
	}

	// Update is called once per frame
	void Update()
    {
	
	}

	//================================================================================
	// Java側からのイベントコールバック

	/**
	 * UVC機器が接続された
	 */
	public void OnEventAttach(string args)
	{
		Debug.Log("OnEventAttach(" + args + ")");
	}

	/**
	 * UVC機器へのアクセスのためのパーミッションを取得できた
	 */
	public void OnEventPermission(string args)
	{
		Debug.Log("OnEventPermission(" + args + ")");
		if (!String.IsNullOrEmpty(args))
		{   // argsはdeviceNameのはず
			OpenCamera(args);
		}
	}

	/**
	 * UVC機器をオープンした
	 */
	public void OnEventConnect(string args)
	{
		Debug.Log("OnEventConnect(" + args + ")");
		activeDeviceName = args;
		if (!String.IsNullOrEmpty(args))
		{   // argsはdeviceNameのはず
			StartPreview(args, DEFAULT_WIDTH, DEFAULT_HEIGHT);
		}
	}

	/**
	 * UVC機器をクローズした
	 */
	public void OnEventDisconnect(string args)
	{
		Debug.Log("OnEventDisconnect(" + args + ")");
		CloseCamera(activeDeviceName);
	}

	/**
	 * UVC機器が取り外された
	 */
	public void OnEventDetach(string args)
	{
		Debug.Log("OnEventDetach(" + args + ")");
		CloseCamera(activeDeviceName);
	}

	/**
	 * UVC機器からの映像取得を開始した
	 */
	public void OnStartPreview(string args)
	{
		Debug.Log("OnStartPreview(" + args + ")");
	}

	/**
	 * UVC機器からの映像取得を終了した
	 */
	public void OnStopPreview(string args)
	{
		Debug.Log("OnStopPreview(" + args + ")");
	}

	/**
	 * UVC機器からのステータスイベントを受信した
	 */
	public void OnReceiveStatus(string args)
	{
		Debug.Log("OnReceiveStatus(" + args + ")");
	}

	/**
	 * UVC機器からのボタンイベントを受信した
	 */
	public void OnButtonEvent(string args)
	{
		Debug.Log("OnButtonEvent(" + args + ")");
	}

	//================================================================================
	/**
	 * UnityPlayerActivityを取得
	 */
	public AndroidJavaObject GetCurrentActivity()
	{
		using (AndroidJavaClass playerClass = new AndroidJavaClass(FQCN_UNITY_PLAYER))
		{
			return playerClass.GetStatic<AndroidJavaObject>("currentActivity");
		}
	}

	/**
	 * プラグインを初期化
	 */
	void InitPlugin()
	{
		using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
		{
			clazz.CallStatic("initDeviceDetector",
				GetCurrentActivity(), gameObject.name);
		}
	}

	/**
	 * 指定したUVC機器をオープン要求する
	 */
	void OpenCamera(string deviceName)
	{
		if (!String.IsNullOrEmpty(deviceName))
		{
			using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
			{
				activeCameraId = clazz.CallStatic<Int32>("openDevice",
					GetCurrentActivity(), deviceName, DEFAULT_WIDTH, DEFAULT_HEIGHT);
			}
		}
	}

	/**
	 * 指定したUVC機器をクローズ要求する
	 */
	void CloseCamera(string deviceName)
	{
		if (!String.IsNullOrEmpty(deviceName))
		{
			activeCameraId = 0;
			activeDeviceName = null;
			using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
			{
				clazz.CallStatic("closeDevice",
					GetCurrentActivity(), deviceName);
			}

			StopCoroutine(OnRender());
		}
	}

	/**
	 * UVC機器からの映像受け取り開始要求をする
	 */
	void StartPreview(string deviceName, int width, int height)
	{
		StopCoroutine(OnRender());
	
		var tex = new Texture2D(
					width, height,
					TextureFormat.ARGB32,
					false, /* mipmap */
					true /* linear */);
		GetComponent<Renderer>().material.mainTexture = tex;

		var nativeTexPtr = tex.GetNativeTexturePtr();
		Debug.Log("StartPreview:tex=" + nativeTexPtr);

		using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
		{
			clazz.CallStatic("setPreviewTexture",
				GetCurrentActivity(), deviceName,
				nativeTexPtr.ToInt32(), width, height);
		}

		StartCoroutine(OnRender());
	}

	/**
	 * プラグインでのレンダーイベント取得用native(c/c++)関数
	 */
	[DllImport("uvc-plugin")]
	private static extern IntPtr GetRenderEventFunc();

	/**
	 * レンダーイベント処理用
	 */
	IEnumerator OnRender()
	{
		var renderEventFunc = GetRenderEventFunc();
		for ( ; ; )
		{
			yield return new WaitForEndOfFrame();
			GL.IssuePluginEvent(renderEventFunc, activeCameraId);
		}
	}
}
