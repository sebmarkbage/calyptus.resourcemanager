using System;
using System.Web.UI;
using System.Collections.Generic;

namespace Calyptus.ResourceManager
{
	public class Import : ResourceControl
	{
		public string Src { get { return Name; } set { Name = value; } }

		public string Name { get; set; }
		public string Assembly { get; set; }

		protected override IEnumerable<IResourceLocation> Locations
		{
			get
			{
				return ResourceLocations.GetLocations(BaseLocation, Assembly, Name);
			}
		}

		protected override void RenderTag(HtmlTextWriter writer, IResource resource)
		{
			resource.RenderReferenceTags(writer, UrlFactory, WrittenResources);
		}
	}

	public class Using : Import
	{
	}
}
