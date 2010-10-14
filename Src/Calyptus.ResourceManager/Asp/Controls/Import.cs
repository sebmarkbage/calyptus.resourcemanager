using System;
using System.Web.UI;
using System.Collections.Generic;

namespace Calyptus.ResourceManager
{
	public class Import : WebResourceControl
	{
		public string Src { get { return Name; } set { Name = value; } }

		public string Name { get; set; }
		public string Assembly { get; set; }

		protected override IEnumerable<IResourceLocation> Locations
		{
			get
			{
				var locations = ResourceLocations.GetLocations(BaseLocation, Assembly, Name);
				if (locations == null) throw new Exception("Could not find resource: " + (Name ?? "null"));
				return locations;
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

	public class Use : Import
	{
	}
}
