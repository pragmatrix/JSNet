using System.Collections.Generic;

using JSNet.util;

namespace JSNet.parser
{

	/**
		ButNot
	**/

	sealed class ButNot<InT> : Rule<InT>
	{
		readonly Rule<InT> rule_;
		readonly Rule<InT> butNot_;

		public ButNot(Rule<InT> rule, Rule<InT> butNot)
		{
			rule_ = rule;
			butNot_ = butNot;
		}

		public override Match? doTryMatch(IScope context, Parser<InT>.State state)
		{
			Match? first = context.tryMatch(rule_, state);
			if (first == null) 
				return null;

			Match? second = context.tryMatch(butNot_, state);
			if (state.Control.FailAllRules)
				return null;

			// I assume butNot must also match the left side's length!
			// for example Identifier.butNot(Keyword) where Identifier matches 
			// "document" but keyword matches "do".

			if (second != null && first.Value.Count == second.Value.Count)
				return null;

			return first;
		}

		public override IEnumerable<Request> doRun(Parser<InT>.State state)
		{
			var res = new Result();

			yield return res.tryMatch(rule_, state);

			if (res.Match != null)
			{
				Match firstMatch = res.Match.Value;

				yield return res.tryMatch(butNot_, state);
				if (res.Match == null || firstMatch.Count != res.Match.Value.Count)
				{
					yield return firstMatch;
					yield break;
				}
			}

			yield return null;
		}

		internal override IEnumerable<Rule<InT>> Dependencies
		{
			get 
			{
				yield return rule_;
				yield return butNot_;
			}
		}

	}
}
