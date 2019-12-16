using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_ANDROID && UNITY_2018_3_OR_NEWER
using UnityEngine.Android;
#endif

namespace Serenegiant
{

	public class AndroidUtils
	{
		public const string FQCN_UNITY_PLAYER = "com.unity3d.player.UnityPlayer";
		public const string PERMISSION_CAMERA = "android.permission.CAMERA";

		public static bool isPermissionRequesting { set; get; }

		/**
		 * UnityPlayerActivityを取得
		 */
		public static AndroidJavaObject GetCurrentActivity()
		{
			using (AndroidJavaClass playerClass = new AndroidJavaClass(FQCN_UNITY_PLAYER))
			{
				return playerClass.GetStatic<AndroidJavaObject>("currentActivity");
			}
		}

		/**
		 * 指定したバージョン以降かどうかを確認
		 * @param apiLevel
		 * @return true: 指定したバージョン以降で実行されている, false: 指定したバージョンよりも古い端末で実行されている
		 */
		public static bool CheckAndroidVersion(int apiLevel)
		{
			using (var VERSION = new AndroidJavaClass("android.os.Build$VERSION"))
			{
				return VERSION.GetStatic<int>("SDK_INT") >= apiLevel;
			}
		}

		/**
		 * パーミッションを持っているかどうかを調べる
		 * @param permission
		 * @param true: パーミッションを保持している, false: パーミッションを保持していない
		 */
		public static bool HasPermission(string permission)
		{
			if (CheckAndroidVersion(23))
			{
#if UNITY_2018_3_OR_NEWER
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
		 * @param permission
		 * @param 指定したパーミッションの説明を表示する必要がある
		 */
		public static bool ShouldShowRequestPermissionRationale(string permission)
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

		/***
		 * GrantPermissionでパーミッションを要求したときのコールバック用delegateer
		 */
		public delegate void OnPermission(string permission, bool granted);

		/**
		 * パーミッション要求
		 * @param permission
		 * @param callback
		 */
		public static IEnumerator GrantPermission(string permission, OnPermission callback)
		{
			if (!HasPermission(permission))
			{
				yield return RequestPermission(permission);
			}
			callback(permission, HasPermission(permission));

			yield break;
		}

		/**
		 * カメラパーミッションを要求
		 * @param callback
		 */
		public static IEnumerator GrantCameraPermission(OnPermission callback)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine("GrantCameraPermission:");
#endif
			if (AndroidUtils.CheckAndroidVersion(23))
			{
				// Android9以降ではUVC機器アクセスにもCAMERAパーミッションが必要
				yield return GrantPermission(PERMISSION_CAMERA, callback);
			}
			else
			{
				// Android 9 未満ではパーミッション要求処理は不要
				callback(PERMISSION_CAMERA, true);
			}

			yield break;
		}

		/**
		 * パーミッション要求
		 * @param permission
		 */
		public static IEnumerator RequestPermission(string permission)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"RequestPermission[{Time.frameCount}]:");
#endif
			if (CheckAndroidVersion(23))
			{
				isPermissionRequesting = true;
#if UNITY_2018_3_OR_NEWER
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
		public static IEnumerator WaitPermissionWithTimeout(float timeoutSecs)
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

	} // class AndroidUtils

} // namespace Serenegiant