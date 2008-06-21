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

		public HttpHandlerURLFactory(string appPath)
		{
			AppPath = (appPath == "/") ? null : appPath;
			_urlCache = new Dictionary<IResource, string>();
			_lock = new ReaderWriterLock();
		}

		protected string AppPath { get; private set; }

		private string ToHex(byte[] bytes)
		{
			if (bytes == null) return null;
			StringBuilder sb = new StringBuilder();
			foreach (byte b in bytes)
				sb.AppendFormat("{0:x2}", b);
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
			IProxyResource pr = resource as IProxyResource;
			if (!(resource.Location is ExternalLocation) && pr != null && (pr.CultureSensitive || pr.CultureUISensitive))
			{
				url += "-";
				if (pr.CultureSensitive)
					url += CultureInfo.CurrentCulture.LCID.ToString("x");
				url += "-";
				if (pr.CultureUISensitive)
					url += CultureInfo.CurrentCulture.LCID.ToString("x");
			}
			return url;
		}

		private string GetURLInternal(IResource resource)
		{
			IResourceLocation location = resource.Location;
			string path;
			if (location is TypeLocation)
			{
				TypeLocation tl = location as TypeLocation;
				path = String.Format("{2}/{3}{0}/{1}{4}", HttpUtility.UrlPathEncode(tl.ProxyType.Assembly.GetName().Name), HttpUtility.UrlPathEncode(tl.ProxyType.FullName), AppPath, ResourceHttpHandler.AssemblyPath, ResourceHttpHandler.Extension);
			}
			else if (location is EmbeddedLocation)
			{
				EmbeddedLocation el = location as EmbeddedLocation;
				path = String.Format("{2}/{3}{0}/{1}{4}", HttpUtility.UrlPathEncode(el.Assembly.GetName().Name), HttpUtility.UrlPathEncode(el.ResourceName), AppPath, ResourceHttpHandler.AssemblyPath, ResourceHttpHandler.Extension);
			}
			else if (location is VirtualPathLocation)
			{
				VirtualPathLocation vl = location as VirtualPathLocation;
				path = HttpUtility.UrlPathEncode(vl.VirtualPath);
				if (resource is IProxyResource) path += ResourceHttpHandler.Extension;
			}
			else if (location is ExternalLocation)
			{
				ExternalLocation el = location as ExternalLocation;
				return el.Uri.ToString();
			}
			else
				throw new Exception("Unknown IResourceLocationType");
			return path + "?" + ToHex(resource.Version);
		}
	}
}
