using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using java.lang;

namespace Org.Mozilla.JavaScript
{
	class TokenStream
	{
		private const int EOF_CHAR = -1;

		/// <summary>stuff other than whitespace since start of line</summary>
		private bool dirtyLine;

		private string regExpFlags;

		/// <summary>Set this to an inital non-null value so that the Parser has
		/// something to retrieve even if an error has occured and no
		/// string is found.  Fosters one class of error, but saves lots of code.
		/// </summary>
		private string str = "";
		private double number;

		private char[] stringBuffer = new char[128];
		private int stringBufferTop;
		private ObjToIntMap allstrings = new ObjToIntMap(50);

		/// <summary>Room to backtrace from to < on failed match of the last - in <!--</summary>
		private const int[] ungetBuffer = new int[3];
		private int ungetCursor;

		private bool hitEOF = false;

		private int lineStart = 0;
		private int lineNo;
		private int lineEndChar = -1;

		private string sourceString;
		private StreamReader sourceReader;
		private char[] sourceBuffer;
		private int sourceEnd;
		private int sourceCursor;

		/// <summary>for xml tokenizer</summary>
		private bool xmlIsAttribute;
		private bool xmlIsTagContent;
		private int xmlOpenTagsCount;

		private Parser parser;

		public int LineNo { get { return lineNo; } }
		public string Str { get { return str; } }
		public double Number { get { return number; } }
		public bool EOF { get { return hitEOF; } }

		public TokenStream(Parser parser, StreamReader sourceReader, string sourceString, int lineNo)
		{
			this.lineNo = lineNo;
			this.parser = parser;
			if (sourceReader != null)
			{
				Debug.WriteLineIf(sourceString != null, "sourceString != null");
				this.sourceReader = sourceReader;
				sourceBuffer = new char[512];
				sourceEnd = 0;
			}
			else
			{
				Debug.WriteLineIf(sourceString == null, "sourceString == null");
				sourceString = sourceString;
				sourceEnd = sourceString.length();
			}
			sourceCursor = 0;
		}

		/// <summary>This function uses the cached op, string and number fields in
		/// TokenStream; if getToken has been called since the passed token
		/// was scanned, the op or string printed may be incorrect.
		/// </summary>
		private string TokenTostring(int token)
		{
			if (Token.PrintTrees)
			{
				string name = Token.Name(token);

				switch (token)
				{
					case Token.STRING:
					case Token.REGEXP:
					case Token.NAME:
						return name + " `" + str + "'";

					case Token.NUMBER:
						return "NUMBER " + number;
				}

				return name;
			}
			return "";
		}

		private static bool IsKeyword(string s)
		{
			return Token.EOF != StringToKeyword(s);
		}

