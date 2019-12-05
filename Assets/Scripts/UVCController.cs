#define ENABLE_LOG

using System;
using System.Collections;
using System.Collections.Generic;
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

public class UVCController : MonoBehaviour
{
	private const string FQCN_UNITY_PLAYER = "com.unity3d.player.UnityPlayer";
	private const string FQCN_PLUGIN = "com.serenegiant.uvcplugin.DeviceDetector";
	private const string PERMISSION_CAMERA = "android.permission.CAMERA";
	private const int DEFAULT_WIDTH = 1280;
	private const int DEFAULT_HEIGHT = 720;

	/**
	 * 接続中のカメラの識別文字列
	 */
	private string attachedDeviceName;
	/**
	 * 使用中のUVCカメラ識別文字列
	 */
	private string activeDeviceName;
	/**
	 * プレビュー中のUVCカメラ識別子, レンダーイベント用
	 */
	private Int32 activeCameraId;
	/**
	 * 動的パーミッション要求中かどうか
	 */
	private bool isPermissionRequesting;

	// Start is called before the first frame update
	IEnumerator Start()
	{
		if (CheckAndroidVersion(28))
		{
			// Android 9 以降ではUVC機器へのアクセスにカメラパーミッションが必要
			if (!HasPermission(PERMISSION_CAMERA))
			{
				// Android 9以降でカメラパーミッションがないので要求する
				yield return RequestPermission(PERMISSION_CAMERA);
			}
			if (HasPermission(PERMISSION_CAMERA))
			{
				// カメラパーミッションを取得できた
				InitPlugin();
			}
			else
			if (ShouldShowRequestPermissionRationale(PERMISSION_CAMERA))
			{
				// カメラパーミッションを取得できなかった
				// FIXME 説明用のダイアログ等を表示しないといけない
			}
		}
		else
		{
			// Android 9 未満ではパーミッション要求処理は不要
			InitPlugin();
		}
	}

	void OnApplicationPause(bool pauseStatus)
	{
		if (pauseStatus)
		{
			OnPause();
		}
		else
		{
			OnResume();
		}
	}

	void OnDestroy()
	{
		CloseCamera(activeDeviceName);
	}

	// Update is called once per frame
	void Update()
	{

	}

