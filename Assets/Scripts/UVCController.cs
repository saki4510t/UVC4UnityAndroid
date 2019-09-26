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

	private string activeDeviceName;
	private Int32 activeCameraId;
	private Texture2D activeTexture;

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

	public void OnEventAttach(string args)
	{
		Debug.Log("OnEventAttach(" + args + ")");
	}

	public void OnEventPermission(string args)
	{
		Debug.Log("OnEventPermission(" + args + ")");
		if (!String.IsNullOrEmpty(args))
		{   // argsはdeviceNameのはず
			OpenCamera(args);
		}
	}

	public void OnEventConnect(string args)
	{
		Debug.Log("OnEventConnect(" + args + ")");
		activeDeviceName = args;
		if (!String.IsNullOrEmpty(args))
		{   // argsはdeviceNameのはず
			SetupTexture(args, DEFAULT_WIDTH, DEFAULT_HEIGHT);
		}
	}

	public void OnEventDisconnect(string args)
	{
		Debug.Log("OnEventDisconnect(" + args + ")");
		CloseCamera(activeDeviceName);
	}

	public void OnEventDetach(string args)
	{
		Debug.Log("OnEventDetach(" + args + ")");
		CloseCamera(activeDeviceName);
	}

	public void OnStartPreview(string args)
	{
		Debug.Log("OnStartPreview(" + args + ")");
	}

	public void OnStopPreview(string args)
	{
		Debug.Log("OnStopPreview(" + args + ")");
	}

	public void OnReceiveStatus(string args)
	{
		Debug.Log("OnReceiveStatus(" + args + ")");
	}

	public void OnButtonEvent(string args)
	{
		Debug.Log("OnButtonEvent(" + args + ")");
	}

	//================================================================================

	public AndroidJavaObject GetCurrentActivity()
	{
		using (AndroidJavaClass playerClass = new AndroidJavaClass(FQCN_UNITY_PLAYER))
		{
			return playerClass.GetStatic<AndroidJavaObject>("currentActivity");
		}
	}

	void InitPlugin()
	{
		using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
		{
			clazz.CallStatic("initDeviceDetector",
				GetCurrentActivity(), gameObject.name);
		}
	}

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

	void CloseCamera(string deviceName)
	{
		if (!String.IsNullOrEmpty(deviceName))
		{
			activeCameraId = 0;
			activeDeviceName = null;
			activeTexture = null;
			using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
			{
				clazz.CallStatic("closeDevice",
					GetCurrentActivity(), deviceName);
			}

			StopCoroutine(OnRender());
		}
	}

	void SetupTexture(string deviceName, int width, int height)
	{
		StopCoroutine(OnRender());
	
		activeTexture = new Texture2D(
					width, height,
					TextureFormat.ARGB32,
					false, /* mipmap */
					true /* linear */);
		GetComponent<Renderer>().material.mainTexture = activeTexture;

		var nativeTexPtr = activeTexture.GetNativeTexturePtr();
		Debug.Log("SetupTexture:tex=" + nativeTexPtr);

		using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
		{
			clazz.CallStatic("setPreviewTexture",
				GetCurrentActivity(), deviceName,
				nativeTexPtr.ToInt32(), width, height);
		}

		StartCoroutine(OnRender());
	}

	[DllImport("uvc-plugin")]
	private static extern IntPtr GetRenderEventFunc();

	IEnumerator OnRender()
	{
		for ( ; ; )
		{
			yield return new WaitForEndOfFrame();
			GL.IssuePluginEvent(GetRenderEventFunc(), activeCameraId);
		}
	}
}
