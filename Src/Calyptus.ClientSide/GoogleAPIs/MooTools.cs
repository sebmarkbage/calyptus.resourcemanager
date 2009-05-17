using Calyptus.ResourceManager;
using System.Reflection;

namespace GoogleAPIs
{
	public class MooTools : ResourcePackage
	{
		public MooTools(IResourceConfiguration configuration)
			: base(
				configuration,
				new ExternalLocation("http://ajax.googleapis.com/ajax/libs/mootools/1.11/mootools-yui-compressed.js"),
				ResourceLocations.GetLocations(Assembly.GetExecutingAssembly(), "MooTools.*.js")
			) {}
	}
}