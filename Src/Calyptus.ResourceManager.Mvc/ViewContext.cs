using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Collections.ObjectModel;
using System.IO;
using System.Web.Routing;

namespace Calyptus.ResourceManager
{
	class ViewContext
	{
		RouteBase route;
		RequestContext requestContext;

		public ViewContext(RequestContext requestContext, RouteBase route)
		{
			this.requestContext = requestContext;
			this.route = route;
		}

		private IResourceConfiguration _config;
		public IResourceConfiguration Manager
		{
			get
			{
				if (_config == null)
					_config = ResourceConfigurationManager.GetConfiguration();
				return _config;
			}
		}

		private IResourceURLFactory _urlFactory;
		public virtual IResourceURLFactory UrlFactory
		{
			get
			{
				if (_urlFactory == null)
				{
					if (route == null) _urlFactory = ResourceURLProvider.GetURLProvider().GetURLFactory(HttpContext.Current);
					else _urlFactory = new RoutingResourceURLFactory(route, requestContext);
				}
				return _urlFactory;
			}
		}

		private ICollection<IResource> _writtenResources;
		public virtual ICollection<IResource> WrittenResources
		{
			get
			{
				if (_writtenResources == null) _writtenResources = new Collection<IResource>();
				return _writtenResources;
			}
		}


		public void RenderImportTag(TextWriter writer, IResource resource)
		{
			resource.RenderReferenceTags(writer, UrlFactory, WrittenResources);
		}

		public void RenderIncludeTag(TextWriter writer, IResource resource)
		{
			bool c = !Manager.DebugMode;
			if (Manager.DebugMode && !c) { RenderImportTag(writer, resource); return; }

			if (resource.References != null && (resource is IJavaScriptResource || resource is ICSSResource || resource is IImageResource))
				foreach (IResource res in resource.References)
					RenderImportTag(writer, res);

			var browser = requestContext.HttpContext.Request.Browser;

			bool inclImages = !"IE".Equals(browser.Browser, StringComparison.InvariantCultureIgnoreCase) || browser.MajorVersion > 7;

			IImageResource img = resource as IImageResource;
			if (img != null)
			{
				if (inclImages)
				{
					writer.Write("<img src=\"");
					writer.Write(HttpUtility.HtmlEncode(CSSUrlParser.GetBase64URL(img)));
					writer.Write("\" alt=\"\" />");
				}
				else
					RenderImportTag(writer, resource);
				return;
			}

			IJavaScriptResource js = resource as IJavaScriptResource;
			if (js != null)
			{
				writer.WriteLine("<script type=\"text/javascript\">");
				writer.WriteLine("//<![CDATA[");
				js.RenderJavaScript(writer, UrlFactory, WrittenResources, c);
				writer.WriteLine();
				writer.WriteLine("//]]>");
				writer.WriteLine("</script>");
				return;
			}

			ICSSResource css = resource as ICSSResource;
			if (css != null)
			{
				writer.WriteLine("<style type=\"text/css\">/*<![CDATA[*/");
				css.RenderCSS(writer, UrlFactory, WrittenResources, c, inclImages, null);
				writer.WriteLine();
				writer.Write("/*]]>*/</style>");
				return;
			}

			RenderImportTag(writer, resource);
		}

		public void RenderBuildTag(TextWriter writer, IResource resource)
		{
			bool c = !Manager.DebugMode;
			if (Manager.DebugMode && !c) { RenderImportTag(writer, resource); return; }

			IJavaScriptResource js = resource as IJavaScriptResource;
			if (js != null)
			{
				BuildReferences<IJavaScriptResource>(resource, writer);

				writer.WriteLine("<script type=\"text/javascript\">");
				writer.WriteLine("//<![CDATA[");
				RenderBuild(js, writer, c);
				writer.WriteLine();
				writer.WriteLine("//]]>");
				writer.Write("</script>");
				return;
			}

			var browser = requestContext.HttpContext.Request.Browser;

			bool inclImages = !"IE".Equals(browser.Browser, StringComparison.InvariantCultureIgnoreCase) || browser.MajorVersion > 7;

			ICSSResource css = resource as ICSSResource;
			if (css != null)
			{
				BuildReferences<ICSSResource>(resource, writer);

				writer.WriteLine("<style type=\"text/css\">/*<![CDATA[*/");
				RenderBuild(css, writer, inclImages, c);
				writer.WriteLine();
				writer.Write("/*]]>*/</style>");
				return;
			}

			RenderIncludeTag(writer, resource);
		}

		private void BuildReferences<T>(IResource resource, TextWriter writer) where T : IResource
		{
			if (resource.References != null)
				foreach (IResource res in resource.References)
					if (res is T)
						BuildReferences<T>(res, writer);
					else
						res.RenderReferenceTags(writer, UrlFactory, WrittenResources);
		}

		private void RenderBuild(IJavaScriptResource resource, TextWriter writer, bool compress)
		{
			if (resource.References != null)
				foreach (IResource res in resource.References)
				{
					IJavaScriptResource js = res as IJavaScriptResource;
					if (js != null)
						RenderBuild(js, writer, compress);
				}
			resource.RenderJavaScript(writer, UrlFactory, WrittenResources, compress);
		}

		private void RenderBuild(ICSSResource resource, TextWriter writer, bool inclImages, bool compress)
		{
			if (resource.References != null)
				foreach (IResource res in resource.References)
				{
					ICSSResource css = res as ICSSResource;
					if (css != null)
						RenderBuild(css, writer, inclImages, compress);
				}
			resource.RenderCSS(writer, UrlFactory, WrittenResources, compress, inclImages, null);
		}
	}
}
