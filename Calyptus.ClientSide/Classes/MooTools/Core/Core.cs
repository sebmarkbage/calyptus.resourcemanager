using System;
using Calyptus.ResourceManager;

namespace MooTools
{
	public class Core : ExtendedJavaScriptResource
	{
		public Core() : base(true, null, null, null, new EmbeddedLocation(typeof(Core).Assembly, "Calyptus.ClientSide.JavaScript.MooTools.Core.Core.js")) { }
	}
}
