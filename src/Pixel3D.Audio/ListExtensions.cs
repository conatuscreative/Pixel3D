using System.Collections.Generic;

namespace Pixel3D.Audio
{
	internal static class ListExtensions
	{
		public static void RemoveAtUnordered<T>(this List<T> list, int index)
		{
			int last = list.Count - 1;
			list[index] = list[last];
			list.RemoveAt(last);
		}
	}
}