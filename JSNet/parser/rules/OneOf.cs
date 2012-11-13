using System;
using System.Collections.Generic;

namespace JSNet.parser
{
	/**
		One of rule, match the first one (PEG).
	**/

	sealed class OneOf<InT> : Rule<InT>
	{
		readonly Rule<InT>[] rules_;

		public OneOf(params Rule<InT>[] rules)
		{
			rules_ = rules;
		}

		public override Match? doTryMatch(IScope context, Parser<InT>.State state)
		{
			foreach (var rule in rules_)
			{
				Match? match = context.tryMatch(rule, state);
				if (match != null)
					return match;

				// this reduces the number of tryMatch calls by some percent
				if (state.Control.FailAllRules)
					return null;
			}

			return null;
		}

		public override IEnumerable<Rule<InT>.Request> doRun(Parser<InT>.State state)
		{
			var res = new Result();

			foreach (var rule in rules_)
			{
				yield return res.tryMatch(rule, state);
				if (res.Match != null)
				{
					yield return res.Match;
					yield break;
				}
			}

			yield return null;
		}

		#region Construction Helper

		protected override Rule<InT> append(Rule<InT> r)
		{
			Rule<InT>[] rules = new Rule<InT>[rules_.Length + 1];
			Array.Copy(rules_, rules, rules_.Length);
			rules[rules_.Length] = r;

			return new OneOf<InT>(rules);
		}
		
		#endregion

		public override string ToString()
		{
			return "one of " + string.Join(" ",
					Array.ConvertAll<Rule<InT>, string>(rules_, rule => rule.ToString()));
		}

		internal override IEnumerable<Rule<InT>> Dependencies
		{
			get 
			{
				return rules_;
			}
		}

	}

}
