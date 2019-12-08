#define ENABLE_LOG

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchHandler : MonoBehaviour
{
	private TouchEventManager manager = new TouchEventManager();

	// Start is called before the first frame update
	void Start()
    {
        
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
		}
	}
}
