using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Web;
using System.Security.Cryptography;

namespace Calyptus.ResourceManager
{
	public class ProxyImageResource : PlainImageResource, IProxyResource
	{
		public ProxyImageResource(string mime, FileLocation location) : base(mime, location) { }

		public string ContentType
		{
			get { return Mime; }
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
				return false;
			}
		}

		public void RenderProxy(Stream stream, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources)
		{
			FileLocation location = Location as FileLocation;
			using (Stream s = location.GetStream())
			{
				byte[] buffer = new byte[4048];
				int i;
				while ((i = s.Read(buffer, 0, buffer.Length)) > 0)
					stream.Write(buffer, 0, i);
			}
		}
	}
}
