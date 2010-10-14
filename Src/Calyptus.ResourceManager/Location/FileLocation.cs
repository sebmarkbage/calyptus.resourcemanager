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

		public abstract void MonitorChanges(Action onChange);
		public abstract void StopMonitorChanges(Action onChange);

		public abstract IEnumerable<IResourceLocation> GetRelativeLocations(string name);
		public abstract IResourceLocation GetRelativeLocation(string name);
	}
}
