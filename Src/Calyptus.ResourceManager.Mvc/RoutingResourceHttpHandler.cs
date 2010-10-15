using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Routing;
using System.Web;
using System.Reflection;
using System.Globalization;

namespace Calyptus.ResourceManager
{
	public class RoutingResourceHttpHandler : IHttpHandler
	{
		private IResourceLocation location;
		private string version;
		private string culture;
		private string cultureUI;
		private IResourceURLFactory urlFactory;

		public RoutingResourceHttpHandler(RequestContext context)
		{
			var values = context.RouteData.Values;

			var assembly = (values["assembly"] as string) ?? context.HttpContext.Request.QueryString["assembly"];
			var path = ((values["name"] ?? values["path"]) as string) ?? context.HttpContext.Request.QueryString["name"];

			if (assembly != null)
				location = ResourceLocations.GetAssemblyLocation(Assembly.Load(assembly), path);
			else
				location = new VirtualPathLocation("~/", path);

			version = (context.RouteData.Values["version"] as string) ?? context.HttpContext.Request.QueryString["version"];
			culture = (context.RouteData.Values["culture"] as string) ?? context.HttpContext.Request.QueryString["culture"];
			cultureUI = (context.RouteData.Values["cultureUI"] as string) ?? context.HttpContext.Request.QueryString["cultureUI"];

			urlFactory = new RoutingResourceURLFactory(context.RouteData.Route, context);
		}
	
		public bool IsReusable
		{
			get { return true; }
		}

		private string ToHex(byte[] bytes)
		{
			if (bytes == null) return null;
			StringBuilder sb = new StringBuilder();
			foreach (byte b in bytes)
				sb.AppendFormat("{0:x2}", b);
			return sb.ToString();
		}

		public void ProcessRequest(HttpContext context)
		{
			var config = ResourceConfigurationManager.GetConfiguration();
			var resource = config.GetResource(location);


			if (!String.IsNullOrEmpty(culture))
				System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo(int.Parse(culture, NumberStyles.AllowHexSpecifier));

			if (!String.IsNullOrEmpty(cultureUI))
				System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo(int.Parse(cultureUI, NumberStyles.AllowHexSpecifier));

			string v = ToHex(resource.Version);
			if (v != version)
			{
				var response = context.Response;
				response.RedirectLocation = urlFactory.GetURL(resource);
				response.StatusCode = 301;
				return;
			}


			ResourceHttpHandler.ProcessRequest(context.Request, context.Response, urlFactory, resource, version);
		}
	}
}
