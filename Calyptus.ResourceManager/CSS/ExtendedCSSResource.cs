using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Web;
using System.Security.Cryptography;

namespace Calyptus.ResourceManager
{
	public class ExtendedCSSResource : ICSSResource, IBase64TextProxyResource
	{
		private static ICSSCompressor compressor = new YUICompressor();

		public ExtendedCSSResource(bool? compress, IResource[] references, IImageResource[] imageIncludes, ICSSResource[] includes, ICSSResource[] builds, FileLocation location)
		{
			_compress = compress;
			_builds = builds;
			_references = references;
			_includes = includes;
			_imageIncludes = imageIncludes;
			_location = location;
			_version = ChecksumHelper.GetCombinedChecksum(location.Version, _includes, _builds);
		}

		private FileLocation _location;

		public IResourceLocation Location { get { return _location; } }

		private ICSSResource[] _includes;
		private IResource[] _references;
		private ICSSResource[] _builds;
		private IImageResource[] _imageIncludes;

		private byte[] _version;
		public byte[] Version
		{
			get
			{
				return _version;
			}
		}

		public bool CultureSensitive
		{
			get;
			private set;
		}

		public bool CultureUISensitive
		{
			get;
			private set;
		}

		private bool? _compress;

		public IEnumerable<IResource> References
		{
			get { return _references; }
		}

		public void RenderCSS(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources, bool compress, bool includeImages)
		{
			if (_compress.HasValue)
				compress = _compress.Value;

			if (writtenResources.Contains(this)) return;
			writtenResources.Add(this);

			if (_builds != null)
				foreach (ICSSResource resource in _builds)
					RenderBuild(resource, writer, urlFactory, writtenResources, compress, includeImages);

			if (_includes != null)
				foreach (ICSSResource resource in _includes)
					resource.RenderCSS(writer, urlFactory, writtenResources, compress, includeImages);

			var r = new StreamReader(_location.GetStream());
			var s = r.ReadToEnd();
			r.Close();
			if (compress)
				s = compressor.Compress(s);

			s = CssUrlParserHelper.ConvertUrls(s, Location, urlFactory, includeImages ? _imageIncludes : null);

			writer.Write(s);
		}

		private void RenderBuild(ICSSResource resource, TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources, bool compress, bool includeImages)
		{
			if (resource.References != null)
				foreach (IResource res in resource.References)
				{
					ICSSResource css = res as ICSSResource;
					if (css != null)
						RenderBuild(css, writer, urlFactory, writtenResources, compress, includeImages);
				}
			resource.RenderCSS(writer, urlFactory, writtenResources, compress, includeImages);
		}

		public void RenderReferenceTags(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources)
		{
			if (writtenResources.Contains(this)) return;
			writtenResources.Add(this);

			if (_builds != null)
				foreach (IResource resource in _builds)
				{
					resource.RenderReferenceTags(null, null, writtenResources);
					IProxyResource pr = resource as IProxyResource;
					if (pr != null)
					{
						if (pr.CultureSensitive) CultureSensitive = true;
						if (pr.CultureUISensitive) CultureUISensitive = true;
					}
				}

			if (_includes != null)
				foreach (IResource resource in _includes)
				{
					if (writtenResources.Contains(resource)) continue;
					writtenResources.Add(resource);
					if (resource.References != null)
						foreach (IResource res in resource.References)
							res.RenderReferenceTags(writer, urlFactory, writtenResources);
					IProxyResource pr = resource as IProxyResource;
					if (pr != null)
					{
						if (pr.CultureSensitive) CultureSensitive = true;
						if (pr.CultureUISensitive) CultureUISensitive = true;
					}
				}

			if (_references != null)
				foreach (IResource reference in _references)
					reference.RenderReferenceTags(writer, urlFactory, writtenResources);

			if (writer == null) return;
			writer.Write("<link rel=\"stylesheet\" href=\"");
			writer.Write(HttpUtility.HtmlEncode(urlFactory.GetURL(this)));
			writer.Write("\" type=\"text/css\"/>");
		}

		public string ContentType
		{
			get { return "text/css"; }
		}

		public bool Gzip
		{
			get { return true; }
		}

		public void RenderProxy(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources)
		{
			RenderCSS(writer, urlFactory, writtenResources, !_compress.HasValue || _compress.Value, false);
		}

		public void RenderProxyWithBase64(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources)
		{
			RenderCSS(writer, urlFactory, writtenResources, !_compress.HasValue || _compress.Value, true);
		}

		public void RenderProxy(Stream stream, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources)
		{
			RenderProxy(new StreamWriter(stream), urlFactory, writtenResources);
		}
	}
}
