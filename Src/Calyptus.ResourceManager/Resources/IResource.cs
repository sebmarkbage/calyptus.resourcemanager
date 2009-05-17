using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.IO;

namespace Calyptus.ResourceManager
{
	public interface IResource
	{
		IResourceLocation Location
		{
			get;
		}

		byte[] Version { get; }

		IEnumerable<IResource> References { get; }

		void RenderReferenceTags(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources);
	}
}
