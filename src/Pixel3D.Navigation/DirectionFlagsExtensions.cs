// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
namespace Pixel3D.Navigation
{
	public static class DirectionFlagsExtensions
	{
		public static DirectionFlags GetFlagsTowardsRegion(int x, int z, int regionStartX, int regionEndX,
			int regionStartZ, int regionEndZ)
		{
			DirectionFlags directionX;
			DirectionFlags directionZ;

			if (x < regionStartX)
				directionX = DirectionFlags.East | DirectionFlags.NorthEast | DirectionFlags.SouthEast;
			else if (x >= regionEndX)
				directionX = DirectionFlags.West | DirectionFlags.NorthWest | DirectionFlags.SouthWest;
			else
				directionX = DirectionFlags.All; // don't really care

			var directionXMask = directionX | DirectionFlags.North | DirectionFlags.South;

			if (z < regionStartZ)
				directionZ = DirectionFlags.North | DirectionFlags.NorthEast | DirectionFlags.NorthWest;
			else if (z >= regionEndZ)
				directionZ = DirectionFlags.South | DirectionFlags.SouthEast | DirectionFlags.SouthWest;
			else
				directionZ = DirectionFlags.All; // don't really care

			var directionZMask = directionZ | DirectionFlags.East | DirectionFlags.West;


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