		private static int StringToKeyword(string name)
		{
			// #string_id_map#
			// The following assumes that Token.EOF == 0
			int
			Id_break         = Token.BREAK,
			Id_case          = Token.CASE,
			Id_continue      = Token.CONTINUE,
			Id_default       = Token.DEFAULT,
			Id_delete        = Token.DELPROP,
			Id_do            = Token.DO,
			Id_else          = Token.ELSE,
			Id_export        = Token.EXPORT,
			Id_false         = Token.FALSE,
			Id_for           = Token.FOR,
			Id_function      = Token.FUNCTION,
			Id_if            = Token.IF,
			Id_in            = Token.IN,
			Id_new           = Token.NEW,
			Id_null          = Token.NULL,
			Id_return        = Token.RETURN,
			Id_switch        = Token.SWITCH,
			Id_this          = Token.THIS,
			Id_true          = Token.TRUE,
			Id_typeof        = Token.TYPEOF,
			Id_var           = Token.VAR,
			Id_void          = Token.VOID,
			Id_while         = Token.WHILE,
			Id_with          = Token.WITH,

			// the following are #ifdef RESERVE_JAVA_KEYWORDS in jsscan.c
			Id_abstract      = Token.RESERVED,
			Id_bool			 = Token.RESERVED,
			Id_byte          = Token.RESERVED,
			Id_catch         = Token.CATCH,
			Id_char          = Token.RESERVED,
			Id_class         = Token.RESERVED,
			Id_const         = Token.CONST,
			Id_debugger      = Token.RESERVED,
			Id_double        = Token.RESERVED,
			Id_enum          = Token.RESERVED,
			Id_extends       = Token.RESERVED,
			Id_final         = Token.RESERVED,
			Id_finally       = Token.FINALLY,
			Id_float         = Token.RESERVED,
			Id_goto          = Token.RESERVED,
			Id_implements    = Token.RESERVED,
			Id_import        = Token.IMPORT,
			Id_instanceof    = Token.INSTANCEOF,
			Id_int           = Token.RESERVED,
			Id_interface     = Token.RESERVED,
			Id_long          = Token.RESERVED,
			Id_native        = Token.RESERVED,
			Id_package       = Token.RESERVED,
			Id_private       = Token.RESERVED,
			Id_protected     = Token.RESERVED,
			Id_public        = Token.RESERVED,
			Id_short         = Token.RESERVED,
			Id_static        = Token.RESERVED,
			Id_super         = Token.RESERVED,
			Id_synchronized  = Token.RESERVED,
			Id_throw         = Token.THROW,
			Id_throws        = Token.RESERVED,
			Id_transient     = Token.RESERVED,
			Id_try           = Token.TRY,
			Id_volatile      = Token.RESERVED;

			int id;
			string s = name;
			// #generated# Last update: 2001-06-01 17:45:01 CEST
			L0: { id = 0; string X = null; int c;
				L: switch (s.Length) {
				case 2: c=s[1];
					if (c=='f') { if (s[0]=='i') {id=Id_if; goto L0;} }
					else if (c=='n') { if (s[0]=='i') {id=Id_in; goto L0;} }
					else if (c=='o') { if (s[0]=='d') {id=Id_do; goto L0;} }
					goto L;
				case 3: switch (s[0]) {
					case 'f': if (s[2]=='r' && s[1]=='o') {id=Id_for; goto L0;} goto L;
					case 'i': if (s[2]=='t' && s[1]=='n') {id=Id_int; goto L0;} goto L;
					case 'n': if (s[2]=='w' && s[1]=='e') {id=Id_new; goto L0;} goto L;
					case 't': if (s[2]=='y' && s[1]=='r') {id=Id_try; goto L0;} goto L;
					case 'v': if (s[2]=='r' && s[1]=='a') {id=Id_var; goto L0;} goto L;
					} goto L;
				case 4: switch (s[0]) {
					case 'b': X="byte";id=Id_byte; goto L;
					case 'c': c=s.charAt(3);
						if (c=='e') { if (s[2]=='s' && s[1]=='a') {id=Id_case; goto L0;} }
						else if (c=='r') { if (s[2]=='a' && s[1]=='h') {id=Id_char; goto L0;} }
						goto L;
					case 'e': c=s.charAt(3);
						if (c=='e') { if (s[2]=='s' && s[1]=='l') {id=Id_else; goto L0;} }
						else if (c=='m') { if (s[2]=='u' && s[1]=='n') {id=Id_enum; goto L0;} }
						goto L;
					case 'g': X="goto";id=Id_goto; goto L;
					case 'l': X="long";id=Id_long; goto L;
					case 'n': X="null";id=Id_null; goto L;
					case 't': c=s.charAt(3);
						if (c=='e') { if (s[2]=='u' && s[1]=='r') {id=Id_true; goto L0;} }
						else if (c=='s') { if (s[2]=='i' && s[1]=='h') {id=Id_this; goto L0;} }
						goto L;
					case 'v': X="void";id=Id_void; goto L;
					case 'w': X="with";id=Id_with; goto L;
					} goto L;
				case 5: switch (s[2]) {
					case 'a': X="class";id=Id_class; goto L;
					case 'e': X="break";id=Id_break; goto L;
					case 'i': X="while";id=Id_while; goto L;
					case 'l': X="false";id=Id_false; goto L;
					case 'n': c=s[0];
						if (c=='c') { X="const";id=Id_const; }
						else if (c=='f') { X="final";id=Id_final; }
						goto L;
					case 'o': c=s[0];
						if (c=='f') { X="float";id=Id_float; }
						else if (c=='s') { X="short";id=Id_short; }
						goto L;
					case 'p': X="super";id=Id_super; goto L;
					case 'r': X="throw";id=Id_throw; goto L;
					case 't': X="catch";id=Id_catch; goto L;
					} goto L;
				case 6: switch (s[1]) {
					case 'a': X="native";id=Id_native; goto L;
					case 'e': c=s[0];
						if (c=='d') { X="delete";id=Id_delete; }
						else if (c=='r') { X="return";id=Id_return; }
						goto L;
					case 'h': X="throws";id=Id_throws; goto L;
					case 'm': X="import";id=Id_import; goto L;
					case 'o': X="double";id=Id_double; goto L;
					case 't': X="static";id=Id_static; goto L;
					case 'u': X="public";id=Id_public; goto L;
					case 'w': X="switch";id=Id_switch; goto L;
					case 'x': X="export";id=Id_export; goto L;
					case 'y': X="typeof";id=Id_typeof; goto L;
					} goto L;
				case 7: switch (s[1]) {
					case 'a': X="package";id=Id_package; goto L;
					case 'e': X="default";id=Id_default; goto L;
					case 'i': X="finally";id=Id_finally; goto L;
					case 'o': X="bool";id=Id_bool; goto L;
					case 'r': X="private";id=Id_private; goto L;
					case 'x': X="extends";id=Id_extends; goto L;
					} goto L;
				case 8: switch (s[0]) {
					case 'a': X="abstract";id=Id_abstract; goto L;
					case 'c': X="continue";id=Id_continue; goto L;
					case 'd': X="debugger";id=Id_debugger; goto L;
					case 'f': X="function";id=Id_function; goto L;
					case 'v': X="volatile";id=Id_volatile; goto L;
					} goto L;
				case 9: c=s[0];
					if (c=='i') { X="interface";id=Id_interface; }
					else if (c=='p') { X="protected";id=Id_protected; }
					else if (c=='t') { X="transient";id=Id_transient; }
					goto L;
				case 10: c=s[1];
					if (c=='m') { X="implements";id=Id_implements; }
					else if (c=='n') { X="instanceof";id=Id_instanceof; }
					goto L;
				case 12: X="synchronized";id=Id_synchronized; goto L;
				}
				if (X!=null && X!=s && !X.equals(s)) id = 0;
			}
			// #/generated#
			// #/string_id_map#
			if (id == 0) return Token.EOF;

			return id & 0xff;
		}

