// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.Collections.Generic;

namespace Pixel3D.StateManagement
{
	/// <summary>Thin wrapper around List that, unlike ReadOnlyCollection, does not allocate.</summary>
	/// <remarks>Could implement IList... but that just invites boxing.</remarks>
	public struct ReadOnlyList<T>
	{
		private readonly List<T> list;

		public ReadOnlyList(List<T> list)
		{
			this.list = list;
		}

	    public int Count
	    {
	        get { return list.Count; }
	    }

	    public T this[int index]
	    {
	        get { return list[index]; }
	    }

		// List already has a perfectly serviceable non-allocating enumerator:
		public List<T>.Enumerator GetEnumerator()
		{
			return list.GetEnumerator();
		}
	}
}