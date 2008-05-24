using System.IO;
using System.Security.Cryptography;

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
		public virtual byte[] Version
		{
			get
			{
				if (_version == null)
				{
					using(var s = GetStream())
						_version = ChecksumHelper.GetChecksum(s);
				}
				return _version;
			}
		}
	}
}
