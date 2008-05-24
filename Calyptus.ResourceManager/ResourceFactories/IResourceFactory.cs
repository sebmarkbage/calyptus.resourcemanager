using System;
using System.Collections.Generic;
using System.Text;

namespace Calyptus.ResourceManager
{
	public interface IResourceFactory
	{
		ResourceConfigurationManager FactoryManager { get; set; }
		
		IResource GetResource(IResourceLocation location);
	}
}
