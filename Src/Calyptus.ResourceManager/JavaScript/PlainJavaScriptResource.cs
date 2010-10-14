using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Web;
using System.Security.Cryptography;

namespace Calyptus.ResourceManager
{
	public class PlainJavaScriptResource : IJavaScriptResource
	{
		private static IJavaScriptCompressor compressor = new YUICompressor(); // new Dean.Edwards.ECMAScriptPacker(Dean.Edwards.ECMAScriptPacker.PackerEncoding.None, false, false);

		public PlainJavaScriptResource(IResource[] references, FileLocation location, bool hasContent)
		{
			_hasContent = hasContent;
			_references = references;
			_location = location;
		}

		private bool _hasContent;

		private FileLocation _location;

		public IResourceLocation Location { get { return _location; } }

		public byte[] Version
		{
			get
			{
				return Location.Version;
			}
		}

		private IResource[] _references;

		public IEnumerable<IResource> References
		{
			get { return _references; }
		}

		public void RenderJavaScript(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources, bool compress)
		{
			if (writtenResources.Contains(this)) return;
			writtenResources.Add(this);

			var r = new StreamReader(_location.GetStream());
			var s = r.ReadToEnd();
			r.Close();
			if (compress)
			{
				s = compressor.Compress(s);
			}
			writer.Write(s);
		}

		public void RenderReferenceTags(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources)
		{
			if (writtenResources.Contains(this)) return;
			writtenResources.Add(this);
			if (_references != null)
				foreach (IResource reference in _references)
					reference.RenderReferenceTags(writer, urlFactory, writtenResources);

			if (writer == null || !_hasContent) return;
			writer.Write("<script src=\"");
			writer.Write(HttpUtility.HtmlEncode(urlFactory.GetURL(this)));
			writer.WriteLine("\" type=\"text/javascript\"></script>");
		}

		public bool CanReferenceJavaScript
		{
			get { return true; }
		}

		public override string ToString()
		{
			return Location.ToString();
		}
	}
}
