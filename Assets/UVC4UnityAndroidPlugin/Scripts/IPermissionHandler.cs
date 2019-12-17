using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Serenegiant
{
	public interface IPermissionHandler : IEventSystemHandler
	{
		void OnGrant(string permission);
		void OnDeny(string permission);
		void OnDenyAndNeverAskAgain(string permission);

	} // IPermissionHandler

} // namespace Serenegiant