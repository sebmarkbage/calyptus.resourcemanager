using System;
using System.Diagnostics;
using System.Text;

namespace Org.Mozilla.JavaScript
{
	/// <summary> The following class save decompilation information about the source.
	/// Source information is returned from the parser as a string
	/// associated with function nodes and with the toplevel script.  When
	/// saved in the constant pool of a class, this string will be UTF-8
	/// encoded, and token values will occupy a single byte.
	/// Source is saved (mostly) as token numbers.  The tokens saved pretty
	/// much correspond to the token stream of a 'canonical' representation
	/// of the input program, as directed by the parser.  (There were a few
	/// cases where tokens could have been left out where decompiler could
	/// easily reconstruct them, but I left them in for clarity).  (I also
	/// looked adding source collection to TokenStream instead, where I
	/// could have limited the changes to a few lines in getToken... but
	/// this wouldn't have saved any space in the resulting source
	/// representation, and would have meant that I'd have to duplicate
	/// parser logic in the decompiler to disambiguate situations where
	/// newlines are important.)  The function decompile expands the
	/// tokens back into their string representations, using simple
	/// lookahead to correct spacing and indentation.
	/// 
	/// Assignments are saved as two-token pairs (Token.ASSIGN, op). Number tokens
	/// are stored inline, as a NUMBER token, a character representing the type, and
	/// either 1 or 4 characters representing the bit-encoding of the number.  string
	/// types NAME, STRING and OBJECT are currently stored as a token type,
	/// followed by a character giving the length of the string (assumed to
	/// be less than 2^16), followed by the characters of the string
	/// inlined into the source string.  Changing this to some reference to
	/// to the string in the compiled class' constant pool would probably
	/// save a lot of space... but would require some method of deriving
	/// the final constant pool entry from information available at parse
	/// time.
	/// </summary>
	class Decompiler
	{
		/// <summary>Flag to indicate that the decompilation should omit the
		/// function header and trailing brace.
		/// </summary>
		public const int ONLY_BODY_FLAG = 1 << 0;

		/// <summary>Flag to indicate that the decompilation generates toSource result.</summary>
		public const int TO_SOURCE_FLAG = 1 << 1;

		/// <summary>Decompilation property to specify initial ident value.</summary>
		public const int INITIAL_INDENT_PROP = 1;

		/// <summary>Decompilation property to specify default identation offset.</summary>
		public const int INDENT_GAP_PROP = 2;

		/// <summary>Decompilation property to specify identation offset for case labels.</summary>
		public const int CASE_GAP_PROP = 3;

		/// <summary>Marker to denote the last RC of function so it can be distinguished from
		/// the last RC of object literals in case of function expressions.
		/// </summary>
		private const int FUNCTION_END = Token.LAST_TOKEN + 1;

		private const int FUNCTION_EXPRESSION = 2;

		private char[] sourceBuffer = new char[128];

		/// <summary>Per script/function source buffer top: parent source does not include a
		/// nested functions source and uses function index as a reference instead.
		/// </summary>
		private int sourceTop;

		/// <summary>whether to do a debug print of the source information, when decompiling.</summary>
		private const bool printSource = false;

		public string EncodedSource { get { return SourceTostring(0); } }
		public int CurrentOffset { get { return sourceTop; } }

		int MarkFunctionStart(int functionType)
		{
			int savedOffset = CurrentOffset;
			AddToken(Token.FUNCTION);
			Append((char)functionType);
			return savedOffset;
		}

		int MarkFunctionEnd(int functionStart)
		{
			int offset = CurrentOffset;
			Append((char)FUNCTION_END);
			return offset;
		}

		void AddToken(int token)
		{
			if (!(0 <= token && token <= Token.LAST_TOKEN))
				throw new ArgumentException();

			Append((char)token);
		}

