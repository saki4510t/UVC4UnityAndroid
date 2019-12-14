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

	[Serializable]
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


		/**
		 * Ricohの製品かどうか
		 * @param info
		 */
		public bool IsRicoh()
		{
			return (vid == 1482);
		}

		/**
		 * THETA Sかどうか
		 * @param info
		 */
		public bool IsTHETA_S()
		{
			return (vid == 1482) && (pid == 10001);
		}

		/**
		 * THETA Vかどうか
		 * @param info
		 */
		public bool IsTHETA_V()
		{
			// THETA Vからのpid=872は動かない
			return (vid == 1482) && (pid == 10002);
		}
	} // UVCInfo

} // namespace Serenegiant.UVC

