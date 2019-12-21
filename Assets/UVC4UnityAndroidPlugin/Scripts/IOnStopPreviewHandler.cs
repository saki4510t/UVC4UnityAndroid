using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Serenegiant.UVC
{

	public interface IOnUVCStopHandler
	{
		void OnUVCStopEvent(UVCManager manager, UVCInfo info);

	} // interface IOnStopPreviewEventHandler

}   // namespace Serenegiant.UVC
