/**
	JavaScript Grammar
**/

namespace JSNet
{
	using Production = parser.Production<char>;
	using Rule = parser.Rule<char>;

	static partial class Grammar
	{
		#region Application of custom match function

		static Production newSyntaxProduction(string name)
		{
			Production p = new Production(name);

			// a custom delimiter must be set to ignore 
			// Whitespace, Comments and LineSeparators

			// note: better not use "opt" here 
			// (we don't optimize away common subrules)
			p.Delimiter = SyntaxDelimitersOpt;

			return p;
		}

		#endregion

		// syntax delimiter must be a regular production
		static readonly Production SyntaxDelimiters = new Production("SyntaxDelimiters");
		static readonly Production SyntaxDelimitersOpt = new Production("SyntaxDelimitersOpt");
		static readonly Production SyntaxDelimiterToken = new Production("SyntaxDelimiter");
		
		static void initSyntax()
		{
			SyntaxDelimitersOpt.Rule
				= SyntaxDelimiters.opt;

#if !USE_ANY
			SyntaxDelimiters.Rule
				= SyntaxDelimiters + SyntaxDelimiterToken
				| SyntaxDelimiterToken
				;
#else
			SyntaxDelimiters.Rule
				= SyntaxDelimiterToken + SyntaxDelimiterToken.any
				;
#endif
			SyntaxDelimiterToken.Rule
				= WhiteSpace
				| Comment
				| LineTerminator;

			initExpressions();
			initStatements();
			initFunctionDefinition();
			initProgram();
		}

		#region 11 Expressions

		static void initExpressions()
		{
			initPrimaryExpressions();
			initLeftHandSideExpressions();
			initPostfixExpressions();
			initUnaryOperators();
			initMultiplicativeOperators();
			initAdditiveOperatos();
			initBitwiseShiftOperators();
			initRelationalOperators();
			initEqualityOperators();
			initBinaryBitwiseOperators();
			initBinaryLogicalOperators();
			initConditionalOperator();
			initAssignmentOperators();
			initCommaOperator();
		}

		#region 11.1 Primary Expressions

		static readonly Production PrimaryExpression = newSyntaxProduction("PrimaryExpression");
		static readonly Production ArrayLiteral = newSyntaxProduction("ArrayLiteral");
		static readonly Production ElementList = newSyntaxProduction("ElementList");
		static readonly Production Elison = newSyntaxProduction("Elison");
		static readonly Production ObjectLiteral = newSyntaxProduction("ObjectLiteral");
		static readonly Production PropertyNameAndValueList = newSyntaxProduction("PropertyNameAndValueList");
		static readonly Production PropertyName = newSyntaxProduction("PropertyName");

		static void initPrimaryExpressions()
		{
			// note: moved "this at the end" , otherwise it an identifier 
			// beginning with "this" would be matched

			PrimaryExpression.Rule
				= Identifier
				| Literal
				| ArrayLiteral
				| ObjectLiteral
				| "(" + Expression + ")"
				| "this"
				;

			ArrayLiteral.Rule
				= "[" + ElementList + "," + Elison.opt + "]"
				| "[" + ElementList + "]"
				| "[" + Elison.opt + "]"
				;

			ElementList.Rule
				= ElementList + "," + Elison.opt + AssignmentExpression
				| Elison.opt + AssignmentExpression
				;

#if !USE_ANY
			Elison.Rule
				= Elison + ","
				| ","
				;
#else
			Elison.Rule
				= (Rule)"," + ((Rule)",").any;
#endif

			ObjectLiteral.Rule
				= (Rule)"{" + "}"
				| "{" + PropertyNameAndValueList + "}"
				;

			PropertyNameAndValueList.Rule
				= PropertyNameAndValueList + "," + PropertyName + ":" + AssignmentExpression
				| PropertyName + ":" + AssignmentExpression
				;

			PropertyName.Rule
				= Identifier
				| StringLiteral
				| NumericLiteral
				;
		}

		#endregion

		#region 11.2 Left-Hand-Side Expressions

		static readonly Production MemberExpression = newSyntaxProduction("MemberExpression");

