﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Calyptus.ResourceManager
{
	public interface IImageResource : IResource
	{
		string ContentType { get; }
		byte[] GetImageData();
	}
}
