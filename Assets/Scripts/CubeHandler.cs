#define ENABLE_LOG

using System;
using UnityEngine;
using System.Linq;

using Serenegiant.UVC.Android;

public class CubeHandler : MonoBehaviour
{
	private UVCController uvcController;
	private Transform taregtTransform;
	private Vector3 force = new Vector3();

	private const float FACTOR = 1.0f;
	private const float DECAY_RATE = 0.90f;

	// Start is called before the first frame update
	void Start()
	{	
		uvcController = gameObject.GetComponent<UVCController>();
		taregtTransform = gameObject.transform;
	}

	// Update is called once per frame
	void Update()
	{
		// タッチ数取得
		int touchCount = Input.touches
			.Count(t => t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled);

		if (touchCount == 1)
		{
			Touch t = Input.touches.First();
			switch (t.phase)
			{
				case TouchPhase.Moved:
					// 移動量に応じて角度計算
					float xAngle = t.deltaPosition.y * FACTOR;
					float yAngle = -t.deltaPosition.x * FACTOR;

					force += new Vector3(xAngle, yAngle, 0.0f);
					break;
			}

		}
	}

	void FixedUpdate()
	{
		// 回転させる
		taregtTransform.Rotate(force * Time.deltaTime, Space.World);
		// 減衰させる
		force *= DECAY_RATE;
	}

	/**
		* オブジェクトにタッチしたときの処理
		*/
	public void OnClick()
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine("OnClick:");
#endif
		//if (uvcController != null)
		//{
		//	uvcController.Toggle();
		//}
	}
}
