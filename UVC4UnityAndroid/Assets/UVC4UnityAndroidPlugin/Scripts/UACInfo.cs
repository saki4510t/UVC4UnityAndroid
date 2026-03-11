/*
 * Copyright (c) 2014 - 2026 t_saki@serenegiant.com 
 */
using System;
using System.Runtime.InteropServices;

namespace Serenegiant.UVC
{
	/**
	 * UACに対応している場合にUACの情報を保持するための構造体
	 * nativeプラグインから値を取得する際に使用する
	 * FIXME nativeプラグインから値を受け取る構造体とUnity用のクラスを分けたほうがいいかも
	 */
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct UACInfo
    {
        public Int32 deviceId;
        public Int32 channels;
        public Int32 resolution;
        public Int32 samplingFreq;
		public Int32 packetBytes;

		public override string ToString()
		{
			return $"{base.ToString()}(channels={channels},resolution={resolution},freq={samplingFreq},bytes={packetBytes})";
		}
	} // UACInfo

} // namespace Serenegiant.UVC
