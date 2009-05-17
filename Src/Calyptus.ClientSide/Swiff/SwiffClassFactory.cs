using System;
using Calyptus.ResourceManager;

namespace Calyptus.ClientSide.Swiff
{
	public class SwiffClassFactory : ResourceFactoryBase
	{
		public override IResource GetResource(IResourceLocation location)
		{
			FileLocation l = location as FileLocation;
			if (l == null) return null;
			if (!l.FileName.EndsWith(".class.swf", StringComparison.InvariantCultureIgnoreCase)) return null;

			string className = l.FileName.Substring(0, l.FileName.Length - 10);

			IResource swiffCode = base.Configuration.GetResource(new EmbeddedLocation(typeof(SwiffClassFactory).Assembly, "Calyptus.SwiffClass.js"));

			if (l is EmbeddedLocation)
				return new ProxySwiffClassResource(l, className, swiffCode);
			else
				return new PlainSwiffClassResource(l, className, swiffCode);
		}
	}
}
