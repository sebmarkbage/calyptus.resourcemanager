using System;
using System.Collections.Generic;
using Calyptus.ResourceManager;
using System.IO;

namespace Calyptus.ClientSide.Swiff
{
	public class PlainSwiffClassResource : PlainFlashResource, IJavaScriptResource
	{
		private string _className;
		private IResource _swiffCode;

		public PlainSwiffClassResource(FileLocation location, string className, IResource swiffCode) : base(location)
		{
			if (className == null) throw new NullReferenceException("className cannot be null.");
			if (className == "") throw new FormatException("className cannot be an empty string.");

			_swiffCode = swiffCode;
			_className = className;
		}

		public void RenderJavaScript(TextWriter writer, IResourceURLFactory urlFactory, ICollection<IResource> writtenResources, bool compress)
		{
			writer.Write("var ");
			writer.Write(_className);
			writer.Write(" = new SwiffClass('");
			writer.Write(urlFactory.GetURL(this));
			writer.WriteLine("');");
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
