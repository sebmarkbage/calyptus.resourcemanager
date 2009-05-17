using System.IO;
using System.Security.Cryptography;
using System;
using System.Collections.Generic;

namespace Calyptus.ResourceManager
{
	public abstract class FileLocation : IResourceLocation
	{
		public abstract Stream GetStream();
		public abstract string FileName
		{
			get;
		}

		private byte[] _version;
		public byte[] Version
		{
			get
			{
				if (_version == null)
				{
					_version = GetVersion();
				}
				return _version;
			}
		}

		protected virtual byte[] GetVersion()
		{
			using (var s = GetStream())
				return ChecksumHelper.GetChecksum(s);
		}

		protected virtual void OnChanged()
		{
			if (_version == null || !_version.Equals(GetVersion()))
				Changed();
		}

		public virtual void MonitorChanges(Action onChange)
		{
			Changed += onChange;
		}

		public event Action Changed;

		public abstract IEnumerable<IResourceLocation> GetRelativeLocations(string name);
		public abstract IResourceLocation GetRelativeLocation(string name);
	}
}
