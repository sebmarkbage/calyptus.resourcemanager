using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;
using System.Web;
using System.Reflection;

namespace Calyptus.ResourceManager
{
	public class FileResourceProxy : IJavaScriptResource, ITextProxyResource
	{
		private const string EXTENSION = ".resources";

		public FileResourceProxy(TypeLocation location)
		{
			Manager = new System.Resources.ResourceManager(location.ProxyType);
			this.Location = location;
			SetVersions(location.ProxyType.Assembly);
		}

		public FileResourceProxy(EmbeddedLocation location)
		{
			string bn = location.ResourceName;
			if (!bn.EndsWith(EXTENSION, StringComparison.InvariantCultureIgnoreCase)) throw new Exception("Invalid file ending.");
			bn = bn.Substring(0, bn.Length - EXTENSION.Length);
			Manager = new System.Resources.ResourceManager(bn, location.Assembly);
			this.Location = location;
			SetVersions(location.Assembly);
		}

		protected System.Resources.ResourceManager Manager
		{
			get;
			private set;
		}

		public IEnumerable<IResource> References
		{
			get { return null;  }
		}

		public IResourceLocation Location
		{
			get; private set;
		}

		public byte[] Version
		{
			get {
				if (CultureInfo.CurrentUICulture == CultureInfo.InvariantCulture)
					return _mainVer;
				else
					return _satVer;
			}
		}

		private byte[] _mainVer;
		private byte[] _satVer;

		private void SetVersions(Assembly a)
		{
			_mainVer = GetVerBytes(a.GetName().Version);
			object[] atts = a.GetCustomAttributes(typeof(System.Resources.SatelliteContractVersionAttribute), false);
			if (atts != null && atts.Length > 0)
			{
				try
				{
					_satVer = GetVerBytes(new Version(((System.Resources.SatelliteContractVersionAttribute)atts[0]).Version));
					return;
				}
				catch { }
			}
			_satVer = _mainVer;
		}

		private byte[] GetVerBytes(Version version)
		{
			return new byte[] {
				(byte)((version.Major << 4 & 240) | (version.Minor & 15)),
				(byte)(version.Build & 255),
				(byte)(version.Revision >> 8 & 255),
				(byte)(version.Revision & 255)
			};
		}

		public bool CultureSensitive
		{
			get
			{
				return false;
			}
		}

		public bool CultureUISensitive
		{
			get
			{
				return true;
			}
		}

		public void RenderJavaScript(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources, bool compress)
		{
			CultureInfo culture = CultureInfo.CurrentUICulture;

			// Create namespace
			string bn = Manager.BaseName;
			string[] names = bn.Split('.');
			StringBuilder fullName = new StringBuilder();
			for(int i = 0; i < names.Length - 1; i++)
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

			// Write resources
			writer.Write(bn);
			writer.Write("={");

			List<int> addedKeyHashes = new List<int>();

			bool f = true;
			while(true)
			{
				System.Resources.ResourceSet set = Manager.GetResourceSet(culture, true, false);
				if (set != null)
				{
					System.Collections.IDictionaryEnumerator en = set.GetEnumerator();
					while (en.MoveNext())
					{
						if (en.Key is string && en.Value is string)
						{
							int hash = en.Key.GetHashCode();
							if (addedKeyHashes.Contains(hash)) continue;
							addedKeyHashes.Add(hash);
							if (f) f = false; else writer.Write(',');
							WriteJSEncodedString(writer, (string)en.Key);
							writer.Write(':');
							WriteJSEncodedString(writer, (string)en.Value);
						}
					}
				}
				if (culture == CultureInfo.InvariantCulture) break;
				culture = culture.Parent;
			}
			writer.Write("};");
		}

		private void WriteJSEncodedString(TextWriter writer, string value)
		{
			if (value == null) { writer.Write("null"); return; }
			writer.Write('\'');
			foreach (char c in value)
				switch(c)
				{
					case '\\': writer.Write("\\\\"); break;
					case '\b': writer.Write("\b"); break;
					case '\f': writer.Write("\f"); break;
					case '\r': break;
					case '\n': writer.Write("\n"); break;
					case '\t': writer.Write("\t"); break;
					case '\'': writer.Write("\\'"); break;
					default: writer.Write(c); break;
				}
			writer.Write('\'');
		}

		public void RenderReferenceTags(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources)
		{
			if (writtenResources.Contains(this)) return;
			writtenResources.Add(this);

			if (writer == null) return;
			writer.Write("<script src=\"");
			writer.Write(HttpUtility.HtmlEncode(urlFactory.GetURL(this)));
			writer.Write("\" type=\"text/javascript\"></script>");
		}

		public void RenderProxy(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources)
		{
			RenderJavaScript(writer, urlFactory, writtenResources, true);
		}

		public void RenderProxy(Stream stream, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources)
		{
			RenderProxy(new StreamWriter(stream), urlFactory, writtenResources);
		}

		public string ContentType
		{
			get { return "text/javascript"; }
		}

		public bool Gzip
		{
			get { return true; }
		}

		public bool CanReferenceJavaScript
		{
			get { return true; }
		}
	}
}
