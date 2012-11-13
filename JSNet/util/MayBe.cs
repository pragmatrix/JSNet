/**
	A nullable (maybe) type which can be used on reference and value types.

	(we would use Nullable<T> if reference types would be possible).
**/

using System.Diagnostics;

namespace JSNet.util
{
	public struct Nothing
	{ };
	
	public struct Maybe<ValueT>
	{
		public readonly bool Valid;
		ValueT value_;

		public ValueT Value
		{
			get
			{
				Debug.Assert(Valid);
				return value_;
			}
		}

		public Maybe(ValueT value)
		{
			Valid = true;
			value_ = value;
		}

		public override bool Equals(object obj)
		{
			Maybe<ValueT> r = (Maybe<ValueT>)obj;
			return this == r;
		}

		public override int GetHashCode()
		{
			if (!Valid)
				return Valid.GetHashCode();

			// don't think that it makes sense to eor the has code
			// of the true here

			return value_.GetHashCode();
		}

		public override string ToString()
		{
			return Valid ? value_.ToString() : "invalid";
		}

		public static bool operator == (Maybe<ValueT> l, Maybe<ValueT> r)
		{
			if (l.Valid != r.Valid)
				return false;
			if (!l.Valid)
				return true;

			return object.Equals(l.Value, r.Value);
		}

		public static bool operator !=(Maybe<ValueT> l, Maybe<ValueT> r)
		{
			return !(l == r);
		}

		public static implicit operator Maybe<ValueT>(ValueT value)
		{
			return new Maybe<ValueT>(value);
		}

		public static implicit operator Maybe<ValueT>(Nothing nothing)
		{
			return new Maybe<ValueT>();
		}
	}
}
