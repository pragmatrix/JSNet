/**
	JavaScript lexical grammar.
**/

// relaxed parsing of decimal, support 001 for example (test case data.js)

#define RELAX_DECIMALS

using System;
using System.Globalization;
using System.Collections.Generic;

namespace JSNet
{
	using Parser = parser.Parser<char>;
	using Production = parser.Production<char>;
	using Rule = parser.Rule<char>;
	using Terminal = parser.Terminal<char>;
	using MatchFunctor = parser.MatchFunctor<char>;
	using State = parser.Parser<char>.State;

	public static class Test
	{
		public static void parseJavaScript(IEnumerable<char> input)
		{
			Grammar.parse(input);
		}
	};

	public sealed class Token
	{ };

	static partial class Grammar
	{
		static Grammar()
		{
			initLexicalConventions();
			initSyntax();
		}

		public static void parse(IEnumerable<char> input)
		{
			IEnumerable<char> preprocessed = removeFormatControlCharacters(input);
			Parser parser = new Parser('\n');

			try
			{
				// note: cannot process regexps for now, these are syntax dependent and 
				// only makes sense when we implement the syntax here!

#if TEST_LEXER
				parser.parseAny(new List<char>(preprocessed).ToArray(), InputElementDiv);
#else
				char[] file = new List<char>(preprocessed).ToArray();


				uint? consumed = parser.parse(new List<char>(preprocessed).ToArray(), Program);
				if (consumed == null)
					throw new Exception("unable to parse JavaScript");

				if (consumed.Value != file.Length)
					throw new Exception(string.Format("unable to parse JavaScript, consumed {0} of {1} input characters", consumed.Value, file.Length)); 

#endif
			}
			catch (Parser.Exception e)
			{
				uint offset = e.Offset;
				List<char> content = new List<char>(input);
				int line = 1;
				int column = 1;

				for (int i = 0; i != offset; ++i, ++column)
				{
					// todo: use official line terminators
					if (content[i] == '\n')
					{
						++line;
						column = 1;
					}
				}

				string eStr = string.Format("parse error at line {0}, column {1}", line, column);

				throw new Exception(eStr, e);
			}

		}


		#region 6. Source Text

		static readonly Terminal SourceCharacter = 
			(Terminal)delegate(State c) 
			{
				return c.available(1) ? 1 : 0; 
			};

		#endregion
		
		#region 7. Lexical Conventions

		static readonly Production InputElementDiv = new Production("InputElementDiv");
		static readonly Production InputElementRegExp = new Production("InputElementRegExp");

		static void initLexicalConventions()
		{
			InputElementDiv.Rule
				= WhiteSpace
				| LineTerminator
				| Comment
				| Token
				| DivPunctuator;

			// InputElementDiv.Rule
			//	= DecimalDigits;

			InputElementRegExp.Rule
				= WhiteSpace
				| LineTerminator
				| Comment
				| Token
				| RegularExpressionLiteral;

			initWhiteSpace();
			initLineTerminators();
			initComments();
			initTokens();
			initIdentifiers();
			initPunctuators();
			initLiterals();
			initStringLiterals();
		}

		#region 7.1 Unicode Format-Control Characters

		static IEnumerable<char> removeFormatControlCharacters(IEnumerable<char> input)
		{
			foreach (char c in input)
			{
				if (Char.GetUnicodeCategory(c) != UnicodeCategory.Format)
					yield return c;
			}
		}

		#endregion

		#region 7.2 White Space

		static readonly Terminal TAB = '\u0009';
		static readonly Terminal VT = '\u000b';
		static readonly Terminal FF = '\u000c';
		static readonly Terminal SP = '\u0020';
		static readonly Terminal NBSP = '\u00a0';
		static readonly Terminal USP = new MatchFunctor(
			delegate(State state)
			{
				if (!state.available(1))
					return 0;

				return Char.GetUnicodeCategory(state[0]) == UnicodeCategory.SpaceSeparator ? 1 : 0;
			});

		static readonly Production WhiteSpace = new Production("WhiteSpace");

