using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Calyptus.ResourceManager.Packager
{
	public class Package
	{
		public string Name { get; private set; }

		public IResourceLocation this[string componentName]
		{
			get { throw new NotImplementedException(); }
		}

		public bool HasLocation(IResourceLocation location)
		{
			throw new NotImplementedException();
		}
	}
}