		private int GetToken()
		{
	        int c;
			while(true)
			{
				// Eat whitespace, possibly sensitive to newlines.
				while(true)
				{
					c = GetChar();
					if(c == EOF_CHAR)
						return Token.EOF;
					else if(c == '\n')
					{
						dirtyLine = false;
						return Token.EOL;
					}
					else if(!IsJSSpace(c))
					{
						if(c != '-')
							dirtyLine = true;

						break;
					}
				}

            if (c == '@') return Token.XMLATTR;

            // identifier/keyword/instanceof?
            // watch out for starting with a <backslash>
            bool identifierStart;
            bool isUnicodeEscapeStart = false;
            if (c == '\\')
			{
                c = GetChar();
                if (c == 'u')
				{
                    identifierStart = true;
                    isUnicodeEscapeStart = true;
                    stringBufferTop = 0;
                }
				else
				{
                    identifierStart = false;
                    UnGetChar(c);
                    c = '\\';
                }
            }
			else
			{
                identifierStart = Character.IsJavaIdentifierStart((char)c);
                if (identifierStart)
				{
                    stringBufferTop = 0;
                    AddToString(c);
                }
            }

            if (identifierStart)
			{
                bool containsEscape = isUnicodeEscapeStart;
                while(true)
				{
                    if (isUnicodeEscapeStart)
					{
                        // strictly speaking we should probably push-back
                        // all the bad characters if the <backslash>uXXXX
                        // sequence is malformed. But since there isn't a
                        // correct context(is there?) for a bad Unicode
                        // escape sequence in an identifier, we can report
                        // an error here.
                        int escapeVal = 0;
                        for (int i = 0; i != 4; ++i)
						{
                            c = GetChar();
                            escapeVal = ScriptRuntime.XDigitToInt(c, escapeVal);
                            // Next check takes care about c < 0 and bad escape
                            if (escapeVal < 0) break;
                        }
                        if (escapeVal < 0)
						{
                            parser.addError("msg.invalid.escape");
                            return Token.ERROR;
                        }
                        AddTostring(escapeVal);
                        isUnicodeEscapeStart = false;
                    }
					else
					{
                        c = GetChar();
                        if (c == '\\')
						{
                            c = GetChar();
                            if (c == 'u') {
                                isUnicodeEscapeStart = true;
                                containsEscape = true;
                            }
							else
							{
                                //TODO: parser.addError("msg.illegal.character");
                                return Token.ERROR;
                            }
                        }
						else
						{
                            if (c == EOF_CHAR || !Character.isJavaIdentifierPart((char)c))
                            {
                                break;
                            }
                            AddTostring(c);
                        }
                    }
                }
                UnGetChar(c);

                string str = GetstringFromBuffer();
                if (!containsEscape)
				{
                    // OPT we shouldn't have to make a string (object!) to
                    // check if it's a keyword.

                    // Return the corresponding token if it's a keyword
                    int result = StringToKeyword(str);
                    if (result != Token.EOF)
					{
                        if (result != Token.RESERVED)
						{
                            return result;
                        }
						else if (!parser.compilerEnv.ReservedKeywordAsIdentifier)
                        {
                            return result;
                        }
						else
						{
                            // If implementation permits to use future reserved
                            // keywords in violation with the EcmaScript,
                            // treat it as name but issue warning
                            // TODO: parser.addWarning("msg.reserved.keyword", str);
                        }
                    }
                }
                Str = (string)allstrings.intern(str);
                return Token.NAME;
            }

            // is it a number?
            if (IsDigit(c) || (c == '.' && IsDigit(PeekChar())))
			{
                stringBufferTop = 0;
                int bas = 10;

                if (c == '0')
				{
                    c = GetChar();
                    if (c == 'x' || c == 'X')
					{
                        bas = 16;
                        c = GetChar();
                    }
					else if(IsDigit(c))
                        bas = 8;
					else
                        AddTostring('0');
                }

                if (bas == 16)
				{
                    while (0 <= ScriptRuntime.XDigitToInt(c, 0))
					{
                        AddTostring(c);
                        c = GetChar();
                    }
                }
				else
				{
                    while ('0' <= c && c <= '9')
					{
                        /*
                         * We permit 08 and 09 as decimal numbers, which
                         * makes our behavior a superset of the ECMA
                         * numeric grammar.  We might not always be so
                         * permissive, so we warn about it.
                         */
                        if (bas == 8 && c >= '8')
						{
                            //TODO: parser.AddWarning("msg.bad.octal.literal", c == '8' ? "8" : "9");
                            bas = 10;
                        }
                        AddTostring(c);
                        c = GetChar();
                    }
                }

                bool isInteger = true;

                if (bas == 10 && (c == '.' || c == 'e' || c == 'E'))
				{
                    isInteger = false;
                    if (c == '.')
					{
                        do
						{
                            AddTostring(c);
                            c = GetChar();
                        } while (IsDigit(c));
                    }
                    if (c == 'e' || c == 'E')
					{
                        AddTostring(c);
                        c = GetChar();
                        if (c == '+' || c == '-')
						{
                            AddTostring(c);
                            c = GetChar();
                        }
                        if (!IsDigit(c))
						{
                            parser.addError("msg.missing.exponent");
                            return Token.ERROR;
                        }
                        do
						{
                            AddTostring(c);
                            c = GetChar();
                        } while (IsDigit(c));
                    }
                }
                UnGetChar(c);
                string numstring = GetstringFromBuffer();

                double dval;
                if (bas == 10 && !isInteger)
				{
                    try
					{
                        // Use Java conversion to number from string...
                        dval = Double.Parse(numstring);
                    }
                    catch (FormatException ex)
					{
                        //TODO:parser.addError("msg.caught.nfe");
                        return Token.ERROR;
                    }
                }
				else
				{
                    dval = ScriptRuntime.StringToNumber(numstring, 0, base);
                }

				Number = dval;
                return Token.NUMBER;
            }

            // is it a string?
            if (c == '"' || c == '\'')
			{
                // We attempt to accumulate a string the fast way, by
                // building it directly out of the reader.  But if there
                // are any escaped characters in the string, we revert to
                // building it out of a stringBuffer.

                int quoteChar = c;
                stringBufferTop = 0;

                c = GetChar();
                while (c != quoteChar)
				{
                    if (c == '\n' || c == EOF_CHAR)
					{
                        UnGetChar(c);
                        //TODO: parser.addError("msg.unterminated.string.lit");
                        return Token.ERROR;
                    }

                    if (c == '\\')
					{
                        // We've hit an escaped character

                        c = GetChar();

                        switch (c)
						{

                            case '\\': // backslash
                            case 'b':  // backspace
                            case 'f':  // form feed
                            case 'n':  // line feed
                            case 'r':  // carriage return
                            case 't':  // horizontal tab
                            case 'v':  // vertical tab
                            case 'd':  // octal sequence
                            case 'u':  // unicode sequence
                            case 'x':  // hexadecimal sequence
                                // Only keep the '\' character for those
                                // characters that need to be escaped...
                                // Don't escape quoting characters...
                                AddTostring('\\');
                                AddTostring(c);
                                break;

                            case '\n':
                                // Remove line terminator after escape
                                break;

                            default:
                                if (IsDigit(c))
								{
                                    // Octal representation of a character.
                                    // Preserve the escaping (see Y! bug #1637286)
                                    AddTostring('\\');
                                }
                                AddTostring(c);
                                break;
                        }

                    }
					else
					{

                        AddTostring(c);
                    }

                    c = GetChar();
                }

                string str = GetstringFromBuffer();
                //TODO: Str = (string)allstrings.intern(str);
                return Token.STRING;
            }

            switch (c)
			{
            case ';': return Token.SEMI;
            case '[': return Token.LB;
            case ']': return Token.RB;
            case '{': return Token.LC;
            case '}': return Token.RC;
            case '(': return Token.LP;
            case ')': return Token.RP;
            case ',': return Token.COMMA;
            case '?': return Token.HOOK;
            case ':':
                if (MatchChar(':'))
				{
                    return Token.COLONCOLON;
                }
				else
				{
                    return Token.COLON;
                }
            case '.':
                if (MatchChar('.')) {
                    return Token.DOTDOT;
                }
				else if (MatchChar('('))
				{
                    return Token.DOTQUERY;
                }
				else
				{
                    return Token.DOT;
                }

            case '|':
                if (MatchChar('|'))
				{
                    return Token.OR;
                }
				else if (MatchChar('='))
				{
                    return Token.ASSIGN_BITOR;
                }
				else
				{
                    return Token.BITOR;
                }

            case '^':
                if (MatchChar('='))
				{
                    return Token.ASSIGN_BITXOR;
                }
				else
				{
                    return Token.BITXOR;
                }

            case '&':
                if (MatchChar('&'))
				{
                    return Token.AND;
                }
				else if (MatchChar('='))
				{
                    return Token.ASSIGN_BITAND;
                }
				else
				{
                    return Token.BITAND;
                }

            case '=':
                if (MatchChar('='))
				{
                    if (MatchChar('='))
                        return Token.SHEQ;
                    else
                        return Token.EQ;
                }
				else
				{
                    return Token.ASSIGN;
                }

            case '!':
                if (MatchChar('='))
				{
                    if (MatchChar('='))
                        return Token.SHNE;
                    else
                        return Token.NE;
                } else {
                    return Token.NOT;
                }

            case '<':
                /* NB:treat HTML begin-comment as comment-till-eol */
                if (MatchChar('!'))
				{
                    if (MatchChar('-'))
					{
                        if (MatchChar('-'))
						{
                            SkipLine();
                            continue;
                        }
                        UnGetChar('-');
                    }
                    UnGetChar('!');
                }
                if (MatchChar('<'))
				{
                    if (MatchChar('='))
					{
                        return Token.ASSIGN_LSH;
                    }
					else
					{
                        return Token.LSH;
                    }
                }
				else
				{
                    if (MatchChar('='))
					{
                        return Token.LE;
                    }
					else
					{
                        return Token.LT;
                    }
                }

            case '>':
                if (MatchChar('>'))
				{
                    if (MatchChar('>'))
					{
                        if (MatchChar('='))
						{
                            return Token.ASSIGN_URSH;
                        }
						else
						{
                            return Token.URSH;
                        }
                    }
					else
					{
                        if (MatchChar('='))
						{
                            return Token.ASSIGN_RSH;
                        }
						else
						{
                            return Token.RSH;
                        }
                    }
                }
				else
				{
                    if (MatchChar('='))
					{
                        return Token.GE;
                    }
					else
					{
                        return Token.GT;
                    }
                }

            case '*':
                if (MatchChar('='))
				{
                    return Token.ASSIGN_MUL;
                }
				else
				{
                    return Token.MUL;
                }

            case '/':
                // is it a // comment?
                if (MatchChar('/'))
				{
                    SkipLine();
                    continue;
                }
                if (MatchChar('*'))
				{
                    bool lookForSlash = false;
					StringBuilder sb = new StringBuilder();
                    while(true)
					{
                        c = GetChar();
                        if (c == EOF_CHAR)
						{
                            //TODO: parser.addError("msg.unterminated.comment");
                            return Token.ERROR;
                        }
                        sb.Append((char) c);
                        if (c == '*')
						{
                            lookForSlash = true;
                        }
						else if (c == '/')
						{
                            if (lookForSlash) {
								sb.Remove(sb.Length-2, sb.Length);
                                string s = sb.ToString();
                                if (s.StartsWith("!") ||
                                        s.StartsWith("@cc_on") ||
                                        s.StartsWith("@if") ||
                                        s.StartsWith("@elif") ||
                                        s.StartsWith("@else") ||
                                        s.StartsWith("@end")) {
                                    if (s.StartsWith("!"))
                                        str = s.Substring(1); // Remove the leading '!'
									else
                                        str = s;

                                    return Token.SPECIALCOMMENT;
                                }
								else
                                    continue;
                            }
                        }
						else
                            lookForSlash = false;
                    }
                }

                if (MatchChar('='))
                    return Token.ASSIGN_DIV;
				else
                    return Token.DIV;

            case '%':
                if (MatchChar('='))
                    return Token.ASSIGN_MOD;
                else
                    return Token.MOD;

            case '~':
                return Token.BITNOT;

            case '+':
                if (MatchChar('='))
                    return Token.ASSIGN_ADD;
                else if (MatchChar('+'))
                    return Token.INC;
                else
                    return Token.ADD;

            case '-':
                if (MatchChar('='))
                    c = Token.ASSIGN_SUB;
				else if (MatchChar('-'))
				{
                    if (!dirtyLine)
					{
                        // treat HTML end-comment after possible whitespace
                        // after line start as comment-utill-eol
                        if (MatchChar('>'))
						{
                            SkipLine();
                            continue;
                        }
                    }
                    c = Token.DEC;
                }
				else
                    c = Token.SUB;

                dirtyLine = true;
                return c;

            default:
                //TODO: parser.addError("msg.illegal.character");
                return Token.ERROR;
            }
        }
    }

