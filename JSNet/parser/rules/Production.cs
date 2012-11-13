// #define NEW_LR

using System.Collections.Generic;
using System.Diagnostics;
using JSNet.util;
using System.Linq;
using System;


namespace JSNet.parser
{
	/**
		A production specification.
	**/

	sealed class Production<InT> : Rule<InT>, Rule<InT>.IScope
	{
		readonly string name_;
		
		// not readonly, may be initialized later.
		Rule<InT> rule_;

		// (optional) delimiter production!
		Production<InT> delimiter_;

		// The cache for final matches.
		// todo: optimize: array would be preferable!
		readonly Dictionary<uint, Match?> matches_ = new Dictionary<uint, Match?>();

		readonly Dictionary<uint, Match> lrMatches_ = new Dictionary<uint, Match>();

		enum Cmd
		{
			/**
				Left recursion has happened. We fail the current production and
				shift the state in case the production succeeds, then another 
				evaluation is put on the stack.
			**/

			LeftRecursion,

			/**
				Regular recursion detected:

				1. We fail all productions until the root production is reached.
				2. We reuse that state and process it.

				state of Command contains the state at which the recursion happened.
			**/
	
			
			RegularRecursion
		};

		struct Command
		{
			public Command(Cmd cmd, Parser<InT>.State state)
			{
				Cmd = cmd;
				State = state;
			}

			public readonly Cmd Cmd;
			public readonly Parser<InT>.State State;
		}

		public Production(string name)
		{
			name_ = name;
		}


		/**
			Reset production parsing state.
		**/

		public void reset()
		{
			matches_.Clear();
			lrMatches_.Clear();

			processingStack_.Clear();
			active_ = false;
			leftRecursion_ = false;
			regularRecursionState_ = null;
		}

		#region Main Parsing Algorithm (yes, that's it)

		//
		// state
		//

		// this is the processing stack for this production
		readonly Stack<Parser<InT>.State> processingStack_ = new Stack<Parser<InT>.State>();

		// Is this production currently in evaluation?
		bool active_;

		// Left recursion detected?
		bool leftRecursion_;

		// Regular recursion detected?
		Parser<InT>.State? regularRecursionState_;

		// the current state, because we prevent recursion this is not changed in a tryMatch below
		Parser<InT>.State currentState_;

		public override Match? doTryMatch(IScope context, Parser<InT>.State state)
		{
			// we cannot be called in fail all rules state!
			Debug.Assert(!state.Control.FailAllRules);

			// immediately return match if any is available
			Match? match;
			if (matches_.TryGetValue(state.Offset, out match))
			{
				state.Stats.notifyCacheHit();

				logT("--| {0} @ {1}: returning match from cache: '{2}'", state.Position, this, toString(match));
				return match;
			}

			state.Stats.notifyCacheMiss();

			if (active_)
				return handleRecursion(context, state);

			uint initialOffset = state.Offset;

			active_ = true;

			for(;;)
			{
				// be optimistic (recursive evaluations may prevent finishing the match)

				logT("+++ {0} @ {1} beginning evaluation {2} pending evaluations: {3}", state.Position, this, state, processingStack_.Count);

				// must set current state to detect left recursion
				currentState_ = state;
				// note: use production context to match rule_
				match = rule_.doTryMatch(this, state);
				Debug.Assert(state == currentState_);
				match = interpretCommand(state, match);

				// we were not able to remove the FailAllRules flag, then fail.
				if (state.Control.FailAllRules)
				{
					Debug.Assert(match == null);
					processingStack_.Clear();
				}

				if (processingStack_.Count == 0)
					break;

				state = processingStack_.Pop();
			}

			Debug.Assert(state.Control.FailAllRules || state.Offset == initialOffset);

			// first match on stack is final match.

			active_ = false;

			// cleanup (todo: make us state less again and move control away from Production)
			// note: cleaning up the matches drops our cache hits by > 10 % and increases processing
			// by >20%
			// matches_.Clear();

			// either the command stack is empty or there is exactly one left recursion, which we will continue
			// on

			if (match != null)
				logD("*** " + state.Position + " -> matched: " + match.Value.Count + " " + state.ToShortString() + " --> " + this.ToString());

			return match;
		}

		#endregion

		Match? handleRecursion(IScope scope, Parser<InT>.State state)
		{
			Debug.Assert(active_);

			logT("--| {0} @ {1}: no match yet, recursing", state.Position, this);
			// no match yet, but recursed, push on stack caller needs to resolve that.

			if (currentState_ == state)
			{

				// note: multiple left recursions may happen!
				leftRecursion_ = true;

				Match lrMatch;
				if (lrMatches_.TryGetValue(state.Offset, out lrMatch))
				{
					logT("    left recursion: returning previous match for left recursion: {0}", lrMatch);
					return lrMatch;
				}
				else
					logT("    left recursion: no previous result for this evaluation");
			}
			else
			{
				logT("    regular recursion, failing all rules");

				// note: regular recursion can happen only once per rule, 
				// we fail all rules now

				Debug.Assert(!state.Control.FailAllRules);
				Debug.Assert(regularRecursionState_ == null);
				regularRecursionState_ = state;

				// fail all rules now, don't allow any entry stored to cache,
				// until someone pops the command
				state.Control.FailAllRules = true;
			}

			// in both cases we fail.
			return null;
		}

