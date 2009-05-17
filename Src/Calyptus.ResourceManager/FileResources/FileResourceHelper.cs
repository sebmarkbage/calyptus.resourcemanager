using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Calyptus.ResourceManager
{
	public static class FileResourceHelper
	{
		public static IResourceLocation GetRelatedResourceLocation(IResourceLocation location)
		{
			EmbeddedLocation el = location as EmbeddedLocation;
			if (el != null)
			{
				string fn = el.ResourceName + ".resources";
				if (ResourcesExists(el.Assembly, fn)) return new EmbeddedLocation(el.Assembly, fn);
				int i = el.ResourceName.LastIndexOf('.');
				if (i > 0)
				{
					fn = el.ResourceName.Substring(0, i) + ".resources";
					if (ResourcesExists(el.Assembly, fn)) return new EmbeddedLocation(el.Assembly, fn);
				}
				return null;
			}

			TypeLocation tl = location as TypeLocation;
			if (tl != null)
			{
				string fn = tl.ProxyType.FullName + ".resources";
				if (ResourcesExists(tl.ProxyType.Assembly, fn)) return new EmbeddedLocation(tl.ProxyType.Assembly, fn);
			}

			return null;
		}

		private static bool ResourcesExists(Assembly assembly, string name)
		{
			return assembly.GetManifestResourceInfo(name) != null;
		}
	}
}
