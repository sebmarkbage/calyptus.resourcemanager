﻿using Calyptus.ResourceManager;
using System.Reflection;

namespace GoogleAPIs
{
	public class MooTools : ResourcePackage
	{
		public MooTools(IResourceConfiguration configuration)
			: base(
				configuration,
				new ExternalLocation("http://ajax.googleapis.com/ajax/libs/mootools/1.2.1/mootools-yui-compressed.js"),
				ResourceLocations.GetAssemblyLocations(Assembly.GetExecutingAssembly(), "MooTools.*.js")
			) {}
	}
}