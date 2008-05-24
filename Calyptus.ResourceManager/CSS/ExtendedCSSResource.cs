using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Web;
using System.Security.Cryptography;

namespace Calyptus.ResourceManager
{
	public class ExtendedCSSResource : ICSSResource, IProxyResource
	{
		private static ICSSCompressor compressor = new YUICompressor();

		public ExtendedCSSResource(bool? compress, IResource[] references, IResource[] includes, IResource[] builds, FileLocation location)
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

		public void RenderCSS(TextWriter writer, ICollection<IResource> writtenResources, bool compress)
		{
			if (_compress.HasValue)
				compress = _compress.Value;

			if (writtenResources.Contains(this)) return;
			writtenResources.Add(this);

			if (_builds != null)
				foreach (ICSSResource resource in _builds)
					RenderBuild(resource, writer, writtenResources, compress);

			if (_includes != null)
				foreach (ICSSResource resource in _includes)
					resource.RenderCSS(writer, writtenResources, compress);

			var r = new StreamReader(_location.GetStream());
			var s = r.ReadToEnd();
			r.Close();
			if (compress)
			{
				s = compressor.Compress(s);
			}
			writer.Write(s);
		}

		private void RenderBuild(ICSSResource resource, TextWriter writer, ICollection<IResource> writtenResources, bool compress)
		{
			if (resource.References != null)
				foreach (IResource res in resource.References)
				{
					ICSSResource css = res as ICSSResource;
					if (css != null)
						RenderBuild(css, writer, writtenResources, compress);
				}
			resource.RenderCSS(writer, writtenResources, compress);
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
			writer.Write("<link rel=\"stylesheet\" href=\"");
			writer.Write(HttpUtility.HtmlEncode(factory.GetURL(this)));
			writer.Write("\" type=\"text/css\"/>");
		}

		public string ContentType
		{
			get { return "text/css"; }
		}

		public void RenderProxy(TextWriter writer, ICollection<IResource> writtenResources)
		{
			RenderCSS(writer, writtenResources, !_compress.HasValue || _compress.Value);
		}
	}
}
