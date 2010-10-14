using System;
using System.Collections.Generic;
using System.Text;
using Yahoo.Yui.Compressor;
using System.Globalization;

namespace Calyptus.ResourceManager
{
	public class YUICompressor : ICSSCompressor, IJavaScriptCompressor
	{
		public string CompressCSS(string content)
		{
			return CssCompressor.Compress(content, 0, CssCompressionType.Hybrid);
		}

		public string CompressJavaScript(string content)
		{
			return JavaScriptCompressor.Compress(content, false, false, false, false, -1, Encoding.UTF8, CultureInfo.InvariantCulture);
		}

		string ICSSCompressor.Compress(string content)
		{
			return CompressCSS(content);
		}

		string IJavaScriptCompressor.Compress(string content)
		{
			return CompressJavaScript(content);
		}
	}
}
