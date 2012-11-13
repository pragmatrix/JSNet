using System.Collections.Generic;
using System.Diagnostics;
namespace JSNet.parser
{
	/**
		A rule.
	**/

	abstract partial class Rule<InT>
	{
		public delegate Match? MatchFunction(Parser<InT>.State state);

		public Rule()
		{
		}

		#region Rule Matching

		/**
			The actual match function of this rule!
		**/

		public abstract Match? doTryMatch(IScope context, Parser<InT>.State state);
		public abstract IEnumerable<Request> doRun(Parser<InT>.State state);

		#endregion

		#region Grammar Specification

		public static implicit operator Rule<InT>(string terminal)
		{
			return (Terminal<InT>)terminal;
		}

		public static implicit operator Rule<InT>(InT terminal)
		{
			return new ElementTerminal<InT>(terminal);
		}

		public static Rule<InT> operator |(Rule<InT> l, Rule<InT> r)
		{
			if (l is OneOf<InT>)
				return l.append(r);

			return new OneOf<InT>(l, r);
		}

		public static Rule<InT> operator +(Rule<InT> l, Rule<InT> r)
		{
			return new Sequence<InT>(l, r);
		}

		public Rule<InT> opt
		{
			get
			{
				return new OneOf<InT>(this, Rule<InT>.Empty);
			}
		}

		public Rule<InT> any
		{
			get
			{
				return new Any<InT>(this);
			}
		}

		public Rule<InT> butNot(Rule<InT> other)
		{
			return new ButNot<InT>(this, other);
		}

		public static Rule<InT> oneOf(params Rule<InT>[] others)
		{
			return new OneOf<InT>(others);
		}

		public Rule<InT> lookaheadNotOf(Rule<InT> notOf)
		{
			return new Sequence<InT>(this, new LookaheadNotOf<InT>(notOf));
		}

		public static Rule<InT> lookaheadNotOf1(Rule<InT> notOf)
		{
			return new LookaheadNotOf<InT>(notOf);
		}

		protected virtual Rule<InT> append(Rule<InT> r)
		{
			return null;
		}

		public static readonly Rule<InT> Empty = new Empty<InT>();
		
		#endregion

		#region Maintenance

		/**
			Return all direct dependencies of this rule.
		**/
		
		internal abstract IEnumerable<Rule<InT>> Dependencies { get; }
	
		#endregion
	};
}
