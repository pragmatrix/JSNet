/**
	Generic parser implementation.
**/


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using JSNet.util;

namespace JSNet.parser
{
	/**
		Parser.
	**/

	sealed partial class Parser<InT>
	{
		readonly Maybe<InT> lineTerminator_;

		public Parser()
		{
		}

		public Parser(InT lineTerminator)
		{
			lineTerminator_ = lineTerminator;
		}

		/**
			Parse one production and return the count of input elements that have
			been consumed.

			If the target production has any delimiters set, all delimiters before 
			_and after_ the target production has been matched are consumed, too. 
		
			If the target production is not matched, nothing is consumed.
		**/

		public uint? parse(InT[] input, Production<InT> production)
		{
			State state = new State(new Control(input, production, lineTerminator_));

			// delimiter before is automatically consumed.

			uint? result = parseProduction(production, state);

			// delimiter after must be consumed explicitly!

			if (result != null && production.Delimiter != null)
			{
				uint? delimiterMatch;
				state += result.Value;

				Func<uint?, bool> canContinue = del => del != null && del.Value != 0;

				while (canContinue(delimiterMatch = parseProduction(production.Delimiter, state)))
				{
					result += delimiterMatch;
					state += delimiterMatch.Value;
				}
			}

			if (state.Control.MatchedElements != 0)
			{
				Log.D("First unmatched position: " + state.Control.getPosition(state.Control.MatchedElements));
				Log.D(new State(state.Control, state.Control.MatchedElements).ToLongString());
				Array.ForEach(state.Control.MatchedElementsProductions, p => Log.D("\t" + p.ToString()));
			}

#if DEBUG
			state.Stats.print();
#endif
			return result;
		}

		/**
			Parse any number of the productions.
		**/

		public void parseAny(InT[] input, Production<InT> production)
		{
			State state = new State(new Control(input, production, lineTerminator_));
			
			while(!state.IsEnd)
			{
				uint? count = parseProduction(production, state);

				if (count == null || count.Value == 0)
					break;
				
				state += count.Value;
			}

			if (!state.IsEnd)
				throw new Exception(state, "no match for " + state.ToString());

#if DEBUG
			state.Stats.print();
#endif
		}

		
		uint? parseProduction(Production<InT> production, State state)
		{
			// note: to support delimiters immediately, we need to call the IScope tryMatch 
			// implementation of the production
			Match? m = production.tryMatch(production, state);
			Log.D("==> final result of {0}: {1}", production, m);
			if (m == null)
				return null;

			return m.Value.Count;
		}

#if false

		/**
			Parse a single production and return the longest match.
		**/

		uint parseProduction(State state, Production<InT> production)
		{
			RunTable rt = new RunTable(state, production);

			while (rt.run()) ;

			Result[] results = rt.Results;

			if (results.Length == 0)
				return 0;

			uint length = 0;
			int index = 0;
			uint offset = state.Offset;

			for (int i = 0; i != results.Length; ++i)
			{
				Result r = results[i];
				uint l = r.State.Offset - offset;

				if (l > length)
				{
					length = l;
					index = i;
				}
			}

			return length;
		}			

		/**
			New non-recursive parser algorithm.
		**/

		/**
			A parsing node.
		**/

		enum NodeType
		{
			Regular,
			Production
		};

		class Node
		{
//			uint nestedCount_;
			
			/// Parent of this node
			public readonly Node Parent;
			public readonly NodeType Type;

			public readonly State State;
			public readonly Rule<InT>.Resolver Resolver;

			public bool HasParent { get { return Parent != null; } }
//			public bool HasNested { get { return nestedCount_ != 0; } }
			public bool IsProduction { get { return Type == NodeType.Production; } }

			protected Node(Node parent, NodeType type, State state, Rule<InT>.Resolver resolver)
			{
				Parent = parent;
				Type = type;
				State = state;
				Resolver = resolver;
			}

			public static Node createRoot(State state, Rule<InT>.Resolver resolver)
			{
				return new Node(null, NodeType.Regular, state, resolver);
			}

			public Node addNested(State state, Rule<InT>.Resolver resolver)
			{
				// ++nestedCount_;
				return new Node(this, NodeType.Regular, state, resolver);
			}

			public ProductionNode addProduction(State state, Production<InT> production, Rule<InT>.Resolver resolver)
			{
				// ++nestedCount_;
				ProductionNode node = new ProductionNode(this, state, production, resolver);

				ProductionNode conflicting = ProductionNode.find(this, state, production);
				if (conflicting != null)
				{
					conflicting.addBlocked(node);
					return null;
				}

				return node;
			}

			public void removeFromParent()
			{
				Debug.Assert(nestedCount_ == 0);

				if (Parent != null)
				{
					Debug.Assert(Parent.nestedCount_ != 0);
					--Parent.nestedCount_;
				}
			}

			public override string ToString()
			{
				return State.ToString() + " " + Resolver.ToString();
			}

		};

		sealed class ProductionNode : Node
		{
			// note: this is actually not the Production<InT> instead, it is 
			// the nested (first and only) rule of the production.

			readonly Production<InT> Production;

			/// A list of blocked, nested productions, that have the same state.
			/// This is used to handle left recursion!

			List<ProductionNode> blockedNested_;

			public ProductionNode(Node parent, State state, Production<InT> production, Rule<InT>.Resolver resolver)
				: base(parent, NodeType.Production, state, resolver)
			{
				Production = production;
			}

			public void addBlocked(ProductionNode blocked)
			{
				if (blockedNested_ == null)
					blockedNested_ = new List<ProductionNode>();

				blockedNested_.Add(blocked);
			}

			/**
				Return a parent production for the given state and the given rule, 
				starting at this node.
			**/

