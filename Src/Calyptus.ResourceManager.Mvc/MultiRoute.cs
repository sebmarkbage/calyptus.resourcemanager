using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Routing;
using System.Web;

namespace Calyptus.ResourceManager
{
	class MultiRoute : RouteBase
	{
		private IEnumerable<RouteBase> routes;

		public MultiRoute(IEnumerable<RouteBase> routes)
		{
			this.routes = routes;
		}

		public override RouteData GetRouteData(HttpContextBase context)
		{
			foreach (var route in routes)
			{
				var data = route.GetRouteData(context);
				if (data != null) return data;
			}
			return null;
		}

		public override VirtualPathData GetVirtualPath(RequestContext context, RouteValueDictionary values)
		{
			foreach (var route in routes)
			{
				var data = route.GetVirtualPath(context, values);
				if (data != null) return data;
			}
			return null;
		}
	}
}
