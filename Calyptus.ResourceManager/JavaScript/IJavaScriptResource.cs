using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Calyptus.ResourceManager
{
	public interface IJavaScriptResource : IResource
	{
		void RenderJavaScript(TextWriter writer, ICollection<IResource> writtenResources, bool compress);
	}
}
