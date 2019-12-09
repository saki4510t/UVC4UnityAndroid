#define ENABLE_LOG

using System;
using UnityEngine;

using Serenegiant.UVC.Android;

public class CubeHandler : MonoBehaviour
{
	private UVCController uvcController;
	private Transform targetTransform;

	// Start is called before the first frame update
	void Start()
	{
		uvcController = gameObject.GetComponent<UVCController>();
		targetTransform = gameObject.transform;
	}

	// Update is called once per frame
	void Update()
	{

	}

	void FixedUpdate()
	{
		targetTransform.Rotate(-0.2f, 0.1f, 0.05f);
	}

	/**
		* オブジェクトにタッチしたときの処理
		*/
	public void OnClick()
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine("OnClick:");
#endif
		if (uvcController != null)
		{
			uvcController.Toggle();
		}
	}
}
