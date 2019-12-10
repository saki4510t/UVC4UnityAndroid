using Serenegiant.UVC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
			return FindSize(formats, 3840, 1920);
		} else if (IsTHETA_S(info))
		{
			return FindSize(formats, 1920, 1080);
		} else {
			return null;
		}
	}

	private bool IsTHETA_S(UVCInfo info)
	{
		return (info.vid == 1482) && (info.pid == 10001);
	}

	private bool IsTHETA_V(UVCInfo info)
	{
		return (info.vid == 1482) && (info.pid == 872);
	}

	private SupportedFormats.Size FindSize(SupportedFormats formats, int width, int height)
	{
		return formats.Find(width, height);
	}
}
