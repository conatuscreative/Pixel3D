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


        #region Serialization

        const int beforeVersion17WorldPhysicsMaximumHeight = 10000;


        public virtual void Serialize(LevelSerializeContext context)
        {
            Debug.Assert(!Asserts.enabled || mask.Valid);
            mask.Serialize(context.bw);

            if (context.Version >= 15)
            {
                context.bw.Write(startY);

                if(context.Version < 17 && endY != beforeVersion17WorldPhysicsMaximumHeight)
                    context.bw.Write(endY-1); // <- Old version had an inclusive upper bound
                else
                    context.bw.Write(endY);
            }

            if(!context.monitor)
                regionIndex = context.nextRegionIndex++;
        }


        protected void Deserialize(LevelDeserializeContext context)
        {
			mask = context.br.DeserializeMaskData(context.FastReadHack);

            if (context.Version >= 15)
            {
                startY = context.br.ReadInt32();
                endY = context.br.ReadInt32();

                if(context.Version < 17 && endY != beforeVersion17WorldPhysicsMaximumHeight)
                    endY++; // <- Old version had an inclusive upper bound
            }
            else
            {
                startY = 0;
                endY = WorldPhysics.MaximumHeight;
            }

            regionIndex = context.nextRegionIndex++;
        }


        /// <summary>Deserialize into new object instance</summary>
        public Region(LevelDeserializeContext context)
        {
            Deserialize(context);
        }


        #endregion

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