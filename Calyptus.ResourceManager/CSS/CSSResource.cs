using System;
using System.Collections.Generic;
using System.Text;

namespace Calyptus.ResourceManager
{
	public class CSSResource : ICSSResource
	{
		public string GetCSS()
		{
			throw new NotImplementedException();
		}

		public IResourceLocation Location
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public IResource[] GetReferences()
		{
			throw new NotImplementedException();
		}

		public Compress Compress
		{
			get
			{
				throw new System.NotImplementedException();
			}
			set
			{
			}
		}

		public IResource[] GetIncludes()
		{
			throw new NotImplementedException();
		}

		public string GetVersion()
		{
			throw new NotImplementedException();
		}
	}
}
