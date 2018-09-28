// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Pixel3D.Strings
{
	/// <summary>
	///     So we don't ever have to return a list...
	/// </summary>
	public struct StringList : IEnumerable<string>
	{
		public StringList(List<string> stringList, StringBank.StringRange stringRange)
		{
			list = stringList;
			range = stringRange;
		}

		private readonly List<string> list;
		private readonly StringBank.StringRange range;

	    public int Count
	    {
	        get { return range.count; }
	    }

	    public string this[int index]
		{
			get
			{
				if ((uint) index < range.count)
					return list[range.start + index];
				throw new ArgumentOutOfRangeException();
			}
		}


		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}

		public struct Enumerator : IEnumerator<string>
		{
			private int i;
			private readonly List<string> list;
			private readonly StringBank.StringRange range;

			public Enumerator(StringList owner) : this()
			{
				i = 0;
				Current = null;
				list = owner.list;
				range = owner.range;
			}

			public void Reset()
			{
				i = 0;
				Current = null;
			}

			public void Dispose()
			{
			}

			public string Current { get; private set; }

		    object IEnumerator.Current
		    {
		        get { return Current; }
		    }

		    public bool MoveNext()
			{
				if (i < range.count)
				{
					Current = list[range.start + i];
					i++;
					return true;
				}

				return false;
			}
		}
	}
}