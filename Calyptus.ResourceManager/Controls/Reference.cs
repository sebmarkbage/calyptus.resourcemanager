using System;
using System.Web.UI;

namespace Calyptus.ResourceManager
{
	public class Reference : ResourceControl
	{
		public string Src { get { return Name; } set { Name = value; } }

		public string Name { get; set; }
		public string Assembly { get; set; }
		//public string Type { get; set; }

		protected override IResourceLocation Location
		{
			get
			{
				return LocationHelper.GetLocation(BaseLocation, Assembly, Name);
			}
		}

		protected override void RenderTag(HtmlTextWriter writer)
		{
			Resource.RenderReferenceTags(writer, UrlFactory, WrittenResources);
		}
	}
}
