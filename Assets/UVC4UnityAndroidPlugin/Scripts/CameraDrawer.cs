#define ENABLE_LOG

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Serenegiant.UVC
{

	public class CameraDrawer : MonoBehaviour, IUVCEventHandler
	{
		// THETA VのH.264映像: 3840x1920@30fps, H.264
		// THETA SのH.264映像: 1920x1080@30fps, H.264
		// 普通のUVC機器: 1280x720/1920x1080 MJPEG

		/**
		 * IUVCSelectorがセットされていないとき
		 * またはIUVCSelectorが解像度選択時にnullを
		 * 返したときのデフォルトの解像度(幅)
		 */
		public int VideoWidth = 1280;
		/**
		 * IUVCSelectorがセットされていないとき
		 * またはIUVCSelectorが解像度選択時にnullを
		 * 返したときのデフォルトの解像度(高さ)
		 */
		public int VideoHeight = 720;
		/**
		 * UVC機器とのネゴシエーション時に
		 * H.264を優先してネゴシエーションするかどうか
		 * Android実機のみ有効
		 * true:	H.264 > MJPEG > YUV
		 * false:	MJPEG > H.264 > YUV
		 */
		public bool PreferH264 = true;

		/**
		 * UVC機器からの映像の描画先Materialを保持しているGameObject
		 * 設定していない場合はこのスクリプトを割当てたのと同じGameObjecを使う。
		 */
		public GameObject[] RenderTargets;

		/**
		 * UVC機器とその解像度を選択するためのインターフェース
		 */
		public IUVCSelector UVCSelector;

		/**
		 * WebCamDevice/WebCamTextureを使うときの機器名
		 * 一致するか含んでいるカメラを選択する
		 */
		public string WebCameraDeviceKeyword;

		//--------------------------------------------------------------------------------
		/**
		 * UVC機器からの映像の描画先Material
		 * TargetGameObjectから取得する
		 * 優先順位：
		 *	 TargetGameObjectのSkybox
		 *	 > TargetGameObjectのRenderer
		 *	 > TargetGameObjectのMaterial
		 * いずれの方法でも取得できなければStartでUnityExceptionを投げる
		 */
		private Material[] TargetMaterials;
		/**
		 * オリジナルのテクスチャ
		 * UVCカメラ映像受け取り用テクスチャをセットする前に
		 * GetComponent<Renderer>().material.mainTextureに設定されていた値
		 */
		private Texture[] savedTextures;
		/**
		 * プレビュー中フラグ
		 */
		private bool isPreviewing;

		/**
		 * 接続中のカメラの識別文字列
		 * Android実機の場合はUVC機器のデバイス名
		 * エディタ・PCの場合はWebCamDevice#name
		 */
		private string attachedDeviceName;

		private string activeDeviceName {
			get { return uvcController != null ? uvcController.activeDeviceName : null;  }
			set
			{
				if (uvcController != null)
				{
					uvcController.activeDeviceName = value;
				}
			}
		}
		//--------------------------------------------------------------------------------

#if UNITY_ANDROID
		private UVCController uvcController;
#endif
		private WebCamController webCamController;

		//================================================================================

		// Start is called before the first frame update
		void Start()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine("Start:");
#endif
			UpdateTarget();

		}

//		// Update is called once per frame
//		void Update()
//		{
//
//		}

		//================================================================================
		// 他のコンポーネントからの操作用

		/**
		 * カメラをopenしているか
		 * 映像取得中かどうかはIsPreviewingを使うこと
		 */
		public bool IsOpen()
		{
			return activeDeviceName != null;
		}

		/**
		 * 映像取得中かどうか
		 */
		public bool IsPreviewing()
		{
			return IsOpen() && isPreviewing;
		}

		/**
		 * 映像取得のON/OFF
		 */
		public void Toggle()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"Toggle:{IsPreviewing()}");
