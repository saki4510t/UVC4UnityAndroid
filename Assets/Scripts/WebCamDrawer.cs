using UnityEngine;
using System.Collections;

public class WebCamDrawer : MonoBehaviour
{
	public string deviceNameKeyword = "THETA"; // 使いたいデバイスに含まれる文字列を指定

	void Start()
	{
		StartStreaming();
	}

	void StartStreaming()
	{
		WebCamDevice device = new WebCamDevice();
		if (!FindDevice(ref device))
		{
			Debug.LogError("<" + deviceNameKeyword + ">を含むWebカメラが検出できませんでした。");
			return;
		}
		Debug.Log("find: device.name = " + device.name);
		WebCamTexture webcamTexture = new WebCamTexture(device.name);
		Material mat = GetTargetMaterial();
		if (mat == null)
		{
			return;
		}
		mat.mainTexture = webcamTexture;
		webcamTexture.Play();
	}

	bool FindDevice(ref WebCamDevice target)
	{
		WebCamDevice[] devices = WebCamTexture.devices;
		foreach (WebCamDevice device in devices)
		{
			Debug.Log("device.name = " + device.name);
			if (device.name.Contains(deviceNameKeyword))
			{
				target = device;
				return true;
			}
		}
		return false;
	}

	Material GetTargetMaterial()
	{
		Skybox skybox = GetComponent<Skybox>();
		if (skybox != null)
		{
			return skybox.material;
		}
		Renderer renderer = GetComponent<Renderer>();
		if (renderer != null)
		{
			return renderer.material;
		}
		Debug.LogError("no Renderer/Skybox components found.");
		return null;
	}
}
