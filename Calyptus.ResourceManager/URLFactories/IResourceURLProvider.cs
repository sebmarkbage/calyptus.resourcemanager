using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;

namespace Calyptus.ResourceManager
{
	public interface IResourceURLProvider
	{
		IResourceURLFactory GetURLFactory(HttpContext context);
	}

	public interface IResourceURLControlProvider
	{
		IResourceURLFactory GetURLFactory(Control control);
	}
}
