using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Web;
using System.Security.Cryptography;

namespace Calyptus.ResourceManager
{
	public class ExtendedJavaScriptResource : IJavaScriptResource, IProxyResource
	{
		private static IJavaScriptCompressor compressor = new Dean.Edwards.ECMAScriptPacker(Dean.Edwards.ECMAScriptPacker.PackerEncoding.None, false, false);

		public ExtendedJavaScriptResource(bool? compress, IResource[] references, IResource[] includes, IResource[] builds, FileLocation location)
		{
			_compress = compress;
			_builds = builds;
			_references = references;
			_includes = includes;
			_location = location;
			_version = ChecksumHelper.GetCombinedChecksum(location.Version, _includes, _builds);
		}

		private FileLocation _location;

		public IResourceLocation Location { get { return _location; } }

		private IResource[] _includes;
		private IResource[] _references;
		private IResource[] _builds;

		private byte[] _version;

		public byte[] Version
		{
			get
			{
				return _version;
			}
		}

		private bool? _compress;

		public IEnumerable<IResource> References
		{
			get { return _references; }
		}

		public IEnumerable<IResource> Includes
		{
			get { return _includes; }
		}

		public void RenderJavaScript(TextWriter writer, ICollection<IResource> writtenResources, bool compress)
		{
			if (_compress.HasValue)
				compress = _compress.Value;

			if (writtenResources.Contains(this)) return;
			writtenResources.Add(this);

			if (_builds != null)
				foreach (IJavaScriptResource resource in _builds)
					RenderBuild(resource, writer, writtenResources, compress);

			if (_includes != null)
				foreach (IJavaScriptResource resource in _includes)
					resource.RenderJavaScript(writer, writtenResources, compress);

			if (writer == null) return;

			var r = new StreamReader(_location.GetStream());
			var s = r.ReadToEnd();
			r.Close();
			if (compress)
			{
				s = compressor.Compress(s);
			}
			writer.Write(s);
		}

		private void RenderBuild(IJavaScriptResource resource, TextWriter writer, ICollection<IResource> writtenResources, bool compress)
		{
			if (resource.References != null)
				foreach (IResource res in resource.References)
				{
					IJavaScriptResource js = res as IJavaScriptResource;
					if (js != null)
						RenderBuild(js, writer, writtenResources, compress);
				}
			resource.RenderJavaScript(writer, writtenResources, compress);
		}

		public void RenderReferenceTags(ResourceConfigurationManager factory, TextWriter writer, ICollection<IResource> writtenResources)
		{
			if (writtenResources.Contains(this)) return;
			writtenResources.Add(this);

			if (_builds != null)
				foreach (IResource resource in _builds)
					resource.RenderReferenceTags(null, null, writtenResources);

			if (_includes != null)
				foreach (IResource resource in _includes)
				{
					if (writtenResources.Contains(resource)) continue;
					writtenResources.Add(resource);
					if (resource.References != null)
						foreach (IResource res in resource.References)
							res.RenderReferenceTags(factory, writer, writtenResources);
				}

			if (_references != null)
				foreach (IResource reference in _references)
					reference.RenderReferenceTags(factory, writer, writtenResources);

			if (writer == null) return;
			writer.Write("<script src=\"");
			writer.Write(HttpUtility.HtmlEncode(factory.GetURL(this)));
			writer.Write("\" type=\"text/javascript\"></script>");
		}

		public string ContentType
		{
			get { return "text/javascript"; }
		}

		public void RenderProxy(TextWriter writer, ICollection<IResource> writtenResources)
		{
			RenderJavaScript(writer, writtenResources, !_compress.HasValue || _compress.Value);
		}
	}
}
