using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Calyptus.ResourceManager
{
	interface IMvcResourceURLProvider : IResourceURLProvider
	{
		IResourceURLFactory GetURLFactory(HttpContextBase context);
	}
}
