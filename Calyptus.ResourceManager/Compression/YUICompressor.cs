using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Calyptus.ResourceManager
{
	public class YUICompressor : ICSSCompressor
	{
		public string CompressCSS(string content)
		{
			return YUI.CSSMinify.Minify(content).Trim();
		}

		string ICSSCompressor.Compress(string content)
		{
			return CompressCSS(content);
		}
	}
}
