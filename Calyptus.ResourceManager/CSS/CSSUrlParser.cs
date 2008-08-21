﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Calyptus.ResourceManager
{
	internal static class CSSUrlParser
	{
		private static Regex _urlParser = new Regex("(?<=[\\:\\s]url\\()\\s*[\\'\\\"]?((?<=\\\")[^\\\"]*(?=\\\")|(?<=\\')[^\\']*(?=\\')|[^\\s\\'\\)\\,]+)[\\'\\\"]?\\s*(?:,\\s*[\\'\\\"]?((?<=\\\")[^\\\"]*(?=\\\")|(?<=\\')[^\\']*(?=\\')|[^\\s\\'\\)\\,]+)[\\'\\\"]?)?\\s*(?=\\))", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);
		
		public static string ConvertUrls(string css, IResourceLocation baseLocation, IResourceURLFactory urlFactory, IEnumerable<IImageResource> includedImages)
		{
			return _urlParser.Replace(css, m =>
			{
				string assembly = m.Groups[2].Success ? m.Groups[1].Value : null;
				string filename = m.Groups[2].Success ? m.Groups[2].Value : m.Groups[1].Value;
				IResourceLocation location = ResourceLocations.GetLocation(baseLocation, assembly, filename);
				if (location == null) return m.Groups[0].Value;
				if (includedImages != null)
					foreach (IImageResource res in includedImages)
						if (location.Equals(res.Location))
							return res.GetImageData(false);
				return urlFactory.GetURL(new UnknownResource(location));
			});
		}
	}
}
