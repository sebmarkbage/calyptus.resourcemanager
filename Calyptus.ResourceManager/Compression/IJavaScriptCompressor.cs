using System;
using System.Collections.Generic;
using System.Text;

namespace Calyptus.ResourceManager
{
	public interface IJavaScriptCompressor
	{
		string Compress(string content);
	}
}
