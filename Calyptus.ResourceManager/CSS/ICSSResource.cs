using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Calyptus.ResourceManager
{
	public interface ICSSResource : IResource
	{
		void RenderCSS(TextWriter writer, ICollection<IResource> writtenResources, bool compress);
	}
}
