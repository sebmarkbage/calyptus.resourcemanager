using System;
using System.Collections.Generic;
using System.Text;

namespace Calyptus.ResourceManager
{
	public class ImageFactory : ResourceFactoryBase
	{
		public override IResource GetResource(IResourceLocation location)
		{
			FileLocation l = location as FileLocation;
			if (l == null) return null;

			string f = l.FileName;
			string mime;
			if (f.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) || f.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase))
				mime = "image/jpeg";
			else if (f.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase))
				mime = "image/gif";
			else if (f.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
				mime = "image/png";
			else if (f.EndsWith(".bmp", StringComparison.InvariantCultureIgnoreCase))
				mime = "image/bmp";
			else if (f.EndsWith(".tif", StringComparison.InvariantCultureIgnoreCase) || f.EndsWith(".tiff", StringComparison.InvariantCultureIgnoreCase))
				mime = "image/tiff";
			else
				return null;

			if (l is EmbeddedLocation)
				return new ProxyImageResource(mime, l);
			else
				return new PlainImageResource(mime, l);
		}
	}
}
