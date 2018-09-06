using System;
using Pixel3D.Animations;

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
	        this.heightmap = heightmap ?? throw new ArgumentNullException(nameof(heightmap));
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
		
        #region Serialize

        public void Serialize(AnimationSerializeContext context)
        {
            heightmap.Serialize(context);
            context.bw.Write(heightmapExtendDirection);
        }

        /// <summary>Deserialize into new object instance</summary>
        public ShadowReceiver(AnimationDeserializeContext context)
        {
            heightmap = new Heightmap(context);
            heightmapExtendDirection = context.br.ReadOblique();
        }
		
        #endregion
    }
}
