// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Pixel3D.Serialization.Context
{
	internal class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
	{
		private ReferenceEqualityComparer()
		{
		} // <- no external instancing

		public static ReferenceEqualityComparer<T> Instance { get; private set; }

	    static ReferenceEqualityComparer()
	    {
	        Instance = new ReferenceEqualityComparer<T>();
	    }

		public bool Equals(T x, T y)
		{
			return ReferenceEquals(x, y);
		}

		public int GetHashCode(T obj)
		{
			return RuntimeHelpers.GetHashCode(obj);
		}
	}
}