	//--------------------------------------------------------------------------------
	private IEnumerator OnResume()
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"OnResume:attachedDeviceName={attachedDeviceName},activeDeviceName={activeDeviceName}");
#endif
		isPermissionRequesting = false;
		if (!String.IsNullOrEmpty(attachedDeviceName)
			&& String.IsNullOrEmpty(activeDeviceName))
		{
			// アタッチされた機器があるけどオープンされていないとき
			yield return RequestUsbPermission(attachedDeviceName);
		}
	}

	private void OnPause()
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine("OnPause:");
#endif
	}

	//================================================================================
	// Java側からのイベントコールバック

	/**
	 * UVC機器が接続された
	 */
	public IEnumerator OnEventAttach(string args)
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"OnEventAttach[{Time.frameCount}]:(" + args + ")");
#endif
		if (!String.IsNullOrEmpty(args))
		{   // argsはdeviceName
			attachedDeviceName = args;
			// RequestUsbPermissionをyield returnで直接呼び出すと
			// OnPasue/OnResumeをまたいでフォーカスが戻ってくるまで
			// 関数を抜けない
			// 一方MoveNextを使うとOnPauseの時点で関数を抜ける
			yield return RequestUsbPermission(attachedDeviceName);
			//var v = RequestUsbPermission(args);
			//while (v.MoveNext())
			//{
			//}
		}
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"OnEventAttach[{Time.frameCount}]:finished");
#endif
	}

	/**
	 * UVC機器へのアクセスのためのパーミッションを取得できた
	 */
	public void OnEventPermission(string args)
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"OnEventPermission:({args})");
#endif
		if (!String.IsNullOrEmpty(args))
		{   // argsはdeviceName
			isPermissionRequesting = false;
			OpenCamera(args);
		}
	}

	/**
	 * UVC機器をオープンした
	 */
	public void OnEventConnect(string args)
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"OnEventConnect:({args})");
#endif
	}

	/**
	 * UVC機器をクローズした
	 */
	public void OnEventDisconnect(string args)
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"OnEventDisconnect:({args})");
#endif
		CloseCamera(activeDeviceName);
		attachedDeviceName = null;
	}

	/**
	 * UVC機器が取り外された
	 */
	public void OnEventDetach(string args)
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"OnEventDetach:({args})");
#endif
		CloseCamera(activeDeviceName);
	}

	public void OnEventReady(string args)
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"OnEventReady:({args})");
#endif
		activeDeviceName = args;
		if (!String.IsNullOrEmpty(args))
		{   // argsはdeviceName
			Console.WriteLine($"OnEventReady:supported={GetSupportedVideoSize(args)}");
			StartPreview(args, DEFAULT_WIDTH, DEFAULT_HEIGHT);
		}
	}

	/**
	 * UVC機器からの映像取得を開始した
	 */
	public void OnStartPreview(string args)
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"OnStartPreview:({args})");
#endif
	}

	/**
	 * UVC機器からの映像取得を終了した
	 */
	public void OnStopPreview(string args)
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"OnStopPreview:({args})");
#endif
	}

	/**
	 * UVC機器からのステータスイベントを受信した
	 */
	public void OnReceiveStatus(string args)
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"OnReceiveStatus:({args})");
#endif
	}

	/**
	 * UVC機器からのボタンイベントを受信した
	 */
	public void OnButtonEvent(string args)
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"OnButtonEvent:({args})");
#endif
	}

	//================================================================================
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
				GetCurrentActivity(), gameObject.name);
		}
	}

	/**
	 * 指定したUSB機器をアクセスするパーミッションを持っているかどうかを取得
	 */
	private bool HasUsbPermission(string deviceName)
	{
		if (!String.IsNullOrEmpty(deviceName))
		{
			using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
			{
				return clazz.CallStatic<bool>("hasUsbPermission",
					GetCurrentActivity(), deviceName);
			}
		}
		else
		{
			return false;
		}
	}

	/**
	 * USB機器アクセスのパーミッション要求
	 */
	private IEnumerator RequestUsbPermission(string deviceName)
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"RequestUsbPermission[{Time.frameCount}]:({deviceName})");
#endif
		if (!String.IsNullOrEmpty(deviceName))
		{
			isPermissionRequesting = true;

			using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
			{
				clazz.CallStatic("requestPermission",
					GetCurrentActivity(), deviceName);
			}
			// アプリにフォーカスが戻るまで待機する
			yield return WaitPermissionWithTimeout(1.0f);
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
	 * 指定したUVC機器をオープン要求する
	 */
	private void OpenCamera(string deviceName)
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"OpenCamera:{deviceName}");
#endif
		if (!String.IsNullOrEmpty(deviceName))
		{
			using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
			{
				activeCameraId = clazz.CallStatic<Int32>("openDevice",
					GetCurrentActivity(), deviceName, DEFAULT_WIDTH, DEFAULT_HEIGHT);
			}
		}
		else
		{
			throw new ArgumentException("device name is empty/null");
		}
	}

	/**
	 * 指定したUVC機器をクローズ要求する
	 */
	private void CloseCamera(string deviceName)
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"CloseCamera:{deviceName}");
#endif
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
		else
		{
			throw new ArgumentException("device name is empty/null");
		}
	}

	/**
	 * UVC機器からの映像受け取り開始要求をする
	 */
	private void StartPreview(string deviceName, int width, int height)
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"StartPreview:{deviceName}");
#endif

		StopCoroutine(OnRender());

		if (!String.IsNullOrEmpty(deviceName))
		{
			var tex = new Texture2D(
					width, height,
					TextureFormat.ARGB32,
					false, /* mipmap */
					true /* linear */);
			GetComponent<Renderer>().material.mainTexture = tex;

			var nativeTexPtr = tex.GetNativeTexturePtr();
			Console.WriteLine("StartPreview:tex=" + nativeTexPtr);

			using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
			{
				clazz.CallStatic("setPreviewTexture",
					GetCurrentActivity(), deviceName,
					nativeTexPtr.ToInt32(), width, height);
			}

			StartCoroutine(OnRender());
		}
		else
		{
			throw new ArgumentException("device name is empty/null");
		}
	}

	/**
	 * 指定したUVC機器の対応解像度をjson文字列として取得する
	 */
	private string GetSupportedVideoSize(string deviceName)
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"GetSupportedVideoSize:{deviceName}");
#endif

		if (!String.IsNullOrEmpty(deviceName))
		{
			using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
			{
				return clazz.CallStatic<string>("getSupportedVideoSize",
					GetCurrentActivity(), deviceName);
			}
		}
		else
		{
			throw new ArgumentException("device name is empty/null");
		}
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
		for (; ; )
		{
			yield return new WaitForEndOfFrame();
			GL.IssuePluginEvent(renderEventFunc, activeCameraId);
		}
	}

	//================================================================================
	/**
	 * UnityPlayerActivityを取得
	 */
	private static AndroidJavaObject GetCurrentActivity()
	{
		using (AndroidJavaClass playerClass = new AndroidJavaClass(FQCN_UNITY_PLAYER))
		{
			return playerClass.GetStatic<AndroidJavaObject>("currentActivity");
		}
	}

	/**
	 * 指定したバージョン以降かどうかを確認
	 */
	private static bool CheckAndroidVersion(int apiLevel)
	{
		using (var VERSION = new AndroidJavaClass("android.os.Build$VERSION"))
		{
			return VERSION.GetStatic<int>("SDK_INT") >= apiLevel;
		}
	}

	/**
	 * パーミッションを持っているかどうかを調べる
	 */
	private static bool HasPermission(string permission)
	{
		if (CheckAndroidVersion(23))
		{
#if UNITY_ANDROID && UNITY_2018_3_OR_NEWER
			return Permission.HasUserAuthorizedPermission(permission);
#else
			using (var activity = GetCurrentActivity())
			{
				return activity.Call<int>("checkSelfPermission", permission) == 0;
			}
#endif
		}
		return true;
	}

	/**
	 * 指定したパーミッションの説明を表示する必要があるかどうかを取得
	 */
	private static bool ShouldShowRequestPermissionRationale(string permission)
	{
		if (CheckAndroidVersion(23))
		{
			using (var activity = GetCurrentActivity())
			{
				return activity.Call<bool>("shouldShowRequestPermissionRationale", permission);
			}
		}

		return false;
	}


	/**
	 * パーミッション要求
	 */
	private IEnumerator RequestPermission(string permission)
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"RequestPermission[{Time.frameCount}]:");
#endif
		if (CheckAndroidVersion(23))
		{
			isPermissionRequesting = true;
#if UNITY_ANDROID && UNITY_2018_3_OR_NEWER
			Permission.RequestUserPermission(permission);
#else
			using (var activity = GetCurrentActivity())
			{
				activity.Call("requestPermissions", new string[] { permission }, 0);
			}
#endif
			// アプリにフォーカスが戻るまで待機する
			yield return WaitPermissionWithTimeout(0.5f);
		}
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"RequestPermission[{Time.frameCount}]:finished");
#endif
		yield break;
	}

	/**
	 * isPermissionRequestingが落ちるか指定時間経過するまで待機する
	 * @param timeoutSecs 待機する最大時間[秒]
	 */
	private IEnumerator WaitPermissionWithTimeout(float timeoutSecs)
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"WaitPermissionWithTimeout[{Time.frameCount}]:");
#endif
		float timeElapsed = 0;
		while (isPermissionRequesting)
		{
			if (timeElapsed > timeoutSecs)
			{
				isPermissionRequesting = false;
				yield break;
			}
			timeElapsed += Time.deltaTime;

			yield return null;
		}
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"WaitPermissionWithTimeout[{Time.frameCount}]:finished");
#endif
		yield break;
	}
}
