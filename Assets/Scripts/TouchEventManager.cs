//#define ENABLE_LOG

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchEventManager
{

	public class TouchEvent {
		public bool touched;
		public Vector2 touch_position;
		public TouchPhase touch_phase;

		/**
		 * コンストラクタ
		 * @param touched
		 * @param position
		 * @param phase
		 */
		public TouchEvent(bool touched = false, Vector2? position = null, TouchPhase phase = TouchPhase.Began)
		{
			this.touched = touched;
			if (position == null)
			{
				this.touch_position = new Vector2(0, 0);
			}
			else
			{
				this.touch_position = (Vector2)position;
			}
			this.touch_phase = phase;
		}

		/**
		 * コピーコンストラクタ
		 * @param other
		 */
		public TouchEvent(TouchEvent other)
		{
			this.touched = other.touched;
			this.touch_position = new Vector2(other.touch_position.x, other.touch_position.y);
			this.touch_phase = other.touch_phase;
		}
	}

	public TouchEvent touchEvent = new TouchEvent();

	/**
	 * デフォルトコンストラクタ
	 */
	public TouchEventManager()
	{
	}

	/**
	 * タッチ状態の更新
	 * MonoBehaviourの下位クラスのUpdateから呼ぶこと
	 */
	public void Update()
    {
		touchEvent.touched = false;

		if (Application.isEditor)
		{	// エディタで実行中…マウスの状態でタッチイベントをシミュレートする
			if (Input.GetMouseButtonDown(0))
			{   // タッチしたとき
				touchEvent.touched = true;
				touchEvent.touch_phase = TouchPhase.Began;
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine("タッチした:");
#endif
			}

			if (Input.GetMouseButtonUp(0))
			{   // 離したとき
				touchEvent.touched = true;
				touchEvent.touch_phase = TouchPhase.Ended;
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine("タッチした:");
#endif
			}

			if (Input.GetMouseButton(0))
			{   // 押し続けているとき
				touchEvent.touched = true;
				touchEvent.touch_phase = TouchPhase.Moved;
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine("押し続けている");
#endif
			}

			if (touchEvent.touched)
			{	// タッチイベントがあるときは座標を取得
				touchEvent.touch_position = Input.mousePosition;
			}
		}
		else
		{	// 実機で実行中
			if (Input.touchCount > 0)
			{
				Touch touch = Input.GetTouch(0);
				touchEvent.touch_position = touch.position;
				touchEvent.touch_phase = touch.phase;
				touchEvent.touched = true;
			}
		}
	}

	/**
	 * 現在のタッチ状態を取得
	 */
	public TouchEvent GetTouch()
	{
		return new TouchEvent(touchEvent);
	}
}
