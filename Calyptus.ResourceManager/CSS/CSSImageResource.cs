using System;
using System.Collections.Generic;
using System.Text;

namespace Calyptus.ResourceManager
{
	public class CSSImageResource : ICSSResource
	{
		#region ICSSResource Members

		public void RenderCSS(System.IO.TextWriter writer, ICollection<IResource> writtenResources, bool compress)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IResource Members

		public IResourceLocation Location
		{
			get { throw new NotImplementedException(); }
		}

		public byte[] Version
		{
			get { throw new NotImplementedException(); }
		}

		public IEnumerable<IResource> References
		{
			get { throw new NotImplementedException(); }
		}

		public void RenderReferenceTags(ResourceConfigurationManager factory, System.IO.TextWriter writer, ICollection<IResource> writtenResources)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
