using System;
using System.Collections.Generic;
using System.Text;

namespace Calyptus.ResourceManager
{
	public delegate void Action();
	public interface IResourceLocation
	{
		byte[] Version
		{
			get;
		}

		void MonitorChanges(Action onChanged);
		void StopMonitorChanges(Action onChanged);

		IResourceLocation GetRelativeLocation(string name);
		IEnumerable<IResourceLocation> GetRelativeLocations(string name);
	}
}
