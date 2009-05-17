using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Calyptus.ResourceManager
{
	public abstract class ResourcePackage : IResource
	{
		public ResourcePackage(IResource package, params IResource[] includedResources)
		{
			_package = package;
			if (includedResources != null && includedResources.Length > 0) _includedResources = includedResources;
		}

		public ResourcePackage(IResource package, IEnumerable<IResource> includedResources)
		{
			_package = package;
			if (includedResources == null) return;
			List<IResource> res = new List<IResource>(includedResources);
			if (res.Count > 0)
				_includedResources = res.ToArray();
		}

		public ResourcePackage(IResourceConfiguration configuration, IResourceLocation packageLocation, params IResourceLocation[] locations) : this(configuration, packageLocation, (IEnumerable<IResourceLocation>)locations) { }

		public ResourcePackage(IResourceConfiguration configuration, IResourceLocation packageLocation, IEnumerable<IResourceLocation> locations)
		{
			_package = configuration.GetResource(packageLocation);
			if (locations == null) return;
			List<IResource> res = new List<IResource>();
			foreach (IResourceLocation l in locations)
			{
				IResource r = configuration.GetResource(l);
				if (r != null)
					res.Add(r);
			}
			_includedResources = res.Count > 0 ? res.ToArray() : null;
		}

		private IResource _package;

		private IResource[] _includedResources;

		protected IEnumerable<IResource> IncludedResources
		{
			get
			{
				return _includedResources;
			}
		}

		public IResourceLocation Location
		{
			get { return new TypeLocation(this.GetType()); }
		}

		public byte[] Version
		{
			get { return null; }
		}

		public IEnumerable<IResource> References
		{
			get { return null; }
		}

		public virtual void RenderReferenceTags(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources)
		{
			if (writtenResources.Contains(this)) return;
			writtenResources.Add(this);
			if (_includedResources != null)
				foreach (IResource r in _includedResources)
					r.RenderReferenceTags(null, null, writtenResources);

			if (_package != null)
				_package.RenderReferenceTags(writer, urlFactory, writtenResources);
		}
	}
}
