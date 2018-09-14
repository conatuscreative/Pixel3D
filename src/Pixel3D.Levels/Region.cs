using System.Diagnostics;
using Pixel3D.Animations;
using Pixel3D.Physics;

namespace Pixel3D.Engine.Levels
{
    public class Region
    {
        // Provided to allow parameterless construction (due to presence of deserialization constructor)
        public Region() { }

        /// <summary>The region in the XZ (floor) plane where the mask is active</summary>
        public MaskData mask;

        public int startY;
        public int endY = WorldPhysics.MaximumHeight;

        /// <summary>Index into navigation info for the given region (expect serialization to set these correctly)</summary>
        public int regionIndex;

		public bool Contains(Actor subject)
        {
            return Contains(subject.position);
        }

        public bool Contains(Position position)
        {
            return position.Y >= startY && position.Y < endY && mask.GetOrDefault(position.X, position.Z);
        }

        public bool ContainsXZ(int x, int z)
        {
            if (mask.GetOrDefault(x, z))
            {
                return true;
            }
            return false;
        }
    }
}