#endif
			if (IsOpen())
			{   // UVC機器が接続されている
				if (IsPreviewing())
				{   // 映像取得中
//					CloseCamera(attachedDeviceName);
					StopPreview(attachedDeviceName);
				}
				else
				{   // 映像を取得していない
//					OpenCamera(attachedDeviceName);
					StartPreview(attachedDeviceName);
				}
			}
		}

		/**
		 * 映像描画先のMaterialを再取得する
		 */
		public void ResetMaterial()
		{
			bool prev = IsPreviewing();
			if (prev)
			{
				StopPreview(activeDeviceName);
			}
			UpdateTarget();
			if (prev)
			{
				StartPreview(activeDeviceName);
			}
		}

		//================================================================================

		/**
		 * UVC機器が接続された
		 * @param args UVC機器の識別文字列
		 */
		public void OnEventAttach(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"OnEventAttach[{Time.frameCount}]:(" + args + ")");
#endif
			if (uvcController != null)
			{
				uvcController.OnEventAttach(args);

			}
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"OnEventAttach[{Time.frameCount}]:finished");
#endif
		}

		/**
		 * UVC機器へのアクセスのためのパーミッションを取得できた
		 * @param args UVC機器の識別文字列
		 */
		public void OnEventPermission(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"OnEventPermission:({args})");
#endif
			if (uvcController != null)
			{
				uvcController.OnEventPermission(args);

			}
		}

		/**
		 * UVC機器をオープンした
		 * @param args UVC機器の識別文字列
		 */
		public void OnEventConnect(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"OnEventConnect:({args})");
#endif
			if (uvcController != null)
			{
				uvcController.OnEventConnect(args);

			}
		}

		/**
		 * UVC機器をクローズした
		 * @param args UVC機器の識別文字列
		 */
		public void OnEventDisconnect(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"OnEventDisconnect:({args})");
#endif
			if (uvcController != null)
			{
				uvcController.OnEventDisconnect(args);

			}
		}

		/**
		 * UVC機器が取り外された
		 * @param args UVC機器の識別文字列
		 */
		public void OnEventDetach(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"OnEventReady:({args})");
#endif
			activeDeviceName = args;
			if (!String.IsNullOrEmpty(args))
			{   // argsはdeviceName
				StartPreview(args);
			}
		}

		/**
		 * 映像を受け取れるようになった
		 * @param args UVC機器の識別文字列
		 */
		public void OnEventReady(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"OnEventReady:({args})");
#endif
			if (uvcController != null)
			{
				uvcController.OnEventReady(args);

			}
		}

		/**
		 * UVC機器からの映像取得を開始した
		 * @param args UVC機器の識別文字列
		 */
		public void OnStartPreview(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"OnStartPreview:({args})");
#endif
			if (uvcController != null)
			{
				uvcController.OnStartPreview(args);

			}
		}

		/**
		 * UVC機器からの映像取得を終了した
		 * @param args UVC機器の識別文字列
		 */
		public void OnStopPreview(string args)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"OnStopPreview:({args})");
