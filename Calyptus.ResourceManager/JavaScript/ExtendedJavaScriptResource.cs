using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Web;
using System.Security.Cryptography;

namespace Calyptus.ResourceManager
{
	public class ExtendedJavaScriptResource : IJavaScriptResource, ITextProxyResource
	{
		private static IJavaScriptCompressor compressor = new Dean.Edwards.ECMAScriptPacker(Dean.Edwards.ECMAScriptPacker.PackerEncoding.None, false, false);

		public ExtendedJavaScriptResource(bool? compress, bool defaultCompress, IResource[] references, IResource[] includes, IResource[] builds, FileLocation location, bool hasContent)
		{
			_defaultCompress = defaultCompress;
			_hasContent = hasContent;
			_compress = compress;
			_builds = builds;
			_references = references;
			_includes = includes;
			_location = location;
			_version = ChecksumHelper.GetCombinedChecksum(location.Version, _includes, _builds);
		}

		private bool _hasContent;

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

		private bool _defaultCompress;
		private bool? _compress;

		public IEnumerable<IResource> References
		{
			get { return _references; }
		}

		public void RenderJavaScript(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources, bool compress)
		{
			if (_compress.HasValue)
				compress = _compress.Value;

			if (writtenResources.Contains(this)) return;
			writtenResources.Add(this);

			if (_builds != null)
				foreach (IJavaScriptResource resource in _builds)
					RenderBuild(resource, writer, urlFactory, writtenResources, compress);

			if (_includes != null)
				foreach (IJavaScriptResource resource in _includes)
					resource.RenderJavaScript(writer, urlFactory, writtenResources, compress);

			if (writer == null || !_hasContent) return;

			var r = new StreamReader(_location.GetStream());
			var s = r.ReadToEnd();
			r.Close();
			if (compress)
			{
				s = compressor.Compress(s);
			}
			writer.Write(s);
		}

		private void RenderBuild(IJavaScriptResource resource, TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources, bool compress)
		{
			if (resource.References != null)
				foreach (IResource res in resource.References)
				{
					IJavaScriptResource js = res as IJavaScriptResource;
					if (js != null)
						RenderBuild(js, writer, urlFactory, writtenResources, compress);
				}
			resource.RenderJavaScript(writer, urlFactory, writtenResources, compress);
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

			if (writer == null || (!_hasContent && _builds == null && _includes == null)) return;
			writer.Write("<script src=\"");
			writer.Write(HttpUtility.HtmlEncode(urlFactory.GetURL(this)));
			writer.WriteLine("\" type=\"text/javascript\"></script>");
		}

		public string ContentType
		{
			get { return "text/javascript"; }
		}

		public bool Gzip
		{
			get { return true; }
		}

		public void RenderProxy(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources)
		{
			RenderJavaScript(writer, urlFactory, writtenResources, _defaultCompress);
		}

		public void RenderProxy(Stream stream, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources)
		{
			RenderProxy(new StreamWriter(stream), urlFactory, writtenResources);
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
