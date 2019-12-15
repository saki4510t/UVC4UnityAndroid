//#define ENABLE_LOG

using System;
using System.Collections;
using System.Text.Json;
using UnityEngine;

/*
{
	"formats":[
		{
			"frame_type":7,
			"default":1,
			"size":[
				"640x480",
				"160x90",
				"160x120",
				"176x144",
				"320x180",
				"320x240",
				"352x288",
				"432x240",
				"640x360",
				"800x448",
				"800x600",
				"864x480",
				"960x720",
				"1024x576",
				"1280x720",
				"1600x896",
				"1920x1080"
			],
			"frameRate":[
				[30,24,20,15,10,7.5,5],
				[30,24,20,15,10,7.5,5],
				[30,24,20,15,10,7.5,5],
				[30,24,20,15,10,7.5,5],
				[30,24,20,15,10,7.5,5],
				[30,24,20,15,10,7.5,5],
				[30,24,20,15,10,7.5,5],
				[30,24,20,15,10,7.5,5],
				[30,24,20,15,10,7.5,5],
				[30,24,20,15,10,7.5,5],
				[30,24,20,15,10,7.5,5],
				[30,24,20,15,10,7.5,5],
				[30,24,20,15,10,7.5,5],
				[30,24,20,15,10,7.5,5],
				[30,24,20,15,10,7.5,5],
				[30,24,20,15,10,7.5,5],
				[30,24,20,15,10,7.5,5]
			]
		}
	]
}
*/

namespace Serenegiant.UVC
{

	[Serializable]
	public class SupportedFormats
	{
		[Serializable]
		public class Size
		{
			public int FrameType;
			public int Width;
			public int Height;
			public float[] FrameRate;

			public Size(int frame_type, int width, int height, float[]frameRate)
			{
				FrameType = frame_type;
				Width = width;
				Height = height;
				FrameRate = frameRate;
			}

			public override string ToString()
			{
				return $"{base.ToString()}(FrameType={FrameType},size=({Width}x{Height}),FrameRate=[{string.Join(",", FrameRate)}]";
			}
		}

//		public class FrameFormat : IEnumerable
		[Serializable]
		public class FrameFormat
		{
			public int frame_type { get; set; }
			public int defaultIndex { get; set; }
			public string[] size { get; set; }
			public float[][] frameRate { get; set; }

			public FrameFormat(JsonElement element)
			{
				frame_type = element.GetProperty("frame_type").GetInt32();
				defaultIndex = element.GetProperty("default").GetInt32();
				var sizeArray = element.GetProperty("size");
				var sizeNum = sizeArray.GetArrayLength();
				size = new string[sizeNum];
				if (sizeNum > 0)
				{
					int i = 0;
					foreach (var item in sizeArray.EnumerateArray())
					{
						size[i++] = item.GetString();
					}
					var frameRateArray = element.GetProperty("frameRate");
					frameRate = new float[sizeNum][];
					i = 0;
					foreach (var item in frameRateArray.EnumerateArray())
					{
						frameRate[i] = new float[item.GetArrayLength()];
						int j = 0;
						foreach (var value in item.EnumerateArray())
						{
							frameRate[i][j++] = value.GetSingle();
						}
						i++;
					}
				}

			}

			/**
			 * 対応解像度の個数を取得
			 */
			public int GetNumSize()
			{
				return Math.Min(
					size != null ? size.Length : 0,
					frameRate != null ? frameRate.Length : 0);
			}

//			/**
//			 * Sizeの反復取得用の列挙子を取得
//			 */
//			IEnumerator IEnumerable.GetEnumerator()
//			{
//				return (IEnumerator)GetEnumerator();
//			}

			/**
			 * Sizeの反復取得用の列挙子を取得
			 */
			public SizeEnumerator GetEnumerator()
			{
				return new SizeEnumerator(this);
			}

			/**
			 * 指定したインデックスの解像度のフレームレートが指定範囲内かどうかをチェック
			 * @param ix
			 * @param minFps
			 * @param maxFps
			 */
			private bool IsSupported(int ix, float minFps, float maxFps)
			{
				bool result = false;

				// XXX 呼び出し元でサイズチェックしているのこっちではnullチェックしない
				foreach (float val in frameRate[ix])
				{
					if ((val >= minFps) && (val <= maxFps))
					{
						result = true;
						break;
					}
				}

				return result;
			}

