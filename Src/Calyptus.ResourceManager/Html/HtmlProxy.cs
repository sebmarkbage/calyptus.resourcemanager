using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Web;
using System.Security.Cryptography;

namespace Calyptus.ResourceManager
{
	public class HtmlProxy : IJavaScriptResource, ITextProxyResource
	{
        public HtmlProxy(FileLocation location)
		{
            _includes = new IResource[0];
            _references = _builds = _includes;
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

		public bool CultureSensitive
		{
			get { return false; }
		}

		public bool CultureUISensitive
		{
            get { return false; }
		}

		public IEnumerable<IResource> References
		{
			get { return _references; }
		}

		public void RenderJavaScript(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources, bool compress = false)
		{
			if (writtenResources.Contains(this)) return;
			writtenResources.Add(this);

			if (writer == null) return;

			var r = new StreamReader(_location.GetStream());
			var s = r.ReadToEnd();

            var n = _location.FileName;
            if (n.EndsWith(".html", StringComparison.InvariantCultureIgnoreCase))
                n = n.Substring(0, n.Length - 5);

            n = "Html." + n;

            string[] names = n.Split('.');
            StringBuilder fullName = new StringBuilder();
            for (int i = 0; i < names.Length - 1; i++)
            {
                if (i > 0) fullName.Append('.');
                fullName.Append(names[i]);
                string fn = fullName.ToString();
                writer.Write("if(typeof(");
                writer.Write(fn);
                writer.Write(")!='object'||");
                writer.Write(fn);
                writer.Write("==null)");
                if (i == 0)
                    writer.Write("var ");
                writer.Write(fn);
                writer.Write("={};");
            }

            s = s.Replace("\\", "\\\\").Replace("'", "\'").Replace("\r", "").Replace("\n", "\\\n");
            s = n + " = '" + s + "';";

            r.Close();

			writer.Write(s);
		}

		public void RenderReferenceTags(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources)
		{
			if (writtenResources.Contains(this)) return;
			writtenResources.Add(this);
            
			if (writer == null) return;

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
			RenderJavaScript(writer, urlFactory, writtenResources);
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
