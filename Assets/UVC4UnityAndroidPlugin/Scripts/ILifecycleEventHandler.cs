using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Serenegiant
{
	public interface ILifecycleEventHandler : IEventSystemHandler
	{
		void OnResume();
		void OnPause();

	} // ILifecycleEventHandler

} // namespace Serenegiant
