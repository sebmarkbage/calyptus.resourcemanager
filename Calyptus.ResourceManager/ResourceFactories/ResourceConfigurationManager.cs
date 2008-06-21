using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Web;
using System.Threading;

namespace Calyptus.ResourceManager
{
	public class ResourceConfigurationManager
	{
		private ReaderWriterLock _lock;
		private IResourceURLProvider _urlProvider;
		private IResourceFactory[] _resourceFactories;
		private Dictionary<IResourceLocation, IResource> _resourceCache;

		// TODO: Configurable

		private ResourceConfigurationManager(IResourceURLProvider urlProvider, IResourceFactory[] resourceFactories)
		{
			_resourceCache = new Dictionary<IResourceLocation, IResource>();
			_urlProvider = urlProvider;
			_resourceFactories = resourceFactories;
			foreach (IResourceFactory factory in _resourceFactories)
				factory.FactoryManager = this;
			_lock = new ReaderWriterLock();
		}

		private ResourceConfigurationManager() : this(new HttpHandlerURLProvider(), new IResourceFactory[] {
					new JavaScriptFactory(),
					new FileResourceFactory(),
					new CSSFactory(),
					new ImageFactory()
		}) { }

		public static ResourceConfigurationManager GetFactoryManager(HttpContext context)
		{
			ResourceConfigurationManager m = context.Cache["ResourceConfigurationManager"] as ResourceConfigurationManager;
			if (m == null)
			{
				m = new ResourceConfigurationManager();
				m.DebugMode = context.IsDebuggingEnabled;
				context.Cache.Add("ResourceConfigurationManager", m, null, System.Web.Caching.Cache.NoAbsoluteExpiration, System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.BelowNormal, null);
			}
			return m;
		}

		public IResourceURLProvider URLProvider { get { return _urlProvider; } }

		public IResource GetResource(IResourceLocation location)
		{
			if (location == null) return null;

			IResource res;
			_lock.AcquireReaderLock(3000);
			try
			{
				if (!_resourceCache.TryGetValue(location, out res))
				{
					var c = _lock.UpgradeToWriterLock(3000);
					try
					{
						res = GetResourcePrivate(location) ?? new UnknownResource(location);
						_resourceCache.Add(location, res);
					}
					finally
					{
						_lock.DowngradeFromWriterLock(ref c);
					}
				}
			}
			finally
			{
				_lock.ReleaseReaderLock();
			}
			return res;
		}

		private IResource GetResourcePrivate(IResourceLocation location)
		{
			TypeLocation tl = location as TypeLocation;
			if (tl != null && typeof(IResource).IsAssignableFrom(tl.ProxyType))
				return (IResource)System.Activator.CreateInstance(tl.ProxyType); // Precompiled resource
			else
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
			private set;
		}
	}
}
