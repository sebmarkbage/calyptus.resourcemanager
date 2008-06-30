using System;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Web.Hosting;
using System.Web;

namespace Calyptus.ResourceManager
{
	public static class ResourceLocations
	{
		private static Regex isUrl = new Regex(@"(http|https)\://", RegexOptions.IgnoreCase);

		public static IEnumerable<IResourceLocation> GetLocations(IResourceLocation baseLocation, string name)
		{
			return GetLocations(baseLocation, name);
		}

		public static IEnumerable<IResourceLocation> GetLocations(IResourceLocation baseLocation, string assembly, string name)
		{
			if (assembly != null)
			{
				Assembly a = Assembly.Load(assembly);
				return GetLocations(a, name);
			}
			Regex isUrl = new Regex(@"(http|https)\://", RegexOptions.IgnoreCase);
			if (isUrl.IsMatch(name))
			{
				return GetLocations(new Uri(name));
			}
			else if (baseLocation is ExternalLocation)
			{
				return GetLocations((baseLocation as ExternalLocation).Uri, name);
			}
			else if (baseLocation is VirtualPathLocation)
			{
				List<IResourceLocation> ls = new List<IResourceLocation>(GetLocations((baseLocation as VirtualPathLocation).VirtualPath, name));
				if (ls.Count > 0 && !name.Contains("*")) return ls;
				foreach (Assembly r in System.Web.Compilation.BuildManager.GetReferencedAssemblies())
				{
					IEnumerable<IResourceLocation> l = GetLocations(r, name);
					if (l != null)
						ls.AddRange(l);
				}
				return (ls.Count > 0) ? ls : null;
			}
			else if (baseLocation is EmbeddedLocation || baseLocation is TypeLocation)
			{
				Assembly a = baseLocation is EmbeddedLocation ? (baseLocation as EmbeddedLocation).Assembly : (baseLocation as TypeLocation).ProxyType.Assembly;
				List<IResourceLocation> ls = new List<IResourceLocation>(GetLocations(a, name));
				if (ls.Count > 0 && !name.Contains("*")) return ls;
				foreach (AssemblyName r in a.GetReferencedAssemblies())
				{
					IEnumerable<IResourceLocation> l = GetLocations(Assembly.Load(r), name);
					if (l != null)
						ls.AddRange(l);
				}
				return (ls.Count > 0) ? ls : null;
			}
			else
				throw new Exception("Unknown IResourceLocation");
		}

		public static IEnumerable<IResourceLocation> GetLocations(Uri uri)
		{
			return new IResourceLocation[] { GetLocation(uri) };
		}

		public static IEnumerable<IResourceLocation> GetLocations(Uri uri, string relativePath)
		{
			return new IResourceLocation[] { GetLocation(uri, relativePath) };
		}

		public static IEnumerable<IResourceLocation> GetLocations(Assembly assembly, string name)
		{
			if (name.Contains("*"))
				return GetWildCardLocations(assembly, name);

			IResourceLocation res = GetLocation(assembly, name);
			return res == null ? new IResourceLocation[0] : new IResourceLocation[] { res };
		}

		public static IEnumerable<IResourceLocation> GetLocations(string basePath, string relativePath)
		{
			int i = relativePath.LastIndexOf(System.IO.Path.PathSeparator);
			if (i < 0) i = 0;
			if (relativePath.IndexOf('*', i) >= 0)
				return GetWildCardLocations(basePath, relativePath.Substring(0, i), relativePath.Substring(i));

			IResourceLocation res = GetLocation(basePath, relativePath);
			return res == null ? new IResourceLocation[0] : new IResourceLocation[] { res };
		}

		public static IResourceLocation GetLocation(IResourceLocation baseLocation, string name)
		{
			return GetLocation(baseLocation, null, name);
		}

