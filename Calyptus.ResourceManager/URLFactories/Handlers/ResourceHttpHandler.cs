using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Reflection;
using System.IO;
using System.Globalization;
using System.Threading;
using System.IO.Compression;
using System.Collections.ObjectModel;

namespace Calyptus.ResourceManager
{
	public class ResourceHttpHandler : IHttpHandler
	{
		public bool IsReusable
		{
			get { return true; }
		}

		private string ToHex(byte[] bytes)
		{
			if (bytes == null) return null;
			StringBuilder sb = new StringBuilder();
			foreach (byte b in bytes)
				sb.AppendFormat("{0:x1}", b);
			return sb.ToString();
		}

		public void ProcessRequest(HttpContext context)
		{
			HttpRequest request = context.Request;
			HttpResponse response = context.Response;
			string version = request.QueryString[null];

			/*string c = context.Request.QueryString["c"];
			if (c != null)
				Thread.CurrentThread.CurrentCulture = new CultureInfo(int.Parse(c, NumberStyles.AllowHexSpecifier));
			string cu = context.Request.QueryString["cu"];
			if (cu != null)
				Thread.CurrentThread.CurrentUICulture = new CultureInfo(int.Parse(cu, NumberStyles.AllowHexSpecifier));*/

			string path = request.AppRelativeCurrentExecutionFilePath;
			path = path.Substring(2, path.Length - 10); // Trim off .res.axd

			string assembly = null;
			if (path.StartsWith("assemblies/", StringComparison.InvariantCultureIgnoreCase))
			{
				int i = path.IndexOf('/', 11);
				if (i > 0)
				{
					assembly = path.Substring(11, i - 11);
					path = path.Substring(i + 1);
				}
			}

			IResourceLocation location;
			if (assembly != null)
				location = LocationHelper.GetLocation(Assembly.Load(assembly), path);
			else
				location = LocationHelper.GetLocation("~/", path);

			ResourceConfigurationManager fm = ResourceConfigurationManager.GetFactoryManager();
			IResource res = fm.GetResource(location);
			if (res == null) throw new HttpException(404, "Resource not found");
			string v = ToHex(res.Version);
			if (v != version)
			{
				response.RedirectLocation = String.Format("{0}?{1}", context.Request.Path, v);
				response.StatusCode = 301;
				return;
			}

			if (request.Headers["If-None-Match"] == v)
			{
				response.Cache.VaryByHeaders["If-None-Match"] = true;
				response.StatusCode = 304;
				return;
			}

			IProxyResource r = res as IProxyResource;
			if (r == null) throw new Exception("Resource is not a IProxyResource.");

			response.ContentType = r.ContentType;

			response.Cache.SetETag(version);
			response.Cache.VaryByHeaders["Accept-Encoding"] = true;
			response.Cache.SetCacheability(HttpCacheability.Public);
			response.Cache.SetExpires(DateTime.Now.AddYears(1));
			response.Cache.SetMaxAge(new TimeSpan(365, 0, 0, 0));
			response.Cache.SetValidUntilExpires(true);

			ICollection<IResource> writtenResources = new Collection<IResource>();
			string enc = request.Headers["Accept-Encoding"];
			if (enc != null && (enc.IndexOf("gzip") != -1 || request.Headers["---------------"] != null) && request.UserAgent.IndexOf("MSIE 6.") == -1)
			{
				enc = enc.IndexOf("x-gzip") != -1 ? "x-gzip" : "gzip";
				response.AppendHeader("Content-Encoding", enc);

				using (Stream compressionStream = new GZipStream(response.OutputStream, CompressionMode.Compress))
					using (TextWriter writer = new StreamWriter(compressionStream, Encoding.UTF8))
						r.RenderProxy(writer, writtenResources);
			}
			else
				r.RenderProxy(response.Output, writtenResources);
		}
	}
}