		static readonly Production NewExpression = newSyntaxProduction("NewExpression");
		static readonly Production CallExpression = newSyntaxProduction("CallExpression");
		static readonly Production Arguments = newSyntaxProduction("Arguments");
		static readonly Production ArgumentList = newSyntaxProduction("ArgumentList");
		static readonly Production LeftHandSideExpression = newSyntaxProduction("LeftHandSideExpression");

		private static void initLeftHandSideExpressions()
		{
			// note: reordered

			MemberExpression.Rule 
				= MemberExpression + "[" + Expression + "]"
				| MemberExpression + "." + Identifier
				| PrimaryExpression
				| FunctionExpression
				| "new" + MemberExpression + Arguments
				;

			NewExpression.Rule
				= MemberExpression
				| "new" + NewExpression
				;

			CallExpression.Rule
				= CallExpression + Arguments
				| CallExpression + "[" + Expression + "]"
				| CallExpression + "." + Identifier
				| MemberExpression + Arguments
				;

			Arguments.Rule
				= (Rule) "(" + ")"
				| "(" + ArgumentList + ")"
				;

			// note: changed evaluation order

#if !USE_ANY
			ArgumentList.Rule
				= ArgumentList + "," + AssignmentExpression
				| AssignmentExpression
				;
#else
			ArgumentList.Rule
				= AssignmentExpression + ((Rule)"," + AssignmentExpression).any;
				;
#endif

			LeftHandSideExpression.Rule
				= CallExpression
				| NewExpression
				;
		}

		#endregion

		#region 11.3 Postfix Expressions

		static readonly Production PostfixExpression = newSyntaxProduction("PostfixExpression");


		private static void initPostfixExpressions()
		{

			// note: changed order
			// todo: implement the "no line terminator here" rule

			PostfixExpression.Rule
				= LeftHandSideExpression + /* no line terminator here */ "++"
				| LeftHandSideExpression + /* no line terminator here */ "--"
				| LeftHandSideExpression
				;
		}

		#endregion

		#region 11.4 Unary Operators

		static readonly Production UnaryExpression = newSyntaxProduction("UnaryExpression");
	
		private static void initUnaryOperators()
		{
			UnaryExpression.Rule
				= PostfixExpression
				| "delete" + UnaryExpression
				| "void" + UnaryExpression
				| "typeof" + UnaryExpression
				| "++" + UnaryExpression
				| "--" + UnaryExpression
				| "+" + UnaryExpression
				| "-" + UnaryExpression
				| "~" + UnaryExpression
				| "!" + UnaryExpression
				;
		}

		#endregion

		#region 11.5 Multiplicative Operators

		static readonly Production MultiplicativeExpression = newSyntaxProduction("MultiplicativeExpression");

		private static void initMultiplicativeOperators()
		{
			MultiplicativeExpression.Rule
				= MultiplicativeExpression + "*" + UnaryExpression
				| MultiplicativeExpression + "/" + UnaryExpression
				| MultiplicativeExpression + "%" + UnaryExpression
				| UnaryExpression
				;
		}

		#endregion

		#region 11.6 Additive Operators

		static readonly Production AdditiveExpression = newSyntaxProduction("AdditiveExpression");

		private static void initAdditiveOperatos()
		{
			AdditiveExpression.Rule
				= AdditiveExpression + "+" + MultiplicativeExpression
				| AdditiveExpression + "-" + MultiplicativeExpression
				| MultiplicativeExpression
				;
		}

		#endregion

		#region 11.7 Bitwise Shift Operators

		static readonly Production ShiftExpression = newSyntaxProduction("ShiftExpression");

		private static void initBitwiseShiftOperators()
		{
			ShiftExpression.Rule
				= ShiftExpression + "<<" + AdditiveExpression
				| ShiftExpression + ">>" + AdditiveExpression
				| ShiftExpression + ">>>" + AdditiveExpression
				| AdditiveExpression
				;
		}

		#endregion

		#region 11.8 Relational Operators

		static readonly Production RelationalExpression = newSyntaxProduction("RelationalExpression");
		static readonly Production RelationalExpressionNoIn = newSyntaxProduction("RelationalExpressionNoIn");

