using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Web;
using System.Security.Cryptography;

namespace Calyptus.ResourceManager
{
	public class PlainImageResource : IImageResource
	{
		public PlainImageResource(string mimeType, FileLocation location)
		{
			_mime = mimeType;
			_location = location;
		}

		private string _mime;
		private FileLocation _location;

		public string ContentType { get { return _mime; } }

		public IResourceLocation Location { get { return _location; } }

		public byte[] Version
		{
			get
			{
				return Location.Version;
			}
		}

		public IEnumerable<IResource> References
		{
			get { return null; }
		}

		public byte[] GetImageData()
		{
			Stream s = _location.GetStream();
			byte[] data = new byte[s.Length > 0 ? s.Length : 32768];
			int index = 0;
			int i;
			while((i = s.Read(data, index, data.Length - index)) > 0)
			{
				index += i;
				Array.Resize(ref data, index + 32768);
			}
			return data;
		}



		public void RenderReferenceTags(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources)
		{
			writer.Write("<img src=\"");
			writer.Write(HttpUtility.HtmlEncode(urlFactory.GetURL(this)));
			writer.Write("\" alt=\"\" />");
		}
	}
}
