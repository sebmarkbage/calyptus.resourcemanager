using System;
using System.Web.UI;
using System.IO;

namespace Calyptus.ResourceManager
{
	public class Build : Include
	{
		protected override void RenderTag(HtmlTextWriter writer, IResource resource)
		{
			bool c = Compress == Compress.Always || (!Manager.DebugMode && Compress != Compress.Never);
			if (Manager.DebugMode && !c) { base.RenderTag(writer, resource); return; }

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

			bool inclImages = !"IE".Equals(Context.Request.Browser.Browser, StringComparison.InvariantCultureIgnoreCase) || Context.Request.Browser.MajorVersion > 7;

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

			base.RenderTag(writer, resource);
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
			resource.RenderJavaScript(writer, WrittenResources, compress);
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