		private static void initRelationalOperators()
		{
			RelationalExpression.Rule
				= RelationalExpression + "<" + ShiftExpression
				| RelationalExpression + ">" + ShiftExpression
				| RelationalExpression + "<=" + ShiftExpression
				| RelationalExpression + ">=" + ShiftExpression
				| RelationalExpression + "instanceof" + ShiftExpression
				| RelationalExpression + "in" + ShiftExpression
				| ShiftExpression
				;

			RelationalExpressionNoIn.Rule
				= RelationalExpressionNoIn + "<" + ShiftExpression
				| RelationalExpressionNoIn + ">" + ShiftExpression
				| RelationalExpressionNoIn + "<=" + ShiftExpression
				| RelationalExpressionNoIn + ">=" + ShiftExpression
				| RelationalExpressionNoIn + "instanceof" + ShiftExpression
				| ShiftExpression
				;
		}

		#endregion

		#region 11.9 Equality Operators

		static readonly Production EqualityExpression = newSyntaxProduction("EqualityExpression");
		static readonly Production EqualityExpressionNoIn = newSyntaxProduction("EqualityExpressionNoIn");

		private static void initEqualityOperators()
		{
			// changed order

			EqualityExpression.Rule
				= EqualityExpression + "==" + RelationalExpression
				| EqualityExpression + "!=" + RelationalExpression
				| EqualityExpression + "===" + RelationalExpression
				| EqualityExpression + "!==" + RelationalExpression
				| RelationalExpression
				;

			EqualityExpressionNoIn.Rule
				= EqualityExpressionNoIn + "==" + RelationalExpressionNoIn
				| EqualityExpressionNoIn + "!=" + RelationalExpressionNoIn
				| EqualityExpressionNoIn + "===" + RelationalExpressionNoIn
				| EqualityExpressionNoIn + "!==" + RelationalExpressionNoIn
				| RelationalExpressionNoIn
				;
		}

		#endregion

		#region 11.10 Binary Bitwise Operators

		static readonly Production BitwiseANDExpression = newSyntaxProduction("BitwiseANDExpression");
		static readonly Production BitwiseANDExpressionNoIn = newSyntaxProduction("BitwiseANDExpressionNoIn");
		static readonly Production BitwiseXORExpression = newSyntaxProduction("BitwiseXORExpression");
		static readonly Production BitwiseXORExpressionNoIn = newSyntaxProduction("BitwiseXORExpressionNoIn");
		static readonly Production BitwiseORExpression = newSyntaxProduction("BitwiseORExpression");
		static readonly Production BitwiseORExpressionNoIn = newSyntaxProduction("BitwiseORExpressionNoIn");

		private static void initBinaryBitwiseOperators()
		{
			BitwiseANDExpression.Rule
				= BitwiseANDExpression + "&" + EqualityExpression
				| EqualityExpression
				;

			BitwiseANDExpressionNoIn.Rule
				= BitwiseANDExpressionNoIn + "&" + EqualityExpressionNoIn
				| EqualityExpressionNoIn
				;

			
			BitwiseXORExpression.Rule
				= BitwiseXORExpression + "^" + BitwiseANDExpression
				| BitwiseANDExpression
				;

			BitwiseXORExpressionNoIn.Rule
				= BitwiseXORExpressionNoIn + "^" + BitwiseANDExpressionNoIn
				| BitwiseANDExpressionNoIn
				;


			BitwiseORExpression.Rule
				= BitwiseORExpression + "|" + BitwiseXORExpression
				| BitwiseXORExpression
				;

			BitwiseORExpressionNoIn.Rule
				= BitwiseORExpressionNoIn + "|" + BitwiseXORExpressionNoIn
				| BitwiseXORExpressionNoIn
				;
		}

		#endregion

		#region 11.11 Binary Logical Operators

		static readonly Production LogicalANDExpression = newSyntaxProduction("LogicalANDExpression");
		static readonly Production LogicalANDExpressionNoIn = newSyntaxProduction("LogicalANDExpressionNoIn");
		static readonly Production LogicalORExpression = newSyntaxProduction("LogicalORExpression");
		static readonly Production LogicalORExpressionNoIn = newSyntaxProduction("LogicalORExpressionNoIn");

