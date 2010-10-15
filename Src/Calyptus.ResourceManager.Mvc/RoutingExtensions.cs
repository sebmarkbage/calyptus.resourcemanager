using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Routing;

namespace Calyptus.ResourceManager
{
	public static class RoutingExtensions
	{
		public static void MapResourceManager(this RouteCollection routes)
		{
			var defaults = new RouteValueDictionary();

			routes.Add(
				"ResourceManager",
				new MultiRoute(new RouteBase[] {

					new Route(
						"resources/assembly/{assembly}/{*name}",
						defaults,
						new ResourceRouteHandler()
					),

					new Route(
						"resources/{*name}",
						defaults,
						new ResourceRouteHandler()
					)

				})
			);
		}
	}
}
