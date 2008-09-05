using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Web;
using System.Security.Cryptography;

namespace Calyptus.ResourceManager
{
	public class ProxyFlashResource : PlainFlashResource, IProxyResource
	{
		public ProxyFlashResource(FileLocation location) : base(location) { }

		public string ContentType
		{
			get { return "application/x-shockwave-flash"; }
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
				byte[] buffer = new byte[0x1000];
				int i;
				while ((i = s.Read(buffer, 0, buffer.Length)) > 0)
					stream.Write(buffer, 0, i);
			}
		}
	}
}
