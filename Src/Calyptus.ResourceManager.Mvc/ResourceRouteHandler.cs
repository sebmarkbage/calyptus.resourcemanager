using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Routing;
using System.Web;

namespace Calyptus.ResourceManager
{
	class ResourceRouteHandler : IRouteHandler
	{
		public IHttpHandler GetHttpHandler(RequestContext requestContext)
		{
			return new RoutingResourceHttpHandler(requestContext);
		}
	}
}
