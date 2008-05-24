using System;
using System.Web;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;

namespace Calyptus.ResourceManager
{
	public class HttpHandlerURLFactory : IResourceURLFactory
	{
		private Dictionary<IResource, string> _urlCache;
		private ReaderWriterLock _lock;

		public HttpHandlerURLFactory()
		{
			_urlCache = new Dictionary<IResource, string>();
			_lock = new ReaderWriterLock();
		}

		private string ToHex(byte[] bytes)
		{
			if (bytes == null) return null;
			StringBuilder sb = new StringBuilder();
			foreach (byte b in bytes)
				sb.AppendFormat("{0:x1}", b);
			return sb.ToString();
		}

		public string GetURL(IResource resource)
		{
			string url;
			_lock.AcquireReaderLock(3000);
			try
			{
				if (!_urlCache.TryGetValue(resource, out url))
				{
					var c = _lock.UpgradeToWriterLock(3000);
					try
					{
						_urlCache.Add(resource, url = GetURLInternal(resource));
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
			return url;
		}

		private string GetURLInternal(IResource resource)
		{
			string AppPath = HttpContext.Current.Request.ApplicationPath;
			if (AppPath == "/") AppPath = null;
			IResourceLocation location = resource.Location;
			string version = ToHex(resource.Version);
			string path;
			if (location is TypeLocation)
			{
				TypeLocation tl = location as TypeLocation;
				path = String.Format("{2}/Assemblies/{0}/{1}.res.axd", HttpUtility.UrlPathEncode(tl.ProxyType.Assembly.GetName().Name), HttpUtility.UrlPathEncode(tl.ProxyType.FullName), AppPath);
			}
			else if (location is EmbeddedLocation)
			{
				EmbeddedLocation el = location as EmbeddedLocation;
				path = String.Format("{2}/Assemblies/{0}/{1}.res.axd", HttpUtility.UrlPathEncode(el.Assembly.GetName().Name), HttpUtility.UrlPathEncode(el.ResourceName), AppPath);
			}
			else if (location is VirtualPathLocation)
			{
				VirtualPathLocation vl = location as VirtualPathLocation;
				path = String.Format(resource is IProxyResource ? "{0}.res.axd" : "{0}", HttpUtility.UrlPathEncode(vl.VirtualPath));
			}
			else
				throw new Exception("Unknown IResourceLocationType");
			return String.Format("{0}?{1}", path, version);
		}
	}
}
