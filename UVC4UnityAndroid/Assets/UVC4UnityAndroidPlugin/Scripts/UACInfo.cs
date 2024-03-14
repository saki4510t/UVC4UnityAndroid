using System;
using System.Runtime.InteropServices;

namespace Serenegiant.UVC
{
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
	}
}
