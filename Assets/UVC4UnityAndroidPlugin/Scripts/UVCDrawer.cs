#define ENABLE_LOG

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Serenegiant.UVC
{

	public class UVCDrawer : MonoBehaviour,
		UVCEventHandler.IOnUVCAttachHandler, UVCEventHandler.IOnUVCDetachHandler,
		UVCEventHandler.IOnUVCSelectSizeHandler,
		UVCEventHandler.IOnUVCStartHandler, UVCEventHandler.IOnUVCStopHandler
	{
		/**
		 * UVC機器からの映像の描画先Materialを保持しているGameObject
		 * 設定していない場合はこのスクリプトを割当てたのと同じGameObjecを使う。
		 */
		public List<RenderTargetSettings> CameraRenderSettings;

		//--------------------------------------------------------------------------------
		private const string TAG = "UVCDrawer#";

		/**
		 * カメラ毎の設定保持用
		 */
		private class TargetInfo
		{
			public readonly int Count;
			/**
			 * UVC機器からの映像の描画先Material
			 * TargetGameObjectから取得する
			 * 優先順位：
			 *	 TargetGameObjectのSkybox
			 *	 > TargetGameObjectのRenderer
			 *	 > TargetGameObjectのRawImage
			 *	 > TargetGameObjectのMaterial
			 * いずれの方法でも取得できなければStartでUnityExceptionを投げる
			 */
			public readonly UnityEngine.Object[] TargetMaterials;
			/**
			 * オリジナルのテクスチャ
			 * UVCカメラ映像受け取り用テクスチャをセットする前に
			 * GetComponent<Renderer>().material.mainTextureに設定されていた値
			 */
			public readonly Texture[] SavedTextures;

			public Quaternion[] quaternions;
			public bool isActive = false;

			public TargetInfo(int targetNums)
			{
				Count = targetNums;
				TargetMaterials = new UnityEngine.Object[targetNums];
				SavedTextures = new Texture[targetNums];
				quaternions = new Quaternion[targetNums];
			}

			public void RestoreTexture()
			{
				for (int i = 0; i < Count; i++)
				{
					var target = TargetMaterials[i];
					if (target is Material)
					{
						(target as Material).mainTexture = SavedTextures[i];
					}
					else if (target is RawImage)
					{
						(target as RawImage).texture = SavedTextures[i];
					}
					SavedTextures[i] = null;
					quaternions[i] = Quaternion.identity;
				}
			}

			public void ClearTextures()
			{
				for (int i = 0; i < SavedTextures.Length; i++)
				{
					SavedTextures[i] = null;
				}
			}
		}

		/**
		 * カメラ毎の設定値
		 */
		private TargetInfo[] targetInfos;

		/**
		 * ハンドリングしているカメラとTargetInfoを結びつけるための連想配列
		 * string(deviceName) - index ペアを保持する
		 */
		private Dictionary<string, int> cameraIndies = new Dictionary<string, int>();

		//================================================================================

		// Start is called before the first frame update
		void Start()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}Start:");
#endif
			UpdateTarget();

		}

		// Update is called once per frame
		void Update()
		{

		}

		//================================================================================

		public bool OnUVCAttachEvent(UVCManager manager, UVCInfo info)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnUVCAttachEvent:{info}");
#endif
			var result = !info.IsRicoh
				|| (info.IsTHETA_S || info.IsTHETA_V);

			if (result)
			{
				CreateIfNotExist(info.deviceName);
			}

			return result;
		}

		public void OnUVCDetachEvent(UVCManager manager, UVCInfo info)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnUVCDetachEvent:{info}");
#endif
			Remove(info.deviceName);
		}

		public SupportedFormats.Size OnUVCSelectSize(UVCManager manager, UVCInfo info, SupportedFormats formats)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnUVCSelectSize:{info}");
#endif
			if (info.IsTHETA_V)
			{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine($"{TAG}OnUVCSelectSize:THETA V");
#endif
				return FindSize(formats, 3840, 1920);
			}
			else if (info.IsTHETA_S)
			{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine($"{TAG}OnUVCSelectSize:THETA S");
#endif
				return FindSize(formats, 1920, 1080);
			}
			else
			{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine($"{TAG}OnUVCSelectSize:other UVC device,{info}");
#endif
				return null;
			}
		}

		public void OnUVCStartEvent(UVCManager manager, UVCInfo info, Texture tex)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnUVCStartEvent:{info}");
#endif
			HandleOnStartPreview(info.deviceName, tex);
		}

		public void OnUVCStopEvent(UVCManager manager, UVCInfo info)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnUVCStopEvent:{info}");
