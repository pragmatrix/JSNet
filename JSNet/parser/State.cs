using System.Diagnostics;
using System.Collections.Generic;
using System;
using System.Text;
using JSNet.util;

namespace JSNet.parser
{
	sealed partial class Parser<InT>
	{
		/**
			Parser state
		**/

		public struct State
		{
			public readonly Control Control;
			public readonly uint Offset;

			InT[] Stream
			{
				get { return Control.Stream; }
			}

			public string Position
			{
				get
				{
					return Control.getPosition(Offset);
				}
			}

			public Statistics Stats
			{
				get
				{
					return Control.Stats;
				}
			}

			public State(Control control)
				: this(control, 0)
			{ }

			public State(Control control, uint offset)
			{
				Control = control;
				Offset = offset;
			}

			public InT this[uint offset]
			{
				get
				{
					Debug.Assert(offset >= 0);
					uint o = Offset + offset;
					if (o >= Stream.Length)
						throw new ArgumentOutOfRangeException("accessing state @" + Offset);
					return Stream[(int)o];
				}
			}

			/**
				Returns true if the number of elements are available.
			**/

			public bool available(int count)
			{
				// note: zero is ok for symmetry (Match constructor may check using 0)
				Debug.Assert(count >= 0);

				return available((uint)count);
			}

			public bool available(uint count)
			{
				uint b = Offset + count;
				return b <= Stream.Length;
			}

			public bool IsEnd
			{
				get
				{
					return Offset == Stream.Length;
				}
			}

			public static State operator +(State state, Match? m)
			{
				Debug.Assert(m != null);
				return state + m.Value.Count;
			}

			public static State operator +(State state, uint count)
			{
				Debug.Assert(state.available(count));
				return new State(state.Control, state.Offset + count);
			}

			public static bool operator ==(State l, State r)
			{
				return l.Offset == r.Offset && r.Control == l.Control;
			}

			public static bool operator !=(State l, State r)
			{
				return !(l == r);
			}

			public override bool Equals(object obj)
			{
				return (obj is State) && (State)obj == this;
			}

			public override int GetHashCode()
			{
				return Offset.GetHashCode() ^ Control.GetHashCode();
			}

			public override string ToString()
			{
				return ToShortString();
			}

			public string ToShortString()
			{
				return ToString(0, 16);
			}

			public string ToLongString()
			{
				return ToString(16, 32);
			}

			public string ToString(uint pre, uint post)
			{
				uint showBefore = Math.Min(pre, Offset);
				uint showAfter = Math.Min(post, (uint)Stream.Length - Offset);

				StringBuilder sb = new StringBuilder();

				sb.Append("@" + Offset + ": '");

				for (uint i = Offset - showBefore; i != Offset; ++i)
					sb.Append(makePrintable(Stream[i]));

				sb.Append("|->");

				for (uint i = Offset; i != Offset + showAfter; ++i)
					sb.Append(makePrintable(Stream[i]));

				sb.Append("'");

				return sb.ToString();
			}

			static string makePrintable(object obj)
			{
				if (obj == null)
					return "null";

				if (obj is char)
				{
					char c = (char)obj;
					if (c.CharacterCategory() == CharacterCategory.Other)
					{
						return string.Format("%{0:x4}", (uint)c);
					}
				}

				return obj.ToString();
			}

		}
	}
}
