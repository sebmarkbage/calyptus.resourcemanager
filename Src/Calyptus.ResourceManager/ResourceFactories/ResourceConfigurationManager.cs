using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Web;
using System.Threading;

namespace Calyptus.ResourceManager
{
	public class ResourceConfigurationManager : IResourceConfiguration
	{
		//private ReaderWriterLock _lock;
		private Action _resetAction;
		private IResourceFactory[] _resourceFactories;
		private Dictionary<IResourceLocation, IResource> _resourceCache;

		// TODO: Configurable

		protected ResourceConfigurationManager(IResourceFactory[] resourceFactories)
		{
			_resourceCache = new Dictionary<IResourceLocation, IResource>();
			_resourceFactories = resourceFactories;
			foreach (IResourceFactory factory in _resourceFactories)
				factory.Configuration = this;
			//_lock = new ReaderWriterLock();
			_resetAction = new Action(Reset);
		}

		protected ResourceConfigurationManager() : this(new IResourceFactory[] {
					new JavaScriptFactory(),
					new FileResourceFactory(),
					new LESSFactory(),
					new CSSFactory(),
					new ImageFactory(),
					new FlashFactory()
		}) { }

		public static IResourceConfiguration GetConfiguration()
		{
			ResourceConfigurationManager m = HttpRuntime.Cache["ResourceConfigurationManager"] as ResourceConfigurationManager;
			if (m == null)
			{
				m = new ResourceConfigurationManager();
				try
				{
					HttpContext context = HttpContext.Current;
					if (context != null)
						m.DebugMode = context.IsDebuggingEnabled;
				}
				catch { }
				HttpRuntime.Cache.Add("ResourceConfigurationManager", m, null, System.Web.Caching.Cache.NoAbsoluteExpiration, System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.BelowNormal, null);
			}
			return m;
		}

		protected virtual void Reset()
		{
			foreach (var key in _resourceCache.Keys) key.StopMonitorChanges(_resetAction);
			_resourceCache.Clear();
		}

		public IResource GetResource(IResourceLocation location)
		{
			if (location == null) return null;

			IResource res;
			//_lock.AcquireReaderLock(3000);
			try
			{
				if (!_resourceCache.TryGetValue(location, out res))
				{
					//var c = _lock.UpgradeToWriterLock(3000);
					try
					{
						_resourceCache.Add(location, null); // Adding it as null first prevents recursive reference errors
						res = GetResourcePrivate(location) ?? new UnknownResource(location);
						_resourceCache[location] = res;
						location.MonitorChanges(_resetAction);
					}
					finally
					{
						//_lock.DowngradeFromWriterLock(ref c);
					}
				}
			}
			finally
			{
				//_lock.ReleaseReaderLock();
			}
			if (res == null) throw new Exception(String.Format("The resource {0} contains recursive references. This is not supported.", location.ToString()));
			return res;
		}

		private IResource GetResourcePrivate(IResourceLocation location)
		{
			TypeLocation tl = location as TypeLocation;
			if (tl != null && typeof(IResource).IsAssignableFrom(tl.ProxyType))
			{
				ConstructorInfo c = tl.ProxyType.GetConstructor(new Type[] { typeof(IResourceConfiguration) });
				if (c != null)
					return (IResource)c.Invoke(new object[] { this });
				c = tl.ProxyType.GetConstructor(new Type[0]);
				if (c != null)
					return (IResource)c.Invoke(new object[0]);
			}
			foreach (IResourceFactory factory in _resourceFactories)
			{
				IResource res = factory.GetResource(location);
				if (res != null) return res;
			}
			if (tl != null)
			{
				IResourceLocation fr = FileResourceHelper.GetRelatedResourceLocation(tl);
				if (fr != null)
				{
					IResource res;
					if (!_resourceCache.TryGetValue(fr, out res))
						res = GetResourcePrivate(fr);
					return res;
				}
			}
			return null;
		}

		public bool DebugMode
		{
			get;
			protected set;
		}
	}
}
