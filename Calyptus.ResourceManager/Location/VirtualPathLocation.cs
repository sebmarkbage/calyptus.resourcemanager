using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Web;
using System.Web.Hosting;
using System.Reflection;

namespace Calyptus.ResourceManager
{
	public class VirtualPathLocation : FileLocation
	{
		public VirtualPathLocation(string absolutePath)
		{
			VirtualPath = VirtualPathUtility.ToAbsolute(absolutePath);
		}

		public VirtualPathLocation(string basePath, string relativePath)
		{
			VirtualPath = VirtualPathUtility.Combine(basePath, relativePath);
			VirtualPath = VirtualPathUtility.ToAbsolute(VirtualPath);
		}

		private static List<VirtualPathLocation> _monitoredPaths;

		private static void MonitorFileChanges(VirtualPathLocation location)
		{
			if (_monitoredPaths == null) _monitoredPaths = new List<VirtualPathLocation>();

			bool alreadyMonitored = _monitoredPaths.Contains(location);
			_monitoredPaths.Add(location);

			if (alreadyMonitored) return;

			VirtualPathProvider vp = HostingEnvironment.VirtualPathProvider;
			System.Web.Caching.CacheDependency cd = vp.GetCacheDependency(location.VirtualPath, new string[] { location.VirtualPath }, DateTime.UtcNow);
			HostingEnvironment.Cache.Add("VirtualPathMonitor:" + location.VirtualPath, 0, cd, System.Web.Caching.Cache.NoAbsoluteExpiration, System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.NotRemovable, (k, v, r) =>
			{
				if (_monitoredPaths == null) return;
				int i = 0;
				while (i < _monitoredPaths.Count)
				{
					VirtualPathLocation l = _monitoredPaths[i];
					if (l.Equals(location))
					{
						l.OnChanged();
						_monitoredPaths.RemoveAt(i);
					}
					else
						i++;
				}
				if (_monitoredPaths.Count == 0) _monitoredPaths = null;
			});
		}

		public string VirtualPath
		{
			get;
			private set;
		}

		protected VirtualFile VirtualFile
		{
			get
			{
				VirtualPathProvider vp = HostingEnvironment.VirtualPathProvider;
				if (!vp.FileExists(VirtualPath))
					return null;
				return vp.GetFile(VirtualPath);
			}
		}

		public override Stream GetStream()
		{
			System.Web.Hosting.VirtualFile vf = VirtualFile;
			if (vf == null) return null;

			return vf.Open();
		}

		public override string FileName
		{
			get { return System.Web.VirtualPathUtility.GetFileName(VirtualPath); }
		}

		public override bool Equals(object obj)
		{
			VirtualPathLocation l = obj as VirtualPathLocation;
			if (l == null) return false;
			return l.VirtualPath.Equals(VirtualPath, StringComparison.InvariantCultureIgnoreCase);
		}

		public override int GetHashCode()
		{
			return VirtualPath.ToLower().GetHashCode();
		}

		public override string ToString()
		{
			return VirtualPath;
		}

		public override void MonitorChanges(Action onChange)
		{
			MonitorFileChanges(this);
			base.MonitorChanges(onChange);
		}
	}
}
