using System;
using System.Diagnostics;
using Pixel3D.Engine.Levels;

namespace Pixel3D.Engine.Navigation
{
    [Flags]
    public enum EdgeType : ushort
    {
        // NOTE: These are flags because the pathfinding routine rejects edges with a mask
        // IMPORTANT: This type is serialized by value (do not change or remove values without versioning in serializer)

        None = 0,

        Run = 1,

        Jump = 16,
        Drop = 32,

        Climb = 64,
        LongJump = 128,

        Invalid = 0xFF,
    }

    // I wish this struct were smaller... -AR
    public struct NavEdge
    {
        public DirectionNumber direction;
        public EdgeType type;
        public byte nextSector;

        public NavRegion region;


        #region Serialization

        public void Serialize(LevelSerializeContext context)
        {
            if(context.Version < 19)
                throw new Exception("Cannot write backwards-compatible level before version 19");
            else
            {
                context.bw.Write((byte)direction);
                context.bw.Write((byte)type);
            }

            context.bw.Write(nextSector);
            context.bw.Write(region.startX);
            context.bw.Write(region.startZ);
            context.bw.Write(region.endX);
            context.bw.Write(region.endZ);
        }

        public NavEdge(LevelDeserializeContext context)
        {
            if(context.Version < 19)
            {
                // NOTE: Legacy bits were: North, South, East, West, Jump, Drop, Climb
                int oldTypeValue = context.br.ReadByte();
                const int oldDirectionBits = 1|2|4|8;
                switch(oldTypeValue & oldDirectionBits)
	            {
                    case 1: direction = DirectionNumber.North; break;
                    case 2: direction = DirectionNumber.South; break;
                    case 4: direction = DirectionNumber.East;  break;
                    case 8: direction = DirectionNumber.West;  break;
		            default:
                        direction = 0; // <- garbage, but easy to re-generate
                        Debug.Assert(false, "Nonsense direction value, need to regenerate level file");
                        break;
	            }

                type = (EdgeType)(oldTypeValue & ~oldDirectionBits);
            }
            else
            {
                direction = (DirectionNumber)context.br.ReadByte();
                type = (EdgeType)context.br.ReadByte();
            }
            
            nextSector = context.br.ReadByte();
            region.startX = context.br.ReadInt32();
            region.startZ = context.br.ReadInt32();
            region.endX = context.br.ReadInt32();
            region.endZ = context.br.ReadInt32();
        }

        #endregion

    }


    public struct EdgeRange
    {
        public int index;
        public int count;
    }

}