		private static void initBinaryLogicalOperators()
		{
			LogicalANDExpression.Rule
				= LogicalANDExpression + "&&" + BitwiseORExpression
				| BitwiseORExpression
				;

			LogicalANDExpressionNoIn.Rule
				= LogicalANDExpressionNoIn + "&&" + BitwiseORExpressionNoIn
				| BitwiseORExpressionNoIn
				;
			
			LogicalORExpression.Rule
				= LogicalORExpression + "||" + LogicalANDExpression
				| LogicalANDExpression
				;

			LogicalORExpressionNoIn.Rule
				= LogicalORExpressionNoIn + "||" + LogicalANDExpressionNoIn
				| LogicalANDExpressionNoIn
				;
		}

		#endregion

		#region 11.12 Conditional Operator

		static readonly Production ConditionalExpression = newSyntaxProduction("ConditionalExpression");
		static readonly Production ConditionalExpressionNoIn = newSyntaxProduction("ConditionalExpressionNoIn");

		private static void initConditionalOperator()
		{
			ConditionalExpression.Rule
				= LogicalORExpression + "?" + AssignmentExpression + ":" + AssignmentExpression
				| LogicalORExpression
				;

			ConditionalExpressionNoIn.Rule
				= LogicalORExpressionNoIn + "?" + AssignmentExpression + ":" + AssignmentExpressionNoIn
				| LogicalORExpressionNoIn
				;
		}

		#endregion

		#region 11.13 Assignment Operators

		static readonly Production AssignmentExpression = newSyntaxProduction("AssignmentExpression");
		static readonly Production AssignmentExpressionNoIn = newSyntaxProduction("AssignmentExpressionNoIn");
		static readonly Production AssignmentOperator = newSyntaxProduction("AssignmentOperator");

		private static void initAssignmentOperators()
		{
			AssignmentExpression.Rule
				= LeftHandSideExpression + AssignmentOperator + AssignmentExpression
				| ConditionalExpression
				;
			
			AssignmentExpressionNoIn.Rule
				= LeftHandSideExpression + AssignmentOperator + AssignmentExpressionNoIn
				| ConditionalExpressionNoIn
				;
			
			AssignmentOperator.Rule
				= Rule.oneOf("=", "*=", "/=", "%=", "+=", "-=", "<<=", ">>=", ">>>=", "&=", "^=", "|=")
				;

		}

		#endregion

		static readonly Production Expression = newSyntaxProduction("Expression");
		static readonly Production ExpressionNoIn = newSyntaxProduction("ExpressionNoIn");

		#region 11.14 Comma Operator

		private static void initCommaOperator()
		{
#if !USE_ANY
			Expression.Rule
				= Expression + "," + AssignmentExpression
				| AssignmentExpression
				;

			ExpressionNoIn.Rule
				= ExpressionNoIn + "," + AssignmentExpressionNoIn
				| AssignmentExpressionNoIn
				;
#else
			Expression.Rule
				= AssignmentExpression + ((Rule)"," + AssignmentExpression).any;
				;

			ExpressionNoIn.Rule
				= AssignmentExpressionNoIn + ((Rule)"," + AssignmentExpressionNoIn).any;
				;
#endif
		}

		#endregion
		#endregion

		#region 12 Statements

		static readonly Production Statement = newSyntaxProduction("Statement");
		static readonly Production Block = newSyntaxProduction("Block");
		static readonly Production StatementList = newSyntaxProduction("StatementList");
		
		static readonly Production VariableStatement = newSyntaxProduction("VariableStatement");
		static readonly Production VariableDeclarationList = newSyntaxProduction("VariableDeclarationList");
		static readonly Production VariableDeclarationListNoIn = newSyntaxProduction("VariableDeclarationListNoIn");
		static readonly Production VariableDeclaration = newSyntaxProduction("VariableDeclaration");
		static readonly Production VariableDeclarationNoIn = newSyntaxProduction("VariableDeclarationNoIn");
		static readonly Production Initialiser = newSyntaxProduction("Initialiser");
		static readonly Production InitialiserNoIn = newSyntaxProduction("InitialiserNoIn");
		