	private static bool IsAlpha(int c)
	{
		// Use 'Z' < 'a'
		if (c <= 'Z')
			return 'A' <= c;
		else
			return 'a' <= c && c <= 'z';
	}

    static bool IsDigit(int c)
    {
        return '0' <= c && c <= '9';
    }

    /* As defined in ECMA.  jsscan.c uses C isspace() (which allows
     * \v, I think.)  note that code in GetChar() implicitly accepts
     * '\r' == \u000D as well.
     */
	private static bool IsJSSpace(int c)
    {
        if (c <= 127)
            return c == 0x20 || c == 0x9 || c == 0xC || c == 0xB;
        else
            return c == 0xA0 || Character.getType((char)c) == Character.SPACE_SEPARATOR;
    }

    private static boolean IsJSFormatChar(int c)
    {
        return c > 127 && Character.getType((char)c) == Character.FORMAT;
    }

    /**
     * Parser calls the method when it gets / or /= in literal context.
     */
    private void ReadRegExp(int startToken)
    {
		stringBufferTop = 0;
        if (startToken == Token.ASSIGN_DIV)
		{
            // Miss-scanned /=
            AddToString('=');
        }
		else
		{
            if (startToken != Token.DIV)
				throw ApplicationException();
        }

        int c;
        bool inClass = false;
        while ((c = GetChar()) != '/' || inClass)
		{
            if (c == '\n' || c == EOF_CHAR)
			{
                UnGetChar(c);
                //TODO: throw parser.reportError("msg.unterminated.re.lit");
            }
            if (c == '\\')
			{
                AddToString(c);
                c = GetChar();
            }
			else if (c == '[')
                inClass = true;
			else if (c == ']')
                inClass = false;

            AddToString(c);
        }
        int reEnd = stringBufferTop;

        while (true)
		{
            if (MatchChar('g'))
                AddToString('g');
            else if (MatchChar('i'))
                AddToString('i');
            else if (MatchChar('m'))
                AddToString('m');
            else
                break;
        }

        //if (IsAlpha(PeekChar()))
			//TODO: throw parser.reportError("msg.invalid.re.flag");
		str = new string(stringBuffer, 0, reEnd);
		regExpFlags = new string(stringBuffer, reEnd, stringBufferTop - reEnd);
    }

