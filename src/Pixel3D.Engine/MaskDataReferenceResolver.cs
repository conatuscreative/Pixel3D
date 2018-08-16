using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Pixel3D.Animations;

namespace Pixel3D
{
    public class MaskDataReferenceResolver : ICustomMaskDataReader
    {
        public MaskDataReferenceResolver(List<uint[]> packedDataArrays, BinaryReader br)
        {
            this.packedDataArrays = packedDataArrays;
            this.br = br;
        }

        BinaryReader br;
        List<uint[]> packedDataArrays;

        public uint[] Read(int length)
        {
            int index = br.ReadInt32();
            Debug.Assert(packedDataArrays[index].Length == length);
            return packedDataArrays[index];
        }
    }
}
