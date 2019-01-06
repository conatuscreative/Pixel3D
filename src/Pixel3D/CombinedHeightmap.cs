// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.Collections.Generic;

namespace Pixel3D
{
    public interface IHeightmapObject
    {
        Heightmap Heightmap { get; }
        Position Position { get; }
        bool FlipX { get; }
    }
	
    public class CombinedHeightmap
    {
        public Heightmap levelHeightmap;

        public List<IHeightmapObject> heightmapObjects = new List<IHeightmapObject>();
    }
}