		static readonly Production EmptyStatement = newSyntaxProduction("EmptyStatement");
		static readonly Production ExpressionStatement = newSyntaxProduction("ExpressionStatement");
		static readonly Production IfStatement = newSyntaxProduction("IfStatement");

		static readonly Production IterationStatement = newSyntaxProduction("IterationStatement");
		static readonly Production ContinueStatement = newSyntaxProduction("ContinueStatement");
		static readonly Production BreakStatement = newSyntaxProduction("BreakStatement");
		static readonly Production ReturnStatement = newSyntaxProduction("ReturnStatement");
		
		static readonly Production WithStatement = newSyntaxProduction("WithStatement");
		
		static readonly Production SwitchStatement = newSyntaxProduction("SwitchStatement");
		static readonly Production CaseBlock = newSyntaxProduction("CaseBlock");
		static readonly Production CaseClauses = newSyntaxProduction("CaseClauses");
		static readonly Production CaseClause = newSyntaxProduction("CaseClause");
		static readonly Production DefaultClause = newSyntaxProduction("DefaultClause");
		
		static readonly Production LabelledStatement = newSyntaxProduction("LabelledStatement");

		static readonly Production ThrowStatement = newSyntaxProduction("ThrowStatement");
		static readonly Production TryStatement = newSyntaxProduction("TryStatement");
		static readonly Production Catch = newSyntaxProduction("Catch");
		static readonly Production Finally = newSyntaxProduction("Finally");


		private static void initStatements()
		{
			Statement.Rule
				= Block
				| VariableStatement
				| EmptyStatement
				| ExpressionStatement
				| IfStatement
				| IterationStatement
				| ContinueStatement
				| BreakStatement
				| ReturnStatement
				| WithStatement
				| LabelledStatement
				| SwitchStatement
				| ThrowStatement
				| TryStatement
				;

			Block.Rule
				= "{" + StatementList.opt + "}"
				;

#if !USE_ANY

			StatementList.Rule
				= StatementList + Statement
				| Statement
				;

#else
			StatementList.Rule
				= Statement + Statement.any
				;
#endif

			VariableStatement.Rule
				= "var" + VariableDeclarationList + ";"
				;

#if !USE_ANY
			VariableDeclarationList.Rule
				= VariableDeclarationList + "," + VariableDeclaration
				| VariableDeclaration
				;

			VariableDeclarationListNoIn.Rule
				= VariableDeclarationListNoIn + "," + VariableDeclarationNoIn
				| VariableDeclarationNoIn
				;
#else
			VariableDeclarationList.Rule
				= VariableDeclaration + ((Rule)"," + VariableDeclaration).any;

			VariableDeclarationListNoIn.Rule
				= VariableDeclarationNoIn + ((Rule)"," + VariableDeclarationNoIn).any
				;
#endif

			VariableDeclaration.Rule
				= Identifier + Initialiser.opt
				;

			VariableDeclarationNoIn.Rule
				= Identifier + InitialiserNoIn.opt
				;

			Initialiser.Rule
				= "=" + AssignmentExpression
				;

			InitialiserNoIn.Rule
				= "=" + AssignmentExpressionNoIn
				;


			EmptyStatement.Rule
				= ";"
				;

			// note: we need to be sure that the lookaheadNotOf() filters out identifiers prefixed with
			// "function" here.
			
			ExpressionStatement.Rule
				= Rule.lookaheadNotOf1(Rule.oneOf(",", ((Rule)"function").lookaheadNotOf(IdentifierPart))) + Expression + ";"
				;

			IfStatement.Rule
				= (Rule)"if" + "(" + Expression + ")" + Statement + "else" + Statement
				| (Rule)"if" + "(" + Expression + ")" + Statement
				;

			IterationStatement.Rule
				= "do" + Statement + "while" + "(" + Expression + ")" + ";"
				| (Rule) "while" + "(" + Expression + ")" + Statement
				| (Rule) "for" + "(" + ExpressionNoIn.opt + ";" + Expression.opt + ";" + Expression.opt + ")" + Statement
				| (Rule) "for" + "(" + "var" + VariableDeclarationListNoIn + ";" + Expression.opt + ";" + Expression.opt + ")" + Statement
				| (Rule) "for" + "(" + LeftHandSideExpression + "in" + Expression + ")" + Statement
				| (Rule) "for" + "(" + "var" + VariableDeclarationNoIn + "in" + Expression + ")" + Statement
				;

			ContinueStatement.Rule
				= "continue" + /* todo: no line terminator here */ Identifier.opt + ";"
				;

			BreakStatement.Rule
				= "break" + /* todo: no line terminator here */ Identifier.opt + ";"
				;

			ReturnStatement.Rule
				= "return" + /* todo no line termiantor here */ Expression.opt + ";"
				;

			WithStatement.Rule
				= (Rule) "with" + "(" + Expression + ")" + Statement
				;

			SwitchStatement.Rule
				= (Rule) "switch" + "(" + Expression + ")" + CaseBlock
				;
			
			// reordered:

			CaseBlock.Rule
				= "{" + CaseClauses.opt + DefaultClause + CaseClauses.opt + "}"
				| "{" + CaseClauses.opt + "}"
				;
			
#if !USE_ANY
			// for some reason, the any version is slower???

			CaseClauses.Rule
				= CaseClauses + CaseClause
				| CaseClause
				;
#else
			CaseClauses.Rule
				= CaseClause + CaseClause.any
				;
#endif

			CaseClause.Rule
				= "case" + Expression + ":" + StatementList.opt
				;
			
			DefaultClause.Rule
				= (Rule) "default" + ":" + StatementList.opt
				;

			LabelledStatement.Rule
				= Identifier + ":" + Statement
				;

			ThrowStatement.Rule
				= "throw" /* todo: no line terminator here */ + Expression + ";"
				;

			// note: reordered

			TryStatement.Rule
				= "try" + Block + Catch + Finally
				| "try" + Block + Catch
				| "try" + Block + Finally
				;

			Catch.Rule
				= (Rule) "catch" + "(" + Identifier + ")" + Block
				;

			Finally.Rule
				= "finally" + Block
				;
		}

