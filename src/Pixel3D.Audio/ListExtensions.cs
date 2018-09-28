// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.Collections.Generic;

namespace Pixel3D.Audio
{
	internal static class ListExtensions
	{
		public static void RemoveAtUnordered<T>(this List<T> list, int index)
		{
			var last = list.Count - 1;
			list[index] = list[last];
			list.RemoveAt(last);
		}
	}
}