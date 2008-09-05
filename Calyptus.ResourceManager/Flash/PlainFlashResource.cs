using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Web;
using System.Security.Cryptography;

namespace Calyptus.ResourceManager
{
	public class PlainFlashResource : IFlashResource
	{
		public PlainFlashResource(FileLocation location)
		{
			_location = location;
		}

		private FileLocation _location;

		public IResourceLocation Location { get { return _location; } }

		public virtual byte[] Version
		{
			get
			{
				return Location.Version;
			}
		}

		public virtual IEnumerable<IResource> References
		{
			get { return null; }
		}

		public virtual void RenderReferenceTags(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources)
		{
			// TODO: Render object tag
			//writer.Write("<img src=\"");
			//writer.Write(HttpUtility.HtmlEncode(urlFactory.GetURL(this)));
			//writer.Write("\" alt=\"\" />");
		}
	}
}
