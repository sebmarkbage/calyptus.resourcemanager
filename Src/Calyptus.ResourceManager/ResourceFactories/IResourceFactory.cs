using System;
using System.Collections.Generic;
using System.Text;

namespace Calyptus.ResourceManager
{
	public interface IResourceFactory
	{
		IResourceConfiguration Configuration { set; }
		
		IResource GetResource(IResourceLocation location);
	}
}
