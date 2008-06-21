using System;
using System.Collections.Generic;
using System.Text;

namespace Calyptus.ResourceManager
{
	public class ImageFactory : ResourceFactoryBase
	{
		private Dictionary<string, string> _mimeExtensions = new Dictionary<string, string>
		{
			{ ".jpe", "image/jpeg" },
			{ ".jpg", "image/jpeg" },
			{ ".jpeg", "image/jpeg" },
			{ ".gif", "image/gif" },
			{ ".png", "image/png" },
			{ ".tif", "image/tiff" },
			{ ".tiff", "image/tiff" },
			{ ".bmp", "image/bmp" }
		};

		public override IResource GetResource(IResourceLocation location)
		{
			FileLocation l = location as FileLocation;
			if (l == null) return null;

			string mime = GetMime(l.FileName);
			if (mime == null)
			{
				ExternalLocation el = l as ExternalLocation;
				if (el == null)
					return null;
				mime = el.Mime;
				if (!IsValidMime(mime))
					return null;
			}

			if (l is EmbeddedLocation)
				return new ProxyImageResource(mime, l);
			else
				return new PlainImageResource(mime, l);
		}

		private string GetMime(string filename)
		{
			foreach (KeyValuePair<string, string> vp in _mimeExtensions)
				if (filename.EndsWith(vp.Key, StringComparison.InvariantCultureIgnoreCase))
					return vp.Value;
			return null;
		}

		private bool IsValidMime(string mime)
		{
			if (mime == null) return false;
			mime = mime.Trim();
			foreach (string v in _mimeExtensions.Values)
				if (v.Equals(mime, StringComparison.InvariantCultureIgnoreCase))
					return true;
			return false;
		}
	}
}
