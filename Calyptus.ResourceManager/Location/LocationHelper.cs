using System;
using System.Text;
using System.Reflection;

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
			else
				throw new Exception("Unknown IResourceLocation");
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
