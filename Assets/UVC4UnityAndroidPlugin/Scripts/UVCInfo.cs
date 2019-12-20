//#define ENABLE_LOG

using System;
using System.Text.Json;

/*
 * THETA V
 * {"vid":1482,"pid":872}
 */

namespace Serenegiant.UVC
{

	[Serializable]
	public class UVCInfo
	{
		public readonly int vid;
		public readonly int pid;
		public readonly string name;

		public static UVCInfo Parse(string jsonString)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"UVCInfo:{jsonString}");
#endif
			UVCInfo result;
			try
			{
				var element = JsonDocument.Parse(jsonString).RootElement;
				result = new UVCInfo(
					element.GetProperty("vid").GetInt32(),
					element.GetProperty("pid").GetInt32(),
					null);
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

		public UVCInfo(int vid, int pid, string name)
		{
			this.vid = vid;
			this.pid = pid;
			this.name = name;
		}

		public override string ToString()
		{
			return $"{base.ToString()}(vid={vid},pid={pid},name={name})";
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

