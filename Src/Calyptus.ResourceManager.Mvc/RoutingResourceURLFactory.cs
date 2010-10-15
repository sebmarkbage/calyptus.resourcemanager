using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Routing;
using System.Web.Mvc;
using System.Globalization;

namespace Calyptus.ResourceManager
{
	class RoutingResourceURLFactory : IResourceURLFactory
	{
		private RouteBase route;
		private RequestContext context;

		public RoutingResourceURLFactory(RouteBase route, RequestContext context)
		{
			this.route = route;
			this.context = context;
		}

		public string GetURL(IResource resource)
		{
			return GetURL(route, context, resource);
		}

		internal static string GetURL(RouteBase route, RequestContext context, IResource resource)
		{
			var location = resource.Location;

			var values = new RouteValueDictionary();

			if (location is TypeLocation)
			{
				TypeLocation tl = location as TypeLocation;
				values.Add("assembly", tl.ProxyType.Assembly.GetName().Name);
				values.Add("name", tl.ProxyType.FullName);
			}
			else if (location is EmbeddedLocation)
			{
				EmbeddedLocation el = location as EmbeddedLocation;
				values.Add("assembly", el.Assembly.GetName().Name);
				values.Add("name", el.ResourceName);
			}
			else if (location is VirtualPathLocation)
			{
				VirtualPathLocation vl = location as VirtualPathLocation;
				if (!(resource is IProxyResource))
					return UrlHelper.GenerateContentUrl(vl.VirtualPath, context.HttpContext);
				var p = vl.VirtualPath;
				if (p[0] == '/') p = p.Substring(1);
				values.Add("name", p);
			}
			else if (location is ExternalLocation)
			{
				ExternalLocation el = location as ExternalLocation;
				return el.Uri.ToString();
			}
			else
				throw new Exception("Unknown IResourceLocationType");
			
			var pr = resource as IProxyResource;
			if (pr != null && (pr.CultureSensitive || pr.CultureUISensitive))
			{
				if (pr.CultureSensitive)
					values.Add("culture", CultureInfo.CurrentCulture.LCID.ToString("x"));
				if (pr.CultureUISensitive)
					values.Add("cultureUI", CultureInfo.CurrentUICulture.LCID.ToString("x"));
			}

			values.Add("version", ToHex(resource.Version));

			var virtualPath = route.GetVirtualPath(context, values);
			if (virtualPath == null) throw new Exception("Routing is incomplete.");

			var url = UrlHelper.GenerateContentUrl("~/" + virtualPath.VirtualPath, context.HttpContext);
			return url;
		}

		private static string ToHex(byte[] bytes)
		{
			if (bytes == null) return null;
			StringBuilder sb = new StringBuilder();
			foreach (byte b in bytes)
				sb.AppendFormat("{0:x2}", b);
			return sb.ToString();
		}
	}
}
