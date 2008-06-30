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
			if (l == null) return null;
			if (!l.FileName.EndsWith(".css", StringComparison.InvariantCultureIgnoreCase))
			{
				ExternalLocation el = l as ExternalLocation;
				if (el == null || el.Mime == null || !el.Mime.EndsWith("/css", StringComparison.InvariantCultureIgnoreCase))
					return null;
			}

			Stream s = l.GetStream();
			if (s == null) return null;

			SyntaxReader reader;
			using (TextReader r = new StreamReader(s))
				reader = new SyntaxReader(r, false, location, null);

			Compress compress = reader.Compress == null ? Compress.Release : (Compress)Enum.Parse(typeof(Compress), reader.Compress, true);
			bool cmpr = (compress == Compress.Always || (compress == Compress.Release && !Configuration.DebugMode));

			var includes = new List<ICSSResource>();
			var imageIncludes = new List<IImageResource>();
			var builds = new List<ICSSResource>();
			var references = new List<IResource>();
			if (reader.References != null)
				foreach (var rl in reader.References)
				{
					var resource = Configuration.GetResource(rl);
					if (resource == null) throw new Exception(String.Format("Resource '{0}' is not a valid resource.", rl));
					if (!references.Contains(resource))
						references.Add(resource);
				}
			if (reader.Includes != null)
				foreach (var rl in reader.Includes)
				{
					if (location.Equals(rl)) continue;
					var resource = Configuration.GetResource(rl);
					if (resource == null) throw new Exception(String.Format("Resource '{0}' is not a valid resource.", rl));
					if (resource is ICSSResource && cmpr)
						includes.Add((ICSSResource)resource);
					else if (resource is IImageResource)
					{
						if (!Configuration.DebugMode)
							imageIncludes.Add((IImageResource)resource);
					}
					else if (!references.Contains(resource))
						references.Add(resource);
				}
			if (reader.Builds != null)
				foreach (var rl in reader.Builds)
				{
					var resource = Configuration.GetResource(rl);
					if (resource == null) throw new Exception(String.Format("Resource '{0}' is not a valid resource.", rl));
					if (resource is ICSSResource && cmpr)
						builds.Add((ICSSResource) resource);
					else if (resource is IImageResource)
					{
						if (!Configuration.DebugMode)
							imageIncludes.Add((IImageResource)resource);
					}
					else if (!references.Contains(resource))
						references.Add(resource);
				}
			if ((reader.Compress == null || compress == Compress.Never) && reader.Includes == null && reader.Builds == null && l is VirtualPathLocation)
				return new PlainCSSResource(references.Count > 0 ? references.ToArray() : null, l, reader.HasContent);
			else
				return new ExtendedCSSResource(
					reader.Compress == null ? (bool?)null : cmpr,
					references.Count > 0 ? references.ToArray() : null,
					imageIncludes.Count > 0 ? imageIncludes.ToArray() : null,
					includes.Count > 0 ? includes.ToArray() : null,
					builds.Count > 0 ? builds.ToArray() : null,
					l,
					reader.HasContent);
		}
	}
}
