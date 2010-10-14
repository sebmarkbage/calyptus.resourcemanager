using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Hosting;
using System.Web.Caching;

namespace Calyptus.ResourceManager
{
	internal class VirtualPathMonitor
	{
		private string path;
		private Guid id;
		private bool alreadyMonitored;
		private List<Action> callbacks;
		private int currentExecutionIndex;

		public VirtualPathMonitor(string path)
		{
			id = Guid.NewGuid();
			this.path = path;
			this.callbacks = new List<Action>();
			currentExecutionIndex = -1;
		}

		public void Subscribe(Action callback)
		{
			if (!alreadyMonitored) AddMonitor();
			if (!callbacks.Contains(callback)) callbacks.Add(callback);
		}

		private void AddMonitor()
		{
			VirtualPathProvider vp = HostingEnvironment.VirtualPathProvider;
			CacheDependency cd = vp.GetCacheDependency(path, new string[] { path }, DateTime.UtcNow);
			HostingEnvironment.Cache.Add(id.ToString(), 0, cd, System.Web.Caching.Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.NotRemovable, new CacheItemRemovedCallback(OnChange));
		}

		private void OnChange(string key, object value, CacheItemRemovedReason reason)
		{
			this.Fire();
			if (this.callbacks.Count > 0) AddMonitor();
			else alreadyMonitored = false;
		}

		public void Unsubscribe(Action callback)
		{
			int i = callbacks.IndexOf(callback);
			callbacks.RemoveAt(i);
			if (i <= currentExecutionIndex) currentExecutionIndex--;
			if (callbacks.Count == 0)
			{
				HostingEnvironment.Cache.Remove(id.ToString());
				alreadyMonitored = false;
			}
		}

		public void Fire()
		{
			lock (this)
			{
				try
				{
					for (currentExecutionIndex = 0; currentExecutionIndex < callbacks.Count; currentExecutionIndex++)
						callbacks[currentExecutionIndex]();
				}
				finally
				{
					currentExecutionIndex = -1;
				}
			}
		}

		public bool HasCallbacks
		{
			get
			{
				return callbacks.Count > 0;
			}
		}
	}
}
