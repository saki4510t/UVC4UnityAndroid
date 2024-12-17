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

		public const UInt32 FRAME_TYPE_UNKNOWN		= 0x000000;
		public const UInt32 FRAME_TYPE_MJPEG		= 0x000007;
		public const UInt32 FRAME_TYPE_H264			= 0x000014;
		public const UInt32 FRAME_TYPE_H264_FRAME	= 0x030011;

		public static UVCVideoSize INVALID = new UVCVideoSize();

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

		/**
		 * 無効な解像度設定を生成するコンストラクタ
		 */
		private UVCVideoSize()
		{
			FrameType = FRAME_TYPE_UNKNOWN;
			FrameIndex = 0;
			Width = 0;
			Height = 0;
			FrameIntervalType = 0;
			FrameIntervals = new int[0];
			Fps = new float[0];
		}

		/**
		 * 有効な解像度設定かどうかを取得する
		 */
		public bool IsValid
		{
			get { return (FrameType != FRAME_TYPE_UNKNOWN) && (Width != 0) && (Height != 0) && (FrameIntervals.Length > 0) && (Fps.Length > 0); }
		}

		public override string ToString()
		{
			return $"{base.ToString()}(frameType={FrameType},frameIndex={FrameIndex},size=({Width},{Height}),fps=[{string.Join(",", Fps)}]";
		}

		/**
		 * Equalsとは別にFrameType/FrameIndex/Width/Heightが一致すればtrue
		 */
		public bool IsSameValue(object obj)
		{
			if (!(obj is UVCVideoSize)) return false;
			var other = (UVCVideoSize)obj;
			return (FrameType == other.FrameType) && (FrameIndex == other.FrameIndex) && (Width == other.Width) && (Height == other.Height);
		}
	
		/**
		 * 対応する映像サイズ設定配列を取得する
		 * @param deviceId
		 */
		public static UVCVideoSize[] GetSupportedSize(Int32 deviceId)
		{
			var result = new UVCVideoSize[0];
			Int32 numSupported = 0;
			UVCVideoSizeFromCpp size = new UVCVideoSizeFromCpp();
			// 対応している映像サイズ設定の個数を取得
			if (GetSupportedSize(deviceId, 0, ref numSupported, ref size) == 0)
			{
				result = new UVCVideoSize[numSupported];
				for (int i = 0; i < numSupported; i++)
				{
					size.FrameType = FRAME_TYPE_UNKNOWN;
					if (GetSupportedSize(deviceId, i, ref numSupported, ref size) == 0)
					{
						result[i] = new UVCVideoSize(size);
					} else
					{   // null避けに無効なUVCVideoSizeを入れておく
						result[i] = INVALID;
					}
				}
			} else
			{
				throw new ArgumentException("failed to get supported video size");
			}

			return result;
		}

		/**
		 * 完全一致または最も近い解像度設定を探す
		 * @param supporteds 対応解像度設定配列
		 * @param frameType FRAME_TYPE_UNKNOWNならワイルドカードとして任意のフレームタイプと一致する
		 * @param width
		 * @param height
		 */
		public static UVCVideoSize FindNearest(UVCVideoSize[] supporteds, UInt32 frameType, UInt32 width, UInt32 height)
		{
			var found = INVALID;
			if (frameType != FRAME_TYPE_UNKNOWN)
			{
				// 完全一致を探す
				foreach (var size in supporteds)
				{
					if ((size.FrameType == frameType) && (size.Width == width) && (size.Height == height))
					{
						found = size;
						break;
					}
				}
			}
			if (!found.IsValid)
			{   // フレームタイプが一致する最も近い解像度設定を探す
				var prev = UInt32.MaxValue;
				foreach (var size in supporteds)
				{
					if ((size.FrameType == frameType) || (frameType == FRAME_TYPE_UNKNOWN))
					{
						var dw = size.Width - width;
						var dh = size.Height - height;
						var d = dw * dw + dh * dh;
						if (d < prev)
						{
							prev = d;
							found = size;
						}
					}
				}
			}

			return found;
		}

		/**
		 * プラグイン側から解像度設定を読み込む
		 * @param deviceId
		 * @param index 0～numSupportedの解像度インデックス
		 * @param numSupported 対応する解像度設定の合計個数が代入される変数
		 * @param size 指定したindexの解像度設定が代入される変数
		 */
		[DllImport("unityuvcplugin", CallingConvention = CallingConvention.StdCall)]
		private static extern Int32 GetSupportedSize(Int32 deviceId, Int32 index, ref Int32 numSupported, ref UVCVideoSizeFromCpp size);


		/**
		 * C++の共有ライブラリ側からUVCの映像サイズ設定を受け取るための構造体
		 */
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		internal struct UVCVideoSizeFromCpp
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
	} // UVCVideoSize

}
