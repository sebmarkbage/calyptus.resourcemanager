using System;
using EcmaScript.NET;

namespace Yahoo.Yui.Compressor
{
	public class CustomErrorReporter : ErrorReporter
	{
		public CustomErrorReporter(bool isVerboseLogging)
		{
		}

		public virtual void Warning(string message,
			string sourceName,
			int line,
			string lineSource,
			int lineOffset)
		{

		}

		public virtual void Error(string message,
			string sourceName,
			int line,
			string lineSource,
			int lineOffset)
		{
			throw new Calyptus.ResourceManager.ParsingException(message, line, lineOffset, lineSource);
		}

		public virtual EcmaScriptRuntimeException RuntimeError(string message,
			string sourceName,
			int line,
			string lineSource,
			int lineOffset)
		{
			throw new Calyptus.ResourceManager.ParsingException(message, line, lineOffset, lineSource);
		}
	}
}