using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Serenegiant.UVC
{

	public interface IUVCEventHandler : IEventSystemHandler
	{
		void OnEventAttach(string args);
		void OnEventPermission(string args);
		void OnEventConnect(string args);
		void OnEventDisconnect(string args);
		void OnEventDetach(string args);
		void OnEventReady(string args);
		void OnStartPreview(string args);
		void OnStopPreview(string args);

	}   // IUVCEventHandler

}	// namespace Serenegiant.UVC