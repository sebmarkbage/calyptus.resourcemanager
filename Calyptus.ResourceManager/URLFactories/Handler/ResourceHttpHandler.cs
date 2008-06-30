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
				sb.AppendFormat("{0:x2}", b);
			return sb.ToString();
		}

		private static string _assemblyPath = "Assemblies/";
		public static string AssemblyPath
		{
			get
			{
				return _assemblyPath;
			}
			set
			{
				_assemblyPath = value.Trim('/') + "/";
			}
		}

		private static string _extension = ".res.axd";
		public static string Extension
		{
			get
			{
				return _extension;
			}
			set
			{
				_extension = value;
			}
		}

		public void ProcessRequest(HttpContext context)
		{
			HttpRequest request = context.Request;
			HttpResponse response = context.Response;
			string[] ps = request.QueryString[null].Split('-');
			string version = ps.Length > 0 ? ps[0] : null;

			/*string c = context.Request.QueryString["c"];
			if (c != null)
				Thread.CurrentThread.CurrentCulture = new CultureInfo(int.Parse(c, NumberStyles.AllowHexSpecifier));
			string cu = context.Request.QueryString["cu"];
			if (cu != null)
				Thread.CurrentThread.CurrentUICulture = new CultureInfo(int.Parse(cu, NumberStyles.AllowHexSpecifier));*/

			string path = request.AppRelativeCurrentExecutionFilePath;
			path = path.Substring(2, path.Length - 10); // Trim off .res.axd

			string assembly = null;
			if (path.StartsWith(_assemblyPath, StringComparison.InvariantCultureIgnoreCase))
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
				location = ResourceLocations.GetLocation(Assembly.Load(assembly), path);
			else
				location = ResourceLocations.GetLocation("~/", path);

			IResourceConfiguration config = ResourceConfigurationManager.GetConfiguration();
			IResource res = config.GetResource(location);
			if (res == null) throw new HttpException(404, "Resource not found");

			IProxyResource r = res as IProxyResource;
			if (r == null) throw new Exception("Resource is not a IProxyResource.");

			if (ps.Length > 1 && ps[1] != "")
				System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo(int.Parse(ps[1], NumberStyles.AllowHexSpecifier));

			if (ps.Length > 2 && ps[2] != "")
				System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo(int.Parse(ps[2], NumberStyles.AllowHexSpecifier));

			string v = ToHex(res.Version);
			if (v != version)
			{
				response.RedirectLocation = String.Format("{0}?{1}{2}{3}", context.Request.Path, v, ps.Length > 1 ? "-" + ps[1] : null, ps.Length > 2 ? "-" + ps[2] : null);
				response.StatusCode = 301;
				return;
			}

			if (request.Headers["If-None-Match"] == v)
			{
				response.Cache.VaryByHeaders["If-None-Match"] = true;
				response.StatusCode = 304;
				return;
			}

			response.ContentType = r.ContentType;

			response.Cache.SetETag(version);
			response.Cache.VaryByHeaders["Accept-Encoding"] = true;
			response.Cache.SetCacheability(HttpCacheability.Public);
			response.Cache.SetExpires(DateTime.Now.AddYears(1));
			response.Cache.SetMaxAge(new TimeSpan(365, 0, 0, 0));
			response.Cache.SetValidUntilExpires(true);

			ICollection<IResource> writtenResources = new Collection<IResource>();
			IResourceURLFactory urlFactory = config.URLProvider.GetURLFactory(context);

			bool base64support = !"IE".Equals(request.Browser.Browser, StringComparison.InvariantCultureIgnoreCase) || request.Browser.MajorVersion > 7;
			ITextProxyResource tr;
			IBase64TextProxyResource cr;

			string enc = request.Headers["Accept-Encoding"];
			if ((tr = r as ITextProxyResource) != null && tr.Gzip && enc != null && (enc.IndexOf("gzip") != -1 || request.Headers["---------------"] != null) && request.UserAgent.IndexOf("MSIE 6.") == -1)
			{
				enc = enc.IndexOf("x-gzip") != -1 ? "x-gzip" : "gzip";
				response.AppendHeader("Content-Encoding", enc);

				using (Stream compressionStream = new GZipStream(response.OutputStream, CompressionMode.Compress))
				using (TextWriter writer = new StreamWriter(compressionStream, Encoding.UTF8))
					if (base64support && (cr = tr as IBase64TextProxyResource) != null)
						cr.RenderProxyWithBase64(writer, urlFactory, writtenResources);
					else
						tr.RenderProxy(writer, urlFactory, writtenResources);
			}
			else if (base64support && (cr = tr as IBase64TextProxyResource) != null)
				cr.RenderProxyWithBase64(response.Output, urlFactory, writtenResources);
			else if (tr != null)
				tr.RenderProxy(response.Output, urlFactory, writtenResources);
			else
				r.RenderProxy(response.OutputStream, urlFactory, writtenResources);
		}
	}
}