		public static IResourceLocation GetLocation(IResourceLocation baseLocation, string assembly, string name)
		{
			if (assembly != null)
			{
				Assembly a = Assembly.Load(assembly);
				return GetLocation(a, name);
			}
			if (isUrl.IsMatch(name))
			{
				return GetLocation(new Uri(name));
			}
			else if (baseLocation is ExternalLocation)
			{
				return GetLocation((baseLocation as ExternalLocation).Uri, name);
			}
			else if (baseLocation is VirtualPathLocation)
			{
				IResourceLocation l = GetLocation((baseLocation as VirtualPathLocation).VirtualPath, name);
				if (l == null)
					foreach (Assembly r in System.Web.Compilation.BuildManager.GetReferencedAssemblies())
					{
						l = GetLocation(r, name);
						if (l != null) break;
					}
				return l;
			}
			else if (baseLocation is EmbeddedLocation || baseLocation is TypeLocation)
			{
				Assembly a = baseLocation is EmbeddedLocation ? (baseLocation as EmbeddedLocation).Assembly : (baseLocation as TypeLocation).ProxyType.Assembly;
				IResourceLocation l = GetLocation(a, name);
				if (l == null)
					foreach (AssemblyName r in a.GetReferencedAssemblies())
					{
						l = GetLocation(Assembly.Load(r), name);
						if (l != null) break;
					}
				return l;
			}
			else
				throw new Exception("Unknown IResourceLocation");
		}

		public static IResourceLocation GetLocation(Uri uri)
		{
			return new ExternalLocation(uri);
		}

		public static IResourceLocation GetLocation(Uri uri, string relativePath)
		{
			return new ExternalLocation(new Uri(uri, relativePath));
		}

		public static IResourceLocation GetLocation(Assembly assembly, string name)
		{
			Type type = assembly.GetType(name, false, false);
			if (type != null)
				return new TypeLocation(type);
			else if (assembly.GetManifestResourceInfo(name) != null)
				return new EmbeddedLocation(assembly, name);
			else
				return null;
		}

		public static IResourceLocation GetLocation(string basePath, string relativePath)
		{
			string path = VirtualPathUtility.Combine(basePath, relativePath);
			if (HostingEnvironment.VirtualPathProvider.FileExists(path))
				return new VirtualPathLocation(basePath, relativePath);
			else
				return null;
		}
		
		private static IEnumerable<IResourceLocation> GetWildCardLocations(Assembly assembly, string name)
		{
			if (name.Equals("*"))
			{
				foreach (Type t in assembly.GetExportedTypes())
					yield return new TypeLocation(t);
			}
			else if (name.EndsWith(".*"))
			{
				string ns = name.Substring(0, name.Length - 2);
				foreach (Type t in assembly.GetExportedTypes())
					if (t.Namespace.Equals(ns) || t.Namespace.StartsWith(ns))
						yield return new TypeLocation(t);
			}
			foreach (string resourceName in assembly.GetManifestResourceNames())
				if (WildCardMatch(resourceName, name, StringComparison.Ordinal))
					yield return new EmbeddedLocation(assembly, resourceName);
		}

		private static IEnumerable<IResourceLocation> GetWildCardLocations(string basePath, string relativePath, string filename)
		{
			string path = string.IsNullOrEmpty(relativePath) ? basePath : VirtualPathUtility.Combine(basePath, relativePath);
			path = VirtualPathUtility.ToAbsolute(path);
			VirtualDirectory dir = HostingEnvironment.VirtualPathProvider.GetDirectory(path);
			foreach (VirtualFile file in dir.Files)
				if (WildCardMatch(file.Name, filename, StringComparison.OrdinalIgnoreCase))
					yield return new VirtualPathLocation(basePath, VirtualPathUtility.Combine(path, file.Name));
		}

		private static bool WildCardMatch(string str, string filter, StringComparison comparisonType)
		{
			int si = 0;
			int fi = 0;
			while (si < str.Length && fi < filter.Length && filter[fi] != '*')
			{
				char fc = filter[fi];
				if (fc != '?' && !(new string(fc, 1)).Equals(new string(str[si], 1), comparisonType))
					return false;
				si++;
				fi++;
			}

			if (fi == filter.Length)
				return str.Length == filter.Length;

			int cp = 0;
			int mp = 0;
			while (si < str.Length)
			{
				if (fi < filter.Length && filter[fi] == '*')
				{
					if ((fi++) >= filter.Length)
						return true;
					mp = fi;
					cp = si + 1;
				}
				else if (fi < filter.Length && (filter[fi] == '?' || (new string(filter[fi], 1)).Equals(new string(str[si], 1), comparisonType)))
				{
					fi++;
					si++;
				}
				else
				{
					fi = mp;
					si = cp++;
				}
			}

			while (fi < filter.Length && filter[fi] == '*')
				fi++;
			return fi >= filter.Length;
		}
	}
}
