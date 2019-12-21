using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Serenegiant.UVC
{

	public interface IOnUVCDetachHandler
	{
		void OnUVCDetachEvent(UVCManager manager, UVCInfo info);

	} // interface IOnDetachEventHandler

}   // namespace Serenegiant.UVC