#endif
			if (uvcController != null)
			{
				uvcController.OnStopPreview(args);

			}
		}

		//================================================================================
		/**
		 * 描画先を更新
		 */
		private void UpdateTarget()
		{
			bool found = false;
			if ((RenderTargets != null) && (RenderTargets.Length > 0))
			{
				UVCSelector = GetUVCSelector(RenderTargets);
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine($"UpdateTarget:UVCSelector={UVCSelector}");
#endif

				TargetMaterials = new Material[RenderTargets.Length];
				int i = 0;
				foreach (var target in RenderTargets)
				{
					if (target != null)
					{
						var material = TargetMaterials[i++] = GetTargetMaterial(target);
						if (material != null)
						{
							found = true;
						}
#if (!NDEBUG && DEBUG && ENABLE_LOG)
						Console.WriteLine($"UpdateTarget:material={material}");
#endif
					}
				}
			}
			if (!found)
			{   // 描画先が1つも見つからなかったときはこのスクリプトが
				// AddComponentされているGameObjectからの取得を試みる
				// XXX RenderTargetsにgameObjectをセットする？
				TargetMaterials = new Material[1];
				TargetMaterials[0] = GetTargetMaterial(gameObject);
				found = TargetMaterials[0] != null;
			}

			if (!found)
			{
				throw new UnityException("no target material found.");
			}
		}

		/**
		 * テクスチャとして映像を描画するMaterialを取得する
		 * 指定したGameObjectにSkybox/Renderer/MaterialがあればそれからMaterialを取得する
		 * それぞれが複数割り当てられている場合最初に見つかった使用可能ものを返す
		 * 優先度: Skybox > Renderer > Material
		 * @param target
		 * @return 見つからなければnullを返す
		 */
		Material GetTargetMaterial(GameObject target/*NonNull*/)
		{
			// Skyboxの取得を試みる
			var skyboxs = target.GetComponents<Skybox>();
			if (skyboxs != null)
			{
				foreach (var skybox in skyboxs)
				{
					if (skybox.isActiveAndEnabled && (skybox.material != null))
					{
						RenderSettings.skybox = skybox.material;
						return skybox.material;
					}
				}
			}
			// Skyboxが取得できなければRendererの取得を試みる
			var renderers = target.GetComponents<Renderer>();
			if (renderers != null)
			{
				foreach (var renderer in renderers)
				{
					if (renderer.enabled && (renderer.material != null))
					{
						return renderer.material;
					}

				}
			}
			// SkyboxもRendererもが取得できなければMaterialの取得を試みる
			var material = target.GetComponent<Material>();
			if (material != null)
			{
				return material;
			}
			return null;
		}

		/**
		 * IUVCSelectorを取得する
		 * UVCSelectorが設定されていればそれを返す
		 * UVCSelectorが見つからないときはTargetGameObjectから取得を試みる
		 * さらに見つからなければこのスクリプトがaddされているGameObjectから取得を試みる
		 * @return 見つからなければnull
		 */
		IUVCSelector GetUVCSelector(GameObject[] targets)
		{
			if (UVCSelector != null)
			{
				return UVCSelector;
			}

			IUVCSelector selector;
			foreach (var target in targets)
			{
				if (target != null)
				{
					selector = target.GetComponent(typeof(IUVCSelector)) as IUVCSelector;
					if (selector != null)
					{
						return selector;
					}
				}
			}

			selector = GetComponent(typeof(IUVCSelector)) as IUVCSelector;
			return selector;
		}

		//--------------------------------------------------------------------------------
		private void OpenCamera(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"OpenCamera:{deviceName}");
#endif
#if UNITY_ANDROID
			if (!Application.isEditor)
			{
				if (uvcController != null)
				{
					uvcController.OpenCamera(deviceName);
				}
			}
			else
			{
				// FIXME 未実装
			}
#else
				// FIXME 未実装
#endif
		}

		private void CloseCamera(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"OpenCamera:{deviceName}");
#endif
#if UNITY_ANDROID
			if (!Application.isEditor)
			{
				if (uvcController != null)
				{
					uvcController.CloseCamera(deviceName);
				}
			}
			else
			{
				// FIXME 未実装
			}
#else
				// FIXME 未実装
