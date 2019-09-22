using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UVCController : MonoBehaviour
{
	private const string FQCN_UNITY_PLAYER = "com.unity3d.player.UnityPlayer";
	private const string FQCN_PLUGIN = "com.serenegiant.uvcplugin.DeviceDetector";
	private const int DEFAULT_WIDTH = 1280;
	private const int DEFAULT_HEIGHT = 720;

	private string activeDeviceName;

	// Start is called before the first frame update
	void Start()
    {
		InitPlugin();
	}

	void OnDestroy()
	{
		if (!String.IsNullOrEmpty(activeDeviceName))
		{
			CloseCamera(activeDeviceName);
			activeDeviceName = null;
		}
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
	}

	public void OnEventDetach(string args)
	{
		Debug.Log("OnEventDetach(" + args + ")");
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
		using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
		{
			clazz.CallStatic("openDevice",
				GetCurrentActivity(), deviceName, DEFAULT_WIDTH, DEFAULT_HEIGHT);
		}
	}

	void CloseCamera(string deviceName)
	{
		using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
		{
			clazz.CallStatic("closeDevice",
				GetCurrentActivity(), deviceName);
		}
	}

	void SetupTexture(string deviceName, int width, int height)
	{
		var tex = new Texture2D(
					width, height,
					TextureFormat.ARGB32,
					false, /* mipmap */
					true /* linear */);
		GetComponent<Renderer>().material.mainTexture = tex;

		using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
		{
			clazz.CallStatic("setPreviewTexture",
				GetCurrentActivity(), deviceName, tex.GetNativeTexturePtr().ToInt32());
		}
	}
}
