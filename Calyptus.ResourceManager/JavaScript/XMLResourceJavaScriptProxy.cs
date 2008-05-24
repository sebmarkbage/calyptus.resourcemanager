using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Calyptus.ResourceManager
{
	public class JavaScriptXMLResourceProxy : IJavaScriptResource, IProxyResource
	{
		public IEnumerable<IResource> References
		{
			get { return null;  }
		}

		public IResourceLocation Location
		{
			get { throw new NotImplementedException(); }
		}

		public string ContentType
		{
			get { return "text/javascript"; }
		}

		public byte[] Version
		{
			get { return Location.Version; }
		}

		public void RenderJavaScript(TextWriter writer, ICollection<IResource> writtenResources, bool compress)
		{
			throw new NotImplementedException();
		}

		public void RenderProxy(TextWriter writer, ICollection<IResource> writtenResources)
		{
			throw new NotImplementedException();
		}

		public void RenderReferenceTags(ResourceConfigurationManager factory, TextWriter writer, ICollection<IResource> writtenResources)
		{
			throw new NotImplementedException();
		}
	}
}
