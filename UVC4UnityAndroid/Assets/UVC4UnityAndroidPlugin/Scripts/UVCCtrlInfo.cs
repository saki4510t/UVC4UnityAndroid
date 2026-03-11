/*
 * Copyright (c) 2014 - 2026 t_saki@serenegiant.com 
 */
using System;
using System.Runtime.InteropServices;

namespace Serenegiant.UVC
{
	/**
	 * UVC機器のコントロールユニット・プロセッシングユニットで対応しているコントロールタイプの設定値情報を保持する構造体
	 * nativeプラグインから値を取得する際に使用する
	 * FIXME nativeプラグインから値を受け取る構造体とUnity用のクラスを分けたほうがいいかも
	 */
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct UVCCtrlInfo
    {
        public UInt64 type;
        public Int32 initialized;
        public Int32 hasMinMax;
        public Int32 def;
        public Int32 current;
        public Int32 res;
        public Int32 min;
        public Int32 max;

		public override string ToString()
		{
			return $"{base.ToString()}(type={type},min={min},max={max},def={def},current={current})";
		}
	} // UVCCtrlInfo

} // namespace Serenegiant.UVC