		void AddEOL(int token)
		{
			if (!(0 <= token && token <= Token.LAST_TOKEN))
				throw new ArgumentException();

			Append((char)token);
			Append((char)Token.EOL);
		}

		void AddName(string str)
		{
			AddToken(Token.NAME);
			AppendString(str);
		}

		void Addstring(string str)
		{
			AddToken(Token.STRING);
			AppendString(str);
		}

		void AddRegexp(string regexp, string flags)
		{
			AddToken(Token.REGEXP);
			AppendString('/' + regexp + '/' + flags);
		}

		void AddJScriptConditionalComment(string str)
		{
			AddToken(Token.SPECIALCOMMENT);
			AppendString(str);
		}

		void AddNumber(double n)
		{
			AddToken(Token.NUMBER);

			/* encode the number in the source stream.
			 * Save as NUMBER type (char | char char char char)
			 * where type is
			 * 'D' - double, 'S' - short, 'J' - long.

			 * We need to retain float vs. integer type info to keep the
			 * behavior of liveconnect type-guessing the same after
			 * decompilation.  (Liveconnect tries to present 1.0 to Java
			 * as a float/double)
			 * OPT: This is no longer true. We could compress the format.

			 * This may not be the most space-efficient encoding;
			 * the chars created below may take up to 3 bytes in
			 * constant pool UTF-8 encoding, so a Double could take
			 * up to 12 bytes.
			 */

			long lbits = (long)n;
			if (lbits != n)
			{
				// if it's floating point, save as a Double bit pattern.
				// (12/15/97 our scanner only returns Double for f.p.)
				lbits = BitConverter.DoubleToInt64Bits(n);
				Append('D');
				Append((char)(lbits >> 48));
				Append((char)(lbits >> 32));
				Append((char)(lbits >> 16));
				Append((char)lbits);
			}
			else
			{
				// we can ignore negative values, bc they're already prefixed
				// by NEG
				if (lbits < 0) Debug.Fail("lbits < 0"); //Kit.codeBug();

				// will it fit in a char?
				// this gives a short encoding for integer values up to 2^16.
				if (lbits <= char.MaxValue)
				{
					Append('S');
					Append((char)lbits);
				}
				else
				{ // Integral, but won't fit in a char. Store as a long.
					Append('J');
					Append((char)(lbits >> 48));
					Append((char)(lbits >> 32));
					Append((char)(lbits >> 16));
					Append((char)lbits);
				}
			}
		}

		private void AppendString(string str)
		{
			int L = str.Length;
			int lengthEncodingSize = 1;
			if (L >= 0x8000)
				lengthEncodingSize = 2;

			int nextTop = sourceTop + lengthEncodingSize + L;
			if (nextTop > sourceBuffer.Length)
				IncreaseSourceCapacity(nextTop);

			if (L >= 0x8000)
			{
				// Use 2 chars to encode strings exceeding 32K, were the highest
				// bit in the first char indicates presence of the next byte
				sourceBuffer[sourceTop] = (char)(0x8000 | (L >> 16)); // TODO : SourceBuffer[SourceTop] = (char)(0x8000 | (L >>> 16));
				++sourceTop;
			}
			sourceBuffer[sourceTop] = (char)L;
			++sourceTop;
			char[] chrs = str.ToCharArray(0, L);
			chrs.CopyTo(sourceBuffer, sourceTop);
			sourceTop = nextTop;
		}

		private void Append(char c)
		{
			if (sourceTop == sourceBuffer.Length)
				IncreaseSourceCapacity(sourceTop + 1);

			sourceBuffer[sourceTop] = c;
			++sourceTop;
		}

		private void IncreaseSourceCapacity(int minimalCapacity)
		{
			// Call this only when capacity increase is must
			Debug.WriteLineIf(minimalCapacity <= sourceBuffer.Length, "minimalCapacity <= SourceBuffer.Length");
			int newCapacity = sourceBuffer.Length * 2;
			if (newCapacity < minimalCapacity)
				newCapacity = minimalCapacity;

			char[] tmp = new char[newCapacity];
			sourceBuffer.CopyTo(tmp, 0);
			sourceBuffer = tmp;
		}

