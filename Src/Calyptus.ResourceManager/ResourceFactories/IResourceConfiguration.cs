﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Calyptus.ResourceManager
{
	public interface IResourceConfiguration
	{
		IResource GetResource(IResourceLocation location);
		bool DebugMode { get; }
	}
}
