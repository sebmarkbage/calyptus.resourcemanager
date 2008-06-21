using System;
using System.Collections.Generic;
using System.Text;

namespace Calyptus.ResourceManager
{
	public class FileResourceFactory : ResourceFactoryBase
	{
		public override IResource GetResource(IResourceLocation location)
		{
			EmbeddedLocation el = location as EmbeddedLocation;
			if (el != null && el.FileName.EndsWith(".resources", StringComparison.InvariantCultureIgnoreCase))
				return new FileResourceProxy(el);
			else
				return null;
		}
	}
}