		private string SourceTostring(int offset)
		{
			Debug.WriteLineIf(offset < 0 || sourceTop < offset, "offset < 0 || SourceTop < offset");
			return new string(sourceBuffer, offset, sourceTop - offset);
		}

		/**
		 * Decompile the source information associated with this js
		 * function/script back into a string.  For the most part, this
		 * just means translating tokens back to their string
		 * representations; there's a little bit of lookahead logic to
		 * decide the proper spacing/indentation.  Most of the work in
		 * mapping the original source to the prettyprinted decompiled
		 * version is done by the parser.
		 *
		 * @param source encoded source tree presentation
		 *
		 * @param flags flags to select output format
		 *
		 * @param properties indentation properties
		 *
		 */
		/*
		public static string Decompile(string source, int flags)
		{
			int length = source.Length;
			if (length == 0) return "";

			int indent = 0;
			int indentGap = 4;
			int caseGap = 2;

			StringBuilder result = new StringBuilder();
			bool justFunctionBody = (0 != (flags & Decompiler.ONLY_BODY_FLAG));
			bool toSource = (0 != (flags & Decompiler.TO_SOURCE_FLAG));

			// Spew tokens in source, for debugging.
			// as TYPE number char
			if (printSource)
			{
				Debug.WriteLine("Length: " + length);
				for (int c = 0; c < length; ++c)
				{
					// Note that tokenToName will fail unless Context.printTrees
					// is true.
					string tokenname = null;
					if (Token.PrintNames)
						tokenname = Token.Name(source[c]);

					if (tokenname == null)
						tokenname = "---";

					string pad = tokenname.Length > 7 ? "\t" : "\t\t";
					Debug.WriteLine(tokenname + pad + (int)source[c] + "\t'" + ScriptRuntime.EscapeString(source.Substring(c, c + 1)) + "'");
				}
			}

			int braceNesting = 0;
			bool afterFirstEOL = false;
			int i = 0;
			int topFunctionType;
			if (source[i] == Token.SCRIPT)
			{
				++i;
				topFunctionType = -1;
			}
			else
			{
				topFunctionType = source[i + 1];
			}

			if (!toSource)
			{
				// add an initial newline to exactly match js.
				result.Append('\n');
				for (int j = 0; j < indent; j++)
					result.Append(' ');
			}
			else
			{
				if (topFunctionType == FUNCTION_EXPRESSION)
				{
					result.Append('(');
				}
			}

			while (i < length)
			{
				switch (source[i])
				{
					case Token.GET:
					case Token.SET:
						result.Append(source[i] == Token.GET ? "get " : "set ");
						++i;
						i = PrintSourceString(source, i + 1, false, result);
						// Now increment one more to get past the FUNCTION token
						++i;
						break;

					case Token.NAME:
					case Token.REGEXP:  // re-wrapped in '/'s in parser...
						i = PrintSourceString(source, i + 1, false, result);
						continue;

					case Token.STRING:
						i = PrintSourceString(source, i + 1, true, result);
						continue;

					case Token.NUMBER:
						i = PrintSourceNumber(source, i + 1, result);
						continue;

					case Token.TRUE:
						result.Append("true");
						break;

					case Token.FALSE:
						result.Append("false");
						break;

					case Token.NULL:
						result.Append("null");
						break;

					case Token.THIS:
						result.Append("this");
						break;

					case Token.FUNCTION:
						++i; // skip function type
						result.Append("function ");
						break;

					case FUNCTION_END:
						// Do nothing
						break;

					case Token.COMMA:
						result.Append(", ");
						break;

					case Token.LC:
						++braceNesting;
						if (Token.EOL == GetNext(source, length, i))
							indent += indentGap;
						result.Append('{');
						break;

					case Token.RC:
						{
							--braceNesting;
							//don't print the closing RC if it closes the
							//toplevel function and we're called from
							//decompileFunctionBody.
							
							if (justFunctionBody && braceNesting == 0)
								break;

							result.Append('}');
							switch (GetNext(source, length, i))
							{
								case Token.EOL:
								case FUNCTION_END:
									indent -= indentGap;
									break;
								case Token.WHILE:
								case Token.ELSE:
									indent -= indentGap;
									result.Append(' ');
									break;
							}
							break;
						}
					case Token.LP:
						result.Append('(');
						break;

					case Token.RP:
						result.Append(')');
						if (Token.LC == GetNext(source, length, i))
							result.Append(' ');
						break;

					case Token.LB:
						result.Append('[');
						break;

					case Token.RB:
						result.Append(']');
						break;

					case Token.EOL:
						{
							if (toSource) break;
							bool newLine = true;
							if (!afterFirstEOL)
							{
								afterFirstEOL = true;
								if (justFunctionBody)
								{
									//throw away just added 'function name(...) {'
									//and restore the original indent
									
									result = new StringBuilder();
									indent -= indentGap;
									newLine = false;
								}
							}
							if (newLine)
								result.Append('\n');

							//add indent if any tokens remain,
							//less setback if next token is
							//a label, case or default.
							
							if (i + 1 < length)
							{
								int less = 0;
								int nextToken = source[i + 1];
								if (nextToken == Token.CASE || nextToken == Token.DEFAULT)
									less = indentGap - caseGap;

								else if (nextToken == Token.RC)
									less = indentGap;

								//elaborate check against label... skip past a
								//following inlined NAME and look for a COLON.
								
								else if (nextToken == Token.NAME)
								{
									int afterName = GetSourcestringEnd(source, i + 2);
									if (source[afterName] == Token.COLON)
										less = indentGap;
								}

								for (; less < indent; less++)
									result.Append(' ');
							}
							break;
						}
					case Token.DOT:
						result.Append('.');
						break;

					case Token.NEW:
						result.Append("new ");
						break;

					case Token.DELPROP:
						result.Append("delete ");
						break;

					case Token.IF:
						result.Append("if ");
						break;

					case Token.ELSE:
						result.Append("else ");
						break;

					case Token.FOR:
						result.Append("for ");
						break;

					case Token.IN:
						result.Append(" in ");
						break;

					case Token.WITH:
						result.Append("with ");
						break;

					case Token.WHILE:
						result.Append("while ");
						break;

					case Token.DO:
						result.Append("do ");
						break;

					case Token.TRY:
						result.Append("try ");
						break;

					case Token.CATCH:
						result.Append("catch ");
						break;

					case Token.FINALLY:
						result.Append("finally ");
						break;

					case Token.THROW:
						result.Append("throw ");
						break;

					case Token.SWITCH:
						result.Append("switch ");
						break;

					case Token.BREAK:
						result.Append("break");
						if (Token.NAME == GetNext(source, length, i))
							result.Append(' ');
						break;

					case Token.CONTINUE:
						result.Append("continue");
						if (Token.NAME == GetNext(source, length, i))
							result.Append(' ');
						break;

					case Token.CASE:
						result.Append("case ");
						break;

					case Token.DEFAULT:
						result.Append("default");
						break;

					case Token.RETURN:
						result.Append("return");
						if (Token.SEMI != GetNext(source, length, i))
							result.Append(' ');
						break;

					case Token.VAR:
						result.Append("var ");
						break;

					case Token.SEMI:
						result.Append(';');
						if (Token.EOL != GetNext(source, length, i))
							result.Append(' '); // separators in FOR

						break;

					case Token.ASSIGN:
						result.Append(" = ");
						break;

					case Token.ASSIGN_ADD:
						result.Append(" += ");
						break;

					case Token.ASSIGN_SUB:
						result.Append(" -= ");
						break;

					case Token.ASSIGN_MUL:
						result.Append(" *= ");
						break;

					case Token.ASSIGN_DIV:
						result.Append(" /= ");
						break;

					case Token.ASSIGN_MOD:
						result.Append(" %= ");
						break;

					case Token.ASSIGN_BITOR:
						result.Append(" |= ");
						break;

					case Token.ASSIGN_BITXOR:
						result.Append(" ^= ");
						break;

					case Token.ASSIGN_BITAND:
						result.Append(" &= ");
						break;

					case Token.ASSIGN_LSH:
						result.Append(" <<= ");
						break;

					case Token.ASSIGN_RSH:
						result.Append(" >>= ");
						break;

					case Token.ASSIGN_URSH:
						result.Append(" >>>= ");
						break;

					case Token.HOOK:
						result.Append(" ? ");
						break;

					case Token.OBJECTLIT:
						// pun OBJECTLIT to mean colon in objlit property
						// initialization.
						// This needs to be distinct from COLON in the general case
						// to distinguish from the colon in a ternary... which needs
						// different spacing.
						result.Append(':');
						break;

					case Token.COLON:
						if (Token.EOL == GetNext(source, length, i))
							// it's the end of a label
							result.Append(':');
						else
							// it's the middle part of a ternary
							result.Append(" : ");
						break;

					case Token.OR:
						result.Append(" || ");
						break;

					case Token.AND:
						result.Append(" && ");
						break;

					case Token.BITOR:
						result.Append(" | ");
						break;

					case Token.BITXOR:
						result.Append(" ^ ");
						break;

					case Token.BITAND:
						result.Append(" & ");
						break;

					case Token.SHEQ:
						result.Append(" === ");
						break;

					case Token.SHNE:
						result.Append(" !== ");
						break;

					case Token.EQ:
						result.Append(" == ");
						break;

					case Token.NE:
						result.Append(" != ");
						break;

					case Token.LE:
						result.Append(" <= ");
						break;

					case Token.LT:
						result.Append(" < ");
						break;

					case Token.GE:
						result.Append(" >= ");
						break;

					case Token.GT:
						result.Append(" > ");
						break;

					case Token.INSTANCEOF:
						result.Append(" instanceof ");
						break;

					case Token.LSH:
						result.Append(" << ");
						break;

					case Token.RSH:
						result.Append(" >> ");
						break;

					case Token.URSH:
						result.Append(" >>> ");
						break;

					case Token.TYPEOF:
						result.Append("typeof ");
						break;

					case Token.VOID:
						result.Append("void ");
						break;

					case Token.CONST:
						result.Append("const ");
						break;

					case Token.NOT:
						result.Append('!');
						break;

					case Token.BITNOT:
						result.Append('~');
						break;

					case Token.POS:
						result.Append('+');
						break;

					case Token.NEG:
						result.Append('-');
						break;

					case Token.INC:
						result.Append("++");
						break;

					case Token.DEC:
						result.Append("--");
						break;

					case Token.ADD:
						result.Append(" + ");
						break;

					case Token.SUB:
						result.Append(" - ");
						break;

					case Token.MUL:
						result.Append(" * ");
						break;

					case Token.DIV:
						result.Append(" / ");
						break;

					case Token.MOD:
						result.Append(" % ");
						break;

					case Token.COLONCOLON:
						result.Append("::");
						break;

					case Token.DOTDOT:
						result.Append("..");
						break;

					case Token.DOTQUERY:
						result.Append(".(");
						break;

					case Token.XMLATTR:
						result.Append('@');
						break;

					default:
						// If we don't know how to decompile it, raise an exception.
						throw new ApplicationException("Token: " + Token.Name(source[i]));
				}
				++i;
			}

			if (!toSource)
			{
				// add that trailing newline if it's an outermost function.
				if (!justFunctionBody)
					result.Append('\n');
			}
			else
			{
				if (topFunctionType == FUNCTION_EXPRESSION)
					result.Append(')');
			}

			return result.ToString();
		}
		*/
		private static int GetNext(string source, int length, int i)
		{
			return (i + 1 < length) ? source[i + 1] : Token.EOF;
		}

