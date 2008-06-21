using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Calyptus.ResourceManager
{
	public class JavaScriptFactory : ResourceFactoryBase
	{
		public override IResource GetResource(IResourceLocation location)
		{
			FileLocation l = location as FileLocation;
			if (l == null) return null;
			if (!l.FileName.EndsWith(".js", StringComparison.InvariantCultureIgnoreCase))
			{
				ExternalLocation el = l as ExternalLocation;
				if (el == null || el.Mime == null || (!el.Mime.EndsWith("/javascript", StringComparison.InvariantCultureIgnoreCase) && !el.Mime.EndsWith("/x-javascript", StringComparison.InvariantCultureIgnoreCase)))
					return null;
			}

			Stream s = l.GetStream();
			if (s == null) return null;

			SyntaxReader reader;
			using (TextReader r = new StreamReader(s))
				reader = new SyntaxReader(r, true);

			Compress compress = reader.Compress == null ? Compress.Release : (Compress)Enum.Parse(typeof(Compress), reader.Compress, true);

			var includes = new List<IResource>();
			var builds = new List<IResource>();
			var references = new List<IResource>();
			if (reader.References != null)
				foreach (var r in reader.References)
				{
					IResourceLocation rl = r.GetLocation(location);
					if (location.Equals(rl)) continue;
					var resource = Configuration.GetResource(rl);
					if (resource == null) throw new Exception(String.Format("Resource '{0}' is not a valid resource.", r.Assembly == null ? r.Filename : r.Assembly + ", " + r.Filename));
					if (!references.Contains(resource))
						references.Add(resource);
				}
			if (reader.Includes != null)
				foreach (var r in reader.Includes)
				{
					IResourceLocation rl = r.GetLocation(location);
					if (location.Equals(rl)) continue;
					var resource = Configuration.GetResource(rl);
					if (resource == null) throw new Exception(String.Format("Resource '{0}' is not a valid resource.", r.Assembly == null ? r.Filename : r.Assembly + ", " + r.Filename));
					if (resource is IJavaScriptResource && !Configuration.DebugMode)
						includes.Add(resource);
					else if (!references.Contains(resource))
						references.Add(resource); // throw new Exception(String.Format("Resource {0}, {1} is not a JavaScript resource.", r.Assembly, r.Filename));
				}
			if (reader.Builds != null)
				foreach (var r in reader.Builds)
				{
					IResourceLocation rl = r.GetLocation(location);
					if (location.Equals(rl)) continue;
					var resource = Configuration.GetResource(rl);
					if (resource == null) throw new Exception(String.Format("Resource '{0}' is not a valid resource.", r.Assembly == null ? r.Filename : r.Assembly + ", " + r.Filename));
					if (resource is IJavaScriptResource && !Configuration.DebugMode)
						builds.Add(resource);
					else if (!references.Contains(resource))
						references.Add(resource); // throw new Exception(String.Format("Resource {0}, {1} is not a JavaScript resource and cannot be included.", r.Assembly, r.Filename));
				}

			IResourceLocation rf = FileResourceHelper.GetRelatedResourceLocation(location);
			if (rf != null)
			{
				IResource rs = Configuration.GetResource(rf);
				if (rs != null)
				{
					if (rs is IJavaScriptResource && !Configuration.DebugMode)
						includes.Add(rs);
					else
						references.Add(rs);
				}
			}

			if ((reader.Compress == null || compress == Compress.Never) && reader.Includes == null && reader.Builds == null && !(l is EmbeddedLocation))
				return new PlainJavaScriptResource(references.Count > 0 ? references.ToArray() : null, l, reader.HasContent);
			else
				return new ExtendedJavaScriptResource(
					reader.Compress == null ? (bool?)null : (compress == Compress.Always || (compress == Compress.Release && !Configuration.DebugMode)),
					references.Count > 0 ? references.ToArray() : null,
					includes.Count > 0 ? includes.ToArray() : null,
					builds.Count > 0 ? builds.ToArray() : null,
					l,
					reader.HasContent);
		}
	}
}
