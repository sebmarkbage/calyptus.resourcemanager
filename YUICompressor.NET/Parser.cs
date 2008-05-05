using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Mozilla.JavaScript
{
	class Parser
	{
		// TokenInformation flags : currentFlaggedToken stores them together
		// with token type
		public const int CLEAR_TI_MASK  = 0xFFFF;   // mask to clear token information bits
        public const int TI_AFTER_EOL   = 1 << 16;  // first token of the source line
        public const int TI_CHECK_LABEL = 1 << 17;  // indicates to check for label

		public CompilerEnvirons compilerEnv;
		private string sourceURI;
		private bool calledByCompileFunction;

		private TokenStream ts;
		private int currentFlaggedToken;
		private int syntaxErrorCount;

		private IRFactory nf;

		private int nestingOfFunction;

		private Decompiler decompiler;
		private string encodedSource;

		// The following are per function variables and should be saved/restored
		// during function parsing.
		// XXX Move to separated class?
		ScriptOrFnNode currentScriptOrFn;
		private int nestingOfWith;
		private Hashtable labelSet; // map of label names into nodes
		private ObjArray loopSet;
		private ObjArray loopAndSwitchSet;
		private bool hasReturnValue;
		private int functionEndFlags;
		// end of per function variables

		public void AddStrictWarning(string messageId, string messageArg)
		{
			if (compilerEnv.StrictMode)
				AddWarning(messageId, messageArg);
		}

		public void AddWarning(string messageId, string messageArg)
		{
			string message = ScriptRuntime.getMessage1(messageId, messageArg);
			if (compilerEnv.reportWarningAsError()) {
				++syntaxErrorCount;
				errorReporter.error(message, sourceURI, ts.getLineno(),
									ts.getLine(), ts.getOffset());
			} else
				errorReporter.warning(message, sourceURI, ts.getLineno(),
									  ts.getLine(), ts.getOffset());
		}

		public void AddError(String messageId)
		{
			++syntaxErrorCount;
			String message = ScriptRuntime.getMessage0(messageId);
			errorReporter.error(message, sourceURI, ts.getLineno(),
								ts.getLine(), ts.getOffset());
		}

		public void AddError(String messageId, String messageArg)
		{
			++syntaxErrorCount;
			String message = ScriptRuntime.getMessage1(messageId, messageArg);
			errorReporter.error(message, sourceURI, ts.getLineno(),
								ts.getLine(), ts.getOffset());
		}

		RuntimeException reportError(String messageId)
		{
			addError(messageId);

			// Throw a ParserException exception to unwind the recursive descent
			// parse.
			throw new ParserException();
		}

		public Parser(CompilerEnvirons compilerEnv)
		{
			compilerEnv = compilerEnv;
		}

		protected Decompiler CreateDecompiler(CompilerEnvirons compilerEnv)
		{
			return new Decompiler();
		}

		private int PeekToken()
		{
			int tt = currentFlaggedToken;
			if (tt == Token.EOF)
			{

				while ((tt = ts.GetToken()) == Token.SPECIALCOMMENT)
				{
					/* Support for JScript conditional comments */
					//TODO: decompiler.addJScriptConditionalComment(ts.getstring());
				}

				if (tt == Token.EOL)
				{
					do
					{
						tt = ts.GetToken();

						if (tt == Token.SPECIALCOMMENT)
						{
							/* Support for JScript conditional comments */
							//TODO: decompiler.addJScriptConditionalComment(ts.getstring());
						}

					} while (tt == Token.EOL || tt == Token.SPECIALCOMMENT);
					tt |= TI_AFTER_EOL;
				}
				currentFlaggedToken = tt;
			}
			return tt & CLEAR_TI_MASK;
		}

		private int PeekFlaggedToken()
		{
			PeekToken();
			return currentFlaggedToken;
		}

		private void ConsumeToken()
		{
			currentFlaggedToken = Token.EOF;
		}

		private int NextToken()
		{
			int tt = PeekToken();
			ConsumeToken();
			return tt;
		}

		private int NextFlaggedToken()
		{
			PeekToken();
			int ttFlagged = currentFlaggedToken;
			ConsumeToken();
			return ttFlagged;
		}

		private bool MatchToken(int toMatch)
		{
			int tt = PeekToken();
			if (tt != toMatch) {
				return false;
			}
			ConsumeToken();
			return true;
		}

		private int PeekTokenOrEOL()
		{
			int tt = PeekToken();
			// Check for last peeked token flags
			if ((currentFlaggedToken & TI_AFTER_EOL) != 0)
				tt = Token.EOL;

			return tt;
		}

		private void SetCheckForLabel()
		{
			//if ((currentFlaggedToken & CLEAR_TI_MASK) != Token.NAME)
			//TODO: throw Kit.codeBug();

			currentFlaggedToken |= TI_CHECK_LABEL;
		}

		private void MustMatchToken(int toMatch, string messageId)
		{
			//if (!MatchToken(toMatch))
			//TODO: reportError(messageId);
		}

		private void MustHaveXML()
		{
			if (!compilerEnv.XmlAvialable)
			{
				reportError("msg.XML.not.available");
			}
		}

		public string getEncodedSource()
		{
			return encodedSource;
		}

		public bool eof()
		{
			return ts.eof();
		}

		bool insideFunction()
		{
			return nestingOfFunction != 0;
		}

		private Node enterLoop(Node loopLabel)
		{
			Node loop = nf.createLoopNode(loopLabel, ts.getLineno());
			if (loopSet == null) {
				loopSet = new ObjArray();
				if (loopAndSwitchSet == null) {
					loopAndSwitchSet = new ObjArray();
				}
			}
			loopSet.push(loop);
			loopAndSwitchSet.push(loop);
			return loop;
		}

		private void exitLoop()
		{
			loopSet.pop();
			loopAndSwitchSet.pop();
		}

		private Node enterSwitch(Node switchSelector, int lineno)
		{
			Node switchNode = nf.createSwitch(switchSelector, lineno);
			if (loopAndSwitchSet == null) {
				loopAndSwitchSet = new ObjArray();
			}
			loopAndSwitchSet.push(switchNode);
			return switchNode;
		}

		private void exitSwitch()
		{
			loopAndSwitchSet.pop();
		}

		/*
		 * Build a parse tree from the given sourcestring.
		 *
		 * @return an Object representing the parsed
		 * program.  If the parse fails, null will be returned.  (The
		 * parse failure will result in a call to the ErrorReporter from
		 * CompilerEnvirons.)
		 */
		public ScriptOrFnNode parse(string sourcestring,
									string sourceURI, int lineno)
		{
			this.sourceURI = sourceURI;
			this.ts = new TokenStream(this, null, sourcestring, lineno);
			try {
				return parse();
			} catch (IOException ex) {
				// Should never happen
				throw new IllegalStateException();
			}
		}

		/*
		 * Build a parse tree from the given sourcestring.
		 *
		 * @return an Object representing the parsed
		 * program.  If the parse fails, null will be returned.  (The
		 * parse failure will result in a call to the ErrorReporter from
		 * CompilerEnvirons.)
		 */
		public ScriptOrFnNode parse(Reader sourceReader,
									string sourceURI, int lineno)
			throws IOException
		{
			this.sourceURI = sourceURI;
			this.ts = new TokenStream(this, sourceReader, null, lineno);
			return parse();
		}

		private ScriptOrFnNode parse()
			throws IOException
		{
			this.decompiler = createDecompiler(compilerEnv);
			this.nf = new IRFactory(this);
			currentScriptOrFn = nf.createScript();
			int sourceStartOffset = decompiler.getCurrentOffset();
			this.encodedSource = null;
			decompiler.addToken(Token.SCRIPT);

			this.currentFlaggedToken = Token.EOF;
			this.syntaxErrorCount = 0;

			int baseLineno = ts.getLineno();  // line number where source starts

			/* so we have something to add nodes to until
			 * we've collected all the source */
			Node pn = nf.createLeaf(Token.BLOCK);

			try {
				for (;;) {
					int tt = PeekToken();

					if (tt <= Token.EOF) {
						break;
					}

					Node n;
					if (tt == Token.FUNCTION) {
						ConsumeToken();
						try {
							n = function(calledByCompileFunction
										 ? FunctionNode.FUNCTION_EXPRESSION
										 : FunctionNode.FUNCTION_STATEMENT);
						} catch (ParserException e) {
							break;
						}
					} else {
						n = statement();
					}
					nf.addChildToBack(pn, n);
				}
			} catch (StackOverflowError ex) {
				string msg = ScriptRuntime.getMessage0(
					"msg.too.deep.parser.recursion");
				throw Context.reportRuntimeError(msg, sourceURI,
												 ts.getLineno(), null, 0);
			}

			if (this.syntaxErrorCount != 0) {
				string msg = string.valueOf(this.syntaxErrorCount);
				msg = ScriptRuntime.getMessage1("msg.got.syntax.errors", msg);
				throw errorReporter.runtimeError(msg, sourceURI, baseLineno,
												 null, 0);
			}

			currentScriptOrFn.setSourceName(sourceURI);
			currentScriptOrFn.setBaseLineno(baseLineno);
			currentScriptOrFn.setEndLineno(ts.getLineno());

			int sourceEndOffset = decompiler.getCurrentOffset();
			currentScriptOrFn.setEncodedSourceBounds(sourceStartOffset,
													 sourceEndOffset);

			nf.initScript(currentScriptOrFn, pn);

			if (compilerEnv.isGeneratingSource()) {
				encodedSource = decompiler.getEncodedSource();
			}
			this.decompiler = null; // It helps GC

			return currentScriptOrFn;
		}

		/*
		 * The C version of this function takes an argument list,
		 * which doesn't seem to be needed for tree generation...
		 * it'd only be useful for checking argument hiding, which
		 * I'm not doing anyway...
		 */
		private Node parseFunctionBody()
			throws IOException
		{
			++nestingOfFunction;
			Node pn = nf.createBlock(ts.getLineno());
			try {
				bodyLoop: for (;;) {
					Node n;
					int tt = PeekToken();
					switch (tt) {
					  case Token.ERROR:
					  case Token.EOF:
					  case Token.RC:
						break bodyLoop;

					  case Token.FUNCTION:
						ConsumeToken();
						n = function(FunctionNode.FUNCTION_STATEMENT);
						break;
					  default:
						n = statement();
						break;
					}
					nf.addChildToBack(pn, n);
				}
			} catch (ParserException e) {
				// Ignore it
			} finally {
				--nestingOfFunction;
			}

			return pn;
		}

		private Node function(int functionType)
			throws IOException, ParserException
		{
			int syntheticType = functionType;
			int baseLineno = ts.getLineno();  // line number where source starts

			int functionSourceStart = decompiler.markFunctionStart(functionType);
			string name;
			Node memberExprNode = null;
			if (MatchToken(Token.NAME)) {
				name = ts.getstring();
				decompiler.addName(name);
				if (!MatchToken(Token.LP)) {
					if (compilerEnv.isAllowMemberExprAsFunctionName()) {
						// Extension to ECMA: if 'function <name>' does not follow
						// by '(', assume <name> starts memberExpr
						Node memberExprHead = nf.createName(name);
						name = "";
						memberExprNode = memberExprTail(false, memberExprHead);
					}
					MustMatchToken(Token.LP, "msg.no.paren.parms");
				}
			} else if (MatchToken(Token.LP)) {
				// Anonymous function
				name = "";
			} else {
				name = "";
				if (compilerEnv.isAllowMemberExprAsFunctionName()) {
					// Note that memberExpr can not start with '(' like
					// in function (1+2).tostring(), because 'function (' already
					// processed as anonymous function
					memberExprNode = memberExpr(false);
				}
				MustMatchToken(Token.LP, "msg.no.paren.parms");
			}

			if (memberExprNode != null) {
				syntheticType = FunctionNode.FUNCTION_EXPRESSION;
			}

			bool nested = insideFunction();

			FunctionNode fnNode = nf.createFunction(name);
			if (nested || nestingOfWith > 0) {
				// 1. Nested functions are not affected by the dynamic scope flag
				// as dynamic scope is already a parent of their scope.
				// 2. Functions defined under the with statement also immune to
				// this setup, in which case dynamic scope is ignored in favor
				// of with object.
				fnNode.itsIgnoreDynamicScope = true;
			}

			int functionIndex = currentScriptOrFn.addFunction(fnNode);

			int functionSourceEnd;

			ScriptOrFnNode savedScriptOrFn = currentScriptOrFn;
			currentScriptOrFn = fnNode;
			int savedNestingOfWith = nestingOfWith;
			nestingOfWith = 0;
			Hashtable savedLabelSet = labelSet;
			labelSet = null;
			ObjArray savedLoopSet = loopSet;
			loopSet = null;
			ObjArray savedLoopAndSwitchSet = loopAndSwitchSet;
			loopAndSwitchSet = null;
			bool savedHasReturnValue = hasReturnValue;
			int savedFunctionEndFlags = functionEndFlags;

			Node body;
			try {
				decompiler.addToken(Token.LP);
				if (!MatchToken(Token.RP)) {
					bool first = true;
					do {
						if (!first)
							decompiler.addToken(Token.COMMA);
						first = false;
						MustMatchToken(Token.NAME, "msg.no.parm");
						string s = ts.getstring();
						if (fnNode.hasParamOrVar(s)) {
							addWarning("msg.dup.parms", s);
						}
						fnNode.addParam(s);
						decompiler.addName(s);
					} while (MatchToken(Token.COMMA));

					MustMatchToken(Token.RP, "msg.no.paren.after.parms");
				}
				decompiler.addToken(Token.RP);

				MustMatchToken(Token.LC, "msg.no.brace.body");
				decompiler.addEOL(Token.LC);
				body = parseFunctionBody();
				MustMatchToken(Token.RC, "msg.no.brace.after.body");

				if (compilerEnv.isStrictMode() && !body.hasConsistentReturnUsage())
				{
				  string msg = name.length() > 0 ? "msg.no.return.value"
												 : "msg.anon.no.return.value";
				  addStrictWarning(msg, name);
				}

				decompiler.addToken(Token.RC);
				functionSourceEnd = decompiler.markFunctionEnd(functionSourceStart);
				if (functionType != FunctionNode.FUNCTION_EXPRESSION) {
					// Add EOL only if function is not part of expression
					// since it gets SEMI + EOL from Statement in that case
					decompiler.addToken(Token.EOL);
				}
			}
			finally {
				hasReturnValue = savedHasReturnValue;
				functionEndFlags = savedFunctionEndFlags;
				loopAndSwitchSet = savedLoopAndSwitchSet;
				loopSet = savedLoopSet;
				labelSet = savedLabelSet;
				nestingOfWith = savedNestingOfWith;
				currentScriptOrFn = savedScriptOrFn;
			}

			fnNode.setEncodedSourceBounds(functionSourceStart, functionSourceEnd);
			fnNode.setSourceName(sourceURI);
			fnNode.setBaseLineno(baseLineno);
			fnNode.setEndLineno(ts.getLineno());

			if (name != null) {
			  int index = currentScriptOrFn.getParamOrVarIndex(name);
			  if (index >= 0 && index < currentScriptOrFn.getParamCount())
				addStrictWarning("msg.var.hides.arg", name);
			}

			Node pn = nf.initFunction(fnNode, functionIndex, body, syntheticType);
			if (memberExprNode != null) {
				pn = nf.createAssignment(Token.ASSIGN, memberExprNode, pn);
				if (functionType != FunctionNode.FUNCTION_EXPRESSION) {
					// XXX check JScript behavior: should it be createExprStatement?
					pn = nf.createExprStatementNoReturn(pn, baseLineno);
				}
			}
			return pn;
		}

		private Node statements()
			throws IOException
		{
			Node pn = nf.createBlock(ts.getLineno());

			int tt;
			while((tt = PeekToken()) > Token.EOF && tt != Token.RC) {
				nf.addChildToBack(pn, statement());
			}

			return pn;
		}

		private Node condition()
			throws IOException, ParserException
		{
			MustMatchToken(Token.LP, "msg.no.paren.cond");
			decompiler.addToken(Token.LP);
			Node pn = expr(false);
			MustMatchToken(Token.RP, "msg.no.paren.after.cond");
			decompiler.addToken(Token.RP);

			// Report strict warning on code like "if (a = 7) ...". Suppress the
			// warning if the condition is parenthesized, like "if ((a = 7)) ...".
			if (pn.getProp(Node.PARENTHESIZED_PROP) == null &&
				(pn.getType() == Token.SETNAME || pn.getType() == Token.SETPROP ||
				 pn.getType() == Token.SETELEM))
			{
				addStrictWarning("msg.equal.as.assign", "");
			}
			return pn;
		}

		// match a NAME; return null if no match.
		private Node matchJumpLabelName()
			throws IOException, ParserException
		{
			Node label = null;

			int tt = PeekTokenOrEOL();
			if (tt == Token.NAME) {
				ConsumeToken();
				string name = ts.getstring();
				decompiler.addName(name);
				if (labelSet != null) {
					label = (Node)labelSet.get(name);
				}
				if (label == null) {
					reportError("msg.undef.label");
				}
			}

			return label;
		}

		private Node statement()
			throws IOException
		{
			try {
				Node pn = statementHelper(null);
				if (pn != null) {
					if (compilerEnv.isStrictMode() && !pn.hasSideEffects())
						addStrictWarning("msg.no.side.effects", "");
					return pn;
				}
			} catch (ParserException e) { }

			// skip to end of statement
			int lineno = ts.getLineno();
			guessingStatementEnd: for (;;) {
				int tt = PeekTokenOrEOL();
				ConsumeToken();
				switch (tt) {
				  case Token.ERROR:
				  case Token.EOF:
				  case Token.EOL:
				  case Token.SEMI:
					break guessingStatementEnd;
				}
			}
			return nf.createExprStatement(nf.createName("error"), lineno);
		}

		/**
		 * Whether the "catch (e: e instanceof Exception) { ... }" syntax
		 * is implemented.
		 */

		private Node statementHelper(Node statementLabel)
			throws IOException, ParserException
		{
			Node pn = null;

			int tt;

			tt = PeekToken();

			switch(tt) {
			  case Token.IF: {
				ConsumeToken();

				decompiler.addToken(Token.IF);
				int lineno = ts.getLineno();
				Node cond = condition();
				decompiler.addEOL(Token.LC);
				Node ifTrue = statement();
				Node ifFalse = null;
				if (MatchToken(Token.ELSE)) {
					decompiler.addToken(Token.RC);
					decompiler.addToken(Token.ELSE);
					decompiler.addEOL(Token.LC);
					ifFalse = statement();
				}
				decompiler.addEOL(Token.RC);
				pn = nf.createIf(cond, ifTrue, ifFalse, lineno);
				return pn;
			  }

			  case Token.SWITCH: {
				ConsumeToken();

				decompiler.addToken(Token.SWITCH);
				int lineno = ts.getLineno();
				MustMatchToken(Token.LP, "msg.no.paren.switch");
				decompiler.addToken(Token.LP);
				pn = enterSwitch(expr(false), lineno);
				try {
					MustMatchToken(Token.RP, "msg.no.paren.after.switch");
					decompiler.addToken(Token.RP);
					MustMatchToken(Token.LC, "msg.no.brace.switch");
					decompiler.addEOL(Token.LC);

					bool hasDefault = false;
					switchLoop: for (;;) {
						tt = nextToken();
						Node caseExpression;
						switch (tt) {
						  case Token.RC:
							break switchLoop;

						  case Token.CASE:
							decompiler.addToken(Token.CASE);
							caseExpression = expr(false);
							MustMatchToken(Token.COLON, "msg.no.colon.case");
							decompiler.addEOL(Token.COLON);
							break;

						  case Token.DEFAULT:
							if (hasDefault) {
								reportError("msg.double.switch.default");
							}
							decompiler.addToken(Token.DEFAULT);
							hasDefault = true;
							caseExpression = null;
							MustMatchToken(Token.COLON, "msg.no.colon.case");
							decompiler.addEOL(Token.COLON);
							break;

						  default:
							reportError("msg.bad.switch");
							break switchLoop;
						}

						Node block = nf.createLeaf(Token.BLOCK);
						while ((tt = PeekToken()) != Token.RC
							   && tt != Token.CASE
							   && tt != Token.DEFAULT
							   && tt != Token.EOF)
						{
							nf.addChildToBack(block, statement());
						}

						// caseExpression == null => add default lable
						nf.addSwitchCase(pn, caseExpression, block);
					}
					decompiler.addEOL(Token.RC);
					nf.closeSwitch(pn);
				} finally {
					exitSwitch();
				}
				return pn;
			  }

			  case Token.WHILE: {
				ConsumeToken();
				decompiler.addToken(Token.WHILE);

				Node loop = enterLoop(statementLabel);
				try {
					Node cond = condition();
					decompiler.addEOL(Token.LC);
					Node body = statement();
					decompiler.addEOL(Token.RC);
					pn = nf.createWhile(loop, cond, body);
				} finally {
					exitLoop();
				}
				return pn;
			  }

			  case Token.DO: {
				ConsumeToken();
				decompiler.addToken(Token.DO);
				decompiler.addEOL(Token.LC);

				Node loop = enterLoop(statementLabel);
				try {
					Node body = statement();
					decompiler.addToken(Token.RC);
					MustMatchToken(Token.WHILE, "msg.no.while.do");
					decompiler.addToken(Token.WHILE);
					Node cond = condition();
					pn = nf.createDoWhile(loop, body, cond);
				} finally {
					exitLoop();
				}
				// Always auto-insert semicon to follow SpiderMonkey:
				// It is required by EMAScript but is ignored by the rest of
				// world, see bug 238945
				MatchToken(Token.SEMI);
				decompiler.addEOL(Token.SEMI);
				return pn;
			  }

			  case Token.FOR: {
				ConsumeToken();
				bool isForEach = false;
				decompiler.addToken(Token.FOR);

				Node loop = enterLoop(statementLabel);
				try {

					Node init;  // Node init is also foo in 'foo in Object'
					Node cond;  // Node cond is also object in 'foo in Object'
					Node incr = null; // to kill warning
					Node body;

					// See if this is a for each () instead of just a for ()
					if (MatchToken(Token.NAME)) {
						decompiler.addName(ts.getstring());
						if (ts.getstring().equals("each")) {
							isForEach = true;
						} else {
							reportError("msg.no.paren.for");
						}
					}

					MustMatchToken(Token.LP, "msg.no.paren.for");
					decompiler.addToken(Token.LP);
					tt = PeekToken();
					if (tt == Token.SEMI) {
						init = nf.createLeaf(Token.EMPTY);
					} else {
						if (tt == Token.VAR) {
							// set init to a var list or initial
							ConsumeToken();    // consume the 'var' token
							init = variables(Token.FOR);
						}
						else {
							init = expr(true);
						}
					}

					if (MatchToken(Token.IN)) {
						decompiler.addToken(Token.IN);
						// 'cond' is the object over which we're iterating
						cond = expr(false);
					} else {  // ordinary for loop
						MustMatchToken(Token.SEMI, "msg.no.semi.for");
						decompiler.addToken(Token.SEMI);
						if (PeekToken() == Token.SEMI) {
							// no loop condition
							cond = nf.createLeaf(Token.EMPTY);
						} else {
							cond = expr(false);
						}

						MustMatchToken(Token.SEMI, "msg.no.semi.for.cond");
						decompiler.addToken(Token.SEMI);
						if (PeekToken() == Token.RP) {
							incr = nf.createLeaf(Token.EMPTY);
						} else {
							incr = expr(false);
						}
					}

					MustMatchToken(Token.RP, "msg.no.paren.for.ctrl");
					decompiler.addToken(Token.RP);
					decompiler.addEOL(Token.LC);
					body = statement();
					decompiler.addEOL(Token.RC);

					if (incr == null) {
						// cond could be null if 'in obj' got eaten
						// by the init node.
						pn = nf.createForIn(loop, init, cond, body, isForEach);
					} else {
						pn = nf.createFor(loop, init, cond, incr, body);
					}
				} finally {
					exitLoop();
				}
				return pn;
			  }

			  case Token.TRY: {
				ConsumeToken();
				int lineno = ts.getLineno();

				Node tryblock;
				Node catchblocks = null;
				Node finallyblock = null;

				decompiler.addToken(Token.TRY);
				decompiler.addEOL(Token.LC);
				tryblock = statement();
				decompiler.addEOL(Token.RC);

				catchblocks = nf.createLeaf(Token.BLOCK);

				bool sawDefaultCatch = false;
				int peek = PeekToken();
				if (peek == Token.CATCH) {
					while (MatchToken(Token.CATCH)) {
						if (sawDefaultCatch) {
							reportError("msg.catch.unreachable");
						}
						decompiler.addToken(Token.CATCH);
						MustMatchToken(Token.LP, "msg.no.paren.catch");
						decompiler.addToken(Token.LP);

						MustMatchToken(Token.NAME, "msg.bad.catchcond");
						string varName = ts.getstring();
						decompiler.addName(varName);

						Node catchCond = null;
						if (MatchToken(Token.IF)) {
							decompiler.addToken(Token.IF);
							catchCond = expr(false);
						} else {
							sawDefaultCatch = true;
						}

						MustMatchToken(Token.RP, "msg.bad.catchcond");
						decompiler.addToken(Token.RP);
						MustMatchToken(Token.LC, "msg.no.brace.catchblock");
						decompiler.addEOL(Token.LC);

						nf.addChildToBack(catchblocks,
							nf.createCatch(varName, catchCond,
										   statements(),
										   ts.getLineno()));

						MustMatchToken(Token.RC, "msg.no.brace.after.body");
						decompiler.addEOL(Token.RC);
					}
				} else if (peek != Token.FINALLY) {
					MustMatchToken(Token.FINALLY, "msg.try.no.catchfinally");
				}

				if (MatchToken(Token.FINALLY)) {
					decompiler.addToken(Token.FINALLY);
					decompiler.addEOL(Token.LC);
					finallyblock = statement();
					decompiler.addEOL(Token.RC);
				}

				pn = nf.createTryCatchFinally(tryblock, catchblocks,
											  finallyblock, lineno);

				return pn;
			  }

			  case Token.THROW: {
				ConsumeToken();
				if (PeekTokenOrEOL() == Token.EOL) {
					// ECMAScript does not allow new lines before throw expression,
					// see bug 256617
					reportError("msg.bad.throw.eol");
				}

				int lineno = ts.getLineno();
				decompiler.addToken(Token.THROW);
				pn = nf.createThrow(expr(false), lineno);
				break;
			  }

			  case Token.BREAK: {
				ConsumeToken();
				int lineno = ts.getLineno();

				decompiler.addToken(Token.BREAK);

				// matchJumpLabelName only matches if there is one
				Node breakStatement = matchJumpLabelName();
				if (breakStatement == null) {
					if (loopAndSwitchSet == null || loopAndSwitchSet.size() == 0) {
						reportError("msg.bad.break");
						return null;
					}
					breakStatement = (Node)loopAndSwitchSet.peek();
				}
				pn = nf.createBreak(breakStatement, lineno);
				break;
			  }

			  case Token.CONTINUE: {
				ConsumeToken();
				int lineno = ts.getLineno();

				decompiler.addToken(Token.CONTINUE);

				Node loop;
				// matchJumpLabelName only matches if there is one
				Node label = matchJumpLabelName();
				if (label == null) {
					if (loopSet == null || loopSet.size() == 0) {
						reportError("msg.continue.outside");
						return null;
					}
					loop = (Node)loopSet.peek();
				} else {
					loop = nf.getLabelLoop(label);
					if (loop == null) {
						reportError("msg.continue.nonloop");
						return null;
					}
				}
				pn = nf.createContinue(loop, lineno);
				break;
			  }

			  case Token.WITH: {
				ConsumeToken();

				decompiler.addToken(Token.WITH);
				int lineno = ts.getLineno();
				MustMatchToken(Token.LP, "msg.no.paren.with");
				decompiler.addToken(Token.LP);
				Node obj = expr(false);
				MustMatchToken(Token.RP, "msg.no.paren.after.with");
				decompiler.addToken(Token.RP);
				decompiler.addEOL(Token.LC);

				++nestingOfWith;
				Node body;
				try {
					body = statement();
				} finally {
					--nestingOfWith;
				}

				decompiler.addEOL(Token.RC);

				pn = nf.createWith(obj, body, lineno);
				return pn;
			  }

			  case Token.CONST:
			  case Token.VAR: {
				ConsumeToken();
				pn = variables(tt);
				break;
			  }

			  case Token.RETURN: {
				if (!insideFunction()) {
					reportError("msg.bad.return");
				}
				ConsumeToken();
				decompiler.addToken(Token.RETURN);
				int lineno = ts.getLineno();

				Node retExpr;
				/* This is ugly, but we don't want to require a semicolon. */
				tt = PeekTokenOrEOL();
				switch (tt) {
				  case Token.SEMI:
				  case Token.RC:
				  case Token.EOF:
				  case Token.EOL:
				  case Token.ERROR:
					retExpr = null;
					break;
				  default:
					retExpr = expr(false);
					hasReturnValue = true;
				}
				pn = nf.createReturn(retExpr, lineno);

				// see if we need a strict mode warning
				if (retExpr == null) {
					if (functionEndFlags == Node.END_RETURNS_VALUE)
						addStrictWarning("msg.return.inconsistent", "");

					functionEndFlags |= Node.END_RETURNS;
				} else {
					if (functionEndFlags == Node.END_RETURNS)
						addStrictWarning("msg.return.inconsistent", "");

					functionEndFlags |= Node.END_RETURNS_VALUE;
				}

				break;
			  }

			  case Token.LC:
				ConsumeToken();
				if (statementLabel != null) {
					decompiler.addToken(Token.LC);
				}
				pn = statements();
				MustMatchToken(Token.RC, "msg.no.brace.block");
				if (statementLabel != null) {
					decompiler.addEOL(Token.RC);
				}
				return pn;

			  case Token.ERROR:
				// Fall thru, to have a node for error recovery to work on
			  case Token.SEMI:
				ConsumeToken();
				pn = nf.createLeaf(Token.EMPTY);
				return pn;

			  case Token.FUNCTION: {
				ConsumeToken();
				pn = function(FunctionNode.FUNCTION_EXPRESSION_STATEMENT);
				return pn;
			  }

			  case Token.DEFAULT :
				ConsumeToken();
				mustHaveXML();

				decompiler.addToken(Token.DEFAULT);
				int nsLine = ts.getLineno();

				if (!(MatchToken(Token.NAME)
					  && ts.getstring().equals("xml")))
				{
					reportError("msg.bad.namespace");
				}
				decompiler.addName(" xml");

				if (!(MatchToken(Token.NAME)
					  && ts.getstring().equals("namespace")))
				{
					reportError("msg.bad.namespace");
				}
				decompiler.addName(" namespace");

				if (!MatchToken(Token.ASSIGN)) {
					reportError("msg.bad.namespace");
				}
				decompiler.addToken(Token.ASSIGN);

				Node expr = expr(false);
				pn = nf.createDefaultNamespace(expr, nsLine);
				break;

			  case Token.NAME: {
				int lineno = ts.getLineno();
				string name = ts.getstring();
				SetCheckForLabel();
				pn = expr(false);
				if (pn.getType() != Token.LABEL) {
					pn = nf.createExprStatement(pn, lineno);
				} else {
					// Parsed the label: push back token should be
					// colon that primaryExpr left untouched.
					if (PeekToken() != Token.COLON) Kit.codeBug();
					ConsumeToken();
					// depend on decompiling lookahead to guess that that
					// last name was a label.
					decompiler.addName(name);
					decompiler.addEOL(Token.COLON);

					if (labelSet == null) {
						labelSet = new Hashtable();
					} else if (labelSet.containsKey(name)) {
						reportError("msg.dup.label");
					}

					bool firstLabel;
					if (statementLabel == null) {
						firstLabel = true;
						statementLabel = pn;
					} else {
						// Discard multiple label nodes and use only
						// the first: it allows to simplify IRFactory
						firstLabel = false;
					}
					labelSet.put(name, statementLabel);
					try {
						pn = statementHelper(statementLabel);
					} finally {
						labelSet.remove(name);
					}
					if (firstLabel) {
						pn = nf.createLabeledStatement(statementLabel, pn);
					}
					return pn;
				}
				break;
			  }

			  default: {
				int lineno = ts.getLineno();
				pn = expr(false);
				pn = nf.createExprStatement(pn, lineno);
				break;
			  }
			}

			int ttFlagged = peekFlaggedToken();
			switch (ttFlagged & CLEAR_TI_MASK) {
			  case Token.SEMI:
				// Consume ';' as a part of expression
				ConsumeToken();
				break;
			  case Token.ERROR:
			  case Token.EOF:
			  case Token.RC:
				// Autoinsert ;
				break;
			  default:
				if ((ttFlagged & TI_AFTER_EOL) == 0) {
					// Report error if no EOL or autoinsert ; otherwise
					reportError("msg.no.semi.stmt");
				}
				break;
			}
			decompiler.addEOL(Token.SEMI);

			return pn;
		}

		/**
		 * Parse a 'var' or 'const' statement, or a 'var' init list in a for
		 * statement.
		 * @param context A token value: either VAR, CONST or FOR depending on
		 * context.
		 * @return The parsed statement
		 * @throws IOException
		 * @throws ParserException
		 */
		private Node variables(int context)
			throws IOException, ParserException
		{
			Node pn;
			bool first = true;

			if (context == Token.CONST){
				pn = nf.createVariables(Token.CONST, ts.getLineno());
				decompiler.addToken(Token.CONST);
			} else {
				pn = nf.createVariables(Token.VAR, ts.getLineno());
				decompiler.addToken(Token.VAR);
			}

			for (;;) {
				Node name;
				Node init;
				MustMatchToken(Token.NAME, "msg.bad.var");
				string s = ts.getstring();

				if (!first)
					decompiler.addToken(Token.COMMA);
				first = false;

				decompiler.addName(s);

				if (context == Token.CONST) {
					if (!currentScriptOrFn.addConst(s)) {
						// We know it's already defined, since addConst passes if
						// it's not defined at all.  The addVar call just confirms
						// what it is.
						if (currentScriptOrFn.addVar(s) != ScriptOrFnNode.DUPLICATE_CONST)
							addError("msg.var.redecl", s);
						else
							addError("msg.const.redecl", s);
					}
				} else {
					int dupState = currentScriptOrFn.addVar(s);
					if (dupState == ScriptOrFnNode.DUPLICATE_CONST)
						addError("msg.const.redecl", s);
					else if (dupState == ScriptOrFnNode.DUPLICATE_PARAMETER)
						addStrictWarning("msg.var.hides.arg", s);
					else if (dupState == ScriptOrFnNode.DUPLICATE_VAR)
						addStrictWarning("msg.var.redecl", s);
				}
				name = nf.createName(s);

				// omitted check for argument hiding

				if (MatchToken(Token.ASSIGN)) {
					decompiler.addToken(Token.ASSIGN);

					init = assignExpr(context == Token.FOR);
					nf.addChildToBack(name, init);
				}
				nf.addChildToBack(pn, name);
				if (!MatchToken(Token.COMMA))
					break;
			}
			return pn;
		}

		private Node expr(bool inForInit)
			throws IOException, ParserException
		{
			Node pn = assignExpr(inForInit);
			while (MatchToken(Token.COMMA)) {
				decompiler.addToken(Token.COMMA);
				if (compilerEnv.isStrictMode() && !pn.hasSideEffects())
					addStrictWarning("msg.no.side.effects", "");
				pn = nf.createBinary(Token.COMMA, pn, assignExpr(inForInit));
			}
			return pn;
		}

		private Node assignExpr(bool inForInit)
			throws IOException, ParserException
		{
			Node pn = condExpr(inForInit);

			int tt = PeekToken();
			if (Token.FIRST_ASSIGN <= tt && tt <= Token.LAST_ASSIGN) {
				ConsumeToken();
				decompiler.addToken(tt);
				pn = nf.createAssignment(tt, pn, assignExpr(inForInit));
			}

			return pn;
		}

		private Node condExpr(bool inForInit)
			throws IOException, ParserException
		{
			Node pn = orExpr(inForInit);

			if (MatchToken(Token.HOOK)) {
				decompiler.addToken(Token.HOOK);
				Node ifTrue = assignExpr(false);
				MustMatchToken(Token.COLON, "msg.no.colon.cond");
				decompiler.addToken(Token.COLON);
				Node ifFalse = assignExpr(inForInit);
				return nf.createCondExpr(pn, ifTrue, ifFalse);
			}

			return pn;
		}

		private Node orExpr(bool inForInit)
			throws IOException, ParserException
		{
			Node pn = andExpr(inForInit);
			if (MatchToken(Token.OR)) {
				decompiler.addToken(Token.OR);
				pn = nf.createBinary(Token.OR, pn, orExpr(inForInit));
			}

			return pn;
		}

		private Node andExpr(bool inForInit)
			throws IOException, ParserException
		{
			Node pn = bitOrExpr(inForInit);
			if (MatchToken(Token.AND)) {
				decompiler.addToken(Token.AND);
				pn = nf.createBinary(Token.AND, pn, andExpr(inForInit));
			}

			return pn;
		}

		private Node bitOrExpr(bool inForInit)
			throws IOException, ParserException
		{
			Node pn = bitXorExpr(inForInit);
			while (MatchToken(Token.BITOR)) {
				decompiler.addToken(Token.BITOR);
				pn = nf.createBinary(Token.BITOR, pn, bitXorExpr(inForInit));
			}
			return pn;
		}

		private Node bitXorExpr(bool inForInit)
			throws IOException, ParserException
		{
			Node pn = bitAndExpr(inForInit);
			while (MatchToken(Token.BITXOR)) {
				decompiler.addToken(Token.BITXOR);
				pn = nf.createBinary(Token.BITXOR, pn, bitAndExpr(inForInit));
			}
			return pn;
		}

		private Node bitAndExpr(bool inForInit)
			throws IOException, ParserException
		{
			Node pn = eqExpr(inForInit);
			while (MatchToken(Token.BITAND)) {
				decompiler.addToken(Token.BITAND);
				pn = nf.createBinary(Token.BITAND, pn, eqExpr(inForInit));
			}
			return pn;
		}

		private Node eqExpr(bool inForInit)
			throws IOException, ParserException
		{
			Node pn = relExpr(inForInit);
			for (;;) {
				int tt = PeekToken();
				switch (tt) {
				  case Token.EQ:
				  case Token.NE:
				  case Token.SHEQ:
				  case Token.SHNE:
					ConsumeToken();
					int decompilerToken = tt;
					int parseToken = tt;
					if (compilerEnv.getLanguageVersion() == Context.VERSION_1_2) {
						// JavaScript 1.2 uses shallow equality for == and != .
						// In addition, convert === and !== for decompiler into
						// == and != since the decompiler is supposed to show
						// canonical source and in 1.2 ===, !== are allowed
						// only as an alias to ==, !=.
						switch (tt) {
						  case Token.EQ:
							parseToken = Token.SHEQ;
							break;
						  case Token.NE:
							parseToken = Token.SHNE;
							break;
						  case Token.SHEQ:
							decompilerToken = Token.EQ;
							break;
						  case Token.SHNE:
							decompilerToken = Token.NE;
							break;
						}
					}
					decompiler.addToken(decompilerToken);
					pn = nf.createBinary(parseToken, pn, relExpr(inForInit));
					continue;
				}
				break;
			}
			return pn;
		}

		private Node relExpr(bool inForInit)
			throws IOException, ParserException
		{
			Node pn = shiftExpr();
			for (;;) {
				int tt = PeekToken();
				switch (tt) {
				  case Token.IN:
					if (inForInit)
						break;
					// fall through
				  case Token.INSTANCEOF:
				  case Token.LE:
				  case Token.LT:
				  case Token.GE:
				  case Token.GT:
					ConsumeToken();
					decompiler.addToken(tt);
					pn = nf.createBinary(tt, pn, shiftExpr());
					continue;
				}
				break;
			}
			return pn;
		}

		private Node shiftExpr()
			throws IOException, ParserException
		{
			Node pn = addExpr();
			for (;;) {
				int tt = PeekToken();
				switch (tt) {
				  case Token.LSH:
				  case Token.URSH:
				  case Token.RSH:
					ConsumeToken();
					decompiler.addToken(tt);
					pn = nf.createBinary(tt, pn, addExpr());
					continue;
				}
				break;
			}
			return pn;
		}

		private Node addExpr()
			throws IOException, ParserException
		{
			Node pn = mulExpr();
			for (;;) {
				int tt = PeekToken();
				if (tt == Token.ADD || tt == Token.SUB) {
					ConsumeToken();
					decompiler.addToken(tt);
					// flushNewLines
					pn = nf.createBinary(tt, pn, mulExpr());
					continue;
				}
				break;
			}

			return pn;
		}

		private Node mulExpr()
			throws IOException, ParserException
		{
			Node pn = unaryExpr();
			for (;;) {
				int tt = PeekToken();
				switch (tt) {
				  case Token.MUL:
				  case Token.DIV:
				  case Token.MOD:
					ConsumeToken();
					decompiler.addToken(tt);
					pn = nf.createBinary(tt, pn, unaryExpr());
					continue;
				}
				break;
			}

			return pn;
		}

		private Node unaryExpr()
			throws IOException, ParserException
		{
			int tt;

			tt = PeekToken();

			switch(tt) {
			case Token.VOID:
			case Token.NOT:
			case Token.BITNOT:
			case Token.TYPEOF:
				ConsumeToken();
				decompiler.addToken(tt);
				return nf.createUnary(tt, unaryExpr());

			case Token.ADD:
				ConsumeToken();
				// Convert to special POS token in decompiler and parse tree
				decompiler.addToken(Token.POS);
				return nf.createUnary(Token.POS, unaryExpr());

			case Token.SUB:
				ConsumeToken();
				// Convert to special NEG token in decompiler and parse tree
				decompiler.addToken(Token.NEG);
				return nf.createUnary(Token.NEG, unaryExpr());

			case Token.INC:
			case Token.DEC:
				ConsumeToken();
				decompiler.addToken(tt);
				return nf.createIncDec(tt, false, memberExpr(true));

			case Token.DELPROP:
				ConsumeToken();
				decompiler.addToken(Token.DELPROP);
				return nf.createUnary(Token.DELPROP, unaryExpr());

			case Token.ERROR:
				ConsumeToken();
				break;

			// XML stream encountered in expression.
			case Token.LT:
				if (compilerEnv.isXmlAvailable()) {
					ConsumeToken();
					Node pn = xmlInitializer();
					return memberExprTail(true, pn);
				}
				// Fall thru to the default handling of RELOP

			default:
				Node pn = memberExpr(true);

				// Don't look across a newline boundary for a postfix incop.
				tt = PeekTokenOrEOL();
				if (tt == Token.INC || tt == Token.DEC) {
					ConsumeToken();
					decompiler.addToken(tt);
					return nf.createIncDec(tt, true, pn);
				}
				return pn;
			}
			return nf.createName("err"); // Only reached on error.  Try to continue.

		}

		private Node xmlInitializer() throws IOException
		{
			int tt = ts.getFirstXMLToken();
			if (tt != Token.XML && tt != Token.XMLEND) {
				reportError("msg.syntax");
				return null;
			}

			/* Make a NEW node to append to. */
			Node pnXML = nf.createLeaf(Token.NEW);

			string xml = ts.getstring();
			bool fAnonymous = xml.trim().startsWith("<>");

			Node pn = nf.createName(fAnonymous ? "XMLList" : "XML");
			nf.addChildToBack(pnXML, pn);

			pn = null;
			Node expr;
			for (;;tt = ts.getNextXMLToken()) {
				switch (tt) {
				case Token.XML:
					xml = ts.getstring();
					decompiler.addName(xml);
					MustMatchToken(Token.LC, "msg.syntax");
					decompiler.addToken(Token.LC);
					expr = (PeekToken() == Token.RC)
						? nf.createstring("")
						: expr(false);
					MustMatchToken(Token.RC, "msg.syntax");
					decompiler.addToken(Token.RC);
					if (pn == null) {
						pn = nf.createstring(xml);
					} else {
						pn = nf.createBinary(Token.ADD, pn, nf.createstring(xml));
					}
					if (ts.isXMLAttribute()) {
						/* Need to put the result in double quotes */
						expr = nf.createUnary(Token.ESCXMLATTR, expr);
						Node prepend = nf.createBinary(Token.ADD,
													   nf.createstring("\""),
													   expr);
						expr = nf.createBinary(Token.ADD,
											   prepend,
											   nf.createstring("\""));
					} else {
						expr = nf.createUnary(Token.ESCXMLTEXT, expr);
					}
					pn = nf.createBinary(Token.ADD, pn, expr);
					break;
				case Token.XMLEND:
					xml = ts.getstring();
					decompiler.addName(xml);
					if (pn == null) {
						pn = nf.createstring(xml);
					} else {
						pn = nf.createBinary(Token.ADD, pn, nf.createstring(xml));
					}

					nf.addChildToBack(pnXML, pn);
					return pnXML;
				default:
					reportError("msg.syntax");
					return null;
				}
			}
		}

		private void argumentList(Node listNode)
			throws IOException, ParserException
		{
			bool matched;
			matched = MatchToken(Token.RP);
			if (!matched) {
				bool first = true;
				do {
					if (!first)
						decompiler.addToken(Token.COMMA);
					first = false;
					nf.addChildToBack(listNode, assignExpr(false));
				} while (MatchToken(Token.COMMA));

				MustMatchToken(Token.RP, "msg.no.paren.arg");
			}
			decompiler.addToken(Token.RP);
		}

		private Node memberExpr(bool allowCallSyntax)
			throws IOException, ParserException
		{
			int tt;

			Node pn;

			/* Check for new expressions. */
			tt = PeekToken();
			if (tt == Token.NEW) {
				/* Eat the NEW token. */
				ConsumeToken();
				decompiler.addToken(Token.NEW);

				/* Make a NEW node to append to. */
				pn = nf.createCallOrNew(Token.NEW, memberExpr(false));

				if (MatchToken(Token.LP)) {
					decompiler.addToken(Token.LP);
					/* Add the arguments to pn, if any are supplied. */
					argumentList(pn);
				}

				/* XXX there's a check in the C source against
				 * "too many constructor arguments" - how many
				 * do we claim to support?
				 */

				/* Experimental syntax:  allow an object literal to follow a new expression,
				 * which will mean a kind of anonymous class built with the JavaAdapter.
				 * the object literal will be passed as an additional argument to the constructor.
				 */
				tt = PeekToken();
				if (tt == Token.LC) {
					nf.addChildToBack(pn, primaryExpr());
				}
			} else {
				pn = primaryExpr();
			}

			return memberExprTail(allowCallSyntax, pn);
		}

		private Node memberExprTail(bool allowCallSyntax, Node pn)
			throws IOException, ParserException
		{
		  tailLoop:
			for (;;) {
				int tt = PeekToken();
				switch (tt) {

				  case Token.DOT:
				  case Token.DOTDOT:
					{
						int memberTypeFlags;
						string s;

						ConsumeToken();
						decompiler.addToken(tt);
						memberTypeFlags = 0;
						if (tt == Token.DOTDOT) {
							mustHaveXML();
							memberTypeFlags = Node.DESCENDANTS_FLAG;
						}
						if (!compilerEnv.isXmlAvailable()) {
							MustMatchToken(Token.NAME, "msg.no.name.after.dot");
							s = ts.getstring();
							decompiler.addName(s);
							pn = nf.createPropertyGet(pn, null, s, memberTypeFlags);
							break;
						}

						tt = nextToken();
						switch (tt) {
						  // handles: name, ns::name, ns::*, ns::[expr]
						  case Token.NAME:
							s = ts.getstring();
							decompiler.addName(s);
							pn = propertyName(pn, s, memberTypeFlags);
							break;

						  // handles: *, *::name, *::*, *::[expr]
						  case Token.MUL:
							decompiler.addName("*");
							pn = propertyName(pn, "*", memberTypeFlags);
							break;

						  // handles: '@attr', '@ns::attr', '@ns::*', '@ns::*',
						  //          '@::attr', '@::*', '@*', '@*::attr', '@*::*'
						  case Token.XMLATTR:
							decompiler.addToken(Token.XMLATTR);
							pn = attributeAccess(pn, memberTypeFlags);
							break;

						  default:
							reportError("msg.no.name.after.dot");
						}
					}
					break;

				  case Token.DOTQUERY:
					ConsumeToken();
					mustHaveXML();
					decompiler.addToken(Token.DOTQUERY);
					pn = nf.createDotQuery(pn, expr(false), ts.getLineno());
					MustMatchToken(Token.RP, "msg.no.paren");
					decompiler.addToken(Token.RP);
					break;

				  case Token.LB:
					ConsumeToken();
					decompiler.addToken(Token.LB);
					pn = nf.createElementGet(pn, null, expr(false), 0);
					MustMatchToken(Token.RB, "msg.no.bracket.index");
					decompiler.addToken(Token.RB);
					break;

				  case Token.LP:
					if (!allowCallSyntax) {
						break tailLoop;
					}
					ConsumeToken();
					decompiler.addToken(Token.LP);
					pn = nf.createCallOrNew(Token.CALL, pn);
					/* Add the arguments to pn, if any are supplied. */
					argumentList(pn);
					break;

				  default:
					break tailLoop;
				}
			}
			return pn;
		}

		/*
		 * Xml attribute expression:
		 *   '@attr', '@ns::attr', '@ns::*', '@ns::*', '@*', '@*::attr', '@*::*'
		 */
		private Node attributeAccess(Node pn, int memberTypeFlags)
			throws IOException
		{
			memberTypeFlags |= Node.ATTRIBUTE_FLAG;
			int tt = nextToken();

			switch (tt) {
			  // handles: @name, @ns::name, @ns::*, @ns::[expr]
			  case Token.NAME:
				{
					string s = ts.getstring();
					decompiler.addName(s);
					pn = propertyName(pn, s, memberTypeFlags);
				}
				break;

			  // handles: @*, @*::name, @*::*, @*::[expr]
			  case Token.MUL:
				decompiler.addName("*");
				pn = propertyName(pn, "*", memberTypeFlags);
				break;

			  // handles @[expr]
			  case Token.LB:
				decompiler.addToken(Token.LB);
				pn = nf.createElementGet(pn, null, expr(false), memberTypeFlags);
				MustMatchToken(Token.RB, "msg.no.bracket.index");
				decompiler.addToken(Token.RB);
				break;

			  default:
				reportError("msg.no.name.after.xmlAttr");
				pn = nf.createPropertyGet(pn, null, "?", memberTypeFlags);
				break;
			}

			return pn;
		}

		/**
		 * Check if :: follows name in which case it becomes qualified name
		 */
		private Node propertyName(Node pn, string name, int memberTypeFlags)
			throws IOException, ParserException
		{
			string namespace = null;
			if (MatchToken(Token.COLONCOLON)) {
				decompiler.addToken(Token.COLONCOLON);
				namespace = name;

				int tt = nextToken();
				switch (tt) {
				  // handles name::name
				  case Token.NAME:
					name = ts.getstring();
					decompiler.addName(name);
					break;

				  // handles name::*
				  case Token.MUL:
					decompiler.addName("*");
					name = "*";
					break;

				  // handles name::[expr]
				  case Token.LB:
					decompiler.addToken(Token.LB);
					pn = nf.createElementGet(pn, namespace, expr(false),
											 memberTypeFlags);
					MustMatchToken(Token.RB, "msg.no.bracket.index");
					decompiler.addToken(Token.RB);
					return pn;

				  default:
					reportError("msg.no.name.after.coloncolon");
					name = "?";
				}
			}

			pn = nf.createPropertyGet(pn, namespace, name, memberTypeFlags);
			return pn;
		}

		private Node primaryExpr()
			throws IOException, ParserException
		{
			Node pn;

			int ttFlagged = nextFlaggedToken();
			int tt = ttFlagged & CLEAR_TI_MASK;

			switch(tt) {

			  case Token.FUNCTION:
				return function(FunctionNode.FUNCTION_EXPRESSION);

			  case Token.LB: {
				ObjArray elems = new ObjArray();
				int skipCount = 0;
				decompiler.addToken(Token.LB);
				bool after_lb_or_comma = true;
				for (;;) {
					tt = PeekToken();

					if (tt == Token.COMMA) {
						ConsumeToken();
						decompiler.addToken(Token.COMMA);
						if (!after_lb_or_comma) {
							after_lb_or_comma = true;
						} else {
							elems.add(null);
							++skipCount;
						}
					} else if (tt == Token.RB) {
						ConsumeToken();
						decompiler.addToken(Token.RB);
						break;
					} else {
						if (!after_lb_or_comma) {
							reportError("msg.no.bracket.arg");
						}
						elems.add(assignExpr(false));
						after_lb_or_comma = false;
					}
				}
				return nf.createArrayLiteral(elems, skipCount);
			  }

			  case Token.LC: {
				ObjArray elems = new ObjArray();
				decompiler.addToken(Token.LC);
				if (!MatchToken(Token.RC)) {

					bool first = true;
				commaloop:
					do {
						Object property;

						if (!first)
							decompiler.addToken(Token.COMMA);
						else
							first = false;

						tt = PeekToken();
						switch(tt) {
						  case Token.NAME:
						  case Token.STRING:
							ConsumeToken();
							// map NAMEs to STRINGs in object literal context
							// but tell the decompiler the proper type
							string s = ts.getstring();
							if (tt == Token.NAME) {
								if (s.equals("get") &&
									PeekToken() == Token.NAME) {
									decompiler.addToken(Token.GET);
									ConsumeToken();
									s = ts.getstring();
									decompiler.addName(s);
									property = ScriptRuntime.getIndexObject(s);
									if (!getterSetterProperty(elems, property,
															  true))
										break commaloop;
									break;
								} else if (s.equals("set") &&
										   PeekToken() == Token.NAME) {
									decompiler.addToken(Token.SET);
									ConsumeToken();
									s = ts.getstring();
									decompiler.addName(s);
									property = ScriptRuntime.getIndexObject(s);
									if (!getterSetterProperty(elems, property,
															  false))
										break commaloop;
									break;
								}
								decompiler.addName(s);
							} else {
								decompiler.addstring(s);
							}
							property = ScriptRuntime.getIndexObject(s);
							plainProperty(elems, property);
							break;

						  case Token.NUMBER:
							ConsumeToken();
							double n = ts.getNumber();
							decompiler.addNumber(n);
							property = ScriptRuntime.getIndexObject(n);
							plainProperty(elems, property);
							break;

						  case Token.RC:
							// trailing comma is OK.
							break commaloop;
						default:
							reportError("msg.bad.prop");
							break commaloop;
						}
					} while (MatchToken(Token.COMMA));

					MustMatchToken(Token.RC, "msg.no.brace.prop");
				}
				decompiler.addToken(Token.RC);
				return nf.createObjectLiteral(elems);
			  }

			  case Token.LP:

				/* Brendan's IR-jsparse.c makes a new node tagged with
				 * TOK_LP here... I'm not sure I understand why.  Isn't
				 * the grouping already implicit in the structure of the
				 * parse tree?  also TOK_LP is already overloaded (I
				 * think) in the C IR as 'function call.'  */
				decompiler.addToken(Token.LP);
				pn = expr(false);
				pn.putProp(Node.PARENTHESIZED_PROP, Boolean.TRUE);
				decompiler.addToken(Token.RP);
				MustMatchToken(Token.RP, "msg.no.paren");
				return pn;

			  case Token.XMLATTR:
				mustHaveXML();
				decompiler.addToken(Token.XMLATTR);
				pn = attributeAccess(null, 0);
				return pn;

			  case Token.NAME: {
				string name = ts.getstring();
				if ((ttFlagged & TI_CHECK_LABEL) != 0) {
					if (PeekToken() == Token.COLON) {
						// Do not consume colon, it is used as unwind indicator
						// to return to statementHelper.
						// XXX Better way?
						return nf.createLabel(ts.getLineno());
					}
				}

				decompiler.addName(name);
				if (compilerEnv.isXmlAvailable()) {
					pn = propertyName(null, name, 0);
				} else {
					pn = nf.createName(name);
				}
				return pn;
			  }

			  case Token.NUMBER: {
				double n = ts.getNumber();
				decompiler.addNumber(n);
				return nf.createNumber(n);
			  }

			  case Token.STRING: {
				string s = ts.getstring();
				decompiler.addstring(s);
				return nf.createstring(s);
			  }

			  case Token.DIV:
			  case Token.ASSIGN_DIV: {
				// Got / or /= which should be treated as regexp in fact
				ts.readRegExp(tt);
				string flags = ts.regExpFlags;
				ts.regExpFlags = null;
				string re = ts.getstring();
				decompiler.addRegexp(re, flags);
				int index = currentScriptOrFn.addRegexp(re, flags);
				return nf.createRegExp(index);
			  }

			  case Token.NULL:
			  case Token.THIS:
			  case Token.FALSE:
			  case Token.TRUE:
				decompiler.addToken(tt);
				return nf.createLeaf(tt);

			  case Token.RESERVED:
				reportError("msg.reserved.id");
				break;

			  case Token.ERROR:
				/* the scanner or one of its subroutines reported the error. */
				break;

			  case Token.EOF:
				reportError("msg.unexpected.eof");
				break;

			  default:
				reportError("msg.syntax");
				break;
			}
			return null;    // should never reach here
		}

		private void plainProperty(ObjArray elems, Object property)
				throws IOException {
			MustMatchToken(Token.COLON, "msg.no.colon.prop");

			// OBJLIT is used as ':' in object literal for
			// decompilation to solve spacing ambiguity.
			decompiler.addToken(Token.OBJECTLIT);
			elems.add(property);
			elems.add(assignExpr(false));
		}

		private bool getterSetterProperty(ObjArray elems, Object property,
											 bool isGetter) throws IOException {
			Node f = function(FunctionNode.FUNCTION_EXPRESSION);
			if (f.getType() != Token.FUNCTION) {
				reportError("msg.bad.prop");
				return false;
			}
			int fnIndex = f.getExistingIntProp(Node.FUNCTION_PROP);
			FunctionNode fn = currentScriptOrFn.getFunctionNode(fnIndex);
			if (fn.getFunctionName().length() != 0) {
				reportError("msg.bad.prop");
				return false;
			}
			elems.add(property);
			if (isGetter) {
				elems.add(nf.createUnary(Token.GET, f));
			} else {
				elems.add(nf.createUnary(Token.SET, f));
			}
			return true;
		}
	}
}
