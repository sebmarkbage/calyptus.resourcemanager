using System;
using System.Web.UI;
using System.IO;

namespace Calyptus.ResourceManager
{
	public class Build : Include
	{
		protected override void RenderTag(HtmlTextWriter writer)
		{
			if (Manager.DebugMode) { base.RenderTag(writer); return; }

			IJavaScriptResource js = Resource as IJavaScriptResource;
			if (js != null)
			{
				BuildReferences<IJavaScriptResource>(Resource, writer);

				writer.WriteLine("<script type=\"text/javascript\">");
				writer.WriteLine("//<![CDATA[");
				RenderBuild(js, writer);
				writer.WriteLine();
				writer.WriteLine("//]]>");
				writer.Write("</script>");
				return;
			}

			bool inclImages = !"IE".Equals(Context.Request.Browser.Browser, StringComparison.InvariantCultureIgnoreCase) || Context.Request.Browser.MajorVersion > 7;

			ICSSResource css = Resource as ICSSResource;
			if (css != null)
			{
				BuildReferences<ICSSResource>(Resource, writer);

				writer.WriteLine("<style type=\"text/css\">/*<![CDATA[*/");
				RenderBuild(css, writer, inclImages);
				writer.WriteLine();
				writer.Write("/*]]>*/</style>");
				return;
			}

			throw new Exception("Cannot build non JavaScript or CSS resource on the page");
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

		private void RenderBuild(IJavaScriptResource resource, TextWriter writer)
		{
			if (resource.References != null)
				foreach (IResource res in resource.References)
					{
						IJavaScriptResource js = res as IJavaScriptResource;
						if (js != null)
							RenderBuild(js, writer);
					}
			resource.RenderJavaScript(writer, WrittenResources, Compress != Compress.Never);
		}

		private void RenderBuild(ICSSResource resource, TextWriter writer, bool inclImages)
		{
			if (resource.References != null)
				foreach (IResource res in resource.References)
					{
						ICSSResource css = res as ICSSResource;
						if (css != null)
							RenderBuild(css, writer, inclImages);
					}
			resource.RenderCSS(writer, UrlFactory, WrittenResources, Compress != Compress.Never, inclImages);
		}
	}
}
