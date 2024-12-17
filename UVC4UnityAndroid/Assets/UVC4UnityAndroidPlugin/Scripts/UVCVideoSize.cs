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
		 * �����ȉ𑜓x�ݒ�𐶐�����R���X�g���N�^
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
		 * �L���ȉ𑜓x�ݒ肩�ǂ������擾����
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
		 * Equals�Ƃ͕ʂ�FrameType/FrameIndex/Width/Height����v�����true
		 */
		public bool IsSameValue(object obj)
		{
			if (!(obj is UVCVideoSize)) return false;
			var other = (UVCVideoSize)obj;
			return (FrameType == other.FrameType) && (FrameIndex == other.FrameIndex) && (Width == other.Width) && (Height == other.Height);
		}
	
		/**
		 * �Ή�����f���T�C�Y�ݒ�z����擾����
		 * @param deviceId
		 */
		public static UVCVideoSize[] GetSupportedSize(Int32 deviceId)
		{
			var result = new UVCVideoSize[0];
			Int32 numSupported = 0;
			UVCVideoSizeFromCpp size = new UVCVideoSizeFromCpp();
			// �Ή����Ă���f���T�C�Y�ݒ�̌����擾
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
					{   // null�����ɖ�����UVCVideoSize�����Ă���
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
		 * ���S��v�܂��͍ł��߂��𑜓x�ݒ��T��
		 * @param supporteds �Ή��𑜓x�ݒ�z��
		 * @param frameType FRAME_TYPE_UNKNOWN�Ȃ烏�C���h�J�[�h�Ƃ��ĔC�ӂ̃t���[���^�C�v�ƈ�v����
		 * @param width
		 * @param height
		 */
		public static UVCVideoSize FindNearest(UVCVideoSize[] supporteds, UInt32 frameType, UInt32 width, UInt32 height)
		{
			var found = INVALID;
			if (frameType != FRAME_TYPE_UNKNOWN)
			{
				// ���S��v��T��
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
			{   // �t���[���^�C�v����v����ł��߂��𑜓x�ݒ��T��
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
		 * �v���O�C��������𑜓x�ݒ��ǂݍ���
		 * @param deviceId
		 * @param index 0�`numSupported�̉𑜓x�C���f�b�N�X
		 * @param numSupported �Ή�����𑜓x�ݒ�̍��v������������ϐ�
		 * @param size �w�肵��index�̉𑜓x�ݒ肪��������ϐ�
		 */
		[DllImport("unityuvcplugin", CallingConvention = CallingConvention.StdCall)]
		private static extern Int32 GetSupportedSize(Int32 deviceId, Int32 index, ref Int32 numSupported, ref UVCVideoSizeFromCpp size);


		/**
		 * C++�̋��L���C�u����������UVC�̉f���T�C�Y�ݒ���󂯎�邽�߂̍\����
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
