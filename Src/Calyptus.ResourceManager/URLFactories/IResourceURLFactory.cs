using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;

namespace Calyptus.ResourceManager
{
	public interface IResourceURLFactory
	{
		string GetURL(IResource location);
	}
}
