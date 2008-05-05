using System;
using System.Collections.Generic;
using System.Text;

namespace Com.Yahoo.Platform.YUI.Compressor
{
	class ScriptOrFnScope
	{
		public int BraceNesting { get; private set; }
		public ScriptOrFnScope ParentScope { get; set; }

		public List<ScriptOrFnScope> SubScopes { get; set; }
		public Dictionary<string, JavaScriptIdentifier> Identifiers { get; set; }
		public Dictionary<string, string> Hints { get; set; }

		private bool MarkedForMunging = true;

		public ScriptOrFnScope(int braceNesting, ScriptOrFnScope parentScope)
		{
			BraceNesting = braceNesting;
			SubScopes = new List<ScriptOrFnScope>();
			if (parentScope != null)
				parentScope.SubScopes.Add(this);
		}

		public JavaScriptIdentifier DeclareIdentifier(string symbol)
		{
			if (Identifiers.ContainsKey(symbol))
				return Identifiers[symbol];

			JavaScriptIdentifier identifier = new JavaScriptIdentifier(symbol, this);
			Identifiers.Add(symbol, identifier);
			return identifier;
		}

		private List<string> GetUsedSymbols()
		{
			List<string> result = new List<string>();
			foreach (JavaScriptIdentifier identifier in Identifiers.Values)
				result.Add(identifier.MungedValue ?? identifier.Value);

			return result;
		}

		private List<string> GetAllUsedSymbols()
		{
			List<string> result = new List<string>();
			ScriptOrFnScope scope = this;
			while (scope != null)
			{
				result.AddRange(scope.GetUsedSymbols());
				scope = scope.ParentScope;
			}
			return result;
		}

		public void Munge()
		{
			if (!MarkedForMunging) return;

			int pickFromSet = 1;

			if (ParentScope != null)
			{
				List<string> freeSymbols = new List<string>();
				freeSymbols.AddRange(JavaScriptCompressor.Ones);
				if (freeSymbols.Count == 0)
				{
					pickFromSet = 2;
					freeSymbols.AddRange(JavaScriptCompressor.Twos);
					foreach (string s in GetAllUsedSymbols())
						freeSymbols.Remove(s);
				}
				if (freeSymbols.Count == 0)
				{
					pickFromSet = 3;
					freeSymbols.AddRange(JavaScriptCompressor.Threes);
					foreach (string s in GetAllUsedSymbols())
						freeSymbols.Remove(s);
				}
				if (freeSymbols.Count == 0)
					throw new IndexOutOfRangeException("The Compressor ran out of symbols.");

				foreach (JavaScriptIdentifier i in Identifiers.Values)
				{
					if(freeSymbols.Count == 0)
					{
						pickFromSet++;
						if(pickFromSet == 2)
							freeSymbols.AddRange(JavaScriptCompressor.Twos);
						else if(pickFromSet == 3)
							freeSymbols.AddRange(JavaScriptCompressor.Threes);
						else
							throw new IndexOutOfRangeException("The Compressor ran out of symbols.");
					}

					if(i.MarkedForMunge)
					{
						i.MungedValue = freeSymbols[0];
						freeSymbols.RemoveAt(0);
					}
					else
						i.MungedValue = i.Value;
				}
			}

			foreach (ScriptOrFnScope s in SubScopes)
				 s.Munge();
		}
	}
}
