using System;
using System.Collections.Generic;
using System.Text;

namespace Calyptus.ResourceManager
{
	public abstract class ResourceFactoryBase : IResourceFactory
	{
		public abstract IResource GetResource(IResourceLocation location);

		public IResourceConfiguration Configuration
		{
			get;
			private set;
		}

		IResourceConfiguration IResourceFactory.Configuration
		{
			set { this.Configuration = value; }
		}
	}
}
