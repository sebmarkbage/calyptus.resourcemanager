using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;

namespace Calyptus.ResourceManager
{
	public class JavaScriptResource : IJavaScriptResource, IRenderableResource
	{
		public JavaScriptResource(FactoryManager factoryManager, FileLocation location)
		{
			_location = location;

			Stream s = location.GetStream();
			if (s == null) throw new Exception(String.Format("File could not be opened ({0})", location.FileName));

			SyntaxReader reader;
			using (TextReader r = new StreamReader(s))
				reader = new SyntaxReader(r);

			if (reader.Compress == null && reader.Includes == null && reader.References == null)
			{
				_isPlain = true;
				return;
			}

			Compress = reader.Compress == null ? Compress.Release : (Compress) Enum.Parse(typeof(Compress), reader.Compress, true);

			var includes = new List<IResource>();
			if (reader.Includes != null)
			{
				foreach (var r in reader.Includes)
				{
					var resource = factoryManager.GetResource(r.GetLocation(location));
					if (resource == null) throw new Exception(String.Format("Resource {0}, {1} is not a valid resource.", r.Assembly, r.Filename));
					if (!(resource is IJavaScriptResource)) throw new Exception(String.Format("Resource {0}, {1} is not a JavaScript resource.", r.Assembly, r.Filename));
					includes.Add(resource);
				}
			}
			var references = new List<IResource>();
			if (reader.References != null)
			{
				foreach (var r in reader.References)
				{
					var resource = factoryManager.GetResource(r.GetLocation(location));
					if (resource == null) throw new Exception(String.Format("Resource {0}, {1} is not a valid resource.", r.Assembly, r.Filename));
					references.Add(resource);
				}
			}
			if (includes.Count > 0) _includes = includes.ToArray();
			if (references.Count > 0) _references = references.ToArray();
		}

		private bool _isPlain;

		public string GetJavaScript()
		{
			var r = new StreamReader(_location.GetStream());
			var s = r.ReadToEnd();
			r.Close();
			return s;
		}

		private FileLocation _location;

		public IResourceLocation Location { get { return _location; } }

		private IResource[] _includes;
		private IResource[] _references;

		public IResource[] GetIncludes()
		{
			return _includes;
		}

		public IResource[] GetReferences()
		{
			return _references;
		}

		public Compress Compress
		{
			get;
			private set;
		}

		public string GetVersion()
		{
			return "CRC"; //TODO: Calculate CRC
		}

		public void Render(IOutputWriter writer)
		{
			writer.ContentType = "text/javascript";

			IResource[] reses = GetIncludes();
			if (reses != null)
				foreach (IResource res in reses)
					RenderChild(writer, res, true);

			writer.Render(GetJavaScript());
		}

		public void RenderAllReferences(IOutputWriter writer)
		{
			writer.ContentType = "text/javascript";
			RenderChild(writer, this, true);
		}

		private void RenderChild(IOutputWriter writer, IResource resource, bool renderReferences)
		{
			IResource[] reses = resource.GetIncludes();
			if (reses != null)
				foreach (IResource res in reses)
					RenderChild(writer, res, renderReferences);

			if (renderReferences)
			{
				reses = resource.GetReferences();
				if (reses != null)
					foreach (IResource res in reses)
						RenderChild(writer, res, renderReferences);
			}

			if (!(resource is IJavaScriptResource)) throw new Exception("Cannot include non-javascript reference in a javascript");
			writer.Render((resource as IJavaScriptResource).GetJavaScript());
			writer.Render("\r\n");
		}
	}
}
