#define ENABLE_LOG

using System;
using UnityEngine;

using Serenegiant.UVC.Android;

namespace Serenegiant {
	public class CubeHandler : MonoBehaviour
	{
		private TouchEventManager manager = new TouchEventManager();
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

		void FixedUpdate()
		{
			targetTransform.Rotate(-0.2f, 0.1f, 0.05f);
		}
	}
} // namespace Serenegiant
