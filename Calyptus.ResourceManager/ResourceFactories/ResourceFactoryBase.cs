using System;
using System.Collections.Generic;
using System.Text;

namespace Calyptus.ResourceManager
{
	public abstract class ResourceFactoryBase : IResourceFactory
	{
		public abstract IResource GetResource(IResourceLocation location);

		public ResourceConfigurationManager FactoryManager
		{
			get;
			set;
		}
	}
}
