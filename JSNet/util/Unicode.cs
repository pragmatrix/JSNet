/** @file
	Unicode helper,

	@author Armin Sander <asander@nero.com>

	(c) 2006 Armin Sander
**/

using System;
using System.Globalization;

namespace JSNet.util
{
	public enum CharacterCategory
	{
		Separator,
		Number,
		Letter,
		Punctuation,
		Symbol,
		Mark,
		Other
	};

	static class Unicode
	{

		/**
			Character category for moving to the next work.

			This is basically a direct representation of the first letter of the
			unicode designation.
		**/


		public static CharacterCategory CharacterCategory(this char c)
		{
			switch (Char.GetUnicodeCategory(c))
			{
				case UnicodeCategory.ClosePunctuation:
				case UnicodeCategory.ConnectorPunctuation:
				case UnicodeCategory.DashPunctuation:
				case UnicodeCategory.FinalQuotePunctuation:
				case UnicodeCategory.InitialQuotePunctuation:
				case UnicodeCategory.OpenPunctuation:
				case UnicodeCategory.OtherPunctuation:
					return util.CharacterCategory.Punctuation;

				case UnicodeCategory.Control:
				case UnicodeCategory.OtherNotAssigned:
				case UnicodeCategory.PrivateUse:
				case UnicodeCategory.Surrogate:
				case UnicodeCategory.Format:
					return util.CharacterCategory.Other;

				case UnicodeCategory.CurrencySymbol:
				case UnicodeCategory.MathSymbol:
				case UnicodeCategory.ModifierSymbol:
				case UnicodeCategory.OtherSymbol:
					return util.CharacterCategory.Symbol;

				case UnicodeCategory.DecimalDigitNumber:
				case UnicodeCategory.LetterNumber:
				case UnicodeCategory.OtherNumber:
					return util.CharacterCategory.Number;

				case UnicodeCategory.EnclosingMark:
				case UnicodeCategory.NonSpacingMark:
				case UnicodeCategory.SpacingCombiningMark:
					return util.CharacterCategory.Mark;

				case UnicodeCategory.LineSeparator:
				case UnicodeCategory.ParagraphSeparator:
				case UnicodeCategory.SpaceSeparator:
					return util.CharacterCategory.Separator;

				case UnicodeCategory.LowercaseLetter:
				case UnicodeCategory.ModifierLetter:
				case UnicodeCategory.OtherLetter:
				case UnicodeCategory.TitlecaseLetter:
				case UnicodeCategory.UppercaseLetter:
					return util.CharacterCategory.Letter;
			}

			return util.CharacterCategory.Other;
		}
	}
} // namespace iHDRuntime.util
