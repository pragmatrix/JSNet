/**
	Rule Request specification.
**/

namespace JSNet.parser
{
	abstract partial class Rule<InT>
	{
		/**
			Any rule can issue a request to evaluate another rule at a specific offset,
			and place the result somewhere.
		**/

		public struct Request
		{
			public readonly Rule<InT> Rule;
			public readonly uint? Value;
			public readonly Result Result;

			public Request(Rule<InT> rule, Parser<InT>.State state, Result result)
			{
				Rule = rule;
				Value = state.Offset;
				Result = result;
			}

			Request(Match? match)
			{
				Rule = null;
				if (match != null)
					Value = match.Value.Count;
				else
					Value = null;
				Result = null;
			}

			public Request(uint count)
				: this (new Match(count))
			{}

			public static implicit operator Request(Match? match)
			{
				return new Request(match);
			}
		};

		public sealed class Result
		{
			public Match? Match;

			public Request tryMatch(Rule<InT> rule, Parser<InT>.State state)
			{
				return new Request(rule, state, this);
			}

		};
	}
}
