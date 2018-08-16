using System;
using System.Collections;
using System.Collections.Generic;

namespace Pixel3D.Engine.Strings
{
	/// <summary>
	/// So we don't ever have to return a list...
	/// </summary>
	public struct StringList : IEnumerable<string>
	{
		public StringList(List<string> stringList, StringBank.StringRange stringRange)
		{
			this.list = stringList;
			this.range = stringRange;
		}

		private List<string> list;
		private StringBank.StringRange range;

		public int Count { get { return range.count; } }

		public string this[int index]
		{
			get
			{
				if((uint)index < range.count)
					return list[range.start + index];
				else
					throw new ArgumentOutOfRangeException();
			}
		}


		IEnumerator<string> IEnumerable<string>.GetEnumerator() { return GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		public Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}

		public struct Enumerator : IEnumerator<string>
		{
			int i;
			string current;
			List<string> list;
			StringBank.StringRange range;

			public Enumerator(StringList owner)
			{
				i = 0;
				current = null;
				list = owner.list;
				range = owner.range;
			}

			public void Reset()
			{
				i = 0;
				current = null;
			}

			public void Dispose() { }
			public string Current { get { return current; } }
			object IEnumerator.Current { get { return current; } }

			public bool MoveNext()
			{
				if(i < range.count)
				{
					current = list[range.start + i];
					i++;
					return true;
				}
				return false;
			}
		}

	}
}