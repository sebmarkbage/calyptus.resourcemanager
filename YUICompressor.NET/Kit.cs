using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Mozilla.JavaScript
{
	class Kit
	{
		public static void CodeBug()
		{
			throw new ApplicationException("Failed Assertion");
		}
	}
}