		#endregion

		#region 13 Function Definition

		static readonly Production FunctionDeclaration = newSyntaxProduction("FunctionDeclaration");
		static readonly Production FunctionExpression = newSyntaxProduction("FunctionExpression");
		static readonly Production FormalParameterList = newSyntaxProduction("FormalParameterList");
		static readonly Production FunctionBody = newSyntaxProduction("FunctionBody");

		private static void initFunctionDefinition()
		{

#if false
			FunctionDeclaration.Rule
				= "function" + Identifier + "(" + FormalParameterList.opt + ")" + "{" + FunctionBody + "}"
				;

			FunctionExpression.Rule
				= "function" + Identifier.opt + "(" + FormalParameterList.opt + ")" + "{" + FunctionBody + "}"
				;
#endif
			// note: extended both to support optional empty body.

			FunctionDeclaration.Rule
				= "function" + Identifier + "(" + FormalParameterList.opt + ")" + "{" + FunctionBody.opt + "}"
				;

			FunctionExpression.Rule
				= "function" + Identifier.opt + "(" + FormalParameterList.opt + ")" + "{" + FunctionBody.opt + "}"
				;

			// note: reordered

#if !USE_ANY

			FormalParameterList.Rule
				= FormalParameterList + "," + Identifier
				| Identifier
				;
#else
			FormalParameterList.Rule
				= Identifier + ((Rule)"," + Identifier).any
				;

#endif
			FunctionBody.Rule
				= SourceElements
				;
		}

		#endregion

		#region 14 Program

		static readonly Production Program = newSyntaxProduction("Program");
		static readonly Production SourceElements = newSyntaxProduction("SourceElements");
		static readonly Production SourceElement = newSyntaxProduction("SourceElement");

		private static void initProgram()
		{
			Program.Rule
				= SourceElements
				;

			// note: reordered
#if !USE_ANY
			SourceElements.Rule
				= SourceElements + SourceElement
				| SourceElement
				;
#else
			SourceElements.Rule
				= SourceElement + SourceElement.any
				;
#endif

			SourceElement.Rule
				= Statement
				| FunctionDeclaration
				;
		}

		#endregion
	}
}
