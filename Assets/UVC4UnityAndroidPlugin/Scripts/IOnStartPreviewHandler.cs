using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Serenegiant.UVC
{

	public interface IOnUVCStartHandler
	{
		void OnUVCStartEvent(UVCManager manager, UVCInfo info, Texture tex);

	} // interface IOnStartPreviewEventHandler

}   // namespace Serenegiant.UVC

