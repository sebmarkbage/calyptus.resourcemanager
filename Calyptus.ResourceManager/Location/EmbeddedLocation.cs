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
	}
}
