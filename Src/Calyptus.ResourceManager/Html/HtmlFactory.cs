using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Calyptus.ResourceManager
{
	public class HtmlFactory : ResourceFactoryBase
	{
        public override IResource GetResource(IResourceLocation location)
        {
            FileLocation l = location as FileLocation;
            if (l == null) return null;
            if (!l.FileName.EndsWith(".html", StringComparison.InvariantCultureIgnoreCase))
            {
                ExternalLocation el = l as ExternalLocation;
                if (el == null || el.Mime == null || !el.Mime.EndsWith("/html", StringComparison.InvariantCultureIgnoreCase))
                    return null;
            }

            return new HtmlProxy(l);
        }			
	}
}
