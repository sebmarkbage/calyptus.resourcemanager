using System;
using System.IO;
using System.Collections.Generic;

namespace Calyptus.ResourceManager
{
	public sealed class UnknownResource : IResource
	{
		public UnknownResource(IResourceLocation location)
		{
			this.Location = location;
		}

		public IResourceLocation Location
		{
			get; private set;
		}

		public byte[] Version
		{
			get { return Location.Version; }
		}

		public IEnumerable<IResource> References
		{
			get { return null; }
		}

		public void RenderReferenceTags(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources)
		{
			if (writtenResources.Contains(this)) return;
			writtenResources.Add(this);
			if (writer == null) return;
			writer.Write("<!-- Unknown Resource Type (");
			writer.Write(urlFactory.GetURL(this));
			writer.Write(") -->");
		}

		public override int GetHashCode()
		{
			return Location.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			UnknownResource o = obj as UnknownResource;
			if (o == null) return false;
			return Location.Equals(o.Location);
		}
	}
}
