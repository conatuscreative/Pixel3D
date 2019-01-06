// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Pixel3D.Animations;

namespace Pixel3D.Pipeline
{
	// TODO: Move out of Pipeline (this is used at runtime, not asset build time)
	// TODO: Once these are fixed, don't reference this assembly from engine

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
