using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
