using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Serenegiant.UVC
{
	public interface IOnUVCSelectSizeHandler
	{
		SupportedFormats.Size OnUVCSelectSize(UVCManager manager, UVCInfo info, SupportedFormats formats);

	} // interface IPreviewSizeSelect

} // namespace Serenegiant.UVC