		void storeMatch(Parser<InT>.State state, Match? match)
		{
			Debug.Assert(!matches_.ContainsKey(state.Offset));
			Debug.Assert(!state.Control.FailAllRules);

			logT("--- {0} @ {1}: storing match: {2}", state.Position, this, toString(match));
			matches_[state.Offset] = match;

			if (match == null)
				return;

			state.Control.recordSuccessfulMatch(state.Offset, match.Value.Count, this);
		}

		Match? interpretCommand(Parser<InT>.State currentState, Match? currentMatch)
		{
			bool leftRecursion = leftRecursion_;
			bool regularRecursion = regularRecursionState_ != null;

			if (!leftRecursion && !regularRecursion)
			{
				if (!currentState.Control.FailAllRules)
					storeMatch(currentState, currentMatch);
				else
					Debug.Assert(currentMatch == null);

				return currentMatch;
			}

			// left recursion always happens first, but regular recursion has highest 
			// handling priority

			if (regularRecursion)
			{
				Debug.Assert(currentState.Control.FailAllRules && currentMatch == null);
				currentState.Control.FailAllRules = false;

				logT("--- seen recursion, pushing {0} and {1} on stack", currentState, regularRecursionState_.Value);

				processingStack_.Push(currentState);
				processingStack_.Push(regularRecursionState_.Value);
				regularRecursionState_ = null;

				// ignore left recursion for now
				leftRecursion_ = false;

				return null;
			}

			if (!leftRecursion)
				return null;

			// no other recursion? handle left recursion

			leftRecursion_ = false;

			if (currentState.Control.FailAllRules)
			{
				Debug.Assert(regularRecursionState_ == null);
				return null;
			}

			Match lrMatch;
			if (!lrMatches_.TryGetValue(currentState.Offset, out lrMatch))
			{
				// initial left recursion detected
				if (currentMatch != null && currentMatch.Value.Count != 0)
				{
					logT("--- {0} @ {1}: seen initial left recursion: matched {2}", currentState.Position, this, currentMatch);

					lrMatches_[currentState.Offset] = currentMatch.Value;
					processingStack_.Push(currentState);
				}
				else
				{
					// else initial left recursion, but null or 0 match, store it and return it.
					storeMatch(currentState, currentMatch);
					return currentMatch;
				}

				return null;
			}

			// if we had no match for the rest of the clause, or a zero match, we return
			if (currentMatch == null || currentMatch.Value.Count <= lrMatch.Count)
			{
				logT("--- {0} @ {1}: seen left recursion but no better match: previous {2}, current {3}", currentState.Position, this, lrMatch, currentMatch);

				// store final match and return it!
				// note: we cannot do any reasoning about intermediate matches!
				storeMatch(currentState, lrMatch);
				return lrMatch;
			}
			else
			{
				// longer match, reevaluate again
				logT("--- {0} @ {1}: seen left recursion and better match before: {2} current: {3}", currentState.Position, this, lrMatch, currentMatch);
				lrMatches_[currentState.Offset] = currentMatch.Value;

				processingStack_.Push(currentState);
			}

			return null;
		}

		#region SubRule matching

		/**
			This method is called when a non-production sub rule shall be matched.

			todo: We might use a custom tryMatch functor again to implement subrule matching!
			todo: Evaluate if it is possible to put the delimiter in an "artificial" sequence 
				  as a prefix to each production that is run (the code below... in case of a
				  delimiter looks exactly like a sequence).
		**/

		public Match? tryMatch(Rule<InT> rule, Parser<InT>.State state)
		{
			if (state.Control.FailAllRules)
				return null;

			if (delimiter_ == null)
				return rule.doTryMatch(this, state);

			// first must match delimiter.
			Match? delimiterMatch = delimiter_.tryMatch(delimiter_, state);
			if (delimiterMatch == null)
				return null; // delimiter not matched, cannot match!

			state += delimiterMatch;

			Match? ruleMatch = rule.doTryMatch(this, state);
			if (ruleMatch == null)
				return null;

			return new Match(delimiterMatch.Value.Count + ruleMatch.Value.Count);
		}

		#endregion

		public override IEnumerable<Request> doRun(Parser<InT>.State state)
		{
			throw new NotImplementedException();
		}


		#region Construction Helper

		public Rule<InT> Rule
		{
			set
			{
				rule_ = value;		
			}
		}

		public Production<InT> Delimiter
		{
			set
			{
				delimiter_ = value;
			}
			get
			{
				return delimiter_;
			}

		}


		#endregion

		#region Dependency Management

		internal override IEnumerable<Rule<InT>> Dependencies
		{
			get
			{
				yield return rule_;
				if (delimiter_ != null)
					yield return delimiter_;
			}
		}

		#endregion


		public override string ToString()
		{
			// don't go into _rule, we don't know if this recurses to us again!
			return name_;
		}

		#region Logging Helper

		[Conditional("DEBUG")]
		void logD(string format, params object[] values)
		{
#if !TEST_LEXER
			if (delimiter_ != null)
#endif
				Log.D(format, values);
		}

		[Conditional("TRACE")]
		void logT(string format, params object[] values)
		{
#if !TEST_LEXER
			if (delimiter_ != null)
		//	if (name_ == "SourceElements")

#endif
				Log.T(format, values);
		}

		#endregion

		static string toString(Match? match)
		{
			if (match == null)
				return "null";
			else
				return match.Value.ToString();
		}


	};

}