		private static int GetSourcestringEnd(string source, int offset)
		{
			return PrintSourceString(source, offset, false, null);
		}

		private static int PrintSourceString(string source, int offset, bool asQuotedstring, StringBuilder sb)
		{
			int length = source[offset];
			++offset;
			if ((0x8000 & length) != 0)
			{
				length = ((0x7FFF & length) << 16) | source[offset];
				++offset;
			}
			if (sb != null)
			{
				string str = source.Substring(offset, offset + length);
				if (!asQuotedstring)
					sb.Append(str);
				else
				{
					sb.Append('"');
					sb.Append(EscapeString(str));
					sb.Append('"');
				}
			}
			return offset + length;
		}

		private static int PrintSourceNumber(string source, int offset, StringBuilder sb)
		{
			double number = 0.0;
			char type = source[offset];
			++offset;
			if (type == 'S')
			{
				if (sb != null)
				{
					int ival = source[offset];
					number = ival;
				}
				++offset;
			}
			else if (type == 'J' || type == 'D')
			{
				if (sb != null)
				{
					long lbits;
					lbits = (long)source[offset] << 48;
					lbits |= (long)source[offset + 1] << 32;
					lbits |= (long)source[offset + 2] << 16;
					lbits |= source[offset + 3];
					if (type == 'J')
						number = lbits;
					else
						number = BitConverter.Int64BitsToDouble(lbits);
				}
				offset += 4;
			}
			else
				throw new ApplicationException(); // Bad source

			if (sb != null)
				sb.Append(ScriptRuntime.NumberToString(number, 10));

			return offset;
		}

