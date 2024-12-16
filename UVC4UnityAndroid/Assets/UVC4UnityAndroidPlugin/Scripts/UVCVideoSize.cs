using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Serenegiant.UVC
{
	[Serializable]
	public class UVCVideoSize
	{
		private const string TAG = "UVCVideoSize#";

		public readonly UInt32 frameType;
		public readonly UInt32 frameIndex;
		public readonly UInt32 width;
		public readonly UInt32 height;
		public readonly Int32 frameIntervalType;
		public readonly int[] frameIntervals;
		public readonly float[] fps;

		private UVCVideoSize(UVCVideoSizeFromCpp src)
		{
			frameType = src.frameType;
			frameIndex = src.frameIndex;
			width = src.width;
			height = src.height;
			frameIntervalType = src.frameIntervalType;
			frameIntervals = new int[src.numFrameIntervals];
			Marshal.Copy(src.frameIntercals, frameIntervals, 0, src.numFrameIntervals);
			fps = new float[src.numFps];
			Marshal.Copy(src.fps, fps, 0, src.numFps);
		}

		public override string ToString()
		{
			return $"{base.ToString()}(frameType={frameType},frameIndex={frameIndex},size=({width},{height}),fps=[{string.Join(",", fps)}]";
		}

		/**
		 * �Ή�����f���T�C�Y�ݒ�z����擾����
		 * @param deviceId
		 */
		public static UVCVideoSize[] GetSupportedSize(Int32 deviceId)
		{
			UVCVideoSize[] result = new UVCVideoSize[0];
			Int32 numSupported = 0;
			UVCVideoSizeFromCpp size = new UVCVideoSizeFromCpp();
			// �Ή����Ă���f���T�C�Y�ݒ�̌����擾
			if (GetSupportedSize(deviceId, 0, ref numSupported, ref size) == 0)
			{
				result = new UVCVideoSize[numSupported];
				for (int i = 0; i < numSupported; i++)
				{
					size.frameType = 0;
					size.width = 0;
					size.height = 0;
					if (GetSupportedSize(deviceId, i, ref numSupported, ref size) == 0)
					{
						result[i] = new UVCVideoSize(size);
					}
				}
			} else
			{
				throw new ArgumentException("fauled to get supported video size");
			}

			return result;
		}

		/**
		 * UAC����̉����f�[�^���擾(�Ăяo�����X���b�h���ő��500�~���b�u���b�N����)
		 */
		[DllImport("unityuvcplugin", CallingConvention = CallingConvention.StdCall)]
		private static extern Int32 GetSupportedSize(Int32 deviceId, Int32 index, ref Int32 numSupported, ref UVCVideoSizeFromCpp size);
	}

	/**
	 * C++�̋��L���C�u����������UVC�̉f���T�C�Y�ݒ���󂯎�邽�߂̍\����
	 */
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct UVCVideoSizeFromCpp
	{
		public UInt32 frameType;
		public UInt32 frameIndex;
		public UInt32 width;
		public UInt32 height;
		public Int32 frameIntervalType;
		public IntPtr frameIntercals;
		public Int32 numFrameIntervals;
		public IntPtr fps;
		public Int32 numFps;
	}
}
