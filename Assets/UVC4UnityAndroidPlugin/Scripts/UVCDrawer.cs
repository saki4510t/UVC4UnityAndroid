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
		UVCEventHandler.IUVCDrawer
	{
		/**
		 * 接続時及び描画時のフィルタ用
		 */
		public UVCFilter[] UVCFilters;

		/**
		 * UVC機器からの映像の描画先Materialを保持しているGameObject
		 * 設定していない場合はこのスクリプトを割当てたのと同じGameObjecを使う。
		 */
		public List<GameObject> RenderTargets;

		//--------------------------------------------------------------------------------
		private const string TAG = "UVCDrawer#";

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
		private UnityEngine.Object[] TargetMaterials;
		/**
		 * オリジナルのテクスチャ
		 * UVCカメラ映像受け取り用テクスチャをセットする前に
		 * GetComponent<Renderer>().material.mainTextureに設定されていた値
		 */
		private Texture[] SavedTextures;

		private Quaternion[] quaternions;

		//================================================================================

		// Start is called before the first frame update
		void Start()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}Start:");
#endif
			UpdateTarget();

		}

//		// Update is called once per frame
//		void Update()
//		{
//
//		}

		//================================================================================

		/**
		 * UVC機器が接続された
		 * IOnUVCAttachHandlerの実装
		 * @param manager 呼び出し元のUVCManager
		 * @param device 対象となるUVC機器の情報
		 * @return true: UVC機器を使用する, false: UVC機器を使用しない
		 */
		public bool OnUVCAttachEvent(UVCManager manager, UVCDevice device)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnUVCAttachEvent:{device}");
#endif
			// XXX 今の実装では基本的に全てのUVC機器を受け入れる
			// ただしTHETA SとTHETA Vは映像を取得できないインターフェースがあるのでオミットする
			// CanDrawと同様にUVC機器フィルターをインスペクタで設定できるようにする
			var result = !device.IsRicoh
				|| (device.IsTHETA_S || device.IsTHETA_V);

			result &= UVCFilter.Match(device, UVCFilters);

			return result;
		}

		/**
		 * UVC機器が取り外された
		 * IOnUVCDetachEventHandlerの実装
		 * @param manager 呼び出し元のUVCManager
		 * @param device 対象となるUVC機器の情報
		 */
		public void OnUVCDetachEvent(UVCManager manager, UVCDevice device)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnUVCDetachEvent:{device}");
#endif
		}

		/**
		 * 解像度選択
		 * IOnUVCSelectSizeHandlerの実装
		 * @param manager 呼び出し元のUVCManager
		 * @param device 対象となるUVC機器の情報
		 * @param formats 対応している解像度についての情報
		 */
		public SupportedFormats.Size OnUVCSelectSize(UVCManager manager, UVCDevice device, SupportedFormats formats)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnUVCSelectSize:{device}");
#endif
			if (device.IsTHETA_V)
			{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine($"{TAG}OnUVCSelectSize:THETA V");
#endif
				return FindSize(formats, 3840, 1920);
			}
			else if (device.IsTHETA_S)
			{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine($"{TAG}OnUVCSelectSize:THETA S");
#endif
				return FindSize(formats, 1920, 1080);
			}
			else
			{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine($"{TAG}OnUVCSelectSize:other UVC device,{device}");
#endif
				return null;
			}
		}

		/**
		 * IUVCDrawerが指定したUVC機器の映像を描画できるかどうかを取得
		 * IUVCDrawerの実装
		 * @param manager 呼び出し元のUVCManager
		 * @param device 対象となるUVC機器の情報
		 */
		public bool CanDraw(UVCManager manager, UVCDevice device)
		{
			return  UVCFilter.Match(device, UVCFilters);
		}

		/**
		 * 映像取得を開始した
		 * IUVCDrawerの実装
		 * @param manager 呼び出し元のUVCManager
		 * @param device 対象となるUVC機器の情報
		 * @param tex UVC機器からの映像を受け取るTextureインスタンス
		 */
		public void OnUVCStartEvent(UVCManager manager, UVCDevice device, Texture tex)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnUVCStartEvent:{device}");
#endif
			HandleOnStartPreview(device.deviceName, tex);
		}

		/**
		 * 映像取得を終了した
		 * IUVCDrawerの実装
		 * @param manager 呼び出し元のUVCManager
		 * @param device 対象となるUVC機器の情報
		 */
		public void OnUVCStopEvent(UVCManager manager, UVCDevice device)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnUVCStopEvent:{device}");
#endif
			HandleOnStopPreview(device.deviceName);
		}

		//================================================================================
		/**
		 * 描画先を更新
		 */
		private void UpdateTarget()
		{
			bool found = false;
			if ((RenderTargets != null) && (RenderTargets.Count > 0))
			{
				TargetMaterials = new UnityEngine.Object[RenderTargets.Count];
				SavedTextures = new Texture[RenderTargets.Count];
				quaternions = new Quaternion[RenderTargets.Count];
				int i = 0;
				foreach (var target in RenderTargets)
				{
					if (target != null)
					{
						var material = TargetMaterials[i] = GetTargetMaterial(target);
						if (material != null)
						{
							found = true;
						}
#if (!NDEBUG && DEBUG && ENABLE_LOG)
						Console.WriteLine($"{TAG}UpdateTarget:material={material}");
#endif
					}
					i++;
				}
			}
			if (!found)
			{   // 描画先が1つも見つからなかったときはこのスクリプトが
				// AddComponentされているGameObjectからの取得を試みる
				// XXX RenderTargetsにgameObjectをセットする？
				TargetMaterials = new UnityEngine.Object[1];
				SavedTextures = new Texture[1];
				quaternions = new Quaternion[1];
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

		private void RestoreTexture()
		{
			for (int i = 0; i < TargetMaterials.Length; i++)
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

		private void ClearTextures()
		{
			for (int i = 0; i < SavedTextures.Length; i++)
			{
				SavedTextures[i] = null;
			}
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
			int i = 0;
			foreach (var target in TargetMaterials)
			{
				if (target is Material)
				{
					SavedTextures[i++] = (target as Material).mainTexture;
					(target as Material).mainTexture = tex;
				}
				else if (target is RawImage)
				{
					SavedTextures[i++] = (target as RawImage).texture;
					(target as RawImage).texture = tex;
				}
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
			// 描画先のテクスチャをもとに戻す
			RestoreTexture();
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}HandleOnStopPreview:finished");
#endif
		}

	} // class UVCDrawer

} // namespace Serenegiant.UVC