    bool IsXMLAttribute()
    {
        return xmlIsAttribute;
    }

    int GetFirstXMLToken()
    {
        xmlOpenTagsCount = 0;
        xmlIsAttribute = false;
        xmlIsTagContent = false;
        UnGetChar('<');
        return GetNextXMLToken();
    }

    int GetNextXMLToken()
    {
        stringBufferTop = 0; // remember the XML

        for (int c = GetChar(); c != EOF_CHAR; c = GetChar())
		{
            if (xmlIsTagContent)
			{
                switch (c)
				{
                case '>':
                    AddToString(c);
                    xmlIsTagContent = false;
                    xmlIsAttribute = false;
                    break;
                case '/':
                    AddToString(c);
                    if (PeekChar() == '>') {
                        c = GetChar();
                        AddToString(c);
                        xmlIsTagContent = false;
                        xmlOpenTagsCount--;
                    }
                    break;
                case '{':
                    UnGetChar(c);
                    str = GetstringFromBuffer();
                    return Token.XML;
                case '\'':
                case '"':
                    AddToString(c);
                    if (!ReadQuotedstring(c)) return Token.ERROR;
                    break;
                case '=':
                    AddToString(c);
                    xmlIsAttribute = true;
                    break;
                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    AddToString(c);
                    break;
                default:
                    AddToString(c);
                    xmlIsAttribute = false;
                    break;
                }

                if (!xmlIsTagContent && xmlOpenTagsCount == 0)
				{
                    str = GetstringFromBuffer();
                    return Token.XMLEND;
                }
            }
			else
			{
                switch (c)
				{
                case '<':
                    AddToString(c);
                    c = PeekChar();
                    switch (c)
					{
                    case '!':
                        c = GetChar(); // Skip !
                        AddToString(c);
                        c = PeekChar();
                        switch (c) {
                        case '-':
                            c = GetChar(); // Skip -
                            AddToString(c);
                            c = GetChar();
                            if (c == '-') {
                                AddToString(c);
                                if(!readXmlComment()) return Token.ERROR;
                            } else {
                                // throw away the string in progress
                                stringBufferTop = 0;
                                str = null;
                                //TODO: parser.addError("msg.XML.bad.form");
                                return Token.ERROR;
                            }
                            break;
                        case '[':
                            c = GetChar(); // Skip [
                            AddToString(c);
                            if (GetChar() == 'C' &&
                                GetChar() == 'D' &&
                                GetChar() == 'A' &&
                                GetChar() == 'T' &&
                                GetChar() == 'A' &&
                                GetChar() == '[')
                            {
                                AddToString('C');
                                AddToString('D');
                                AddToString('A');
                                AddToString('T');
                                AddToString('A');
                                AddToString('[');
                                if (!ReadCDATA()) return Token.ERROR;

                            }
							else
							{
                                // throw away the string in progress
                                stringBufferTop = 0;
                                str = null;
                                //TODO: parser.addError("msg.XML.bad.form");
                                return Token.ERROR;
                            }
                            break;
                        default:
                            if(!ReadEntity()) return Token.ERROR;
                            break;
                        }
                        break;
                    case '?':
                        c = GetChar(); // Skip ?
                        AddToString(c);
                        if (!ReadPI()) return Token.ERROR;
                        break;
                    case '/':
                        // End tag
                        c = GetChar(); // Skip /
                        AddToString(c);
                        if (xmlOpenTagsCount == 0) {
                            // throw away the string in progress
                            stringBufferTop = 0;
                            str = null;
                            //TODO: parser.addError("msg.XML.bad.form");
                            return Token.ERROR;
                        }
                        xmlIsTagContent = true;
                        xmlOpenTagsCount--;
                        break;
                    default:
                        // Start tag
                        xmlIsTagContent = true;
                        xmlOpenTagsCount++;
                        break;
                    }
                    break;
                case '{':
                    UnGetChar(c);
                    str = GetstringFromBuffer();
                    return Token.XML;
                default:
                    AddToString(c);
                    break;
                }
            }
			}

			stringBufferTop = 0; // throw away the string in progress
			str = null;
			//TODO: parser.addError("msg.XML.bad.form");
			return Token.ERROR;
		}