		static void initWhiteSpace()
		{
			WhiteSpace.Rule
				= TAB 
				| VT 
				| FF 
				| SP 
				| NBSP 
				| USP;
		}

		#endregion

		#region 7.3 Line Terminators

		static readonly Terminal LF = '\u000a';
		static readonly Terminal CR = '\u000d';
		static readonly Terminal LS = '\u2028';
		static readonly Terminal PS = '\u2029';

		static readonly Production LineTerminator = new Production("LineTerminator");

		static void initLineTerminators()
		{
			LineTerminator.Rule
				= LF
				| CR
				| LS
				| PS;
		}

		#endregion

		#region 7.4 Comments

		static readonly Production Comment = new Production("Comment");
		static readonly Production MultiLineComment = new Production("MultiLineComment");
		static readonly Production MultiLineCommentChars = new Production("MultiLineCommentChars");
		static readonly Production PostAsteriskCommentChars = new Production("PostAsteriskCommentChars");
		static readonly Production MultiLineNotAsteriskChar = new Production("MultiLineNotAsteriskChar");
		static readonly Production MultiLineNotForwardSlashOrAsteriskChar = new Production("MultiLineNotForwardSlashOrAsteriskChar");
		static readonly Production SingleLineComment = new Production("SingleLineComment");
		static readonly Production SingleLineCommentChars = new Production("SingleLineCommentChars");
		static readonly Production SingleLineCommentChar = new Production("SingleLineCommentChar");

		static readonly Production MultiLineTermination = new Production("MultiLineTermination");

		static void initComments()
		{
			Comment.Rule
				= MultiLineComment
				| SingleLineComment;


#if false
			MultiLineComment.Rule
				= "/*" + MultiLineCommentChars.opt + "*/";

			MultiLineCommentChars.Rule 
				= MultiLineNotAsteriskChar + MultiLineCommentChars.opt
				| '*' + PostAsteriskCommentChars.opt;

			PostAsteriskCommentChars.Rule
				= MultiLineNotForwardSlashOrAsteriskChar + MultiLineCommentChars.opt
				| '*' + PostAsteriskCommentChars.opt;

			MultiLineNotAsteriskChar.Rule
				= SourceCharacter.butNot('*');

			MultiLineNotForwardSlashOrAsteriskChar.Rule
				= SourceCharacter.butNot((Rule)'/' | '*');
#else
			// PEG seems to be much simpler in this case:

			MultiLineComment.Rule
				= "/*" + MultiLineTermination;

			MultiLineTermination.Rule
				= "*/"
				| SourceCharacter + MultiLineTermination;

#endif

#if !USE_ANY
			SingleLineComment.Rule 
				= "//" + SingleLineCommentChars.opt;
#else
			SingleLineComment.Rule
				= "//" + SingleLineCommentChar.any;
#endif
			SingleLineCommentChars.Rule
				= SingleLineCommentChar + SingleLineCommentChars.opt;

			SingleLineCommentChar.Rule
				= SourceCharacter.butNot(LineTerminator);
		}

		#endregion

		#region 7.5 Tokens

		static readonly Production Token = new Production("Token");
		static readonly Production ReservedWord = new Production("ReservedWord");
		static readonly Production Keyword = new Production("Keyword");
		static readonly Production FutureReservedWord = new Production("FutureReservedWord");

		static void initTokens()
		{
			Token.Rule
				= ReservedWord
				| Identifier
				| Punctuator
				| NumericLiteral
				| StringLiteral
				;

			ReservedWord.Rule
				= Keyword
				| FutureReservedWord
				| NullLiteral
				| BooleanLiteral
				;

			Keyword.Rule
				= Rule.oneOf(
					"break", "case", "catch", "continue", "default", "delete", "do",
					"else", "finally", "for", "function", "if", "in", "instanceof", 
					"new", "return", "switch", "this", "throw", "try", "typeof",
					"var", "void", "while", "with"
				);

			FutureReservedWord.Rule
				= Rule.oneOf(
					"abstract", "boolean", "byte", "char", "class", "const", "debugger", "double",
					"enum", "export", "extends", "final", "float", "goto", "implements", "import",
					"int", "interface", "long", "native", "package", "private", "protected", "public",
					"short", "static", "super", "synchronized", "throws", "transient", "volatile"
				);
		}

