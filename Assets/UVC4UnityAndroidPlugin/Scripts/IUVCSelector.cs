using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Serenegiant.UVC
{

	/**
	 * 指定したUVC機器を使うことができるかをチェックするインターフェース
	 */
	public interface IUVCSelector
	{
		/**
		 * 指定したUVC機器をオープンするかどうか
		 */
		bool CanSelect(UVCInfo info);
		/**
		 * 指定したUVC機器で使用する解像度を取得
		 */
		SupportedFormats.Size SelectSize(UVCInfo info, SupportedFormats formats);

	} // IUVCSelector

} // namespace Serenegiant.UVC