		private bool ReadQuotedstring(int quote)
		{
			for (int c = GetChar(); c != EOF_CHAR; c = GetChar())
			{
				AddToString(c);
				if (c == quote) return true;
			}

			stringBufferTop = 0; // throw away the string in progress
			str = null;
			//TODO: parser.addError("msg.XML.bad.form");
			return false;
		}

		private bool ReadXmlComment()
		{
			for (int c = GetChar(); c != EOF_CHAR;)
			{
				AddToString(c);
				if (c == '-' && PeekChar() == '-')
				{
					c = GetChar();
					AddToString(c);
					if (PeekChar() == '>')
					{
						c = GetChar(); // Skip >
						AddToString(c);
						return true;
					}
					else
						continue;
				}
				c = GetChar();
			}

			stringBufferTop = 0; // throw away the string in progress
			str = null;
			//TODO: parser.addError("msg.XML.bad.form");
			return false;
		}

		private bool ReadCDATA()
		{
			for (int c = GetChar(); c != EOF_CHAR;)
			{
				AddToString(c);
				if (c == ']' && PeekChar() == ']')
				{
					c = GetChar();
					AddToString(c);
					if (PeekChar() == '>')
					{
						c = GetChar(); // Skip >
						AddToString(c);
						return true;
					}
					else
						continue;
				}
				c = GetChar();
			}

			stringBufferTop = 0; // throw away the string in progress
			str = null;
			//TODO: parser.addError("msg.XML.bad.form");
			return false;
		}

