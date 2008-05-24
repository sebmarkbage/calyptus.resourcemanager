using System;
using System.Collections.Generic;
using System.Text;

namespace Calyptus.ResourceManager
{
	public interface ICSSCompressor
	{
		string Compress(string content);
	}
}
