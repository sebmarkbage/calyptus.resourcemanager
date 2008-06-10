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
		private IResourceURLFactory _urlFactory;
		private IResourceFactory[] _resourceFactories;
		private Dictionary<IResourceLocation, IResource> _resourceCache;

		// TODO: Configurable

		private ResourceConfigurationManager(IResourceURLFactory urlFactory, IResourceFactory[] resourceFactories)
		{
			_resourceCache = new Dictionary<IResourceLocation, IResource>();
			_urlFactory = urlFactory;
			_resourceFactories = resourceFactories;
			_lock = new ReaderWriterLock();
		}

		private ResourceConfigurationManager() : this(new HttpHandlerURLFactory(), new IResourceFactory[] {
					new JavaScriptFactory { FactoryManager = _m },
					new XMLResourceFactory { FactoryManager = _m },
					new CSSFactory { FactoryManager = _m },
					new ImageFactory { FactoryManager = _m }
		}) { }

		private static ResourceConfigurationManager _m;

		public static ResourceConfigurationManager GetFactoryManager()
		{
			if (_m == null)
				_m = new ResourceConfigurationManager();

			return _m;
		}

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

		public string GetURL(IResource resource)
		{
			return _urlFactory.GetURL(resource);
		}

		public IResource GetResource(IResourceLocation location)
		{
			IResource res;
			_lock.AcquireReaderLock(3000);
			try
			{
				if (!_resourceCache.TryGetValue(location, out res))
				{
					var c = _lock.UpgradeToWriterLock(3000);
					try
					{
						TypeLocation tl = location as TypeLocation;
						if (tl != null && typeof(IResource).IsAssignableFrom(tl.ProxyType))
							res = (IResource)System.Activator.CreateInstance(tl.ProxyType); // Precompiled resource
						else
							foreach (IResourceFactory factory in _resourceFactories)
							{
								res = factory.GetResource(location);
								if (res != null) break;
							}
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

		public bool DebugMode
		{
			get;
			private set;
		}
	}
}
