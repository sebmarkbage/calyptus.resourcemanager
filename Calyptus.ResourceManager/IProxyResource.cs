using System;
using System.IO;
using System.Collections.Generic;
using System.Web;
using System.Globalization;

namespace Calyptus.ResourceManager
{
	public interface IProxyResource : IResource
	{
		bool CultureSensitive { get; }
		bool CultureUISensitive { get; }
		string ContentType { get; }
		void RenderProxy(Stream stream, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources);
	}

	public interface ITextProxyResource : IProxyResource
	{
		bool Gzip { get; }
		void RenderProxy(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources);
	}

	public interface IBase64TextProxyResource : ITextProxyResource
	{
		void RenderProxyWithBase64(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources);
	}
}
