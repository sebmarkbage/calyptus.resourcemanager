using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;

namespace Calyptus.ResourceManager
{
	public class EmbeddedLocation : FileLocation
	{
		public EmbeddedLocation(Assembly assembly, string resourceName)
		{
			if (assembly == null) throw new ArgumentNullException("Assembly for an embedded assembly cannot be null.");
			if (assembly.GetManifestResourceInfo(resourceName) == null) throw new ArgumentException("The resource '" + resourceName + "' does not exist within the assembly '" + assembly.GetName().FullName + "'");
			
			Assembly = assembly;
			ResourceName = resourceName;
		}

		public Assembly Assembly
		{
			get;
			private set;
		}

		public string ResourceName
		{
			get;
			private set;
		}

		public override Stream GetStream()
		{
			return Assembly.GetManifestResourceStream(ResourceName);
		}

		public override string FileName
		{
			get { return ResourceName; }
		}

		public override bool Equals(object obj)
		{
			EmbeddedLocation l = obj as EmbeddedLocation;
			if (l == null) return false;
			return l.Assembly.Equals(Assembly) && l.ResourceName.Equals(ResourceName, StringComparison.InvariantCultureIgnoreCase);
		}

		public override int GetHashCode()
		{
			return (Int32) ((Int64) Assembly.GetHashCode() + (Int64)ResourceName.ToLower().GetHashCode() % Int32.MaxValue);
		}

		public override string ToString()
		{
			return Assembly.GetName().Name + ", " + ResourceName;
		}

		public override IResourceLocation GetRelativeLocation(string name)
		{
			IResourceLocation l = ResourceLocations.GetAssemblyLocation(Assembly, name);
			if (l == null)
				foreach (AssemblyName r in Assembly.GetReferencedAssemblies())
				{
					l = ResourceLocations.GetAssemblyLocation(Assembly.Load(r), name);
					if (l != null) break;
				}
			return l;
		}

		public override IEnumerable<IResourceLocation> GetRelativeLocations(string name)
		{
			List<IResourceLocation> ls = new List<IResourceLocation>(ResourceLocations.GetAssemblyLocations(this.Assembly, name));
			if (ls.Count > 0 && !name.Contains("*")) return ls;
			foreach (AssemblyName r in this.Assembly.GetReferencedAssemblies())
			{
				IEnumerable<IResourceLocation> l = ResourceLocations.GetAssemblyLocations(Assembly.Load(r), name);
				if (l != null)
					ls.AddRange(l);
			}
			return (ls.Count > 0) ? ls : null;
		}

		public override void MonitorChanges(Action onChange)
		{
		}

		public override void StopMonitorChanges(Action onChange)
		{
		}
	}
}
