using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Net;

namespace Calyptus.ResourceManager
{
	public class ExternalLocation : FileLocation
	{
		public ExternalLocation(Uri uri)
		{
			this.Uri = uri;
		}

		public Uri Uri
		{
			get;
			private set;
		}

		public override Stream GetStream()
		{
			WebClient client = new WebClient();
			return client.OpenRead(Uri);
		}

		public override string FileName
		{
			get
			{ 
				int i = Uri.AbsolutePath.LastIndexOf('/', 1);
				return i > 0 ? Uri.AbsolutePath.Substring(i + 1) : Uri.AbsolutePath.Substring(1);
			}
		}

		public override bool Equals(object obj)
		{
			ExternalLocation l = obj as ExternalLocation;
			if (l == null) return false;
			return Uri.Equals(l.Uri);
		}

		public override int GetHashCode()
		{
			return Uri.GetHashCode();
		}
	}
}