			/**
			 * 指定した解像度の対応しているかどうかを取得
			 * @param width
			 * @param height
			 * @param minFps(default=0.1f)
			 * @param maxFps(default=121.0f)
			 */
			public bool IsSupported(int width, int height, float minFps = 0.1f, float maxFps = 121.0f)
			{
				var str = $"{width}x{height}";
				bool result = false;

				if (GetNumSize() > 0)	// ヌルポ避け
				{
					var numframeRates = frameRate.Length;
					int i = 0;

					foreach (string item in this.size)
					{
						if (i >= numframeRates)
						{
							break;
						}
						if ((item == str) && IsSupported(i, minFps, maxFps))
						{
							result = true;
							break;
						}
						i++;
					}
				}

				return result;
			}
		}

		/**
		 * Size用の列挙子
		 */
		public class SizeEnumerator : IEnumerator
		{
			private Size[] sizes;
			private int position = -1;

			public SizeEnumerator(FrameFormat format)
			{
				int n = format.GetNumSize();
				sizes = new Size[n];
				if (n > 0)
				{
					var numframeRates = format.frameRate.Length;
					int i = 0;

					foreach (string item in format.size)
					{
						if (i >= numframeRates)
						{
							break;
						}
						string[] sz = item.Split('x');
						sizes[i] = new Size(format.frame_type, int.Parse(sz[0]), int.Parse(sz[1]), format.frameRate[i]);
						i++;
					}
				}
			}
	
			public bool MoveNext()
			{
				position++;
				return (position < sizes.Length);
			}

			public void Reset()
			{
				position = -1;
			}

			object IEnumerator.Current
			{
				get
				{
					return Current;
				}
			}
			public Size Current
			{
				get
				{
					try
					{
						return sizes[position];
					}
					catch (IndexOutOfRangeException)
					{
						throw new InvalidOperationException();
					}
				}
			}
		}

		public FrameFormat[] formats { get; set; }

		/**
		 * JSON文字列として引き渡された対応解像度をパースしてSupportedFormatsとして返す
		 * @param jsonString
		 * @throws ArgumentException
		 */
		public static SupportedFormats Parse(string jsonString)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"SupportedFormats:{jsonString}");
#endif
			SupportedFormats result;
			try
			{
				var elements = JsonDocument.Parse(jsonString).RootElement.GetProperty("formats");
				result = new SupportedFormats();
				if (elements.GetArrayLength() > 0)
				{
					result.formats = new FrameFormat[elements.GetArrayLength()];
					int i = 0;
					foreach (var element in elements.EnumerateArray())
					{
						result.formats[i++] = new FrameFormat(element);
					}
				}
			}
			catch (JsonException e)
			{
				result = null;
				Debug.LogError(e.ToString());
			}

			if (result == null)
			{
				throw new ArgumentException($"failed to parse ({jsonString})");
			}
			return result;
		}

		public SupportedFormats()
		{

		}

		/**
		 * 指定した解像度に対応しているかどうかを確認して対応していればFrameFormatを返す
		 * 対応していなければnullを返す
		 * @param width
		 * @param height
		 * @param minFps(default=0.1f)
		 * @param maxFps(default=121.0f)
		 * @return
		 */
		public FrameFormat IsSupported(int width, int height, float minFps = 0.1f, float maxFps = 121.0f)
		{
			FrameFormat result = null;

			if (formats != null)
			{
				foreach (FrameFormat format in formats)
				{
					if (format.IsSupported(width, height, minFps, maxFps))
					{
						result = format;
						break;
					}
				}
			}

			return result;
		}

		/**
		 * 指定した解像度に対応しているSizeを返す
		 * 対応していなければnullを返す
		 * FIXME 今はフレームレートは無視する
		 * @param width
		 * @param height
		 * @param minFps(default=0.1f)
		 * @param maxFps(default=121.0f)
		 * @return
		 */
		public Size Find(int width, int height, float minFps = 0.1f, float maxFps = 121.0f)
		{
			if (formats != null)
			{
				foreach (FrameFormat format in formats)
				{
					var items = format.GetEnumerator();
					while (items.MoveNext())
					{
						Size sz = items.Current;
						if ((sz.Width == width) && (sz.Height == height))
						{
							foreach (float val in sz.FrameRate)
							{
								if ((val >= minFps) && (val <= maxFps))
								{
									return sz;
								}
							}
						}
					}
				}
			}

			return null;
		}
	
	}   // class SupportedFormats
}   // namespace Serenegiant.UVC