		#endregion

		#region 7.6 Identifiers

		static readonly Production Identifier = new Production("Identifier");
		static readonly Production IdentifierName = new Production("IdentifierName");
		static readonly Production IdentifierStart = new Production("IdentifierStart");
		static readonly Production IdentifierPart = new Production("IdentifierPort");
		static readonly Terminal UnicodeLetter = (Terminal)
			delegate(State state)
			{
				if (!state.available(1))
					return 0;

				UnicodeCategory cat = Char.GetUnicodeCategory(state[0]);
				switch (cat)
				{
					case UnicodeCategory.UppercaseLetter: 
					case UnicodeCategory.LowercaseLetter:
					case UnicodeCategory.TitlecaseLetter:
					case UnicodeCategory.ModifierLetter:
					case UnicodeCategory.OtherLetter:
					case UnicodeCategory.LetterNumber:
						return 1;
				}

				return 0;
			};

		static readonly Terminal UnicodeCombiningMark = (Terminal)
			delegate(State state) 
			{
				if (!state.available(1))
					return 0;

				UnicodeCategory cat = Char.GetUnicodeCategory(state[0]);
				if (cat == UnicodeCategory.NonSpacingMark || cat == UnicodeCategory.SpacingCombiningMark)
					return 1;
				else
					return 0; 
			};

		static readonly Terminal UnicodeDigit = (Terminal)
			delegate(State state) 
			{
				if (!state.available(1))
					return 0;

				return Char.GetUnicodeCategory(state[0]) == UnicodeCategory.DecimalDigitNumber ? 1 : 0; 
			};

		static readonly Terminal UnicodeConnectorPunctation = (Terminal)
			delegate(State state) 
			{
				if (!state.available(1))
					return 0;

				return Char.GetUnicodeCategory(state[0]) == UnicodeCategory.ConnectorPunctuation ? 1 : 0; 
			};

		static readonly Production HexDigit = new Production("HexDigit");

		static void initIdentifiers()
		{
			Identifier.Rule
				= IdentifierName.butNot(ReservedWord);

#if !USE_ANY
			IdentifierName.Rule
				= IdentifierName + IdentifierPart
				| IdentifierStart;

#else
			IdentifierName.Rule
				= IdentifierStart + IdentifierPart.any
				;
#endif
			IdentifierStart.Rule
				= UnicodeLetter
				| '$'
				| '_'
				// TODO: "\\" shall not be part of the identifier
				// TODO" the finally generated Identifier, shall only a valid identifier by these rules (
				// (even when UnicodeEscapeSequence is used)
				| '\\' + UnicodeEscapeSequence;

			IdentifierPart.Rule
				= IdentifierStart
				| UnicodeCombiningMark
				| UnicodeDigit
				| UnicodeConnectorPunctation
				| '\\' + UnicodeEscapeSequence;

			HexDigit.Rule
				= Rule.oneOf(
					'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
					'a', 'b', 'c', 'd', 'e', 'f',
					'A', 'B', 'C', 'D', 'E', 'F');
		}

		#endregion

		#region 7.7 Punctuators

		static readonly Production Punctuator = new Production("Puctuator");
		static readonly Production DivPunctuator = new Production("DivPunctuator");

		static void initPunctuators()
		{
			Punctuator.Rule
				= Rule.oneOf(
					'{', '}', '(', ')', '[', ']',
					'.', ';', ',', '<', '>', "<=",
					">=", "==", "!=", "===", "!==",
					'+', '-', '*', '%', "++", "--",
					"<<", ">>", ">>>", '&', '|', '^',
					'!', '~', "&&", "||", '?', ':',
					'=', "+=", "-=", "*=", "%=", "<<=",
					">>=", ">>>=", "&=", "|=", "^="
				);

			DivPunctuator.Rule
				= Rule.oneOf(
					'/', "/="
				);
		}

		#endregion

