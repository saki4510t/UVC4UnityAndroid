//#define ENABLE_LOG

using System;
using UnityEngine;
using Serenegiant.UVC;

public class UVCSelector : MonoBehaviour, IUVCSelector
{
	//// Start is called before the first frame update
	//void Start()
 //   {
        
 //   }

 //   // Update is called once per frame
 //   void Update()
 //   {
        
 //   }

	public bool CanSelect(UVCInfo info)
	{
		if (info.IsRicoh())
		{
			return info.IsTHETA_S() || info.IsTHETA_V();
		} else {
			return true;
		}
	}

	public SupportedFormats.Size SelectSize(UVCInfo info, SupportedFormats formats)
	{
		if (info.IsTHETA_V())
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine("SelectSize:THETA V");
#endif
			return FindSize(formats, 3840, 1920);
		} else if (info.IsTHETA_S())
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

	private SupportedFormats.Size FindSize(SupportedFormats formats, int width, int height)
	{
		return formats.Find(width, height);
	}
}
