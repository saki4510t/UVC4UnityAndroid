using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Serenegiant.UVC
{
	[System.Serializable]
	public class RenderTargetSettings
	{
		/**
		 * WebCamDevice/WebCamTextureを使うときの機器名
		 * 一致するか含んでいるカメラを選択する
		 */
		public string WebCameraDeviceKeyword;

		/**
		 * 映像描画先
		 */
		public List<GameObject> RenderTargets;

	} // class RenderTargetSettings

}   // namespace Serenegiant.UVC
