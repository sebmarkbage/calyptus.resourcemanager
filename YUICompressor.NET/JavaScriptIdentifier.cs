using System;
using Org.Mozilla.JavaScript;

namespace Com.Yahoo.Platform.YUI.Compressor
{
	class JavaScriptIdentifier : JavaScriptToken
	{
		public bool MarkedForMunge { get; set; }
		public string MungedValue { get; set; }

		private int RefCount = 0;
		private ScriptOrFnScope DeclaredScope;

		public JavaScriptIdentifier(){}
		public JavaScriptIdentifier(string value, ScriptOrFnScope declaredScope) : base(Token.NAME, value)
		{
			DeclaredScope = declaredScope;
			MarkedForMunge = true;
		}
	}
}
