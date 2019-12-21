using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Serenegiant.UVC
{
	public interface IOnUVCAttachHandler
	{
		bool OnUVCAttachEvent(UVCManager manager, UVCInfo info);

	} // interface OnAttachEventHandler

}   // namespace Serenegiant.UVC

