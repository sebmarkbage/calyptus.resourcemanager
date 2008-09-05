using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Calyptus.ResourceManager
{
	public interface IJavaScriptResource : IResource
	{
		bool CanReferenceJavaScript { get; }
		void RenderJavaScript(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources, bool compress);
	}
}
