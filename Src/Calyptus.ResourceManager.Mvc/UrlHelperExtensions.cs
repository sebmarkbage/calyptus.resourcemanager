using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web;

namespace Calyptus.ResourceManager
{
	public static class UrlHelperExtensions
	{
		public static string RouteResource(this UrlHelper helper, string src)
		{
			return RouteResource(helper, null, src);
		}

		public static string RouteResource(this UrlHelper helper, string assembly, string name)
		{
			var route = helper.RouteCollection["ResourceManager"];

			var baselocation = new VirtualPathLocation("~/");
			var location = ResourceLocations.GetLocation(baselocation, assembly, name);
			var config = ResourceConfigurationManager.GetConfiguration();
			var resource = config.GetResource(location);

			if (route == null) return ResourceURLProvider.GetURLProvider().GetURLFactory(HttpContext.Current).GetURL(resource);

			return RoutingResourceURLFactory.GetURL(route, helper.RequestContext, resource);
		}
	}
}
