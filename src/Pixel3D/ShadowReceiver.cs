using System;

namespace Pixel3D
{
    // TODO: Use a different texture format for shadow receivers (Bgra4444?)
    //       Could use Alpha8 on HiDef. But that's smaller (could hide memory usage during development).
    //       Bgra4444 loses precision. But we aren't using it anyway? (Could do 1-bit alpha.)

    /// <summary>Information for a Cel that receives shadows</summary>
    public class ShadowReceiver
    {
        public ShadowReceiver(Heightmap heightmap, Oblique heightmapExtendDirection)
        {
            if (heightmap == null)
                throw new ArgumentNullException("heightmap");
            this.heightmap = heightmap;
            this.heightmapExtendDirection = heightmapExtendDirection;
        }

        public ShadowReceiver(byte height)
        {
            heightmap = new Heightmap(height); // No data heightmap (single height)
        }

        public readonly Heightmap heightmap;
        public Oblique heightmapExtendDirection;

		public ShadowReceiver Clone()
        {
            return new ShadowReceiver(heightmap.Clone(), heightmapExtendDirection);
        }
    }
}