		private bool ReadEntity()
		{
			int declTags = 1;
			for (int c = GetChar(); c != EOF_CHAR; c = GetChar())
			{
				AddToString(c);
				switch (c)
				{
				case '<':
					declTags++;
					break;
				case '>':
					declTags--;
					if (declTags == 0) return true;
					break;
				}
			}

			stringBufferTop = 0; // throw away the string in progress
			str = null;
			//TODO: parser.addError("msg.XML.bad.form");
			return false;
		}

		private bool ReadPI()
		{
			for (int c = GetChar(); c != EOF_CHAR; c = GetChar())
			{
				AddToString(c);
				if (c == '?' && PeekChar() == '>')
				{
					c = GetChar(); // Skip >
					AddToString(c);
					return true;
				}
			}

			stringBufferTop = 0; // throw away the string in progress
			str = null;
			//TODO: parser.addError("msg.XML.bad.form");
			return false;
		}

		private string GetstringFromBuffer()
		{
			return new string(stringBuffer, 0, stringBufferTop);
		}

		private void AddToString(int c)
		{
			int N = stringBufferTop;
			if (N == stringBuffer.length) {
				char[] tmp = new char[stringBuffer.length * 2];
				stringBuffer.CopyTo(tmp, 0);
				stringBuffer = tmp;
			}
			stringBuffer[N] = (char)c;
			stringBufferTop = N + 1;
		}

