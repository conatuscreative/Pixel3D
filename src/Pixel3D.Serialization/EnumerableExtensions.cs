// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pixel3D.Serialization
{
	public static class EnumerableExtensions
	{
		/// <summary>
		///     Sort a given enumeration in a fixed order, required for anything sent over the network in a fixed order
		///     (eg: object fields in generated serializers) or by an assigned ID number (eg: dynamic type dispatch).
		/// </summary>
		public static IEnumerable<T> NetworkOrder<T>(this IEnumerable<T> enumerator, Func<T, string> getName)
		{
			return enumerator.OrderBy(getName, StringComparer.Ordinal);
		}
	}
}