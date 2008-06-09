﻿using System;
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
		private static IJavaScriptCompressor compressor = new Dean.Edwards.ECMAScriptPacker(Dean.Edwards.ECMAScriptPacker.PackerEncoding.None, false, false);

		public PlainJavaScriptResource(IResource[] references, FileLocation location)
		{
			_references = references;
			_location = location;
		}

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

		public void RenderJavaScript(TextWriter writer, ICollection<IResource> writtenResources, bool compress)
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

		public void RenderReferenceTags(ResourceConfigurationManager factory, TextWriter writer, ICollection<IResource> writtenResources)
		{
			if (writtenResources.Contains(this)) return;
			writtenResources.Add(this);
			if (_references != null)
				foreach (IResource reference in _references)
					reference.RenderReferenceTags(factory, writer, writtenResources);

			if (writer == null) return;
			writer.Write("<script src=\"");
			writer.Write(HttpUtility.HtmlEncode(factory.GetURL(this)));
			writer.Write("\" type=\"text/javascript\"></script>");
		}
	}
}