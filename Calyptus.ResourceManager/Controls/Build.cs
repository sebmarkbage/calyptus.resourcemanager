using System;
using System.Web.UI;
using System.IO;

namespace Calyptus.ResourceManager
{
	public class Build : Include
	{
		protected override void RenderTag(HtmlTextWriter writer)
		{
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

			ICSSResource css = Resource as ICSSResource;
			if (css != null)
			{
				BuildReferences<ICSSResource>(Resource, writer);

				writer.WriteLine("<style type=\"text/css\">/*<![CDATA[*/");
				RenderBuild(css, writer);
				writer.WriteLine();
				writer.Write("/*]]>*/</style>");
				return;
			}
			throw new Exception("Cannot include non JavaScript or CSS resource on the page");
		}

		private void BuildReferences<T>(IResource resource, TextWriter writer) where T : IResource
		{
			if (resource.References != null)
				foreach (IResource res in resource.References)
					if (res is T)
						BuildReferences<T>(res, writer);
					else
						res.RenderReferenceTags(Manager, writer, WrittenResources);
		}

		private void RenderBuild(IJavaScriptResource resource, TextWriter writer)
		{
			if (resource.References != null)
				foreach (IResource res in resource.References)
					{
						IJavaScriptResource js = res as IJavaScriptResource;
						if (js != null)
							RenderBuild(js, writer);
						else
							res.RenderReferenceTags(Manager, writer, WrittenResources);
					}
			resource.RenderJavaScript(writer, WrittenResources, Compress != Compress.Never);
		}

		private void RenderBuild(ICSSResource resource, TextWriter writer)
		{
			if (resource.References != null)
				foreach (IResource res in resource.References)
					{
						ICSSResource css = res as ICSSResource;
						if (css != null)
							RenderBuild(css, writer);
						else
							res.RenderReferenceTags(Manager, writer, WrittenResources);
					}
			resource.RenderCSS(writer, WrittenResources, Compress != Compress.Never);
		}
	}
}
