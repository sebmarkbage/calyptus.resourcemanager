using System;
using System.Collections.Generic;
using Calyptus.ResourceManager;
using System.IO;
using System.Text;

namespace Calyptus.ClientSide.Swiff
{
	public class PlainSwiffClassResource : PlainFlashResource, IJavaScriptResource
	{
		private string _className;
		private IResource _swiffCode;
		private string parameters;
		private string properties;

		public PlainSwiffClassResource(FileLocation location, string className, IResource swiffCode) : base(location)
		{
			if (className == null) throw new NullReferenceException("className cannot be null.");
			if (className == "") throw new FormatException("className cannot be an empty string.");

			_swiffCode = swiffCode;
			_className = className;
		}

		public PlainSwiffClassResource(FileLocation location, string className, IResource swiffCode, object parameters) :
			this(location, className, swiffCode)
		{
			throw new NotImplementedException(); // TODO: JavaScriptSerializaiton
		}

		public PlainSwiffClassResource(FileLocation location, string className, IResource swiffCode, Dictionary<string, string> parameters):
			this(location, className, swiffCode)
		{
			StringBuilder sb = new StringBuilder();
			foreach (var pair in parameters)
			{
				if (sb.Length > 0)
					sb.Append(',');
				else
					sb.Append('{');
				sb.Append('\'').Append(pair.Key).Append("':");
				if (pair.Value == null)
					sb.Append("null");
				{
					sb.Append('\'');
					sb.Append(pair.Value.Replace("\\", "\\\\").Replace("'", "\'").Replace("\n", "\\n"));
					sb.Append('\'');
				}
			}
			if (sb.Length > 0)
			{
				sb.Append('}');
				this.parameters = sb.ToString();
			}
		}

		public PlainSwiffClassResource(FileLocation location, string className, IResource swiffCode, Dictionary<string, string> parameters, object properties):
			this(location, className, swiffCode, parameters)
		{
			throw new NotImplementedException(); // TODO: JavaScriptSerializaiton
		}

		public PlainSwiffClassResource(FileLocation location, string className, IResource swiffCode, object parameters, object properties) :
			this(location, className, swiffCode, parameters)
		{
			throw new NotImplementedException(); // TODO: JavaScriptSerializaiton
		}

		public virtual void RenderJavaScript(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources, bool compress)
		{
			writer.Write("var ");
			writer.Write(_className);
			writer.Write(" = new SwiffClass('");
			writer.Write(urlFactory.GetURL(this));
			writer.Write("'");
			if (parameters != null)
			{
				writer.Write(',');
				writer.Write(parameters);
			}
			if (properties != null)
			{
				writer.Write(',');
				writer.Write(properties);
			}
			writer.WriteLine(");");
		}

		public override IEnumerable<IResource> References
		{
			get { return new IResource[] { _swiffCode }; }
		}

		public bool CanReferenceJavaScript
		{
			get { return false; }
		}
	}
}