#endif
			HandleOnStopPreview(info.deviceName);
		}

		//================================================================================
		/**
		 * 描画先を更新
		 */
		private void UpdateTarget()
		{
			bool found = false;
			if ((CameraRenderSettings != null) && (CameraRenderSettings.Count > 0))
			{
				targetInfos = new TargetInfo[CameraRenderSettings.Count];
				int j = 0;
				foreach (var targets in CameraRenderSettings)
				{
					if (targets != null)
					{
						targetInfos[j] = new TargetInfo(targets.RenderTargets.Count);
						int i = 0;
						foreach (var target in targets.RenderTargets)
						{
							var material = targetInfos[j].TargetMaterials[i++] = GetTargetMaterial(target);
							if (material != null)
							{
								found = true;
							}
#if (!NDEBUG && DEBUG && ENABLE_LOG)
							Console.WriteLine($"{TAG}UpdateTarget:material={material}");
#endif
						}
					}
					j++;
				}
			}
			if (!found)
			{   // 描画先が1つも見つからなかったときはこのスクリプトが
				// AddComponentされているGameObjectからの取得を試みる
				// XXX RenderTargetsにgameObjectをセットする？
				targetInfos = new TargetInfo[1];
				targetInfos[0] = new TargetInfo(1);
				targetInfos[0].TargetMaterials[0] = GetTargetMaterial(gameObject);
				found = targetInfos[0].TargetMaterials[0] != null;
			}

			if (!found)
			{
				throw new UnityException("no target material found.");
			}
		}

		/**
		 * テクスチャとして映像を描画するMaterialを取得する
		 * 指定したGameObjectにSkybox/Renderer/RawImage/MaterialがあればそれからMaterialを取得する
		 * それぞれが複数割り当てられている場合最初に見つかった使用可能ものを返す
		 * 優先度: Skybox > Renderer > RawImage > Material
		 * @param target
		 * @return 見つからなければnullを返す
		 */
		UnityEngine.Object GetTargetMaterial(GameObject target/*NonNull*/)
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
			// SkyboxもRendererも取得できなければRawImageの取得を試みる
			var rawImages = target.GetComponents<RawImage>();
			if (rawImages != null)
			{
				foreach (var rawImage in rawImages)
				{
					if (rawImage.enabled && (rawImage.material != null))
					{
						return rawImage;
					}

				}
			}
			// SkyboxもRendererもRawImageも取得できなければMaterialの取得を試みる
			var material = target.GetComponent<Material>();
			if (material != null)
			{
				return material;
			}
			return null;
		}

		//--------------------------------------------------------------------------------
		private int FindCameraIx(string deviceName)
		{
			return cameraIndies.ContainsKey(deviceName) ? cameraIndies[deviceName] : -1;
		}

		/*NonNull*/
		private int CreateIfNotExist(string deviceName)
		{
			if (!cameraIndies.ContainsKey(deviceName))
			{
				int n = cameraIndies.Count;
				int cameraIx = 0;
				foreach (var index in cameraIndies.Values)
				{
					if (index == cameraIx)
					{
						cameraIx++;
					}
				}
				cameraIndies[deviceName] = cameraIx;
			}
			return cameraIndies[deviceName];
		}

		/*Nullable*/
		private int Remove(string deviceName)
		{
			int index = -1;

			if (cameraIndies.ContainsKey(deviceName))
			{
				index = cameraIndies[deviceName];
				cameraIndies.Remove(deviceName);
			}

			return index;
		}

		//--------------------------------------------------------------------------------
		private SupportedFormats.Size FindSize(SupportedFormats formats, int width, int height)
		{
			return formats.Find(width, height);
		}

		/**
 * 映像取得開始時の処理
 * @param tex 映像を受け取るテクスチャ
 */
		private void HandleOnStartPreview(string deviceName, Texture tex)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}HandleOnStartPreview:({tex})");
#endif
			var cameraIx = FindCameraIx(deviceName);
			if ((cameraIx < targetInfos.Length) && (targetInfos[cameraIx] != null))
			{
				targetInfos[cameraIx].isActive = true;
				if (targetInfos[cameraIx].Count > 0)
				{
					int i = 0;
					foreach (var target in targetInfos[cameraIx].TargetMaterials)
					{
						if (target is Material)
						{
							targetInfos[cameraIx].SavedTextures[i++] = (target as Material).mainTexture;
							(target as Material).mainTexture = tex;
						}
						else if (target is RawImage)
						{
							targetInfos[cameraIx].SavedTextures[i++] = (target as RawImage).texture;
							(target as RawImage).texture = tex;
						}
					}
				}
				else
				{
					targetInfos[cameraIx].ClearTextures();
				}
			}
			else
			{
				throw new ArgumentOutOfRangeException();
			}
		}

		/**
		 * 映像取得が終了したときのUnity側の処理
		 * @param deviceName カメラの識別文字列
		 */
		private void HandleOnStopPreview(string deviceName)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}HandleOnStopPreview:{deviceName}");
#endif
			var cameraIx = FindCameraIx(deviceName);
			// 描画先のテクスチャをもとに戻す
			if ((cameraIx < targetInfos.Length) && (targetInfos[cameraIx] != null))
			{
				targetInfos[cameraIx].isActive = false;
				targetInfos[cameraIx].RestoreTexture();
			}
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}HandleOnStopPreview:finished");
#endif
		}

	} // class UVCDrawer

} // namespace Serenegiant.UVC