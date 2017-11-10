using System;

namespace Pixel3D.Navigation
{
    /// <summary>Flags for eight possible directions</summary>
    [Flags]
    public enum DirectionFlags : byte
    {
        East      = (byte)1u << 0,
        NorthEast = (byte)1u << 1,
        North     = (byte)1u << 2,
        NorthWest = (byte)1u << 3,
        West      = (byte)1u << 4,
        SouthWest = (byte)1u << 5,
        South     = (byte)1u << 6,
        SouthEast = (byte)1u << 7,

        None = 0,
        All = 0xFF
    }


    public static class DirectionFlagsExtensions
    {
        public static DirectionFlags GetFlagsTowardsRegion(int x, int z, int regionStartX, int regionEndX, int regionStartZ, int regionEndZ)
        {
            DirectionFlags directionX;
            DirectionFlags directionZ;

            if(x < regionStartX)
                directionX = DirectionFlags.East | DirectionFlags.NorthEast | DirectionFlags.SouthEast;
            else if(x >= regionEndX)
                directionX = DirectionFlags.West | DirectionFlags.NorthWest | DirectionFlags.SouthWest;
            else
                directionX = DirectionFlags.All; // don't really care

            DirectionFlags directionXMask = directionX | (DirectionFlags.North | DirectionFlags.South);

            if(z < regionStartZ)
                directionZ = DirectionFlags.North | DirectionFlags.NorthEast | DirectionFlags.NorthWest;
            else if(z >= regionEndZ)
                directionZ = DirectionFlags.South | DirectionFlags.SouthEast | DirectionFlags.SouthWest;
            else
                directionZ = DirectionFlags.All; // don't really care

            DirectionFlags directionZMask = directionZ | (DirectionFlags.East | DirectionFlags.West);


            var result = (directionX & directionZMask) | (directionZ & directionXMask);
            return result;
        }


        public static bool AnyEast(this DirectionFlags f)
        {
            return (f & (DirectionFlags.East | DirectionFlags.NorthEast | DirectionFlags.SouthEast)) != 0;
        }

        public static bool AnyWest(this DirectionFlags f)
        {
            return (f & (DirectionFlags.West | DirectionFlags.NorthWest | DirectionFlags.SouthWest)) != 0;
        }

        public static bool AnyNorth(this DirectionFlags f)
        {
            return (f & (DirectionFlags.North | DirectionFlags.NorthEast | DirectionFlags.NorthWest)) != 0;
        }

        public static bool AnySouth(this DirectionFlags f)
        {
            return (f & (DirectionFlags.South | DirectionFlags.SouthEast | DirectionFlags.SouthWest)) != 0;
        }

    }

}
