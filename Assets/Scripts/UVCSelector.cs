//#define ENABLE_LOG

using System;
using UnityEngine;
using Serenegiant.UVC;

public class UVCSelector : MonoBehaviour, IUVCSelector
{
	// Start is called before the first frame update
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public bool CanSelect(UVCInfo info)
	{
		return true;
	}

	public SupportedFormats.Size SelectSize(UVCInfo info, SupportedFormats formats)
	{
		if (IsTHETA_V(info))
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine("SelectSize:THETA V");
#endif
			return FindSize(formats, 3840, 1920);
		} else if (IsTHETA_S(info))
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine("SelectSize:THETA S");
#endif
			return FindSize(formats, 1920, 1080);
		} else {
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"SelectSize:other UVC device,{info}");
#endif
			return null;
		}
	}

	private bool IsTHETA_S(UVCInfo info)
	{
		return (info.vid == 1482) && (info.pid == 10001);
	}

	private bool IsTHETA_V(UVCInfo info)
	{
		// THETA Vからのpid=872は動かない
		return (info.vid == 1482) && (info.pid == 10002);
	}

	private SupportedFormats.Size FindSize(SupportedFormats formats, int width, int height)
	{
		return formats.Find(width, height);
	}
}
