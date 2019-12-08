#define ENABLE_LOG

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Serenegiant.UVC.Android;

namespace Serenegiant {
	public class CubeHandler : MonoBehaviour
	{
		private TouchEventManager manager = new TouchEventManager();
		private UVCController uvcController;

		// Start is called before the first frame update
		void Start()
		{
			uvcController = gameObject.GetComponent<UVCController>();
		}

		// Update is called once per frame
		void Update()
		{
			manager.Update();

			TouchEventManager.TouchEvent touch = manager.GetTouch();
			if (touch.state == TouchEventManager.TouchState.Began)
			{   // タッチした時
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine("タッチした:");
#endif
				if (uvcController != null)
				{
					uvcController.Toggle();
				}
			}
		}
	}
} // namespace Serenegiant
