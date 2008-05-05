using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.JScript;

namespace Calyptus.Web
{
	class JavaScriptCompressor
	{
		public static List<string> Ones = new List<string>();
		public static List<string> Twos = new List<string>();
		public static List<string> Threes = new List<string>();

		public static HashSet<string> BuiltIn = new HashSet<string>();
		public static Dictionary<JSToken, string> Literals = new Dictionary<JSToken, string>();
		public static HashSet<string> Reserved = new HashSet<string>();

		public JavaScriptCompressor()
		{
			BuiltIn.Add("NaN");
			BuiltIn.Add("top");

			for (char c = 'A'; c <= 'Z'; c++)
				Ones.Add(c.ToString());

			for (char c = 'a'; c <= 'z'; c++)
				Ones.Add(c.ToString());

			for (int i = 0; i < Ones.Count; i++)
			{
				for (char c = 'A'; c <= 'Z'; c++)
					Twos.Add(Ones[i] + c.ToString());
				for (char c = 'a'; c <= 'z'; c++)
					Twos.Add(Ones[i] + c.ToString());
				for (char c = '0'; c <= '9'; c++)
					Twos.Add(Ones[i] + c.ToString());
			}

			// Remove two-letter JavaScript reserved words and built-in globals...
			Twos.Remove("as");
			Twos.Remove("is");
			Twos.Remove("do");
			Twos.Remove("if");
			Twos.Remove("in");

			// TODO: Remove Builtins

			for (int i = 0; i < Twos.Count; i++)
			{
				for (char c = 'A'; c <= 'Z'; c++)
					Threes.Add(Twos[i] + c.ToString());
				for (char c = 'a'; c <= 'z'; c++)
					Threes.Add(Twos[i] + c.ToString());
				for (char c = '0'; c <= '9'; c++)
					Threes.Add(Twos[i] + c.ToString());
			}

			// Remove three-letter JavaScript reserved words and built-in globals...
			Threes.Remove("for");
			Threes.Remove("int");
			Threes.Remove("new");
			Threes.Remove("try");
			Threes.Remove("use");
			Threes.Remove("var");
			//Threes.removeAll(builtin);

			// That's up to ((26+26)*(1+(26+26+10)))*(1+(26+26+10))-8
			// (206,380 symbols per scope)

			// The following list comes from org/mozilla/javascript/Decompiler.java...
			Literals.Add(Token.GET, "get ");
			Literals.Add(Token.SET, "set ");
			Literals.Add(Token.TRUE, "true");
			Literals.Add(Token.FALSE, "false");
			Literals.Add(Token.NULL, "null");
			Literals.Add(Token.THIS, "this");
			Literals.Add(Token.FUNCTION, "function ");
			Literals.Add(Token.COMMA, ",");
			Literals.Add(Token.LC, "{");
			Literals.Add(Token.RC, "}");
			Literals.Add(Token.LP, "(");
			Literals.Add(Token.RP, ")");
			Literals.Add(Token.LB, "[");
			Literals.Add(Token.RB, "]");
			Literals.Add(Token.DOT, ".");
			Literals.Add(Token.NEW, "new ");
			Literals.Add(Token.DELPROP, "delete ");
			Literals.Add(Token.IF, "if");
			Literals.Add(Token.ELSE, "else");
			Literals.Add(Token.FOR, "for");
			Literals.Add(Token.IN, " in ");
			Literals.Add(Token.WITH, "with");
			Literals.Add(Token.WHILE, "while");
			Literals.Add(Token.DO, "do");
			Literals.Add(Token.TRY, "try");
			Literals.Add(Token.CATCH, "catch");
			Literals.Add(Token.FINALLY, "finally");
			Literals.Add(Token.THROW, "throw ");
			Literals.Add(Token.SWITCH, "switch");
			Literals.Add(Token.BREAK, "break ");
			Literals.Add(Token.CONTINUE, "continue ");
			Literals.Add(Token.CASE, "case ");
			Literals.Add(Token.DEFAULT, "default");
			Literals.Add(Token.RETURN, "return ");
			Literals.Add(Token.VAR, "var ");
			Literals.Add(Token.SEMI, ";");
			Literals.Add(Token.ASSIGN, "=");
			Literals.Add(Token.ASSIGN_ADD, "+=");
			Literals.Add(Token.ASSIGN_SUB, "-=");
			Literals.Add(Token.ASSIGN_MUL, "*=");
			Literals.Add(Token.ASSIGN_DIV, "/=");
			Literals.Add(Token.ASSIGN_MOD, "%=");
			Literals.Add(Token.ASSIGN_BITOR, "|=");
			Literals.Add(Token.ASSIGN_BITXOR, "^=");
			Literals.Add(Token.ASSIGN_BITAND, "&=");
			Literals.Add(Token.ASSIGN_LSH, "<<=");
			Literals.Add(Token.ASSIGN_RSH, ">>=");
			Literals.Add(Token.ASSIGN_URSH, ">>>=");
			Literals.Add(Token.HOOK, "?");
			Literals.Add(Token.OBJECTLIT, ":");
			Literals.Add(Token.COLON, ":");
			Literals.Add(Token.OR, "||");
			Literals.Add(Token.AND, "&&");
			Literals.Add(Token.BITOR, "|");
			Literals.Add(Token.BITXOR, "^");
			Literals.Add(Token.BITAND, "&");
			Literals.Add(Token.SHEQ, "===");
			Literals.Add(Token.SHNE, "!==");
			Literals.Add(Token.EQ, "==");
			Literals.Add(Token.NE, "!=");
			Literals.Add(Token.LE, "<=");
			Literals.Add(Token.LT, "<");
			Literals.Add(Token.GE, ">=");
			Literals.Add(Token.GT, ">");
			Literals.Add(Token.INSTANCEOF, " instanceof ");
			Literals.Add(Token.LSH, "<<");
			Literals.Add(Token.RSH, ">>");
			Literals.Add(Token.URSH, ">>>");
			Literals.Add(Token.TYPEOF, "typeof ");
			Literals.Add(Token.VOID, "void ");
			Literals.Add(Token.CONST, "const ");
			Literals.Add(Token.NOT, "!");
			Literals.Add(Token.BITNOT, "~");
			Literals.Add(Token.POS, "+");
			Literals.Add(Token.NEG, "-");
			Literals.Add(Token.INC, "++");
			Literals.Add(Token.DEC, "--");
			Literals.Add(Token.ADD, "+");
			Literals.Add(Token.SUB, "-");
			Literals.Add(Token.MUL, "*");
			Literals.Add(Token.DIV, "/");
			Literals.Add(Token.MOD, "%");
			Literals.Add(Token.COLONCOLON, "::");
			Literals.Add(Token.DOTDOT, "..");
			Literals.Add(Token.DOTQUERY, ".(");
			Literals.Add(Token.XMLATTR, "@");

			// See http://developer.mozilla.org/en/docs/Core_JavaScript_1.5_Reference:Reserved_Words

			// JavaScript 1.5 reserved words
			Reserved.Add("break");
			Reserved.Add("case");
			Reserved.Add("catch");
			Reserved.Add("continue");
			Reserved.Add("default");
			Reserved.Add("delete");
			Reserved.Add("do");
			Reserved.Add("else");
			Reserved.Add("finally");
			Reserved.Add("for");
			Reserved.Add("function");
			Reserved.Add("if");
			Reserved.Add("in");
			Reserved.Add("instanceof");
			Reserved.Add("new");
			Reserved.Add("return");
			Reserved.Add("switch");
			Reserved.Add("this");
			Reserved.Add("throw");
			Reserved.Add("try");
			Reserved.Add("typeof");
			Reserved.Add("var");
			Reserved.Add("void");
			Reserved.Add("while");
			Reserved.Add("with");
			// Words reserved for future use
			Reserved.Add("abstract");
			Reserved.Add("boolean");
			Reserved.Add("byte");
			Reserved.Add("char");
			Reserved.Add("class");
			Reserved.Add("const");
			Reserved.Add("debugger");
			Reserved.Add("double");
			Reserved.Add("enum");
			Reserved.Add("export");
			Reserved.Add("extends");
			Reserved.Add("final");
			Reserved.Add("float");
			Reserved.Add("goto");
			Reserved.Add("implements");
			Reserved.Add("import");
			Reserved.Add("int");
			Reserved.Add("interface");
			Reserved.Add("long");
			Reserved.Add("native");
			Reserved.Add("package");
			Reserved.Add("private");
			Reserved.Add("protected");
			Reserved.Add("public");
			Reserved.Add("short");
			Reserved.Add("static");
			Reserved.Add("super");
			Reserved.Add("synchronized");
			Reserved.Add("throws");
			Reserved.Add("transient");
			Reserved.Add("volatile");
			// These are not reserved, but should be taken into account
			// in isValidIdentifier (See jslint source code)
			Reserved.Add("arguments");
			Reserved.Add("eval");
			Reserved.Add("true");
			Reserved.Add("false");
			Reserved.Add("Infinity");
			Reserved.Add("NaN");
			Reserved.Add("null");
			Reserved.Add("undefined");
		}

