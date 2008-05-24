using System;
using System.Web.UI;

namespace Calyptus.ResourceManager
{
	public class Include : ResourceControl
	{
		public Include()
		{
			Compress = Compress.Release;
		}

		public string Src { get { return Name; } set { Name = value; } }

		public string Name { get; set; }
		public string Assembly { get; set; }
		//public string Type { get; set; }
		public Compress Compress { get; set; }

		protected override IResourceLocation Location
		{
			get
			{
				return LocationHelper.GetLocation(BaseLocation, Assembly, Name);
			}
		}

		protected override void RenderTag(HtmlTextWriter writer)
		{
			if (Resource.References != null)
				foreach (IResource res in Resource.References)
					res.RenderReferenceTags(Manager, writer, WrittenResources);

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

			ICSSResource css = Resource as ICSSResource;
			if (css != null)
			{
				writer.WriteLine("<style type=\"text/css\">/*<![CDATA[*/");
				css.RenderCSS(writer, WrittenResources, Compress != Compress.Never);
				writer.WriteLine();
				writer.Write("/*]]>*/</style>");
				return;
			}
			throw new Exception("Cannot include non JavaScript or CSS resource on the page");
		}
	}
}
