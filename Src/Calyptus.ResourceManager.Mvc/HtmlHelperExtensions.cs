using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.IO;

namespace Calyptus.ResourceManager
{
	public static class HtmlHelperExtensions
	{
		private static string contextKey = "$ResourceMangerContext$";
		private static ViewContext GetContext(HtmlHelper helper)
		{
			var mgr = helper.ViewContext.TempData[contextKey] as ViewContext;
			if (mgr == null) helper.ViewContext.TempData[contextKey] = mgr = new ViewContext(helper.ViewContext.RequestContext, helper.RouteCollection["ResourceManager"]);
			return mgr;
		}

		public static MvcHtmlString Import(this HtmlHelper helper, string filename)
		{
			return Import(helper, null, filename);
		}
		public static MvcHtmlString Import(this HtmlHelper helper, string assemblyName, string resourceName)
		{
			var writer = new StringWriter();
			var context = GetContext(helper);
			foreach (IResource resource in GetResources(context, helper, assemblyName, resourceName))
				context.RenderImportTag(writer, resource);
			return MvcHtmlString.Create(writer.ToString());
		}

		public static MvcHtmlString Include(this HtmlHelper helper, string filename)
		{
			return Include(helper, null, filename);
		}
		public static MvcHtmlString Include(this HtmlHelper helper, string assemblyName, string resourceName)
		{
			var writer = new StringWriter();
			var context = GetContext(helper);
			foreach (IResource resource in GetResources(context, helper, assemblyName, resourceName))
				context.RenderIncludeTag(writer, resource);
			return MvcHtmlString.Create(writer.ToString());
		}

		public static MvcHtmlString Build(this HtmlHelper helper, string filename)
		{
			return Build(helper, null, filename);
		}
		public static MvcHtmlString Build(this HtmlHelper helper, string assemblyName, string resourceName)
		{
			var writer = new StringWriter();
			var context = GetContext(helper);
			foreach (IResource resource in GetResources(context, helper, assemblyName, resourceName))
				context.RenderBuildTag(writer, resource);
			return MvcHtmlString.Create(writer.ToString());
		}

		private static IEnumerable<IResourceLocation> GetLocations(HtmlHelper helper, string assemblyName, string resourceName)
		{
			var baselocation = new VirtualPathLocation("~/");
			return ResourceLocations.GetLocations(baselocation, assemblyName, resourceName);
		}

		private static IEnumerable<IResource> GetResources(ViewContext context, HtmlHelper helper, string assemblyName, string resourceName)
		{
			foreach (IResourceLocation l in GetLocations(helper, assemblyName, resourceName))
			{
				IResource res = context.Manager.GetResource(l);
				yield return res;
			}
		}
	}
}
