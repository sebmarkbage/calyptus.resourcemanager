using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Calyptus.ResourceManager.Packager
{
	public class PackageConfiguration
	{
		public Package GetPackageByName(string name)
		{
			throw new NotImplementedException();
		}

		public Package GetPackageByLocation(IResourceLocation location)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<IResourceLocation> GetLocations(IResourceLocation baseLocation, string name)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<IResourceLocation> GetLocations(IResourceLocation baseLocation, string packageName, string componentname)
		{
			throw new NotImplementedException();
		}
	}
}
