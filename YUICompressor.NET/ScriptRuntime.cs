using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Org.Mozilla.JavaScript
{
	public class ScriptRuntime
	{
		/// <summary>Helper function for toNumber, parseInt, and TokenStream.getToken.</summary>
		public static double StringToNumber(string s, int start, int radix)
		{
			char digitMax = '9';
			char lowerCaseBound = 'a';
			char upperCaseBound = 'A';
			int len = s.Length;
			if (radix < 10)
				digitMax = (char) ('0' + radix - 1);

			if (radix > 10)
			{
				lowerCaseBound = (char) ('a' + radix - 10);
				upperCaseBound = (char) ('A' + radix - 10);
			}
			int end;
			double sum = 0.0;
			for (end=start; end < len; end++)
			{
				char c = s[end];
				int newDigit;
				if ('0' <= c && c <= digitMax)
					newDigit = c - '0';
				else if ('a' <= c && c < lowerCaseBound)
					newDigit = c - 'a' + 10;
				else if ('A' <= c && c < upperCaseBound)
					newDigit = c - 'A' + 10;
				else
					break;
				sum = sum*radix + newDigit;
			}
			if (start == end)
				return double.NaN;

			if (sum >= 9007199254740992.0)
			{
				if (radix == 10)
				{
					/* If we're accumulating a decimal number and the number
					 * is >= 2^53, then the result from the repeated multiply-add
					 * above may be inaccurate.  Call Java to get the correct
					 * answer.
					 */
					try
					{
						return double.Parse(s.Substring(start, end));
					}
					catch (FormatException)
					{
						return Double.NaN;
					}
				}
				else if (radix == 2 || radix == 4 || radix == 8 || radix == 16 || radix == 32)
				{
					/* The number may also be inaccurate for one of these bases.
					 * This happens if the addition in value*radix + digit causes
					 * a round-down to an even least significant mantissa bit
					 * when the first dropped bit is a one.  If any of the
					 * following digits in the number (which haven't been added
					 * in yet) are nonzero then the correct action would have
					 * been to round up instead of down.  An example of this
					 * occurs when reading the number 0x1000000000000081, which
					 * rounds to 0x1000000000000000 instead of 0x1000000000000100.
					 */
					int bitShiftInChar = 1;
					int digit = 0;

					const int SKIP_LEADING_ZEROS = 0;
					const int FIRST_EXACT_53_BITS = 1;
					const int AFTER_BIT_53         = 2;
					const int ZEROS_AFTER_54 = 3;
					const int MIXED_AFTER_54 = 4;

					int state = SKIP_LEADING_ZEROS;
					int exactBitsLimit = 53;
					double factor = 0.0;
					bool bit53 = false;
					// bit54 is the 54th bit (the first dropped from the mantissa)
					bool bit54 = false;

					while(true)
					{
						if (bitShiftInChar == 1)
						{
							if (start == end)
								break;
							digit = s[start++];
							if ('0' <= digit && digit <= '9')
								digit -= '0';
							else if ('a' <= digit && digit <= 'z')
								digit -= 'a' - 10;
							else
								digit -= 'A' - 10;
							bitShiftInChar = radix;
						}
						bitShiftInChar >>= 1;
						bool bit = (digit & bitShiftInChar) != 0;

						switch (state)
						{
							case SKIP_LEADING_ZEROS:
							if (bit)
							{
								--exactBitsLimit;
								sum = 1.0;
								state = FIRST_EXACT_53_BITS;
							}
							break;
						  case FIRST_EXACT_53_BITS:
							   sum *= 2.0;
							if (bit)
								sum += 1.0;
							--exactBitsLimit;
							if (exactBitsLimit == 0)
							{
								bit53 = bit;
								state = AFTER_BIT_53;
							}
							break;
						  case AFTER_BIT_53:
							bit54 = bit;
							factor = 2.0;
							state = ZEROS_AFTER_54;
							break;
						  case ZEROS_AFTER_54:
							if (bit)
							{
								state = MIXED_AFTER_54;
							}
							factor *= 2;
							break;
						  case MIXED_AFTER_54:
							factor *= 2;
							break;
						}
					}
					switch (state)
					{
					  case SKIP_LEADING_ZEROS:
						sum = 0.0;
						break;
					  case FIRST_EXACT_53_BITS:
					  case AFTER_BIT_53:
						// do nothing
						break;
					  case ZEROS_AFTER_54:
						// x1.1 -> x1 + 1 (round up)
						// x0.1 -> x0 (round down)
						if (bit54 & bit53)
							sum += 1.0;
						sum *= factor;
						break;
					  case MIXED_AFTER_54:
						// x.100...1.. -> x + 1 (round up)
						// x.0anything -> x (round down)
						if (bit54)
							sum += 1.0;
						sum *= factor;
						break;
					}
				}
				/* We don't worry about inaccurate numbers for any other base. */
			}
			return sum;
		}


		/// <summary>For escaping strings printed by object and array literals; not quite the same as 'escape.'</summary>
		public static string EscapeString(string s) { return EscapeString(s, '\''); }
		public static string EscapeString(string s, char escapeQuote)
		{
			if (!(escapeQuote == '"' || escapeQuote == '\''))
				throw new ArgumentException("escapeQuote must be a \" or a \'");

			StringBuilder sb = null;

			for (int i = 0, L = s.Length; i != L; ++i)
			{
				int c = s[i];

				if (' ' <= c && c <= '~' && c != escapeQuote && c != '\\')
				{
					// an ordinary print character (like C isprint()) and not "
					// or \ .
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
					sb.EnsureCapacity(i);
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
					case ' ': escape = ' '; break;
					case '\\': escape = '\\'; break;
				}
				if (escape >= 0)
				{
					// an \escaped sort of character
					sb.Append('\\');
					sb.Append((char)escape);
				}
				else if (c == escapeQuote)
				{
					sb.Append('\\');
					sb.Append(escapeQuote);
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

		public static string NumberToString(double d, int bas) {
			if (d == double.NaN)
				return "NaN";
			if (d == Double.PositiveInfinity)
				return "Infinity";
			if (d == Double.NegativeInfinity)
				return "-Infinity";
			if (d == 0.0)
				return "0";

			if ((bas < 2) || (bas > 36))
				throw new ArgumentException("msg.bad.radix", bas.ToString());

			if (bas != 10)
				throw new NotImplementedException("bas must be 10");
			else
				return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
		}

		public static int XDigitToInt(int c, int accumulator)
		{
			while (true)
			{
				// Use 0..9 < A..Z < a..z
				if (c <= '9')
				{
					c -= '0';
					if (0 <= c) break;
				}
				else if (c <= 'F')
				{
					if ('A' <= c)
					{
						c -= ('A' - 10);
						break;
					}
				}
				else if (c <= 'f')
				{
					if ('a' <= c)
					{
						c -= ('a' - 10);
						break;
					}
				}
				return -1;
			}
			return (accumulator << 4) | c;
		}

		/// <summary>It is public so NativeRegExp can access it.</summary>
		public static bool IsJSLineTerminator(int c)
		{
			// Optimization for faster check for eol character:
			// they do not have 0xDFD0 bits set
			if ((c & 0xDFD0) != 0)
				return false;

			return c == '\n' || c == '\r' || c == 0x2028 || c == 0x2029;
		}
	}
}