		#region 7.8 Literals

		static readonly Production Literal = new Production("Literal");
		static readonly Terminal NullLiteral = "null";
		static readonly Production BooleanLiteral = new Production("BooleanLiteral");


		static void initLiterals()
		{
			Literal.Rule
				= NullLiteral
				| BooleanLiteral
				| NumericLiteral
				| StringLiteral
				;

			BooleanLiteral.Rule
				= (Rule)"true"
				| "false"
				;

			initNumericLiterals();
			initStringLiterals();
			initRegularExpressionLiterals();
		}

		#endregion

		#region 7.8.3 Numeric Literals
		
		// TODO: the source character immediately following a NumericLiteral must not
		// be an IdentifierStart or DecimalDigit (may be we add some fake productions
		// to catch these?)

		static readonly Production NumericLiteral = new Production("NumericLiteral");
		static readonly Production DecimalLiteral = new Production("DecimalLiteral");
		static readonly Production DecimalIntegerLiteral = new Production("DecimalIntegerLiteral");
		static readonly Production DecimalDigits = new Production("DecimalDigits");
		static readonly Production DecimalDigit = new Production("DecimalDigit");
		static readonly Production NonZeroDigit = new Production("NonZeroDigit");
		static readonly Production ExponentPart = new Production("ExponentPart");
		static readonly Production ExponentIndicator = new Production("ExponentIndicator");
		static readonly Production SignedInteger = new Production("SignedInteger");
		static readonly Production HexIntegerLiteral = new Production("HexIntegerLiteral");


		static void initNumericLiterals()
		{
			// note: reordered: important to match HexIntegerLiteral first (because of
			// the leading 0)

			NumericLiteral.Rule
				= HexIntegerLiteral
				| DecimalLiteral;

			DecimalLiteral.Rule
				= DecimalIntegerLiteral + '.' + DecimalDigits.opt + ExponentPart.opt
				| '.' + DecimalDigits + ExponentPart.opt
				| DecimalIntegerLiteral + ExponentPart.opt
				;

#if RELAX_DECIMALS
			DecimalIntegerLiteral.Rule
				= DecimalDigits;
#else
			DecimalIntegerLiteral.Rule
				= '0'
				| NonZeroDigit + DecimalDigits.opt
				;
#endif

#if !USE_ANY
			DecimalDigits.Rule
				= DecimalDigits + DecimalDigit
				| DecimalDigit
				;
#else
			DecimalDigits.Rule
				= DecimalDigit + DecimalDigit.any
				;
#endif

			DecimalDigit.Rule
				= Rule.oneOf('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');

#if !RELAX_DECIMALS
			NonZeroDigit.Rule
				= Rule.oneOf('1', '2', '3', '4', '5', '6', '7', '8', '9');
#endif

			ExponentPart.Rule
				= ExponentIndicator + SignedInteger;

			ExponentIndicator.Rule
				= Rule.oneOf('e', 'E');

			SignedInteger.Rule
				= DecimalDigits
				| '+' + DecimalDigits
				| '-' + DecimalDigits
				;

#if !USE_ANY
			HexIntegerLiteral.Rule
				= HexIntegerLiteral + HexDigit
				| "0x" + HexDigit
				| "0X" + HexDigit
				;
#else
			HexIntegerLiteral.Rule
				= ((Rule)"0x" | "0X") + HexDigit.any;
				 ;
#endif

		}

		#endregion

		#region 7.8.4 String Literals

		static readonly Production StringLiteral = new Production("StringLiteral");
		static readonly Production DoubleStringCharacters = new Production("DoubleStringCharacters");
		static readonly Production SingleStringCharacters = new Production("SingleStringCharacters");
		static readonly Production DoubleStringCharacter = new Production("DoubleStringCharacter");
		static readonly Production SingleStringCharacter = new Production("SingleStirngCharacter");
		static readonly Production EscapeSequence = new Production("EscapeSequence");
		static readonly Production CharacterEscapeSequence = new Production("CharacterEscapeSequence");
		static readonly Production SingleEscapeCharacter = new Production("SingleEscapeSequence");
		static readonly Production NonEscapeCharacter = new Production("NonEscapeCharacter");
		static readonly Production EscapeCharacter = new Production("EscapeCharacter");
		static readonly Production HexEscapeSequence = new Production("HexEscapeSequence");
		static readonly Production UnicodeEscapeSequence = new Production("UnicodeEscapeSequence");

