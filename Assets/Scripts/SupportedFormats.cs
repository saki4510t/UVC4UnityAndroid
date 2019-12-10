//#define ENABLE_LOG

using System;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

/*
{
	"formats":[
		{
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

	public class SupportedFormats
	{
		public class Size
		{
			public int Width;
			public int Height;
			public float[] FrameRate;

			public Size(int width, int height, float[]frameRate)
			{
				Width = width;
				Height = height;
				FrameRate = frameRate;
			}
		}

		public class FrameFormat : IEnumerable
		{
			[JsonPropertyName("frame_type")]
			public int frame_type { get; set; }
			[JsonPropertyName("default")]
			public int defaultIndex { get; set; }
			[JsonPropertyName("size")]
			public string[] size { get; set; }
			[JsonPropertyName("frameRate")]
			public float[][] frameRate { get; set; }

			/**
			 * 対応解像度の個数を取得
			 */
			public int GetNumSize()
			{
				return Math.Min(
					size != null ? size.Length : 0,
					frameRate != null ? frameRate.Length : 0);
			}

			/**
			 * Sizeの反復取得用の列挙子を取得
			 */
			IEnumerator IEnumerable.GetEnumerator()
			{
				return (IEnumerator)GetEnumerator();
			}

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
						sizes[i] = new Size(int.Parse(sz[0]), int.Parse(sz[1]), format.frameRate[i]);
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

		[JsonPropertyName("formats")]
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
				result = JsonSerializer.Deserialize<SupportedFormats>(jsonString);
			}
			catch (JsonException e)
			{
				throw new ArgumentException(e.ToString());
			}

			if (result == null)
			{
				throw new ArgumentException($"failed to parse ({jsonString})");
			}
			return result;
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
		public FrameFormat Find(int width, int height, float minFps = 0.1f, float maxFps = 121.0f)
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
	}   // class SupportedFormats
}   // namespace Serenegiant.UVC