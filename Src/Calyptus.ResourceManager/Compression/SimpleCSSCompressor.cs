using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Calyptus.ResourceManager
{
	internal class SimpleCSSCompressor : ICSSCompressor
	{
		public string Compress(string content)
		{
			Regex comments = new Regex(@"\/\*[^*]*\*+([^/][^*]*\*+)*\/", RegexOptions.Singleline);
			Regex spaces = new Regex(@"\s+", RegexOptions.Singleline);

			content = comments.Replace(content, "");
			content = spaces.Replace(content, " ").Replace("\t", " ").Trim();

			return content;
		}
	}
}
