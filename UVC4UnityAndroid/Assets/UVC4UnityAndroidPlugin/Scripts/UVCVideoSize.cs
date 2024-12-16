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
		 * 対応する映像サイズ設定配列を取得する
		 * @param deviceId
		 */
		public static UVCVideoSize[] GetSupportedSize(Int32 deviceId)
		{
			UVCVideoSize[] result = new UVCVideoSize[0];
			Int32 numSupported = 0;
			UVCVideoSizeFromCpp size = new UVCVideoSizeFromCpp();
			// 対応している映像サイズ設定の個数を取得
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
		 * UACからの音声データを取得(呼び出し元スレッドを最大で500ミリ秒ブロックする)
		 */
		[DllImport("unityuvcplugin", CallingConvention = CallingConvention.StdCall)]
		private static extern Int32 GetSupportedSize(Int32 deviceId, Int32 index, ref Int32 numSupported, ref UVCVideoSizeFromCpp size);
	}

	/**
	 * C++の共有ライブラリ側からUVCの映像サイズ設定を受け取るための構造体
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
