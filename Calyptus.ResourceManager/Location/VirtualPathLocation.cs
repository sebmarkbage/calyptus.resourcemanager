using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Web;

namespace Calyptus.ResourceManager
{
	public class VirtualPathLocation : FileLocation
	{
		public VirtualPathLocation(string basePath, string relativePath)
		{
			VirtualPath = System.Web.VirtualPathUtility.Combine(basePath, relativePath);
			VirtualPath = System.Web.VirtualPathUtility.ToAbsolute(VirtualPath);
		}

		public string VirtualPath
		{
			get;
			private set;
		}

		public override Stream GetStream()
		{
			return System.Web.Hosting.VirtualPathProvider.OpenFile(VirtualPath);
		}

		public override string FileName
		{
			get { return System.Web.VirtualPathUtility.GetFileName(VirtualPath); }
		}

		public override bool Equals(object obj)
		{
			VirtualPathLocation l = obj as VirtualPathLocation;
			if (l == null) return false;
			return l.VirtualPath.Equals(VirtualPath, StringComparison.InvariantCultureIgnoreCase);
		}

		public override int GetHashCode()
		{
			return VirtualPath.ToLower().GetHashCode();
		}
	}
}
