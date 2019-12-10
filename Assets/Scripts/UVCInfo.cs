//#define ENABLE_LOG

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/*
 * THETA V
 * {"vid":1482,"pid":872}
 */

namespace Serenegiant.UVC
{

	public class UVCInfo
	{
		[JsonPropertyName("vid")]
		public int vid { get; set; }
		[JsonPropertyName("pid")]
		public int pid { get; set; }

		public static UVCInfo Parse(string jsonString)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"UVCInfo:{jsonString}");
#endif
			UVCInfo result;
			try
			{
				result = JsonSerializer.Deserialize<UVCInfo>(jsonString);
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

		public UVCInfo()
		{

		}

		public override string ToString()
		{
			return $"{base.ToString()}(vid={vid},pid={pid})";
		}

	} // UVCInfo

} // namespace Serenegiant.UVC

