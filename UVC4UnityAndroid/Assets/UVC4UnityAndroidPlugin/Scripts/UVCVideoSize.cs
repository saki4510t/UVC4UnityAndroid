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

		public readonly UInt32 FrameType;
		public readonly UInt32 FrameIndex;
		public readonly UInt32 Width;
		public readonly UInt32 Height;
		public readonly Int32 FrameIntervalType;
		public readonly int[] FrameIntervals;
		public readonly float[] Fps;

		private UVCVideoSize(UVCVideoSizeFromCpp src)
		{
			FrameType = src.FrameType;
			FrameIndex = src.FrameIndex;
			Width = src.Width;
			Height = src.Height;
			FrameIntervalType = src.FrameIntervalType;
			FrameIntervals = new int[src.NumFrameIntervals];
			Marshal.Copy(src.FrameIntercals, FrameIntervals, 0, src.NumFrameIntervals);
			Fps = new float[src.NumFps];
			Marshal.Copy(src.Fps, Fps, 0, src.NumFps);
		}

		public override string ToString()
		{
			return $"{base.ToString()}(frameType={FrameType},frameIndex={FrameIndex},size=({Width},{Height}),fps=[{string.Join(",", Fps)}]";
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
					size.FrameType = 0;
					size.Width = 0;
					size.Height = 0;
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
		public UInt32 FrameType;
		public UInt32 FrameIndex;
		public UInt32 Width;
		public UInt32 Height;
		public Int32 FrameIntervalType;
		public IntPtr FrameIntercals;
		public Int32 NumFrameIntervals;
		public IntPtr Fps;
		public Int32 NumFps;
	}
}
