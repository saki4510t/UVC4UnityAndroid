//#define ENABLE_LOG
/*
 * Copyright (c) 2014 - 2022 t_saki@serenegiant.com 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Serenegiant.UVC
{

	public class UVCDrawer : MonoBehaviour, IUVCDrawer
	{
		/**
		 * IUVCSelectorがセットされていないとき
		 * またはIUVCSelectorが解像度選択時にnullを
		 * 返したときのデフォルトの解像度(幅)
		 */
		public int DefaultWidth = 1280;
		/**
		 * IUVCSelectorがセットされていないとき
		 * またはIUVCSelectorが解像度選択時にnullを
		 * 返したときのデフォルトの解像度(高さ)
		 */
		public int DefaultHeight = 720;
		/**
		 * 可能な場合にUACから音声取得を行うかどうか
		 */
		public bool UACEnabled = false;
		/**
		 * 接続時及び描画時のフィルタ用
		 */
		public UVCFilter[] UVCFilters;

		/**
		 * UVC機器からの映像の描画先Materialを保持しているGameObject
		 * 設定していない場合はこのスクリプトを割当てたのと同じGameObjecを使う。
		 */
		public List<GameObject> RenderTargets;
		/**
		 * UVC機器のUAC機能で取得した音声を再生するために使用するAudioSourceを保持するGameObject
		 * 設定していない場合はこのスクリプトを割当てたのと同じGameObjecを使う。
		 */
		public GameObject AudioTarget;
	
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
			UpdateRenderTarget();

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
			// ただしTHETA SとTHETA VとTHETA Z1は映像を取得できないインターフェースがあるのでオミットする
			// IsUVCEnabledと同様にUVC機器フィルターをインスペクタで設定できるようにする
			var result = !device.IsRicoh || device.IsTHETA;

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
		 * IUVCDrawerが指定したUVC機器の映像を描画できるかどうかを取得
		 * IUVCDrawerの実装
		 * @param manager 呼び出し元のUVCManager
		 * @param device 対象となるUVC機器の情報
		 */
		public bool IsUVCEnabled(UVCManager manager, UVCDevice device)
		{
			return UVCFilter.Match(device, UVCFilters);
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
			HandleOnStartPreview(tex);
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
			HandleOnStopPreview();
		}

		/**
		 * IUVCDrawerが指定したUAC機器kからの音声を取得を有効にするかどうか取得
		 * XXX とりあえずUACに対応した機器であればtrueを返す, 必要に応じて書き換えること
		 * IUVCDrawerの実装
		 * @param manager 呼び出し元のUVCManager
		 * @param device 対象となるUAC機器の情報
		 */
		public bool IsUACEnabled(UVCManager manager, UVCDevice device)
		{
			return UACEnabled && device.isUAC;
		}

		/**
		 * UAC機器からの音声取得を開始した
		 * @param manager 呼び出し元のUVCManager
		 * @param device 接続されたUVC機器情報
		 * @param audioClip UAC機器からの音声を受け取るAudioClipオブジェクト
		 */
		public void OnUACStartEvent(UVCManager manager, UVCDevice device, AudioClip audioClip)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnUACStartEvent:{device}");
#endif
			HandleOnStartAudio(audioClip);
		}

		/**
		 * UAC機器からの音声取得を終了した
		 * @param manager 呼び出し元のUVCManager
		 * @param device 接続されたUVC機器情報
		 */
		public void OnUACStopEvent(UVCManager manager, UVCDevice device)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnUACStopEvent:{device}");
#endif
			HandleOnStopAudio();
		}

		//================================================================================
		/**
		 * 描画先を更新
		 */
		private void UpdateRenderTarget()
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
						Console.WriteLine($"{TAG}UpdateRenderTarget:material={material}");
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
				try
				{
					if (target is Material)
					{
						(target as Material).mainTexture = SavedTextures[i];
					}
					else if (target is RawImage)
					{
						(target as RawImage).texture = SavedTextures[i];
					}
				}
				catch
				{
					Console.WriteLine($"{TAG}RestoreTexture:Exception cought");
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

		/**
		 * 映像取得開始時の処理
		 * @param tex 映像を受け取るテクスチャ
		 */
		private void HandleOnStartPreview(Texture tex)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}HandleOnStartPreview:({tex})");
#endif
			int i = 0;
			foreach (var target in TargetMaterials)
			{
				if (target is Material)
				{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
					Console.WriteLine($"{TAG}HandleOnStartPreview:assign Texture to Material({target})");
#endif
					SavedTextures[i++] = (target as Material).mainTexture;
					(target as Material).mainTexture = tex;
				}
				else if (target is RawImage)
				{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
					Console.WriteLine($"{TAG}HandleOnStartPreview:assign Texture to RawImage({target})");
#endif
					SavedTextures[i++] = (target as RawImage).texture;
					(target as RawImage).texture = tex;
				}
			}
		}

		/**
		 * 映像取得が終了したときのUnity側の処理
		 */
		private void HandleOnStopPreview()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}HandleOnStopPreview:");
#endif
			// 描画先のテクスチャをもとに戻す
			RestoreTexture();
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}HandleOnStopPreview:finished");
#endif
		}

		/**
		 * UACの音声再生を行うAudioSourceを取得する
		 */
		private AudioSource GetAudioSource()
		{
			AudioSource result = null;
			if (AudioTarget != null)
			{
				result = AudioTarget.GetComponent<AudioSource>();
			}
			if (result == null)
			{
				result = GetComponent<AudioSource>();
			}

#if (!NDEBUG && DEBUG && ENABLE_LOG)
			if (result == null)
			{
				Console.WriteLine($"{TAG}GetAudioSource:audio source not found");
			}
#endif
			return result;
		}

		/**
		 * 音声取得開始した時のUnity側の処理
		 * @param audioClip
		 */
		private void HandleOnStartAudio(AudioClip audioClip)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}HandleOnStartAudio:");
#endif
			var audioSource = GetAudioSource();
			if (audioSource != null)
			{
				audioSource.Stop();
				audioSource.clip = audioClip;
				audioSource.Play();
			}
		}

		/**
		 * 音声取得終了した時のUnity側の処理
		 */
		private void HandleOnStopAudio()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}HandleOnStopAudio:");
#endif
			var audioSource = GetAudioSource();
			if (audioSource != null)
			{
				audioSource.Stop();
				audioSource.clip = null;
			}
		}

	} // class UVCDrawer

} // namespace Serenegiant.UVC
