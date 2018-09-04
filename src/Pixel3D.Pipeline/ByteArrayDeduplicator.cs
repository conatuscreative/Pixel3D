using System.Collections.Generic;

namespace Pixel3D.Pipeline
{
	public class ByteArrayDeduplicator
    {
        public readonly Dictionary<ComparableByteArray, int> indicies = new Dictionary<ComparableByteArray, int>();
        public readonly List<byte[]> arrays = new List<byte[]>();

        int rawBytes, dedupBytes, rawCount, dedupCount;

        public int Add(byte[] originalData)
        {
            rawBytes += originalData.Length;
            rawCount++;

            int index;
            var comparableData = new ComparableByteArray(originalData);
            if(!indicies.TryGetValue(comparableData, out index))
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
