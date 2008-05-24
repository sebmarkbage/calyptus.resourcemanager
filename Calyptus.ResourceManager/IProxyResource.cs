using System;
using System.IO;
using System.Collections.Generic;

namespace Calyptus.ResourceManager
{
	interface IProxyResource : IResource
	{
		string ContentType { get; }
		void RenderProxy(TextWriter writer, ICollection<IResource> writtenResources);
	}
}
