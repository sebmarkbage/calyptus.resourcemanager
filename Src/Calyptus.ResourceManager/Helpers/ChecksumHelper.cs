using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace Calyptus.ResourceManager
{
	public static class ChecksumHelper
	{
		public static byte[] GetChecksum(Stream stream)
		{
			CRC32 crc = new CRC32();
			return BitConverter.GetBytes(crc.GetCrc32(stream));

			//MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
			//return md5.ComputeHash(stream);
		}

		public static byte[] GetCombinedChecksum(byte[] baseChecksum, params IEnumerable<IResource>[] resources)
		{
			return GetChecksum(new CombinedStream(baseChecksum, GetCombinedEnumerator(resources).GetEnumerator()));
		}

		private static IEnumerable<IResource> GetCombinedEnumerator(IEnumerable<IEnumerable<IResource>> resources)
		{
			foreach (IEnumerable<IResource> ress in resources)
				if (ress != null)
					foreach (IResource res in ress)
						yield return res;
		}

		private class CombinedStream : Stream
		{
			private int _index;
			private byte[] _currentVersion;
			IEnumerator<IResource> _en;

			public CombinedStream(byte[] baseVersion, IEnumerator<IResource> resourceEnumerator)
			{
				_en = resourceEnumerator;
				_currentVersion = baseVersion;
				_index = 0;
			}

			public override bool CanRead
			{
				get { return true; }
			}

			public override bool CanSeek
			{
				get { return false; }
			}

			public override bool CanWrite
			{
				get { return false; }
			}

			public override void Flush()
			{
			}

			public override long Length
			{
				get { return 0; }
			}

			public override long Position
			{
				get
				{
					return 0;
				}
				set
				{
					throw new NotImplementedException();
				}
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				if (_currentVersion == null) return -1;
				int i;
				for(i = 0; i < count; i++)
				{
					while (_index >= _currentVersion.Length)
					{
						if (!_en.MoveNext()) return i;
						_currentVersion = _en.Current.Version;
						_index = 0;
					}
					buffer[offset + i] = _currentVersion[_index];
					_index++;
				}
				return i;
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				return 0;
			}

			public override void SetLength(long value)
			{
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
			}
		}

		private class CRC32
		{
			private UInt32[] crc32Table;
			private const int BUFFER_SIZE = 1024;

			public UInt32 GetCrc32(System.IO.Stream stream)
			{
				unchecked
				{
					UInt32 crc32Result;
					crc32Result = 0xFFFFFFFF;
					byte[] buffer = new byte[BUFFER_SIZE];
					int readSize = BUFFER_SIZE;

					int count = stream.Read(buffer, 0, readSize);
					while (count > 0)
					{
						for (int i = 0; i < count; i++)
						{
							crc32Result = ((crc32Result) >> 8) ^ crc32Table[(buffer[i]) ^
							 ((crc32Result) & 0x000000FF)];
						}
						count = stream.Read(buffer, 0, readSize);
					}

					return ~crc32Result;
				}
			}

			public CRC32()
			{
				unchecked
				{
					UInt32 dwPolynomial = 0xEDB88320;
					UInt32 i, j;

					crc32Table = new UInt32[256];

					UInt32 dwCrc;
					for (i = 0; i < 256; i++)
					{
						dwCrc = i;
						for (j = 8; j > 0; j--)
						{
							if ((dwCrc & 1) == 1)
							{
								dwCrc = (dwCrc >> 1) ^ dwPolynomial;
							}
							else
							{
								dwCrc >>= 1;
							}
						}
						crc32Table[i] = dwCrc;
					}
				}
			}
		}
	}
}
