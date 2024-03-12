using System;
using System.Runtime.InteropServices;

namespace Serenegiant.UVC
{
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
	}
}
