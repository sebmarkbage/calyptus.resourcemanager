using System;
using System.Web.UI;
using System.Web;

namespace Calyptus.ResourceManager
{
	public class Include : Import
	{
		public Include()
		{
			Compress = Compress.Release;
		}

		public Compress Compress { get; set; }

		protected override void RenderTag(HtmlTextWriter writer, IResource resource)
		{
			bool c = Compress == Compress.Always || (!Manager.DebugMode && Compress != Compress.Never);
			if (Manager.DebugMode && !c) { base.RenderTag(writer, resource); return; }

			if (resource.References != null && (resource is IJavaScriptResource || resource is ICSSResource || resource is IImageResource))
				foreach (IResource res in resource.References)
					base.RenderTag(writer, res);

			IJavaScriptResource js = resource as IJavaScriptResource;
			if (js != null)
			{
				writer.WriteLine("<script type=\"text/javascript\">");
				writer.WriteLine("//<![CDATA[");
				js.RenderJavaScript(writer, WrittenResources, c);
				writer.WriteLine();
				writer.WriteLine("//]]>");
				writer.WriteLine("</script>");
				return;
			}

			bool inclImages = !"IE".Equals(Context.Request.Browser.Browser, StringComparison.InvariantCultureIgnoreCase) || Context.Request.Browser.MajorVersion > 7;

			ICSSResource css = resource as ICSSResource;
			if (css != null)
			{
				writer.WriteLine("<style type=\"text/css\">/*<![CDATA[*/");
				css.RenderCSS(writer, UrlFactory, WrittenResources, c, inclImages, null);
				writer.WriteLine();
				writer.Write("/*]]>*/</style>");
				return;
			}

			IImageResource img = resource as IImageResource;
			if (img != null)
			{
				if (inclImages)
				{
					writer.Write("<img src=\"");
					writer.WriteEncodedText(img.GetImageData(c));
					writer.Write("\" alt=\"\" />");
				}
				else
					base.RenderTag(writer, resource);
				return;
			}

			base.RenderTag(writer, resource);
		}
	}
}
