using System;
using System.Web;
using System.Web.UI;

namespace Calyptus.ResourceManager
{
	public class HttpHandlerURLProvider : IResourceURLProvider, IResourceURLControlProvider
	{
		private HttpHandlerURLFactory _factory;

		public IResourceURLFactory GetURLFactory(HttpContext context)
		{
			if (_factory == null) _factory = new HttpHandlerURLFactory(context.Request.ApplicationPath);
			return _factory;
		}

		public IResourceURLFactory GetURLFactory(Control control)
		{
			if (_factory == null) _factory = new HttpHandlerURLFactory(control.Page.Request.ApplicationPath);
			return _factory;
		}
	}
}