		private void UnGetChar(int c)
		{
			// can not unread past across line boundary
			if (UngetCursor != 0 && UngetBuffer[UngetCursor - 1] == '\n')
				throw new ApplicationException(); //Kit.codeBug();

			UngetBuffer[UngetCursor++] = c;
		}

		private bool MatchChar(int test)
		{
			int c = GetChar();
			if (c == test)
				return true;
			else
				UnGetChar(c);
				return false;
		}

		private int PeekChar()
		{
			int c = GetChar();
			UnGetChar(c);
			return c;
		}

		private int GetChar()
		{
			if (ungetCursor != 0)
				return ungetBuffer[--ungetCursor];

			while(true)
			{
				int c;
				if (sourceString != null)
				{
					if (sourceCursor == sourceEnd)
					{
						hitEOF = true;
						return EOF_CHAR;
					}
					c = sourceString.charAt(sourceCursor++);
				}
				else
				{
					if (sourceCursor == sourceEnd)
					{
						if (!FillSourceBuffer())
						{
							hitEOF = true;
							return EOF_CHAR;
						}
					}
					c = sourceBuffer[sourceCursor++];
				}

				if (lineEndChar >= 0)
				{
					if (lineEndChar == '\r' && c == '\n')
					{
						lineEndChar = '\n';
						continue;
					}
					lineEndChar = -1;
					lineStart = sourceCursor - 1;
					lineno++;
				}

				if (c <= 127)
				{
					if (c == '\n' || c == '\r') {
						lineEndChar = c;
						c = '\n';
					}
				}
				else
				{
					if (IsJSFormatChar(c))
						continue;

					if (ScriptRuntime.IsJSLineTerminator(c))
					{
						lineEndChar = c;
						c = '\n';
					}
				}
				return c;
			}
		}

		private void SkipLine()
		{
			// skip to end of line
			int c;
			while ((c = GetChar()) != EOF_CHAR && c != '\n') { }
			UnGetChar(c);
		}

		private int GetOffset()
		{
			int n = sourceCursor - lineStart;
			if (lineEndChar >= 0) { --n; }
			return n;
		}

		private string GetLine()
		{
			if (sourceString != null)
			{
				// string case
				int lineEnd = sourceCursor;
				if (lineEndChar >= 0)
					--lineEnd;
				else
				{
					for (; lineEnd != sourceEnd; ++lineEnd)
					{
						int c = sourceString[lineEnd];
						if (ScriptRuntime.isJSLineTerminator(c))
							break;
					}
				}
				return sourceString.Substring(lineStart, lineEnd);
			}
			else
			{
				// Reader case
				int lineLength = sourceCursor - lineStart;
				if (lineEndChar >= 0)
					--lineLength;
				else
				{
					// Read until the end of line
					for (;; ++lineLength)
					{
						int i = lineStart + lineLength;
						if (i == sourceEnd)
						{
							try
							{
								if (!fillSourceBuffer()) { break; }
							}
							catch (IOException ioe)
							{
								// ignore it, we're already displaying an error...
								break;
							}
							// i recalculuation as fillSourceBuffer can move saved
							// line buffer and change lineStart
							i = lineStart + lineLength;
						}
						int c = sourceBuffer[i];
						if (ScriptRuntime.IsJSLineTerminator(c))
							break;
					}
				}
				return new string(sourceBuffer, lineStart, lineLength);
			}
		}

		private bool FillSourceBuffer()
		{
			if (sourceString != null) //TODO: Kit.codeBug();
			if (sourceEnd == sourceBuffer.Length)
			{
				if (lineStart != 0)
				{
					Array.Copy(sourceBuffer, lineStart, sourceBuffer, 0,
									 sourceEnd - lineStart);
					sourceEnd -= lineStart;
					sourceCursor -= lineStart;
					lineStart = 0;
				}
				else
				{
					char[] tmp = new char[sourceBuffer.length * 2];
					Array.Copy(sourceBuffer, 0, tmp, 0, sourceEnd);
					sourceBuffer = tmp;
				}
			}
			int n = sourceReader.Read(sourceBuffer, sourceEnd,
									  sourceBuffer.length - sourceEnd);
			if (n < 0)
				return false;

			sourceEnd += n;
			return true;
		}
	}
}
