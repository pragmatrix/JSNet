/**
	The parser control is holding the parser's input stream and the
	parser's table.
**/

using System.Collections.Generic;
using JSNet.util;
using System.Linq;
using System;

namespace JSNet.parser
{
	sealed partial class Parser<InT>
	{
		internal sealed class Control
		{
			public readonly InT[] Stream;
			readonly Production<InT>[] productions_;

			// Global flag to fail all rules
			public bool FailAllRules;

			uint matchedElements_;
			readonly List<Production<InT>> matchedElementsProductions_ = new List<Production<InT>>();

			// Global entry to indicate the maximum number of elements 
			// that have been matched so far.
			public uint MatchedElements
			{
				get
				{
					return matchedElements_;
				}

			}

			// The productions that matched (at first) up to MatchElements
			// (the most significan first)

			public Production<InT>[] MatchedElementsProductions
			{
				get
				{
					return matchedElementsProductions_.ToArray();
				}
			}

			// todo: read this directly from the various memoization tables if required
			// (this information is only required at the end of parsing and so should not
			// slow down performance)

			public void recordSuccessfulMatch(uint offset, uint match, Production<InT> production)
			{
				uint end = offset + match;
				if (end < matchedElements_)
					return;

				if (end > matchedElements_)
				{
					matchedElements_ = end;
					matchedElementsProductions_.Clear();
				}

				matchedElementsProductions_.Add(production);
			}

			util.Maybe<InT> lineTerminator_;
			public readonly Statistics Stats;
			
			public Control(InT[] stream, Production<InT> root, util.Maybe<InT> lineTerminator)
			{
				Stream = stream;
				productions_ = 
					Closure.build((Rule<InT>)root, p => p.Dependencies)
					.OfType<Production<InT>>()
					.ToArray();

				Stats = new Statistics((ulong)stream.Length, (uint) productions_.Length);
	
				lineTerminator_ = lineTerminator;
				positions_.Add(new Position { Column = 1, Line = 1 });

				setup();
			}

			void setup()
			{
				Array.ForEach(productions_, p => p.reset());
			}

			#region Position

			struct Position
			{
				public uint Line;
				public uint Column;

				public override string ToString()
				{
					return string.Format("{0},{1:d2}", Line, Column);
				}
			}

			List<Position> positions_ = new List<Position>();

			public string getPosition(uint offset)
			{
				if (!lineTerminator_.Valid)
					return offset.ToString();
				
				while (positions_.Count < offset+1)
				{
					Position pos = positions_[positions_.Count - 1];
					if (lineTerminator_.Value.Equals(Stream[positions_.Count - 1]))
					{
						++pos.Line;
						pos.Column = 1;
					}
					else
						++pos.Column;

					positions_.Add(pos);
				}

				return offset.ToString() + " " + positions_[(int)offset].ToString();
			}

			#endregion

		};
	}
}
