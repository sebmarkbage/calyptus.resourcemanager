﻿using System;
using Calyptus.ClientSide.Swiff;
using Calyptus.ResourceManager;
using System.Collections.Generic;

namespace Calyptus.Upload
{
	public class Swiff : ProxySwiffClassResource
	{
		public Swiff(IResourceConfiguration config)
			: base(
				new EmbeddedLocation(typeof(Swiff).Assembly, "Calyptus.ClientSide.Lib.CompiledSwiff.Upload.swf"),
				"Upload_Swiff",
				config.GetResource(new EmbeddedLocation(typeof(Swiff).Assembly, "Calyptus.SwiffClass.js")),
				new Dictionary<string, string>
				{
					{ "wMode", "transparent" }
				}
			) { }
	}
}
