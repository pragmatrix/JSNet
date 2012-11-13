/**
	Match Information record embedded in Rule<InT>
**/

using System.Diagnostics;
using System;

namespace JSNet.parser
{
	#region Match Information Record

	/**
		A match information record.
	**/

	public struct Match
	{
		/// The number of input (InT> elements that match
		public readonly uint Count;


		public Match(uint count)
		{
			Count = count;
		}

		/**
			Combine two contiguous matches.
		**/

		public static Match operator + (Match l, Match r)
		{
			return new Match(l.Count + r.Count);
		}

		public static bool operator >(Match l, Match r)
		{
			return l.Count > r.Count;
		}

		public static bool operator <(Match l, Match r)
		{
			return l.Count < r.Count;
		}

		public static bool operator <=(Match l, Match r)
		{
			return !(l.Count > r.Count);
		}

		public static bool operator >=(Match l, Match r)
		{
			return !(l.Count < r.Count);
		}

		public static readonly Match[] EmptyArray = new Match[0];

		public override string ToString()
		{
			return "matched " + Count.ToString();
		}
	}
#if false
	static class MatchHelper
	{
		public static uint? asCount(this Nullable<Match> m)
		{
			if (m == null)
				return null;
			return m.Value.Count;
		}

	}
#endif

	#endregion
}
