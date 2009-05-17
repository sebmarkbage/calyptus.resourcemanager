using System;

namespace Calyptus.ResourceManager
{
	public class ParsingException : Exception
	{
		private string message;
		public int Line { get; private set; }
		public string LineSource { get; private set; }
		public int LineOffset { get; private set; }

		public ParsingException(string message, int line, int lineoffset, string linesource)
			: base()
		{
			this.message = message;
			this.Line = line;
			this.LineOffset = lineoffset;
			this.LineSource = linesource;
		}

		public override string Message
		{
			get
			{
				return message;
			}
		}
	}
}
