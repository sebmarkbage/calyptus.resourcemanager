using System;
using Org.Mozilla.JavaScript;

namespace Com.Yahoo.Platform.YUI.Compressor
{
	class JavaScriptToken
	{
		public Token Type { get; set; }
		public string Value { get; set; }

		public JavaScriptToken(Token type, string value)
		{
			Type = type;
			Value = value;
		}
	}
}
