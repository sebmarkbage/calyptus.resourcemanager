using System;

namespace Org.Mozilla.JavaScript
{
	static class Token
	{
		// debug flags
		public static bool PrintTrees = false;
		public static bool PrintICode = false;
		public static bool PrintNames = PrintTrees || PrintICode;


		public const int ERROR = -1;			// well-known as the only code < EOF
		public const int EOF = 0;				// end of file token - (not EOF_CHAR)
		public const int EOL = 1;				// end of line

		// Interpreter reuses the following as bytecodes
		public const int FIRST_BYTECODE_TOKEN = 2;

		public const int ENTERWITH = 2;
		public const int LEAVEWITH = 3;
		public const int RETURN = 4;
		public const int GOTO = 5;
		public const int IFEQ = 6;
		public const int IFNE = 7;
		public const int SETNAME = 8;
		public const int BITOR = 9;
		public const int BITXOR = 10;
		public const int BITAND = 11;
		public const int EQ = 12;
		public const int NE = 13;
		public const int LT = 14;
		public const int LE = 15;
		public const int GT = 16;
		public const int GE = 17;
		public const int LSH = 18;
		public const int RSH = 19;
		public const int URSH = 20;
		public const int ADD = 21;
		public const int SUB = 22;
		public const int MUL = 23;
		public const int DIV = 24;
		public const int MOD = 25;
		public const int NOT = 26;
		public const int BITNOT = 27;
		public const int POS = 28;
		public const int NEG = 29;
		public const int NEW = 30;
		public const int DELPROP = 31;
		public const int TYPEOF = 32;
		public const int GETPROP = 33;
		public const int SETPROP = 34;
		public const int GETELEM = 35;
		public const int SETELEM = 36;
		public const int CALL = 37;
		public const int NAME = 38;
		public const int NUMBER = 39;
		public const int STRING = 40;
		public const int NULL = 41;
		public const int THIS = 42;
		public const int FALSE = 43;
		public const int TRUE = 44;
		public const int SHEQ = 45;				// shallow equality (===)
		public const int SHNE = 46;				// shallow inequality (!==)
		public const int REGEXP = 47;
		public const int BINDNAME = 48;
		public const int THROW = 49;
		public const int RETHROW = 50;			// rethrow caught execetion: catch (e if ) use it
		public const int IN = 51;
		public const int INSTANCEOF = 52;
		public const int LOCAL_LOAD = 53;
		public const int GETVAR = 54;
		public const int SETVAR = 55;
		public const int CATCH_SCOPE = 56;
		public const int ENUM_INIT_KEYS = 57;
		public const int ENUM_INIT_VALUES = 58;
		public const int ENUM_NEXT = 59;
		public const int ENUM_ID = 60;
		public const int THISFN = 61;
		public const int RETURN_RESULT = 62;		// to return prevoisly stored return result
		public const int ARRAYLIT = 63;			// array literal
		public const int OBJECTLIT = 64;			// object literal
		public const int GET_REF = 65;			// *reference
		public const int SET_REF = 66;			// *reference = something
		public const int DEL_REF = 67;			// delete reference
		public const int REF_CALL = 68;			// f(args) = something or f(args)++
		public const int REF_SPECIAL = 69;		// reference for special properties like __proto

		// For XML support:
		public const int DEFAULTNAMESPACE = 70;	// default xml namespace =
		public const int ESCXMLATTR = 71;
		public const int ESCXMLTEXT = 72;
		public const int REF_MEMBER = 73;		// Reference for x.@y; x..y etc.
		public const int REF_NS_MEMBER = 74;		// Reference for x.ns::y; x..ns::y etc.
		public const int REF_NAME = 75;			// Reference for @y; @[y] etc.
		public const int REF_NS_NAME = 76;		// Reference for ns::y; @ns::y@[y] etc.

		// End of interpreter bytecodes
		public const int LAST_BYTECODE_TOKEN = REF_NS_NAME;

		public const int TRY = 77;
		public const int SEMI = 78;				// semicolon
		public const int LB = 79;				// left and right brackets
		public const int RB = 80;
		public const int LC = 81;				// left and right curlies (braces)
		public const int RC = 82;
		public const int LP = 83;				// left and right parentheses
		public const int RP = 84;
		public const int COMMA = 85;				// comma operator

		public const int ASSIGN = 86;			// simple assignment (=)
		public const int ASSIGN_BITOR = 87;		// |=
		public const int ASSIGN_BITXOR = 88;	// ^=
		public const int ASSIGN_BITAND = 89;	// |=
		public const int ASSIGN_LSH = 90;		// <<=
		public const int ASSIGN_RSH = 91;		// >>=
		public const int ASSIGN_URSH = 92;		// >>>=
		public const int ASSIGN_ADD = 93;		// +=
		public const int ASSIGN_SUB = 94;		// -=
		public const int ASSIGN_MUL = 95;		// *=
		public const int ASSIGN_DIV = 96;		// /=
		public const int ASSIGN_MOD = 97;		// %=

