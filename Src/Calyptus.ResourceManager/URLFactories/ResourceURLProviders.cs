using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Calyptus.ResourceManager
{
	public static class ResourceURLProvider
	{
		private static IResourceURLProvider provider;
		public static IResourceURLProvider GetURLProvider()
		{
			if (provider == null) provider = new HttpHandlerURLProvider();
			return provider;
		}
	}
}