#endif
		}

		/**
		 * UVC機器/カメラからの映像受け取り開始要求をする
		 * IUVCSelectorが設定されているときはUVCSelector#SelectSizeから映像サイズの取得を試みる
		 * IUVCSelectorが設定されていないかUVCSelector#SelectSizeがnullを返したときは
		 * スクリプトに設定されているVideoWidth,VideoHeightを使う
		 * @param deviceName カメラの識別文字列
		 */
		private void StartPreview(string deviceName)
		{
			int width = VideoWidth;
			int height = VideoHeight;

			var supportedVideoSize = GetSupportedVideoSize(deviceName);
			if (supportedVideoSize == null)
			{
				throw new ArgumentException("fauled to get supported video size");
			}

			// 解像度の選択処理
			if (UVCSelector != null)
			{
				var size = UVCSelector.SelectSize(GetInfo(deviceName), supportedVideoSize);
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine($"StartPreview:selected={size}");
#endif
				if (size != null)
				{
					width = size.Width;
					height = size.Height;
				}
			}

			// 対応解像度のチェック
			if (supportedVideoSize.Find(width, height/*,minFps=0.1f, maxFps=121.0f*/) == null)
			{   // 指定した解像度に対応していない
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine($"StartPreview:{width}x{height} is NOT supported.");
				Console.WriteLine($"Info={GetInfo(deviceName)}");
				Console.WriteLine($"supportedVideoSize={supportedVideoSize}");
#endif
				throw new ArgumentOutOfRangeException($"{width}x{height} is NOT supported.");
			}
#if UNITY_ANDROID
			if (uvcController != null)
			{
				uvcController.StartPreview(deviceName, width, height);
			}
#else

#endif
		}

		/**
		 * UVC機器/カメラからの映像受けとりを終了要求をする
		 * @param deviceName カメラの識別文字列
		 */
		private void StopPreview(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"StopPreview:{deviceName}");
#endif

			HandleOnStopPreview(deviceName);
#if UNITY_ANDROID
			if (uvcController != null)
			{
				uvcController.StopPreview(deviceName);
			}
#else
			// FIXME 未実装
#endif
		}

		/**
		 * 映像取得開始時の処理
		 * @param tex 映像を受け取るテクスチャ
		 */
		private void HandleOnStartPreview(Texture tex)
		{
			if ((TargetMaterials != null) && (TargetMaterials.Length > 0))
			{
				int i = 0;
				savedTextures = new Texture[TargetMaterials.Length];
				foreach (var material in TargetMaterials)
				{
					if (material != null)
					{
						savedTextures[i++] = material.mainTexture;
						material.mainTexture = tex;
					}
				}
			}
			else
			{
				savedTextures = null;
			}
		}

		/**
		 * 映像取得が終了したときのUnity側の処理
		 * @param deviceName カメラの識別文字列
		 */
		private void HandleOnStopPreview(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"HandleOnStopPreview:{deviceName}");
#endif
			isPreviewing = false;
			// 描画先のテクスチャをもとに戻す
			var n = Math.Min(
				(TargetMaterials != null) ? TargetMaterials.Length : 0,
				(savedTextures != null) ? savedTextures.Length : 0);
			if (n > 0)
			{
				int i = 0;
				foreach (var material in TargetMaterials)
				{
					material.mainTexture = savedTextures[i];
					savedTextures[i++] = null;
				}
			}
			savedTextures = null;
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine("HandleOnStopPreview:finished");
#endif
		}

		/**
 * 指定したUVC機器の情報(今はvidとpid)をJSON文字列として取得する
 * @param deviceName UVC機器の識別文字列
 */
		private UVCInfo GetInfo(string deviceName)
		{
			if (uvcController != null)
			{
				return uvcController.GetInfo(deviceName);
			}
			if (webCamController != null)
			{
				return webCamController.GetInfo(deviceName);
			}
			return null;
		}

		/**
		 * 指定したUVC機器の対応解像度をjson文字列として取得する
		 * @param deviceName UVC機器の識別文字列
		 */
		private SupportedFormats GetSupportedVideoSize(string deviceName)
		{
			if (uvcController != null)
			{
				return uvcController.GetSupportedVideoSize(deviceName);
			}
			if (webCamController != null)
			{
				return webCamController.GetSupportedVideoSize(deviceName);
			}
			return null;
		}

	}   // CameraDrawer

}	// namespace Serenegiant.UVC
