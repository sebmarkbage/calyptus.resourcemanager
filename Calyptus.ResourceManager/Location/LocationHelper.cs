using System;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Calyptus.ResourceManager
{
	public static class LocationHelper
	{
		public static IResourceLocation GetLocation(IResourceLocation baseLocation, string assembly, string name)
		{
			if (assembly != null)
			{
				Assembly a = Assembly.Load(assembly);
				return GetLocation(a, name);
			}
			Regex isUrl = new Regex(@"(http|https)\://", RegexOptions.IgnoreCase);
			if (isUrl.IsMatch(name))
			{
				return GetLocation(new Uri(name));
			}
			else if (baseLocation is EmbeddedLocation)
			{
				Assembly a = (baseLocation as EmbeddedLocation).Assembly;
				return GetLocation(a, name);
			}
			else if (baseLocation is TypeLocation)
			{
				Assembly a = (baseLocation as TypeLocation).ProxyType.Assembly;
				return GetLocation(a, name);
			}
			else if (baseLocation is VirtualPathLocation)
			{
				return GetLocation((baseLocation as VirtualPathLocation).VirtualPath, name);
			}
			else if (baseLocation is ExternalLocation)
			{
				return GetLocation((baseLocation as ExternalLocation).Uri, name);
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
			else
				return new EmbeddedLocation(assembly, name);
		}

		public static IResourceLocation GetLocation(string basePath, string relativePath)
		{
			return new VirtualPathLocation(basePath, relativePath);
		}
	}
}
