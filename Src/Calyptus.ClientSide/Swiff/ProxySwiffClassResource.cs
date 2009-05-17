using System;
using System.Collections.Generic;
using Calyptus.ResourceManager;
using System.IO;

namespace Calyptus.ClientSide.Swiff
{
	public class ProxySwiffClassResource : PlainSwiffClassResource, IProxyResource
	{
		public ProxySwiffClassResource(FileLocation location, string className, IResource swiffCode) 
			: base(location, className, swiffCode)
		{
		}

		public ProxySwiffClassResource(FileLocation location, string className, IResource swiffCode, Dictionary<string, string> parameters)
			: base(location, className, swiffCode, parameters)
		{
		}

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
