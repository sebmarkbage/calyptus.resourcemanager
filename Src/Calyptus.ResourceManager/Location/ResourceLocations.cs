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
		private static Regex isUrl = new Regex(@"(http|https|file|ftp)\://", RegexOptions.IgnoreCase);

		public static IEnumerable<IResourceLocation> GetLocations(IResourceLocation baseLocation, string assembly, string name)
		{
			if (assembly != null)
			{
				Assembly a = Assembly.Load(assembly);
				return GetAssemblyLocations(a, name);
			}
			if (isUrl.IsMatch(name))
			{
				return new IResourceLocation[] { new ExternalLocation(name) };
			}
			return baseLocation.GetRelativeLocations(name);
		}

		public static IResourceLocation GetLocation(IResourceLocation baseLocation, string assembly, string name)
		{
			if (assembly != null)
			{
				Assembly a = Assembly.Load(assembly);
				return GetAssemblyLocation(a, name);
			}
			if (isUrl.IsMatch(name))
			{
				return new ExternalLocation(name);
			}
			return baseLocation.GetRelativeLocation(name);
		}

		public static IEnumerable<IResourceLocation> GetAssemblyLocations(Assembly assembly, string name)
		{
			if (name.Contains("*"))
				return GetWildCardLocations(assembly, name);

			IResourceLocation res = GetAssemblyLocation(assembly, name);
			return res == null ? new IResourceLocation[0] : new IResourceLocation[] { res };
		}

		public static IResourceLocation GetAssemblyLocation(Assembly assembly, string name)
		{
			Type type = assembly.GetType(name, false, false);
			if (type != null)
				return new TypeLocation(type);
			else if (assembly.GetManifestResourceInfo(name) != null)
				return new EmbeddedLocation(assembly, name);
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

		public static bool WildCardMatch(string str, string filter, StringComparison comparisonType)
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
