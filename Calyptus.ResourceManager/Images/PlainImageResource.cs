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

		protected string Mime { get { return _mime; } }

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

		public string GetImageData(bool compress)
		{
			StringBuilder sb = new StringBuilder("data:");
			sb.Append(_mime);
			sb.Append(";base64,");

			Stream s = _location.GetStream();
			byte[] data = new byte[s.Length > 0 ? s.Length : 32768];
			int index = 0;
			int i;
			while((i = s.Read(data, index, data.Length - index)) > 0)
			{
				index += i;
				Array.Resize(ref data, index + 32768);
			}
			sb.Append(Convert.ToBase64String(data, 0, index, Base64FormattingOptions.None));
			return sb.ToString();
		}



		public void RenderReferenceTags(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources)
		{
			writer.Write("<img src=\"");
			writer.Write(HttpUtility.HtmlEncode(urlFactory.GetURL(this)));
			writer.Write("\" alt=\"\" />");
		}
	}
}
