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
			VerifyPath();
		}

		public VirtualPathLocation(string basePath, string relativePath)
		{
			VirtualPath = VirtualPathUtility.Combine(basePath, relativePath);
			VirtualPath = VirtualPathUtility.ToAbsolute(VirtualPath);
			VerifyPath();
		}

		private void VerifyPath()
		{
			VirtualPathProvider vp = HostingEnvironment.VirtualPathProvider;
			if (!vp.FileExists(VirtualPath) && !vp.DirectoryExists(VirtualPath)) throw new ArgumentException("The file or directory '" + VirtualPath + "' doesn't exist in the virtual path.");
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

		private static List<VirtualPathLocation> _monitoredPaths;

		public override void MonitorChanges(Action onChange)
		{
			if (_monitoredPaths == null) _monitoredPaths = new List<VirtualPathLocation>();

			bool alreadyMonitored = _monitoredPaths.Contains(this);
			_monitoredPaths.Add(this);

			if (!alreadyMonitored)
			{
				VirtualPathProvider vp = HostingEnvironment.VirtualPathProvider;
				System.Web.Caching.CacheDependency cd = vp.GetCacheDependency(this.VirtualPath, new string[] { this.VirtualPath }, DateTime.UtcNow);
				HostingEnvironment.Cache.Add("VirtualPathMonitor:" + this.VirtualPath, 0, cd, System.Web.Caching.Cache.NoAbsoluteExpiration, System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.NotRemovable, (k, v, r) =>
				{
					if (_monitoredPaths == null) return;
					int i = 0;
					while (i < _monitoredPaths.Count)
					{
						VirtualPathLocation l = _monitoredPaths[i];
						if (l.Equals(this))
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

			base.MonitorChanges(onChange);
		}

		public override IResourceLocation GetRelativeLocation(string name)
		{
			string path = VirtualPathUtility.Combine(VirtualPathUtility.GetDirectory(this.VirtualPath), name);
			if (HostingEnvironment.VirtualPathProvider.FileExists(path))
				return new VirtualPathLocation(path);
			foreach (Assembly r in System.Web.Compilation.BuildManager.GetReferencedAssemblies())
			{
				IResourceLocation l = ResourceLocations.GetAssemblyLocation(r, name);
				if (l != null) return l;
			}
			return null;
		}

		public override IEnumerable<IResourceLocation> GetRelativeLocations(string name)
		{
			List<IResourceLocation> ls = new List<IResourceLocation>();
			int i = name.LastIndexOf(System.IO.Path.PathSeparator);
			if (i < 0) i = 0;
			if (name.IndexOf('*', i) < 0)
			{
				IResourceLocation l = GetRelativeLocation(name);
				if (l != null) return new IResourceLocation[] { l };
			}
			else
			{
				string path = VirtualPathUtility.Combine(VirtualPathUtility.GetDirectory(this.VirtualPath), name.Substring(0, i)),
					filename = name.Substring(i);
				path = VirtualPathUtility.ToAbsolute(path);
				VirtualDirectory dir = HostingEnvironment.VirtualPathProvider.GetDirectory(path);
				foreach (VirtualFile file in dir.Files)
					if (ResourceLocations.WildCardMatch(file.Name, filename, StringComparison.OrdinalIgnoreCase))
						ls.Add(new VirtualPathLocation(file.VirtualPath));
			}
			foreach (Assembly r in System.Web.Compilation.BuildManager.GetReferencedAssemblies())
			{
				IEnumerable<IResourceLocation> l = ResourceLocations.GetAssemblyLocations(r, name);
				if (l != null)
					ls.AddRange(l);
			}
			return (ls.Count > 0) ? ls : null;
		}
	}
}