		private static int CountChar(string haystack, char needle)
		{
			int count = 0;
			char[] chars = haystack.ToCharArray();
			foreach (char c in chars)
				 if(c == needle) count++;

			return count;
		}

		private static int PrintSourceString(string source, int offset, StringBuilder sb)
		{
			int length = source[offset];
			++offset;
			if ((0x8000 & length) != 0)
			{
				length = ((0x7FFF & length) << 16) | source[offset];
				++offset;
			}
			if(sb != null)
				sb.Append(source.Substring(offset, offset + length));

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
					number = source[offset];

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
					lbits |= (long)source[offset + 3];
					if (type == 'J')
						number = lbits;
					else
						number = BitConverter.Int64BitsToDouble(lbits);
				}
				offset += 4;
			}
			else
				throw new ApplicationException();

			if (sb != null)
				sb.Append(number.ToString()); // ScriptRuntime.numberToString(number, 10)

			return offset;
		}
		/*
		private static List<JavaScriptToken> Parse(StreamReader sr)
		{
			Parser parser = new Parser();
			int offset = 0;
			int length = source.length();
			ArrayList tokens = new ArrayList();
			StringBuffer sb = new StringBuffer();

			while (offset < length) {
				int tt = source.charAt(offset++);
				switch (tt) {

					case Token.SPECIALCOMMENT:
					case Token.NAME:
					case Token.REGEXP:
					case Token.STRING:
						sb.setLength(0);
						offset = printSourceString(source, offset, sb);
						tokens.add(new JavaScriptToken(tt, sb.toString()));
						break;

					case Token.NUMBER:
						sb.setLength(0);
						offset = printSourceNumber(source, offset, sb);
						tokens.add(new JavaScriptToken(tt, sb.toString()));
						break;

					default:
						String literal = (String) literals.get(new Integer(tt));
						if (literal != null) {
							tokens.add(new JavaScriptToken(tt, literal));
						}
						break;
				}
			}
			return tokens;
		}

		*/
	//    private static ArrayList parse(Reader in, ErrorReporter reporter)
	//        throws IOException, EvaluatorException {

	//    CompilerEnvirons env = new CompilerEnvirons();
	//    Parser parser = new Parser(env, reporter);
	//    parser.parse(in, null, 1);
	//    String source = parser.getEncodedSource();

	//    int offset = 0;
	//    int length = source.length();
	//    ArrayList tokens = new ArrayList();
	//    StringBuffer sb = new StringBuffer();

	//    while (offset < length) {
	//        int tt = source.charAt(offset++);
	//        switch (tt) {

	//            case Token.SPECIALCOMMENT:
	//            case Token.NAME:
	//            case Token.REGEXP:
	//            case Token.STRING:
	//                sb.setLength(0);
	//                offset = printSourceString(source, offset, sb);
	//                tokens.add(new JavaScriptToken(tt, sb.toString()));
	//                break;

	//            case Token.NUMBER:
	//                sb.setLength(0);
	//                offset = printSourceNumber(source, offset, sb);
	//                tokens.add(new JavaScriptToken(tt, sb.toString()));
	//                break;

	//            default:
	//                String literal = (String) literals.get(new Integer(tt));
	//                if (literal != null) {
	//                    tokens.add(new JavaScriptToken(tt, literal));
	//                }
	//                break;
	//        }
	//    }

	//    return tokens;
	//}
	}
}
