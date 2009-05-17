using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Calyptus.ResourceManager
{
	public interface ICSSResource : IResource
	{
		bool CanReferenceCSS { get; }
		void RenderCSS(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources, bool compress, bool includeImages, IEnumerable<IImageResource> parentIncludedImages);
	}
}