		public const int FIRST_ASSIGN = ASSIGN;
		public const int LAST_ASSIGN = ASSIGN_MOD;

		public const int HOOK = 98;				// conditional (?:)
		public const int COLON = 99;
		public const int OR = 100;				// logical or (||)
		public const int AND = 101;				// logical and (&&)
		public const int INC = 102;				// increment/decrement (++ --)
		public const int DEC = 103;
		public const int DOT = 104;				// member operator (.)
		public const int FUNCTION = 105;		// function keyword
		public const int EXPORT = 106;			// export keyword
		public const int IMPORT = 107;			// import keyword
		public const int IF = 108;				// if keyword
		public const int ELSE = 109;			// else keyword
		public const int SWITCH = 110;			// switch keyword
		public const int CASE = 111;			// case keyword
		public const int DEFAULT = 112;			// default keyword
		public const int WHILE = 113;			// while keyword
		public const int DO = 114;				// do keyword
		public const int FOR = 115;				// for keyword
		public const int BREAK = 116;			// break keyword
		public const int CONTINUE = 117;		// continue keyword
		public const int VAR = 118;				// var keyword
		public const int WITH = 119;			// with keyword
		public const int CATCH = 120;			// catch keyword
		public const int FINALLY = 121;			// finally keyword
		public const int VOID = 122;			// void keyword
		public const int RESERVED = 123;		// reserved keywords

		public const int EMPTY = 124;

		/* types used for the parse tree - these never get returned
		* by the scanner.
		*/

		public const int BLOCK = 125;			// statement block
		public const int LABEL = 126;			// label
		public const int TARGET = 127;
		public const int LOOP = 128;
		public const int EXPR_VOID = 129;		// expression statement in functions
		public const int EXPR_RESULT = 130;		// expression statement in scripts
		public const int JSR = 131;
		public const int SCRIPT = 132;			// top-level node for entire script
		public const int TYPEOFNAME = 133;		// for typeof(simple-name)
		public const int USE_STACK = 134;
		public const int SETPROP_OP = 135;		// x.y op= something
		public const int SETELEM_OP = 136;		// x[y] op= something
		public const int LOCAL_BLOCK = 137;
		public const int SET_REF_OP = 138;		// *reference op= something

		// For XML support:
		public const int DOTDOT = 139;			// member operator (..)
		public const int COLONCOLON = 140;		// namespace::name
		public const int XML = 141;				// XML type
		public const int DOTQUERY = 142;		// .() -- e.g.; x.emps.emp.(name == "terry")
		public const int XMLATTR = 143;			// @
		public const int XMLEND = 144;

		// Optimizer-only-tokens
		public const int TO_OBJECT = 145;
		public const int TO_DOUBLE = 146;

		public const int GET = 147;				// JS 1.5 get pseudo keyword
		public const int SET = 148;				// JS 1.5 set pseudo keyword
		public const int CONST = 149;
		public const int SETCONST = 150;
		public const int SETCONSTVAR = 151;

		public const int SPECIALCOMMENT = 152;	// Internet Explorer conditional comment

		public const int LAST_TOKEN = 153;

