﻿using System;
using System.Web.UI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Web;

namespace Calyptus.ResourceManager
{
	public abstract class ResourceControl : System.Web.UI.Control
	{
		private ResourceConfigurationManager _manager;
		protected ResourceConfigurationManager Manager
		{
			get
			{
				if (_manager == null)
					_manager = ResourceConfigurationManager.GetFactoryManager(Context);
				return _manager;
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

		protected abstract IResourceLocation Location
		{
			get;
		}

		private IResource _resource;
		public virtual IResource Resource
		{
			get
			{
				if (_resource == null)
					_resource = Manager.GetResource(Location);
				return _resource;
			}
		}

		private IResourceURLFactory _urlFactory;

		protected virtual IResourceURLFactory UrlFactory
		{
			get
			{
				if (_urlFactory == null)
				{
					IResourceURLProvider p = Manager.URLProvider;
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
			RenderTag(writer);
		}

		protected abstract void RenderTag(HtmlTextWriter writer);
	}
}
