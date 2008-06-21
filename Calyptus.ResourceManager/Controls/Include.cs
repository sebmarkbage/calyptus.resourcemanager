using System;
using System.Web.UI;
using System.Web;

namespace Calyptus.ResourceManager
{
	public class Include : Reference
	{
		public Include()
		{
			Compress = Compress.Release;
		}

		public Compress Compress { get; set; }

		protected override void RenderTag(HtmlTextWriter writer)
		{
			if (Manager.DebugMode) { base.RenderTag(writer); return; }

			if (Resource.References != null)
				foreach (IResource res in Resource.References)
					res.RenderReferenceTags(writer, UrlFactory, WrittenResources);

			IJavaScriptResource js = Resource as IJavaScriptResource;
			if (js != null)
			{
				writer.WriteLine("<script type=\"text/javascript\">");
				writer.WriteLine("//<![CDATA[");
				js.RenderJavaScript(writer, WrittenResources, Compress != Compress.Never);
				writer.WriteLine();
				writer.WriteLine("//]]>");
				writer.Write("</script>");
				return;
			}

			bool inclImages = !"IE".Equals(Context.Request.Browser.Browser, StringComparison.InvariantCultureIgnoreCase) || Context.Request.Browser.MajorVersion > 7;

			ICSSResource css = Resource as ICSSResource;
			if (css != null)
			{
				writer.WriteLine("<style type=\"text/css\">/*<![CDATA[*/");
				css.RenderCSS(writer, UrlFactory, WrittenResources, Compress != Compress.Never, inclImages);
				writer.WriteLine();
				writer.Write("/*]]>*/</style>");
				return;
			}

			IImageResource img = Resource as IImageResource;
			if (img != null)
			{
				if (inclImages)
				{
					writer.Write("<img src=\"");
					writer.WriteEncodedText(img.GetImageData(Compress != Compress.Never));
					writer.Write("\" alt=\"\" />");
				}
				else
					img.RenderReferenceTags(writer, UrlFactory, WrittenResources);
				return;
			}
			throw new Exception("Cannot include non JavaScript, CSS or Image resource on the page");
		}
	}
}
