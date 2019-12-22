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
	public class UVCDevice
	{
		public readonly string deviceName;
		public readonly int vid;
		public readonly int pid;
		public readonly string name;

		public static UVCDevice Parse(string deviceName, string jsonString)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine($"UVCInfo:{jsonString}");
#endif
			UVCDevice result;
			try
			{
				var element = JsonDocument.Parse(jsonString).RootElement;
				JsonElement v;
				string name;
				if (element.TryGetProperty("name", out v))
				{
					name = v.GetString();
				} else
				{
					name = null;
				}

				result = new UVCDevice(
					deviceName,
					element.GetProperty("vid").GetInt32(),
					element.GetProperty("pid").GetInt32(),
					name);
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

		public UVCDevice(string deviceName, int vid, int pid, string name)
		{
			this.deviceName = deviceName;
			this.vid = vid;
			this.pid = pid;
			this.name = name;
		}

		public override string ToString()
		{
			return $"{base.ToString()}(deviceName={deviceName},vid={vid},pid={pid},name={name})";
		}


		/**
		 * Ricohの製品かどうか
		 * @param info
		 */
		public bool IsRicoh
		{
			get { return (vid == 1482); }
		}

		/**
		 * THETA Sかどうか
		 * @param info
		 */
		public bool IsTHETA_S
		{
			get { return (vid == 1482) && (pid == 10001); }
		}

		/**
		 * THETA Vかどうか
		 * @param info
		 */
		public bool IsTHETA_V
		{
			// THETA Vからのpid=872は動かない
			get { return (vid == 1482) && (pid == 10002); }
		}
	} // UVCDevice

} // namespace Serenegiant.UVC