			public static ProductionNode find(Node current, State state, Production<InT> production)
			{
				while (current != null && current.State.Offset == state.Offset)
				{
					if (current.Type == NodeType.Production)
					{
						ProductionNode p = (ProductionNode)current;
						if (p.Production == production)
							return p;
					}

					current = current.Parent;
				}

				return null;
			}

		}


		/**
			A parse result.

			A leaf node (the one that caused the latest match) and the 
			state after the latest match!
		**/

		struct Result
		{
			public readonly Node Node;
			public readonly State State;

			public Result(Node node, State state)
			{
				Node = node;
				State = state;
			}
		}




		/// All leaf nodes currently processing, these are the runs.

		sealed class RunTable
		{
			List<Node> runs_ = new List<Node>();
			List<Result> results_ = new List<Result>();

			public Result[] Results
			{
				get
				{
					return results_.ToArray();
				}
			}
			
			public RunTable(State state, Production<InT> production)
			{
				runs_.Add(Node.createRoot(state, production.resolve()));
			}

			/**
				Processes one parsing run at the leaf nodes and writes the resulting
				nodes back to the leaf node list.

				Returns true if another run is required, false if parsing has ended.
			**/

			public bool run()
			{
				// todo: may reuse same list
				List<Node> newNodes = new List<Node>(runs_.Count);

				// process all leaf nodes
				foreach (Node current in runs_)
				{
					Rule<InT>.Resolver resolver = current.Resolver;

					switch (resolver.Cmd)
					{
						case Rule<InT>.Resolver.Command.Production:
							Debug.Assert(
								resolver.Rules != null &&
								resolver.Rules.Length == 2 &&
								resolver.ResolveMatch == null);

							// note productions may be blocked when they are added to the
							// node graph

							Node node = current.addProduction(current.State, (Production<InT>)resolver.Rules[0], resolver.Rules[1].resolve());
							if (node != null)
								newNodes.Add(node);
							// else blocked
							break;

						case Rule<InT>.Resolver.Command.Match:
						case Rule<InT>.Resolver.Command.Lookahead:
							foreach (Rule<InT> rule in resolver.Rules)
								newNodes.Add(current.addNested(current.State, rule.resolve()));
							break;

						case Rule<InT>.Resolver.Command.Fail:
							propagateMatch(current, newNodes, new Nothing());
							break; 

						case Rule<InT>.Resolver.Command.Succeed:
							Debug.Assert(
								resolver.Rules == null && 
								resolver.ResolveMatch == null);
							
							propagateMatch(current, newNodes, new Match<InT>(current.State, 0));
							break;
							
						case Rule<InT>.Resolver.Command.Immediate:
							Debug.Assert(
								resolver.Rules != null && 
								resolver.Rules.Length == 1 && 
								resolver.ResolveMatch == null);
							
							immediate(current, newNodes, resolver.Rules[0]);
							break;

					}
				}

				runs_ = newNodes;
				return runs_.Count != 0;
			}

			/**
				Run an immediate match and propagate it
			**/

			void immediate(Node current, List<Node> newNodes, Rule<InT> rule)
			{
				propagateMatch(current, newNodes, rule.immediateMatch(current.State));
			}


			/**
				Propagate the successful match up the node hierarchy.
			**/

			void propagateMatch(Node current, List<Node> newNodes, Maybe<Match<InT>> mbMatch)
			{
				Debug.Assert(current != null);

				Node matchNode = current;

				// note: the following loop would be much more logical to be
				// implemented recursively, but this would may cause too much stack for
				// some recursive rules.

				while (current.Parent!= null)
				{
					current = current.Parent;

					Rule<InT>.Resolver resolver = current.Resolver;

					switch (resolver.Cmd)
					{
						case Rule<InT>.Resolver.Command.Production:
							// production rule completed -> production completed, delegate unseen to parent



							break;

						case Rule<InT>.Resolver.Command.Match:
							
							if (!mbMatch.Valid)
								// match failed, propagate failure if we are 
								// not waiting for others to complete
								break;

							Match<InT> match = mbMatch.Value;

							if (resolver.ResolveMatch != null)
							{
								Rule<InT>.Resolver followUpResolver = resolver.ResolveMatch(match);
								newNodes.Add(current.Parent.addNested(match.AdvanceState, followUpResolver));
								return;
							}
							break;

						case Rule<InT>.Resolver.Command.Lookahead:
							Debug.Assert(resolver.ResolveMatch != null);

							Rule<InT>.Resolver followUpResolver2 = resolver.ResolveMatch(mbMatch);
							newNodes.Add(current.Parent.addNested(current.State, followUpResolver2));
							return;


						default:
							Debug.Assert(false);
							break;
					}
				} 

				// this match actually caused the root production to resolve!

				if (mbMatch.Valid)
					results_.Add(new Result(matchNode, mbMatch.Value.AdvanceState));
			}

#endif

	}

#if false

	sealed class Error : Exception
	{
		Node[] nodesInvolved_;

		public Error(string msg, Node[] nodesInvolved)
			: base(msg)
		{
			nodesInvolved_ = nodesInvolved;
		}

		public override string Message
		{
			get
			{
				StringBuilder msg = new StringBuilder();
				msg.Append(base.Message);

				if (nodesInvolved_ != null)
				{
					msg.Append("\nNodes involved:");

					foreach (Node node in nodesInvolved_)
					{
						Node current = node;
						string prefix = "\n  ";
						while (current != null)
						{
							msg.Append(prefix + current.ToString());
							prefix += "  ";
							current = current.Parent;
						};
					}
				}

				return msg.ToString();
			}
		}

	};
#endif
}
