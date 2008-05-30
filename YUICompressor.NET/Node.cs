using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Mozilla.JavaScript
{
	class Node
	{
		public const int FUNCTION_PROP      =  1;
		public const int LOCAL_PROP         =  2;
		public const int LOCAL_BLOCK_PROP   =  3;
		public const int REGEXP_PROP        =  4;
		public const int CASEARRAY_PROP     =  5;
		/*
			the following properties are defined and manipulated by the
			optimizer -
			TARGETBLOCK_PROP - the block referenced by a branch node
			VARIABLE_PROP - the variable referenced by a BIND or NAME node
			ISNUMBER_PROP - this node generates code on Number children and
							delivers a Number result (as opposed to Objects)
			DIRECTCALL_PROP - this call node should emit code to test the function
							  object against the known class and call diret if it
							  matches.
		*/

		public const int TARGETBLOCK_PROP   =  6;
		public const int VARIABLE_PROP      =  7;
		public const int ISNUMBER_PROP      =  8;
		public const int DIRECTCALL_PROP    =  9;
		public const int SPECIALCALL_PROP   = 10;
		public const int SKIP_INDEXES_PROP  = 11; // array of skipped indexes of array literal
		public const int OBJECT_IDS_PROP    = 12; // array of properties for object literal
		public const int INCRDECR_PROP      = 13; // pre or post type of increment/decerement
		public const int CATCH_SCOPE_PROP   = 14; // index of catch scope block in catch
		public const int LABEL_ID_PROP      = 15; // label id: code generation uses it
		public const int MEMBER_TYPE_PROP   = 16; // type of element access operation
		public const int NAME_PROP          = 17; // property name
		public const int CONTROL_BLOCK_PROP = 18; // flags a control block that can drop off
		public const int PARENTHESIZED_PROP = 19; // expression is parenthesized
		public const int GENERATOR_END_PROP = 20;
		public const int DESTRUCTURING_ARRAY_LENGTH = 21;
		public const int DESTRUCTURING_NAMES= 22;
		public const int LAST_PROP          = 22;

		// values of ISNUMBER_PROP to specify
		// which of the children are Number types
		public const int BOTH = 0;
		public const int LEFT = 1;
		public const int RIGHT = 2;

		// values for SPECIALCALL_PROP
		public const int NON_SPECIALCALL  = 0;
		public const int SPECIALCALL_EVAL = 1;
		public const int SPECIALCALL_WITH = 2;

		// flags for INCRDECR_PROP
		public const int DECR_FLAG = 0x1;
		public const int POST_FLAG = 0x2;

		// flags for MEMBER_TYPE_PROP
		public const int PROPERTY_FLAG    = 0x1; // property access: element is valid name
		public const int ATTRIBUTE_FLAG   = 0x2; // x.@y or x..@y
		public const int DESCENDANTS_FLAG = 0x4; // x..y or x..@i

		
		
		public int Type { get; set; } // type of the node; Token.NAME for example
		public Node Next { get; set; } // next sibling
		private Node first;    // first element of a linked list of children
		private Node last;     // last element of a linked list of children
		protected int lineno = -1;

		/**
		 * Linked list of properties. Since vast majority of nodes would have
		 * no more then 2 properties, linked list saves memory and provides
		 * fast lookup. If this does not holds, propListHead can be replaced
		 * by UintMap.
		 */
		private PropListItem propListHead;

		private static class NumberNode : Node
		{
			public double Number { get; set; }
			NumberNode(double number) : base(Token.NUMBER)
			{
				Number = number;
			}
		}

		private static class StringNode : Node
		{
			public string Str { get; set; }
			public Node.Scope Scope { get; set; }
			StringNode(int type, string str) : base(type)
			{
				Str = str;
			}
		}

		public static class Jump : Node
		{
			public Node Target { get; set; }
			private Node target2;
			private Jump jumpNode;

			public Jump(int type) : base(type){}
			Jump(int type, int lineno) : base(type, lineno){}
			Jump(int type, Node child) : base(type, child){}
			Jump(int type, Node child, int lineno) : base(type, child, lineno){}
			
			public Jump GetJumpStatement()
			{
				if (!(Type == Token.BREAK || Type == Token.CONTINUE)) Kit.CodeBug();
				return jumpNode;
			}

			public void SetJumpStatement(Jump jumpStatement)
			{
				if (!(Type == Token.BREAK || Type == Token.CONTINUE)) Kit.CodeBug();
				if (jumpStatement == null) Kit.CodeBug();
				if (jumpNode != null) Kit.CodeBug(); //only once
				jumpNode = jumpStatement;
			}

			public Node GetDefault()
			{
				if (!(Type == Token.SWITCH)) Kit.CodeBug();
				return target2;
			}

			public void SetDefault(Node defaultTarget)
			{
				if (!(Type == Token.SWITCH)) Kit.CodeBug();
				if (defaultTarget.Type != Token.TARGET) Kit.CodeBug();
				if (target2 != null) Kit.CodeBug(); //only once
				target2 = defaultTarget;
			}

			public Node GetFinally()
			{
				if (!(Type == Token.TRY)) Kit.CodeBug();
				return target2;
			}

			public void SetFinally(Node finallyTarget)
			{
				if (!(Type == Token.TRY)) Kit.CodeBug();
				if (finallyTarget.Type != Token.TARGET) Kit.CodeBug();
				if (target2 != null) Kit.CodeBug(); //only once
				target2 = finallyTarget;
			}

			public Jump GetLoop()
			{
				if (!(Type == Token.LABEL)) Kit.CodeBug();
				return jumpNode;
			}

			public void SetLoop(Jump loop)
			{
				if (!(Type == Token.LABEL)) Kit.CodeBug();
				if (loop == null) Kit.CodeBug();
				if (jumpNode != null) Kit.CodeBug(); //only once
				jumpNode = loop;
			}

			public Node GetContinue()
			{
				if (Type != Token.LOOP) Kit.CodeBug();
				return target2;
			}

			public void SetContinue(Node continueTarget)
			{
				if (Type != Token.LOOP) Kit.CodeBug();
				if (continueTarget.Type != Token.TARGET) Kit.CodeBug();
				if (target2 != null) Kit.CodeBug(); //only once
				target2 = continueTarget;
			}
		}
	    
		public static class Symbol
		{
			/**
			 * One of Token.FUNCTION, Token.LP (for parameters), Token.VAR, 
			 * Token.LET, or Token.CONST
			 */
			public int DeclType { get; set; }
			public int Index { get; set; }
			public string Name { get; set; }
			public Node.Scope ContainingTable { get; set; }

			Symbol(int declType, string name)
			{
				DeclType = declType;
				Name = name;
				Index = -1;
			}			
		}
	    
		public static class Scope : Jump
		{
			public Scope(int nodeType) : base(nodeType){}
			public Scope(int nodeType, int lineno) : base(nodeType, lineno){}
			public Scope(int nodeType, Node n, int lineno) : base(nodeType, n, lineno){}
	        
			/*
			 * Creates a new scope node, moving symbol table information
			 * from "scope" to the new node, and making "scope" a nested
			 * scope contained by the new node.
			 * Useful for injecting a new scope in a scope chain.
			 */
			public static Scope SplitScope(Scope scope)
			{
				Scope result = new Scope(scope.getType());
				result.symbolTable = scope.symbolTable;
				scope.symbolTable = null;
				result.parent = scope.parent;
				scope.parent = result;
				result.top = scope.top;
				return result;
			}

			public static void JoinScopes(Scope source, Scope dest)
			{
				source.ensureSymbolTable();
				dest.ensureSymbolTable();
				if (!Collections.disjoint(source.symbolTable.keySet(),
										  dest.symbolTable.keySet()))
				{
					throw Kit.CodeBug();
				}
				dest.symbolTable.putAll(source.symbolTable);
			}
	        
			public void setParent(Scope parent) {
				this.parent = parent;
				this.top = parent == null ? (ScriptOrFnNode)this : parent.top;
			}
	        
			public Scope getParentScope() {
				return parent;
			}
	  
			public Scope getDefiningScope(String name) {
				for (Scope sn=this; sn != null; sn = sn.parent) {
					if (sn.symbolTable == null)
						continue;
					if (sn.symbolTable.containsKey(name))
						return sn;
				}
				return null;
			}
	        
			public Symbol getSymbol(String name) {
				return symbolTable == null ? null : symbolTable.get(name);
			}
	        
			public void putSymbol(String name, Symbol symbol) {
				ensureSymbolTable();
				symbolTable.put(name, symbol);
				symbol.containingTable = this;
				top.addSymbol(symbol);
			}
	        
			public Map<String,Symbol> getSymbolTable() {
				return symbolTable;
			}
	        
			private void ensureSymbolTable() {
				if (symbolTable == null) {
					symbolTable = new LinkedHashMap<String,Symbol>(5);
				}
			}
	  
			// Use LinkedHashMap so that the iteration order is the insertion order
			protected LinkedHashMap<String,Symbol> symbolTable;
			private Scope parent;
			private ScriptOrFnNode top;
		}

		private static class PropListItem
		{
			PropListItem next;
			int type;
			int intValue;
			Object objectValue;
		}


		public Node(int nodeType) {
			type = nodeType;
		}

		public Node(int nodeType, Node child) {
			type = nodeType;
			first = last = child;
			child.next = null;
		}

		public Node(int nodeType, Node left, Node right) {
			type = nodeType;
			first = left;
			last = right;
			left.next = right;
			right.next = null;
		}

		public Node(int nodeType, Node left, Node mid, Node right) {
			type = nodeType;
			first = left;
			last = right;
			left.next = mid;
			mid.next = right;
			right.next = null;
		}

		public Node(int nodeType, int line) {
			type = nodeType;
			lineno = line;
		}

		public Node(int nodeType, Node child, int line) {
			this(nodeType, child);
			lineno = line;
		}

		public Node(int nodeType, Node left, Node right, int line) {
			this(nodeType, left, right);
			lineno = line;
		}

		public Node(int nodeType, Node left, Node mid, Node right, int line) {
			this(nodeType, left, mid, right);
			lineno = line;
		}

		public static Node newNumber(double number) {
			return new NumberNode(number);
		}

		public static Node newString(String str) {
			return new StringNode(Token.STRING, str);
		}

		public static Node newString(int type, String str) {
			return new StringNode(type, str);
		}

		public int getType() {
			return type;
		}

		public void setType(int type) {
			this.type = type;
		}

		public boolean hasChildren() {
			return first != null;
		}

		public Node getFirstChild() {
			return first;
		}

		public Node getLastChild() {
			return last;
		}

		public Node getNext() {
			return next;
		}

		public Node getChildBefore(Node child) {
			if (child == first)
				return null;
			Node n = first;
			while (n.next != child) {
				n = n.next;
				if (n == null)
					throw new RuntimeException("node is not a child");
			}
			return n;
		}

		public Node getLastSibling() {
			Node n = this;
			while (n.next != null) {
				n = n.next;
			}
			return n;
		}

		public void addChildToFront(Node child) {
			child.next = first;
			first = child;
			if (last == null) {
				last = child;
			}
		}

		public void addChildToBack(Node child) {
			child.next = null;
			if (last == null) {
				first = last = child;
				return;
			}
			last.next = child;
			last = child;
		}

		public void addChildrenToFront(Node children) {
			Node lastSib = children.getLastSibling();
			lastSib.next = first;
			first = children;
			if (last == null) {
				last = lastSib;
			}
		}

		public void addChildrenToBack(Node children) {
			if (last != null) {
				last.next = children;
			}
			last = children.getLastSibling();
			if (first == null) {
				first = children;
			}
		}

		/**
		 * Add 'child' before 'node'.
		 */
		public void addChildBefore(Node newChild, Node node) {
			if (newChild.next != null)
				throw new RuntimeException(
						  "newChild had siblings in addChildBefore");
			if (first == node) {
				newChild.next = first;
				first = newChild;
				return;
			}
			Node prev = getChildBefore(node);
			addChildAfter(newChild, prev);
		}

		/**
		 * Add 'child' after 'node'.
		 */
		public void addChildAfter(Node newChild, Node node) {
			if (newChild.next != null)
				throw new RuntimeException(
						  "newChild had siblings in addChildAfter");
			newChild.next = node.next;
			node.next = newChild;
			if (last == node)
				last = newChild;
		}

		public void removeChild(Node child) {
			Node prev = getChildBefore(child);
			if (prev == null)
				first = first.next;
			else
				prev.next = child.next;
			if (child == last) last = prev;
			child.next = null;
		}

		public void replaceChild(Node child, Node newChild) {
			newChild.next = child.next;
			if (child == first) {
				first = newChild;
			} else {
				Node prev = getChildBefore(child);
				prev.next = newChild;
			}
			if (child == last)
				last = newChild;
			child.next = null;
		}

		public void replaceChildAfter(Node prevChild, Node newChild) {
			Node child = prevChild.next;
			newChild.next = child.next;
			prevChild.next = newChild;
			if (child == last)
				last = newChild;
			child.next = null;
		}

		private static String propToString(int propType)
		{
			if (Token.printTrees) {
				// If Context.printTrees is false, the compiler
				// can remove all these strings.
				switch (propType) {
					case FUNCTION_PROP:      return "function";
					case LOCAL_PROP:         return "local";
					case LOCAL_BLOCK_PROP:   return "local_block";
					case REGEXP_PROP:        return "regexp";
					case CASEARRAY_PROP:     return "casearray";

					case TARGETBLOCK_PROP:   return "targetblock";
					case VARIABLE_PROP:      return "variable";
					case ISNUMBER_PROP:      return "isnumber";
					case DIRECTCALL_PROP:    return "directcall";

					case SPECIALCALL_PROP:   return "specialcall";
					case SKIP_INDEXES_PROP:  return "skip_indexes";
					case OBJECT_IDS_PROP:    return "object_ids_prop";
					case INCRDECR_PROP:      return "incrdecr_prop";
					case CATCH_SCOPE_PROP:   return "catch_scope_prop";
					case LABEL_ID_PROP:      return "label_id_prop";
					case MEMBER_TYPE_PROP:   return "member_type_prop";
					case NAME_PROP:          return "name_prop";
					case CONTROL_BLOCK_PROP: return "control_block_prop";
					case PARENTHESIZED_PROP: return "parenthesized_prop";
					case GENERATOR_END_PROP: return "generator_end";
					case DESTRUCTURING_ARRAY_LENGTH:
											 return "destructuring_array_length";
					case DESTRUCTURING_NAMES:return "destructuring_names";

					default: Kit.CodeBug();
				}
			}
			return null;
		}

		private PropListItem lookupProperty(int propType)
		{
			PropListItem x = propListHead;
			while (x != null && propType != x.type) {
				x = x.next;
			}
			return x;
		}

		private PropListItem ensureProperty(int propType)
		{
			PropListItem item = lookupProperty(propType);
			if (item == null) {
				item = new PropListItem();
				item.type = propType;
				item.next = propListHead;
				propListHead = item;
			}
			return item;
		}

		public void removeProp(int propType)
		{
			PropListItem x = propListHead;
			if (x != null) {
				PropListItem prev = null;
				while (x.type != propType) {
					prev = x;
					x = x.next;
					if (x == null) { return; }
				}
				if (prev == null) {
					propListHead = x.next;
				} else {
					prev.next = x.next;
				}
			}
		}

		public Object getProp(int propType)
		{
			PropListItem item = lookupProperty(propType);
			if (item == null) { return null; }
			return item.objectValue;
		}

		public int getIntProp(int propType, int defaultValue)
		{
			PropListItem item = lookupProperty(propType);
			if (item == null) { return defaultValue; }
			return item.intValue;
		}

		public int getExistingIntProp(int propType)
		{
			PropListItem item = lookupProperty(propType);
			if (item == null) { Kit.CodeBug(); }
			return item.intValue;
		}

		public void putProp(int propType, Object prop)
		{
			if (prop == null) {
				removeProp(propType);
			} else {
				PropListItem item = ensureProperty(propType);
				item.objectValue = prop;
			}
		}

		public void putIntProp(int propType, int prop)
		{
			PropListItem item = ensureProperty(propType);
			item.intValue = prop;
		}

		public int getLineno() {
			return lineno;
		}

		/** Can only be called when <tt>getType() == Token.NUMBER</tt> */
		public double getDouble() {
			return ((NumberNode)this).number;
		}

		public void setDouble(double number) {
			((NumberNode)this).number = number;
		}

		/** Can only be called when node has String context. */
		public String getString() {
			return ((StringNode)this).str;
		}

		/** Can only be called when node has String context. */
		public void setString(String s) {
			if (s == null) Kit.CodeBug();
			((StringNode)this).str = s;
		}
	    
		/** Can only be called when node has String context. */
		public Scope getScope() {
			return ((StringNode)this).scope;
		}

		/** Can only be called when node has String context. */
		public void setScope(Scope s) {
			if (s == null) Kit.CodeBug();
			if (!(this instanceof StringNode)) {
				throw Kit.CodeBug();
			}
			((StringNode)this).scope = s;
		}

		public static Node newTarget()
		{
			return new Node(Token.TARGET);
		}

		public int labelId()
		{
			if (type != Token.TARGET && type != Token.YIELD) Kit.CodeBug();
			return getIntProp(LABEL_ID_PROP, -1);
		}

		public void labelId(int labelId)
		{
			if (type != Token.TARGET  && type != Token.YIELD) Kit.CodeBug();
			putIntProp(LABEL_ID_PROP, labelId);
		}
	    

		/**
		 * Does consistent-return analysis on the function body when strict mode is
		 * enabled.
		 *
		 *   function (x) { return (x+1) }
		 * is ok, but
		 *   function (x) { if (x < 0) return (x+1); }
		 * is not becuase the function can potentially return a value when the
		 * condition is satisfied and if not, the function does not explicitly
		 * return value.
		 *
		 * This extends to checking mismatches such as "return" and "return <value>"
		 * used in the same function. Warnings are not emitted if inconsistent
		 * returns exist in code that can be statically shown to be unreachable.
		 * Ex.
		 *   function (x) { while (true) { ... if (..) { return value } ... } }
		 * emits no warning. However if the loop had a break statement, then a
		 * warning would be emitted.
		 *
		 * The consistency analysis looks at control structures such as loops, ifs,
		 * switch, try-catch-finally blocks, examines the reachable code paths and
		 * warns the user about an inconsistent set of termination possibilities.
		 *
		 * Caveat: Since the parser flattens many control structures into almost
		 * straight-line code with gotos, it makes such analysis hard. Hence this
		 * analyser is written to taken advantage of patterns of code generated by
		 * the parser (for loops, try blocks and such) and does not do a full
		 * control flow analysis of the gotos and break/continue statements.
		 * Future changes to the parser will affect this analysis.
		 */

		/**
		 * These flags enumerate the possible ways a statement/function can
		 * terminate. These flags are used by endCheck() and by the Parser to
		 * detect inconsistent return usage.
		 *
		 * END_UNREACHED is reserved for code paths that are assumed to always be
		 * able to execute (example: throw, continue)
		 *
		 * END_DROPS_OFF indicates if the statement can transfer control to the
		 * next one. Statement such as return dont. A compound statement may have
		 * some branch that drops off control to the next statement.
		 *
		 * END_RETURNS indicates that the statement can return (without arguments)
		 * END_RETURNS_VALUE indicates that the statement can return a value.
		 *
		 * A compound statement such as
		 * if (condition) {
		 *   return value;
		 * }
		 * Will be detected as (END_DROPS_OFF | END_RETURN_VALUE) by endCheck()
		 */
		static int END_UNREACHED = 0;
		static int END_DROPS_OFF = 1;
		static int END_RETURNS = 2;
		static int END_RETURNS_VALUE = 4;
		static int END_YIELDS = 8;

		/**
		 * Checks that every return usage in a function body is consistent with the
		 * requirements of strict-mode.
		 * @return true if the function satisfies strict mode requirement.
		 */
		public boolean hasConsistentReturnUsage()
		{
			int n = endCheck();
			return (n & END_RETURNS_VALUE) == 0 ||
				   (n & (END_DROPS_OFF|END_RETURNS|END_YIELDS)) == 0;
		}

		/**
		 * Returns in the then and else blocks must be consistent with each other.
		 * If there is no else block, then the return statement can fall through.
		 * @return logical OR of END_* flags
		 */
		private int endCheckIf()
		{
			Node th, el;
			int rv = END_UNREACHED;

			th = next;
			el = ((Jump)this).target;

			rv = th.endCheck();

			if (el != null)
				rv |= el.endCheck();
			else
				rv |= END_DROPS_OFF;

			return rv;
		}

		/**
		 * Consistency of return statements is checked between the case statements.
		 * If there is no default, then the switch can fall through. If there is a
		 * default,we check to see if all code paths in the default return or if
		 * there is a code path that can fall through.
		 * @return logical OR of END_* flags
		 */
		private int endCheckSwitch()
		{
			Node n;
			int rv = END_UNREACHED;

			// examine the cases
			for (n = first.next; n != null; n = n.next)
			{
				if (n.type == Token.CASE) {
					rv |= ((Jump)n).target.endCheck();
				} else
					break;
			}

			// we don't care how the cases drop into each other
			rv &= ~END_DROPS_OFF;

			// examine the default
			n = ((Jump)this).getDefault();
			if (n != null)
				rv |= n.endCheck();
			else
				rv |= END_DROPS_OFF;

			// remove the switch block
			rv |= getIntProp(CONTROL_BLOCK_PROP, END_UNREACHED);

			return rv;
		}

		/**
		 * If the block has a finally, return consistency is checked in the
		 * finally block. If all code paths in the finally returns, then the
		 * returns in the try-catch blocks don't matter. If there is a code path
		 * that does not return or if there is no finally block, the returns
		 * of the try and catch blocks are checked for mismatch.
		 * @return logical OR of END_* flags
		 */
		private int endCheckTry()
		{
			Node n;
			int rv = END_UNREACHED;

			// check the finally if it exists
			n = ((Jump)this).getFinally();
			if(n != null) {
				rv = n.next.first.endCheck();
			} else {
				rv = END_DROPS_OFF;
			}

			// if the finally block always returns, then none of the returns
			// in the try or catch blocks matter
			if ((rv & END_DROPS_OFF) != 0) {
				rv &= ~END_DROPS_OFF;

				// examine the try block
				rv |= first.endCheck();

				// check each catch block
				n = ((Jump)this).target;
				if (n != null)
				{
					// point to the first catch_scope
					for (n = n.next.first; n != null; n = n.next.next)
					{
						// check the block of user code in the catch_scope
						rv |= n.next.first.next.first.endCheck();
					}
				}
			}

			return rv;
		}

		/**
		 * Return statement in the loop body must be consistent. The default
		 * assumption for any kind of a loop is that it will eventually terminate.
		 * The only exception is a loop with a constant true condition. Code that
		 * follows such a loop is examined only if one can statically determine
		 * that there is a break out of the loop.
		 *  for(<> ; <>; <>) {}
		 *  for(<> in <> ) {}
		 *  while(<>) { }
		 *  do { } while(<>)
		 * @return logical OR of END_* flags
		 */
		private int endCheckLoop()
		{
			Node n;
			int rv = END_UNREACHED;

			// To find the loop body, we look at the second to last node of the
			// loop node, which should be the predicate that the loop should
			// satisfy.
			// The target of the predicate is the loop-body for all 4 kinds of
			// loops.
			for (n = first; n.next != last; n = n.next) {
				/* skip */
			}
			if (n.type != Token.IFEQ)
				return END_DROPS_OFF;

			// The target's next is the loop body block
			rv = ((Jump)n).target.next.endCheck();

			// check to see if the loop condition is true
			if (n.first.type == Token.TRUE)
				rv &= ~END_DROPS_OFF;

			// look for effect of breaks
			rv |= getIntProp(CONTROL_BLOCK_PROP, END_UNREACHED);

			return rv;
		}


		/**
		 * A general block of code is examined statement by statement. If any
		 * statement (even compound ones) returns in all branches, then subsequent
		 * statements are not examined.
		 * @return logical OR of END_* flags
		 */
		private int endCheckBlock()
		{
			Node n;
			int rv = END_DROPS_OFF;

			// check each statment and if the statement can continue onto the next
			// one, then check the next statement
			for (n=first; ((rv & END_DROPS_OFF) != 0) && n != null; n = n.next)
			{
				rv &= ~END_DROPS_OFF;
				rv |= n.endCheck();
			}
			return rv;
		}

		/**
		 * A labelled statement implies that there maybe a break to the label. The
		 * function processes the labelled statement and then checks the
		 * CONTROL_BLOCK_PROP property to see if there is ever a break to the
		 * particular label.
		 * @return logical OR of END_* flags
		 */
		private int endCheckLabel()
		{
			int rv = END_UNREACHED;

			rv = next.endCheck();
			rv |= getIntProp(CONTROL_BLOCK_PROP, END_UNREACHED);

			return rv;
		}

		/**
		 * When a break is encountered annotate the statement being broken
		 * out of by setting its CONTROL_BLOCK_PROP property.
		 * @return logical OR of END_* flags
		 */
		private int endCheckBreak()
		{
			Node n = ((Jump) this).jumpNode;
			n.putIntProp(CONTROL_BLOCK_PROP, END_DROPS_OFF);
			return END_UNREACHED;
		}

		/**
		 * endCheck() examines the body of a function, doing a basic reachability
		 * analysis and returns a combination of flags END_* flags that indicate
		 * how the function execution can terminate. These constitute only the
		 * pessimistic set of termination conditions. It is possible that at
		 * runtime certain code paths will never be actually taken. Hence this
		 * analysis will flag errors in cases where there may not be errors.
		 * @return logical OR of END_* flags
		 */
		private int endCheck()
		{
			switch(type)
			{
				case Token.BREAK:
					return endCheckBreak();

				case Token.EXPR_VOID:
					if (this.first != null)
						return first.endCheck();
					return END_DROPS_OFF;

				case Token.YIELD:
					return END_YIELDS;

				case Token.CONTINUE:
				case Token.THROW:
					return END_UNREACHED;

				case Token.RETURN:
					if (this.first != null)
						return END_RETURNS_VALUE;
					else
						return END_RETURNS;

				case Token.TARGET:
					if (next != null)
						return next.endCheck();
					else
						return END_DROPS_OFF;

				case Token.LOOP:
					return endCheckLoop();

				case Token.LOCAL_BLOCK:
				case Token.BLOCK:
					// there are several special kinds of blocks
					if (first == null)
						return END_DROPS_OFF;

					switch(first.type) {
						case Token.LABEL:
							return first.endCheckLabel();

						case Token.IFNE:
							return first.endCheckIf();

						case Token.SWITCH:
							return first.endCheckSwitch();

						case Token.TRY:
							return first.endCheckTry();

						default:
							return endCheckBlock();
					}

				default:
					return END_DROPS_OFF;
			}
		}

		public boolean hasSideEffects()
		{
			switch (type) {
			  case Token.EXPR_VOID:
			  case Token.COMMA:
				if (last != null)
					return last.hasSideEffects();
				else
					return true;

			  case Token.HOOK:
				if (first == null ||
					first.next == null ||
					first.next.next == null)
					Kit.CodeBug();
				return first.next.hasSideEffects() &&
					   first.next.next.hasSideEffects();

			  case Token.ERROR:         // Avoid cascaded error messages
			  case Token.EXPR_RESULT:
			  case Token.ASSIGN:
			  case Token.ASSIGN_ADD:
			  case Token.ASSIGN_SUB:
			  case Token.ASSIGN_MUL:
			  case Token.ASSIGN_DIV:
			  case Token.ASSIGN_MOD:
			  case Token.ASSIGN_BITOR:
			  case Token.ASSIGN_BITXOR:
			  case Token.ASSIGN_BITAND:
			  case Token.ASSIGN_LSH:
			  case Token.ASSIGN_RSH:
			  case Token.ASSIGN_URSH:
			  case Token.ENTERWITH:
			  case Token.LEAVEWITH:
			  case Token.RETURN:
			  case Token.GOTO:
			  case Token.IFEQ:
			  case Token.IFNE:
			  case Token.NEW:
			  case Token.DELPROP:
			  case Token.SETNAME:
			  case Token.SETPROP:
			  case Token.SETELEM:
			  case Token.CALL:
			  case Token.THROW:
			  case Token.RETHROW:
			  case Token.SETVAR:
			  case Token.CATCH_SCOPE:
			  case Token.RETURN_RESULT:
			  case Token.SET_REF:
			  case Token.DEL_REF:
			  case Token.REF_CALL:
			  case Token.TRY:
			  case Token.SEMI:
			  case Token.INC:
			  case Token.DEC:
			  case Token.EXPORT:
			  case Token.IMPORT:
			  case Token.IF:
			  case Token.ELSE:
			  case Token.SWITCH:
			  case Token.WHILE:
			  case Token.DO:
			  case Token.FOR:
			  case Token.BREAK:
			  case Token.CONTINUE:
			  case Token.VAR:
			  case Token.CONST:
			  case Token.LET:
			  case Token.LETEXPR:
			  case Token.WITH:
			  case Token.WITHEXPR:
			  case Token.CATCH:
			  case Token.FINALLY:
			  case Token.BLOCK:
			  case Token.LABEL:
			  case Token.TARGET:
			  case Token.LOOP:
			  case Token.JSR:
			  case Token.SETPROP_OP:
			  case Token.SETELEM_OP:
			  case Token.LOCAL_BLOCK:
			  case Token.SET_REF_OP:
			  case Token.YIELD:
				return true;

			  default:
				return false;
			}
		}

		public String toString()
		{
			if (Token.printTrees) {
				StringBuffer sb = new StringBuffer();
				toString(new ObjToIntMap(), sb);
				return sb.toString();
			}
			return String.valueOf(type);
		}

		private void toString(ObjToIntMap printIds, StringBuffer sb)
		{
			if (Token.printTrees) {
				sb.append(Token.name(type));
				if (this instanceof StringNode) {
					sb.append(' ');
					sb.append(getString());
					Scope scope = getScope();
					if (scope != null) {
						sb.append("[scope: ");
						appendPrintId(scope, printIds, sb);
						sb.append("]");
					}
				} else if (this instanceof Node.Scope) {
					if (this instanceof ScriptOrFnNode) {
						ScriptOrFnNode sof = (ScriptOrFnNode)this;
						if (this instanceof FunctionNode) {
							FunctionNode fn = (FunctionNode)this;
							sb.append(' ');
							sb.append(fn.getFunctionName());
						}
						sb.append(" [source name: ");
						sb.append(sof.getSourceName());
						sb.append("] [encoded source length: ");
						sb.append(sof.getEncodedSourceEnd()
								  - sof.getEncodedSourceStart());
						sb.append("] [base line: ");
						sb.append(sof.getBaseLineno());
						sb.append("] [end line: ");
						sb.append(sof.getEndLineno());
						sb.append(']');
					}
					if (((Node.Scope)this).symbolTable != null) {
						sb.append(" [scope ");
						appendPrintId(this, printIds, sb);
						sb.append(": ");
						Iterator iter = ((Node.Scope) this).symbolTable.keySet()
							.iterator();
						while (iter.hasNext()) {
							sb.append(iter.next());
							sb.append(" ");
						}
						sb.append("]");
					}
				} else if (this instanceof Jump) {
					Jump jump = (Jump)this;
					if (type == Token.BREAK || type == Token.CONTINUE) {
						sb.append(" [label: ");
						appendPrintId(jump.getJumpStatement(), printIds, sb);
						sb.append(']');
					} else if (type == Token.TRY) {
						Node catchNode = jump.target;
						Node finallyTarget = jump.getFinally();
						if (catchNode != null) {
							sb.append(" [catch: ");
							appendPrintId(catchNode, printIds, sb);
							sb.append(']');
						}
						if (finallyTarget != null) {
							sb.append(" [finally: ");
							appendPrintId(finallyTarget, printIds, sb);
							sb.append(']');
						}
					} else if (type == Token.LABEL || type == Token.LOOP
							   || type == Token.SWITCH)
					{
						sb.append(" [break: ");
						appendPrintId(jump.target, printIds, sb);
						sb.append(']');
						if (type == Token.LOOP) {
							sb.append(" [continue: ");
							appendPrintId(jump.getContinue(), printIds, sb);
							sb.append(']');
						}
					} else {
						sb.append(" [target: ");
						appendPrintId(jump.target, printIds, sb);
						sb.append(']');
					}
				} else if (type == Token.NUMBER) {
					sb.append(' ');
					sb.append(getDouble());
				} else if (type == Token.TARGET) {
					sb.append(' ');
					appendPrintId(this, printIds, sb);
				}
				if (lineno != -1) {
					sb.append(' ');
					sb.append(lineno);
				}

				for (PropListItem x = propListHead; x != null; x = x.next) {
					int type = x.type;
					sb.append(" [");
					sb.append(propToString(type));
					sb.append(": ");
					String value;
					switch (type) {
					  case TARGETBLOCK_PROP : // can't add this as it recurses
						value = "target block property";
						break;
					  case LOCAL_BLOCK_PROP :     // can't add this as it is dull
						value = "last local block";
						break;
					  case ISNUMBER_PROP:
						switch (x.intValue) {
						  case BOTH:
							value = "both";
							break;
						  case RIGHT:
							value = "right";
							break;
						  case LEFT:
							value = "left";
							break;
						  default:
							throw Kit.CodeBug();
						}
						break;
					  case SPECIALCALL_PROP:
						switch (x.intValue) {
						  case SPECIALCALL_EVAL:
							value = "eval";
							break;
						  case SPECIALCALL_WITH:
							value = "with";
							break;
						  default:
							// NON_SPECIALCALL should not be stored
							throw Kit.CodeBug();
						}
						break;
					  case OBJECT_IDS_PROP: {
						Object[] a = (Object[]) x.objectValue;
						value = "[";
						for (int i=0; i < a.length; i++) {
							value += a[i].toString();
							if (i+1 < a.length)
								value += ", ";
						}
						value += "]";
						break;
					  }
					  default :
						Object obj = x.objectValue;
						if (obj != null) {
							value = obj.toString();
						} else {
							value = String.valueOf(x.intValue);
						}
						break;
					}
					sb.append(value);
					sb.append(']');
				}
			}
		}

		public String toStringTree(ScriptOrFnNode treeTop) {
			if (Token.printTrees) {
				StringBuffer sb = new StringBuffer();
				toStringTreeHelper(treeTop, this, null, 0, sb);
				return sb.toString();
			}
			return null;
		}

		private static void toStringTreeHelper(ScriptOrFnNode treeTop, Node n,
											   ObjToIntMap printIds,
											   int level, StringBuffer sb)
		{
			if (Token.printTrees) {
				if (printIds == null) {
					printIds = new ObjToIntMap();
					generatePrintIds(treeTop, printIds);
				}
				for (int i = 0; i != level; ++i) {
					sb.append("    ");
				}
				n.toString(printIds, sb);
				sb.append('\n');
				for (Node cursor = n.getFirstChild(); cursor != null;
					 cursor = cursor.getNext())
				{
					if (cursor.getType() == Token.FUNCTION) {
						int fnIndex = cursor.getExistingIntProp(Node.FUNCTION_PROP);
						FunctionNode fn = treeTop.getFunctionNode(fnIndex);
						toStringTreeHelper(fn, fn, null, level + 1, sb);
					} else {
						toStringTreeHelper(treeTop, cursor, printIds, level + 1, sb);
					}
				}
			}
		}

		private static void generatePrintIds(Node n, ObjToIntMap map)
		{
			if (Token.printTrees) {
				map.put(n, map.size());
				for (Node cursor = n.getFirstChild(); cursor != null;
					 cursor = cursor.getNext())
				{
					generatePrintIds(cursor, map);
				}
			}
		}

		private static void appendPrintId(Node n, ObjToIntMap printIds, StringBuffer sb)
		{
			if (Token.printTrees) {
				if (n != null) {
					int id = printIds.get(n, -1);
					sb.append('#');
					if (id != -1) {
						sb.append(id + 1);
					} else {
						sb.append("<not_available>");
					}
				}
			}
		}
	}
}
