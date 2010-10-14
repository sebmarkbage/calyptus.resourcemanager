using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Calyptus.ResourceManager
{
	public class TypeLocation : IResourceLocation
	{
		public TypeLocation(Type type)
		{
			ProxyType = type;
		}
		public Type ProxyType
		{
			get;
			private set;
		}

		public override bool Equals(object obj)
		{
			TypeLocation l = obj as TypeLocation;
			if (l == null) return false;
			return l.ProxyType.Equals(ProxyType);
		}

		public override int GetHashCode()
		{
			return ProxyType.GetHashCode();
		}

		private byte[] _version;

		public byte[] Version
		{
			get
			{
				if (_version == null)
				{
					Version version = ProxyType.Assembly.GetName().Version;
					_version = new byte[] {
						(byte)((version.Major << 4 & 240) | (version.Minor & 15)),
						(byte)(version.Build & 255),
						(byte)(version.Revision >> 8 & 255),
						(byte)(version.Revision & 255)
					};
				}
				return _version;
			}
		}

		public override string ToString()
		{
			return ProxyType.FullName;
		}

		public void MonitorChanges(Action onChanged)
		{
		}

		public void StopMonitorChanges(Action onChanged)
		{
		}

		public IResourceLocation GetRelativeLocation(string name)
		{
			Assembly a = ProxyType.Assembly;
			IResourceLocation l = ResourceLocations.GetAssemblyLocation(a, name);
			if (l == null)
				foreach (AssemblyName r in a.GetReferencedAssemblies())
				{
					l = ResourceLocations.GetAssemblyLocation(Assembly.Load(r), name);
					if (l != null) break;
				}
			return l;
		}

		public IEnumerable<IResourceLocation> GetRelativeLocations(string name)
		{
			Assembly a = this.ProxyType.Assembly;
			List<IResourceLocation> ls = new List<IResourceLocation>(ResourceLocations.GetAssemblyLocations(a, name));
			if (ls.Count > 0 && !name.Contains("*")) return ls;
			foreach (AssemblyName r in a.GetReferencedAssemblies())
			{
				IEnumerable<IResourceLocation> l = ResourceLocations.GetAssemblyLocations(Assembly.Load(r), name);
				if (l != null)
					ls.AddRange(l);
			}
			return (ls.Count > 0) ? ls : null;
		}
	}
}
