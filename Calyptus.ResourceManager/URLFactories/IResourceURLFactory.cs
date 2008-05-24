using System;
using System.Collections.Generic;
using System.Text;

namespace Calyptus.ResourceManager
{
	public interface IResourceURLFactory
	{
		string GetURL(IResource location);
	}
}
