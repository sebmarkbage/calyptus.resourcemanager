using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Calyptus.ResourceManager
{
	public interface ICSSResource : IResource
	{
		void RenderCSS(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources, bool compress, bool includeImages, IEnumerable<IImageResource> parentIncludedImages);
	}
}
