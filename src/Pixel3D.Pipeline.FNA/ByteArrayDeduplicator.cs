// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.Collections.Generic;

namespace Pixel3D.Pipeline
{
	public class ByteArrayDeduplicator
	{
		public readonly List<byte[]> arrays = new List<byte[]>();
		public readonly Dictionary<ComparableByteArray, int> indicies = new Dictionary<ComparableByteArray, int>();

		private int rawBytes, dedupBytes, rawCount, dedupCount;

		public int Add(byte[] originalData)
		{
			rawBytes += originalData.Length;
			rawCount++;

			int index;
			var comparableData = new ComparableByteArray(originalData);
			if (!indicies.TryGetValue(comparableData, out index))
			{
				index = arrays.Count;
				indicies.Add(comparableData, index);
				arrays.Add(originalData);

				dedupBytes += originalData.Length;
				dedupCount++;
			}

			return index;
		}
	}
}