		/// <summary>For escaping strings printed by object and array literals; not quite
		/// the same as 'escape.'
		/// </summary>
		public static string EscapeString(string s)
		{
			StringBuilder sb = null;

			for (int i = 0, L = s.Length; i != L; ++i)
			{
				int c = s[i];

				if (' ' <= c && c <= '~' && c != '"' && c != '\\')
				{
					// an ordinary print character (like C isprint()) and not "
					// or \ . Note single quote ' is not escaped
					if (sb != null)
					{
						sb.Append((char)c);
					}
					continue;
				}
				if (sb == null)
				{
					sb = new StringBuilder(L + 3);
					sb.Append(s);
					sb.Capacity = i;
				}

				int escape = -1;
				switch (c)
				{
					case '\b': escape = 'b'; break;
					case '\f': escape = 'f'; break;
					case '\n': escape = 'n'; break;
					case '\r': escape = 'r'; break;
					case '\t': escape = 't'; break;
					case 0xb: escape = 'v'; break; // Java lacks \v.
					case '"': escape = '"'; break;
					case ' ': escape = ' '; break;
					case '\\': escape = '\\'; break;
				}
				if (escape >= 0)
				{
					// an \escaped sort of character
					sb.Append('\\');
					sb.Append((char)escape);
				}
				else
				{
					int hexSize;
					if (c < 256)
					{
						// 2-digit hex
						sb.Append("\\x");
						hexSize = 2;
					}
					else
					{
						// Unicode.
						sb.Append("\\u");
						hexSize = 4;
					}
					// append hexadecimal form of c left-padded with 0
					for (int shift = (hexSize - 1) * 4; shift >= 0; shift -= 4)
					{
						int digit = 0xf & (c >> shift);
						int hc = (digit < 10) ? '0' + digit : 'a' - 10 + digit;
						sb.Append((char)hc);
					}
				}
			}

			return (sb == null) ? s : sb.ToString();
		}
	}
}
