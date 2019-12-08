#define ENABLE_LOG

using System;
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
		public class FrameFormat
		{
			[JsonPropertyName("frame_type")]
			public int frameType { get; set; }
			[JsonPropertyName("default")]
			public int defaultIndex { get; set; }
			[JsonPropertyName("size")]
			public string[] size { get; set; }
			[JsonPropertyName("frameRate")]
			public float[][] frameRates { get; set; }

			/**
			 * 対応解像度の個数を取得
			 */
			public int GetNumSize()
			{
				return Math.Min(size.Length, frameRates.Length);
			}

			/**
			 * 指定したインデックスの解像度のフレームレートが指定範囲内かどうかをチェック
			 */
			private bool IsSupported(int ix, float minFps = 0.1f, float maxFps = 121.0f)
			{
				bool result = false;
		
				foreach (float val in frameRates[ix])
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
				var numframeRates = frameRates.Length;
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

				return result;
			}
		}

		[JsonPropertyName("formats")]
		public FrameFormat[] formats { get; set; }

		public static SupportedFormats parse(string jsonString)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"SupportedFormats:{jsonString}");
#endif
			SupportedFormats result = JsonSerializer.Deserialize<SupportedFormats>(jsonString);

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

			foreach (FrameFormat format in formats)
			{
				if (format.IsSupported(width, height, minFps, maxFps))
				{
					result = format;
					break;
				}
			}

			return result;
		}
	}

}