		static void initStringLiterals()
		{
			StringLiteral.Rule
				= '"' + DoubleStringCharacters.opt + '"'
				| '\'' + SingleStringCharacters.opt + '\''
				;

#if !USE_ANY
			DoubleStringCharacters.Rule
				= DoubleStringCharacter + DoubleStringCharacters.opt
				;

			SingleStringCharacters.Rule
				= SingleStringCharacter + SingleStringCharacters.opt
				;
#else
			DoubleStringCharacters.Rule
				= DoubleStringCharacter + DoubleStringCharacter.any
				;

			SingleStringCharacters.Rule
				= SingleStringCharacter + SingleStringCharacter.any
				;
#endif
			DoubleStringCharacter.Rule
				= SourceCharacter.butNot((Rule)'"' | '\\' | LineTerminator)
				| '\\' + EscapeSequence
				;

			SingleStringCharacter.Rule
				= SourceCharacter.butNot((Rule)'\'' | '\\' | LineTerminator)
				| '\\' + EscapeSequence
				;

			EscapeSequence.Rule
				= CharacterEscapeSequence
				| ((Rule)'0').lookaheadNotOf(DecimalDigit)
				| HexEscapeSequence
				| UnicodeEscapeSequence;

			CharacterEscapeSequence.Rule
				= SingleEscapeCharacter
				| NonEscapeCharacter
				;

			SingleEscapeCharacter.Rule
				= Rule.oneOf('\'', '"', '\\', 'b', 'f', 'n', 'r', 't', 'v');

			NonEscapeCharacter.Rule
				= SourceCharacter.butNot(EscapeCharacter | LineTerminator);

			EscapeCharacter.Rule
				= SingleEscapeCharacter
				| DecimalDigit
				| 'x'
				| 'u'
				;

			HexEscapeSequence.Rule
				= 'x' + HexDigit + HexDigit;

			UnicodeEscapeSequence.Rule
				= 'u' + HexDigit + HexDigit + HexDigit + HexDigit;
		}

		#endregion

		#region 7.8.5 Regular Expression Literals

		static readonly Production RegularExpressionLiteral = new Production("RegularExpressionLiteral");
		static readonly Production RegularExpressionBody = new Production("RegularExpressionBody");
		static readonly Production RegularExpressionChars = new Production("RegularExpressionChars");
		static readonly Production RegularExpressionFirstChar = new Production("RegularExpressionFirstChar");
		static readonly Production RegularExpressionChar = new Production("RegularExpressionChar");
		static readonly Production BackslashSequence = new Production("BackslashSequence");
		static readonly Production NonTerminator = new Production("NonTerminator");
		static readonly Production RegularExpressionFlags = new Production("RegularExpressionFlags");

		static void initRegularExpressionLiterals()
		{
			RegularExpressionLiteral.Rule
				= '/' + RegularExpressionBody + '/' + RegularExpressionFlags;

			RegularExpressionBody.Rule
				= RegularExpressionFirstChar + RegularExpressionChars;

			RegularExpressionChars.Rule
				= RegularExpressionChars + RegularExpressionChar
				| Rule.Empty;
				
			RegularExpressionFirstChar.Rule
				= NonTerminator.butNot((Rule)'*' | '\\' | '/')
				| BackslashSequence;

			RegularExpressionChar.Rule
				= NonTerminator.butNot((Rule)'\\' | '/')
				| BackslashSequence;

			BackslashSequence.Rule
				= '\\' + NonTerminator;

			NonTerminator.Rule
				= SourceCharacter.butNot(LineTerminator);

			RegularExpressionFlags.Rule
				= RegularExpressionFlags + IdentifierPart
				| Rule.Empty;
		}

		#endregion

		#endregion
	}
}