		public static string Name(int token)
		{
			if (!PrintNames)
				return token.ToString();

			switch (token)
			{
				case ERROR: return "ERROR";
				case EOF: return "EOF";
				case EOL: return "EOL";
				case ENTERWITH: return "ENTERWITH";
				case LEAVEWITH: return "LEAVEWITH";
				case RETURN: return "RETURN";
				case GOTO: return "GOTO";
				case IFEQ: return "IFEQ";
				case IFNE: return "IFNE";
				case SETNAME: return "SETNAME";
				case BITOR: return "BITOR";
				case BITXOR: return "BITXOR";
				case BITAND: return "BITAND";
				case EQ: return "EQ";
				case NE: return "NE";
				case LT: return "LT";
				case LE: return "LE";
				case GT: return "GT";
				case GE: return "GE";
				case LSH: return "LSH";
				case RSH: return "RSH";
				case URSH: return "URSH";
				case ADD: return "ADD";
				case SUB: return "SUB";
				case MUL: return "MUL";
				case DIV: return "DIV";
				case MOD: return "MOD";
				case NOT: return "NOT";
				case BITNOT: return "BITNOT";
				case POS: return "POS";
				case NEG: return "NEG";
				case NEW: return "NEW";
				case DELPROP: return "DELPROP";
				case TYPEOF: return "TYPEOF";
				case GETPROP: return "GETPROP";
				case SETPROP: return "SETPROP";
				case GETELEM: return "GETELEM";
				case SETELEM: return "SETELEM";
				case CALL: return "CALL";
				case NAME: return "NAME";
				case NUMBER: return "NUMBER";
				case STRING: return "STRING";
				case NULL: return "NULL";
				case THIS: return "THIS";
				case FALSE: return "FALSE";
				case TRUE: return "TRUE";
				case SHEQ: return "SHEQ";
				case SHNE: return "SHNE";
				case REGEXP: return "OBJECT";
				case BINDNAME: return "BINDNAME";
				case THROW: return "THROW";
				case RETHROW: return "RETHROW";
				case IN: return "IN";
				case INSTANCEOF: return "INSTANCEOF";
				case LOCAL_LOAD: return "LOCAL_LOAD";
				case GETVAR: return "GETVAR";
				case SETVAR: return "SETVAR";
				case CATCH_SCOPE: return "CATCH_SCOPE";
				case ENUM_INIT_KEYS: return "ENUM_INIT_KEYS";
				case ENUM_INIT_VALUES: return "ENUM_INIT_VALUES";
				case ENUM_NEXT: return "ENUM_NEXT";
				case ENUM_ID: return "ENUM_ID";
				case THISFN: return "THISFN";
				case RETURN_RESULT: return "RETURN_RESULT";
				case ARRAYLIT: return "ARRAYLIT";
				case OBJECTLIT: return "OBJECTLIT";
				case GET_REF: return "GET_REF";
				case SET_REF: return "SET_REF";
				case DEL_REF: return "DEL_REF";
				case REF_CALL: return "REF_CALL";
				case REF_SPECIAL: return "REF_SPECIAL";
				case DEFAULTNAMESPACE: return "DEFAULTNAMESPACE";
				case ESCXMLTEXT: return "ESCXMLTEXT";
				case ESCXMLATTR: return "ESCXMLATTR";
				case REF_MEMBER: return "REF_MEMBER";
				case REF_NS_MEMBER: return "REF_NS_MEMBER";
				case REF_NAME: return "REF_NAME";
				case REF_NS_NAME: return "REF_NS_NAME";
				case TRY: return "TRY";
				case SEMI: return "SEMI";
				case LB: return "LB";
				case RB: return "RB";
				case LC: return "LC";
				case RC: return "RC";
				case LP: return "LP";
				case RP: return "RP";
				case COMMA: return "COMMA";
				case ASSIGN: return "ASSIGN";
				case ASSIGN_BITOR: return "ASSIGN_BITOR";
				case ASSIGN_BITXOR: return "ASSIGN_BITXOR";
				case ASSIGN_BITAND: return "ASSIGN_BITAND";
				case ASSIGN_LSH: return "ASSIGN_LSH";
				case ASSIGN_RSH: return "ASSIGN_RSH";
				case ASSIGN_URSH: return "ASSIGN_URSH";
				case ASSIGN_ADD: return "ASSIGN_ADD";
				case ASSIGN_SUB: return "ASSIGN_SUB";
				case ASSIGN_MUL: return "ASSIGN_MUL";
				case ASSIGN_DIV: return "ASSIGN_DIV";
				case ASSIGN_MOD: return "ASSIGN_MOD";
				case HOOK: return "HOOK";
				case COLON: return "COLON";
				case OR: return "OR";
				case AND: return "AND";
				case INC: return "INC";
				case DEC: return "DEC";
				case DOT: return "DOT";
				case FUNCTION: return "FUNCTION";
				case EXPORT: return "EXPORT";
				case IMPORT: return "IMPORT";
				case IF: return "IF";
				case ELSE: return "ELSE";
				case SWITCH: return "SWITCH";
				case CASE: return "CASE";
				case DEFAULT: return "DEFAULT";
				case WHILE: return "WHILE";
				case DO: return "DO";
				case FOR: return "FOR";
				case BREAK: return "BREAK";
				case CONTINUE: return "CONTINUE";
				case VAR: return "VAR";
				case WITH: return "WITH";
				case CATCH: return "CATCH";
				case FINALLY: return "FINALLY";
				case RESERVED: return "RESERVED";
				case EMPTY: return "EMPTY";
				case BLOCK: return "BLOCK";
				case LABEL: return "LABEL";
				case TARGET: return "TARGET";
				case LOOP: return "LOOP";
				case EXPR_VOID: return "EXPR_VOID";
				case EXPR_RESULT: return "EXPR_RESULT";
				case JSR: return "JSR";
				case SCRIPT: return "SCRIPT";
				case TYPEOFNAME: return "TYPEOFNAME";
				case USE_STACK: return "USE_STACK";
				case SETPROP_OP: return "SETPROP_OP";
				case SETELEM_OP: return "SETELEM_OP";
				case LOCAL_BLOCK: return "LOCAL_BLOCK";
				case SET_REF_OP: return "SET_REF_OP";
				case DOTDOT: return "DOTDOT";
				case COLONCOLON: return "COLONCOLON";
				case XML: return "XML";
				case DOTQUERY: return "DOTQUERY";
				case XMLATTR: return "XMLATTR";
				case XMLEND: return "XMLEND";
				case TO_OBJECT: return "TO_OBJECT";
				case TO_DOUBLE: return "TO_DOUBLE";
				case GET: return "GET";
				case SET: return "SET";
				case CONST: return "CONST";
				case SETCONST: return "SETCONST";
			}

			// Token without name
			throw new IndexOutOfRangeException("Token: " + token.ToString());
		}
	}
}
