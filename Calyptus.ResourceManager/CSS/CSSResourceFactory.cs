using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Calyptus.ResourceManager
{
	public class CSSFactory : ResourceFactoryBase
	{
		public override IResource GetResource(IResourceLocation location)
		{
			FileLocation l = location as FileLocation;
			if (l == null || !l.FileName.EndsWith(".css", StringComparison.InvariantCultureIgnoreCase)) return null;

			Stream s = l.GetStream();
			if (s == null) return null;

			SyntaxReader reader;
			using (TextReader r = new StreamReader(s))
				reader = new SyntaxReader(r);

			Compress compress = reader.Compress == null ? Compress.Release : (Compress)Enum.Parse(typeof(Compress), reader.Compress, true);

			var includes = new List<IResource>();
			var builds = new List<IResource>();
			var references = new List<IResource>();
			if (reader.Includes != null)
				foreach (var r in reader.Includes)
				{
					IResourceLocation rl = r.GetLocation(location);
					if (location.Equals(rl)) continue;
					var resource = FactoryManager.GetResource(rl);
					if (resource == null) throw new Exception(String.Format("Resource {0}, {1} is not a valid resource.", r.Assembly, r.Filename));
					if (resource is ICSSResource)
						includes.Add(resource);
					else if (!references.Contains(resource))
						references.Add(resource); // throw new Exception(String.Format("Resource {0}, {1} is not a JavaScript resource.", r.Assembly, r.Filename));
				}
			if (reader.Builds != null)
				foreach (var r in reader.Builds)
				{
					IResourceLocation rl = r.GetLocation(location);
					if (location.Equals(rl)) continue;
					var resource = FactoryManager.GetResource(rl);
					if (resource == null) throw new Exception(String.Format("Resource {0}, {1} is not a valid resource.", r.Assembly, r.Filename));
					if (resource is ICSSResource)
						builds.Add(resource);
					else if (!references.Contains(resource))
						references.Add(resource); // throw new Exception(String.Format("Resource {0}, {1} is not a JavaScript resource and cannot be included.", r.Assembly, r.Filename));
				}
			if (reader.References != null)
				foreach (var r in reader.References)
				{
					IResourceLocation rl = r.GetLocation(location);
					if (location.Equals(rl)) continue;
					var resource = FactoryManager.GetResource(rl);
					if (resource == null) throw new Exception(String.Format("Resource {0}, {1} is not a valid resource.", r.Assembly, r.Filename));
					if (!references.Contains(resource))
						references.Add(resource);
				}
			if ((reader.Compress == null || compress == Compress.Never) && reader.Includes == null && reader.Builds == null && l is VirtualPathLocation)
				return new PlainCSSResource(references.Count > 0 ? references.ToArray() : null, l);
			else
				return new ExtendedCSSResource(reader.Compress == null ? (bool?)null : compress != Compress.Never, references.Count > 0 ? references.ToArray() : null, includes.Count > 0 ? includes.ToArray() : null, builds.Count > 0 ? builds.ToArray() : null, l);
		}
	}
}
