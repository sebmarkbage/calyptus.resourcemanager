using System;
using System.Collections.Generic;
using System.Text;

namespace Calyptus.ResourceManager
{
	public class FlashFactory : ResourceFactoryBase
	{
		public override IResource GetResource(IResourceLocation location)
		{
			FileLocation l = location as FileLocation;
			if (l == null) return null;
			if (!l.FileName.EndsWith(".swf", StringComparison.InvariantCultureIgnoreCase))
			{
				ExternalLocation el = l as ExternalLocation;
				if (el == null || el.Mime == null || !el.Mime.Equals("application/x-shockwave-flash", StringComparison.OrdinalIgnoreCase))
					return null;
			}

			if (l is EmbeddedLocation)
				return new ProxyFlashResource(l);
			else
				return new PlainFlashResource(l);
		}
	}
}
