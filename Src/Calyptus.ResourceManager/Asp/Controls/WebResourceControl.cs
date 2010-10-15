using System;
using System.Web.UI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Web;

namespace Calyptus.ResourceManager
{
	public abstract class WebResourceControl : System.Web.UI.Control
	{
		private IResourceConfiguration _config;
		protected IResourceConfiguration Manager
		{
			get
			{
				if (_config == null)
					_config = ResourceConfigurationManager.GetConfiguration();
				return _config;
			}
		}

		private IResourceLocation _mylocation;
		protected IResourceLocation BaseLocation
		{
			get
			{
				if (_mylocation == null)
				{
					_mylocation = new VirtualPathLocation("~/", ResolveUrl("./"));
				}
				return _mylocation;
			}
		}

		protected abstract IEnumerable<IResourceLocation> Locations
		{
			get;
		}

		private List<IResource> _resources;
		public virtual IEnumerable<IResource> Resources
		{
			get
			{
				if (_resources == null)
				{
					_resources = new List<IResource>();
					var ls = Locations;
					if (ls != null)
						foreach (IResourceLocation l in ls)
						{
							IResource res = Manager.GetResource(l);
							if (res != null)
								_resources.Add(res);
						}
				}
				return _resources;
			}
		}

		private IResourceURLFactory _urlFactory;
		protected virtual IResourceURLFactory UrlFactory
		{
			get
			{
				if (_urlFactory == null)
				{
					IResourceURLProvider p = ResourceURLProvider.GetURLProvider();
					IResourceURLControlProvider pc = p as IResourceURLControlProvider;
					if (pc != null)
						_urlFactory = pc.GetURLFactory(this);
					else
						_urlFactory = p.GetURLFactory(Context);
				}
				return _urlFactory;
			}
		}

		private static object _itemKey = new object();
		private ICollection<IResource> _writtenResources;
		protected virtual ICollection<IResource> WrittenResources
		{
			get
			{
				if (_writtenResources == null)
				{
					_writtenResources = this.Page.Items[_itemKey] as ICollection<IResource>;
					if (_writtenResources == null)
						this.Page.Items[_itemKey] = _writtenResources = new Collection<IResource>();
				}
				return _writtenResources;
			}
		}

		protected override void Render(HtmlTextWriter writer)
		{
			foreach (IResource resource in Resources)
				RenderTag(writer, resource);
		}

		protected abstract void RenderTag(HtmlTextWriter writer, IResource resource);
	}
}
