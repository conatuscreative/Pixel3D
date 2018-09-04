using System;
using Pixel3D.Animations;

namespace Pixel3D.Pipeline
{
    /// <summary>
    /// Intercept data from the capture stream and replace it with a mask ID
    /// </summary>
    public class MaskReadAndDeduplicate : ICustomMaskDataReader
    {
        private readonly CaptureStream readStream;
        private readonly ByteArrayDeduplicator byteArrayDeduplicator;

        public MaskReadAndDeduplicate(CaptureStream readStream, ByteArrayDeduplicator byteArrayDeduplicator)
        {
            this.readStream = readStream;
            this.byteArrayDeduplicator = byteArrayDeduplicator;
        }

        public uint[] Read(int length)
        {
            // Pull out the original data for the mask:
            byte[] originalData = new byte[length*4];
            readStream.StreamToCapture.Read(originalData, 0, length*4);

            // Insert it into the mask data deduplicator:
            int id = byteArrayDeduplicator.Add(originalData);

            // Write out the deduplicated ID:
            readStream.CaptureTarget.Write(BitConverter.GetBytes(id), 0, 4);

            // Return some garbage to keep our caller quiet...
            return new uint[length];
        }
    }
}
