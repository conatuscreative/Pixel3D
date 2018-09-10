// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

namespace Pixel3D.Navigation
{
	public static class DriveDirectionExtensions
	{
		// Rotate one step counter-clockwise
		public static DriveDirection Next(this DriveDirection dd)
		{
			// There might be a mathematical solution. But in fewer instructions than a branch miss?
			switch (dd)
			{
				case DriveDirection.East: return DriveDirection.NorthEast;
				case DriveDirection.NorthEast: return DriveDirection.North;
				case DriveDirection.North: return DriveDirection.NorthWest;
				case DriveDirection.NorthWest: return DriveDirection.West;
				case DriveDirection.West: return DriveDirection.SouthWest;
				case DriveDirection.SouthWest: return DriveDirection.South;
				case DriveDirection.South: return DriveDirection.SouthEast;
				case DriveDirection.SouthEast: return DriveDirection.East;

				default:
					return 0;
			}
		}

		// Rotate one step clockwiseS
		public static DriveDirection Previous(this DriveDirection dd)
		{
			// There might be a mathematical solution. But in fewer instructions than a branch miss?
			switch (dd)
			{
				case DriveDirection.East: return DriveDirection.SouthEast;
				case DriveDirection.SouthEast: return DriveDirection.South;
				case DriveDirection.South: return DriveDirection.SouthWest;
				case DriveDirection.SouthWest: return DriveDirection.West;
				case DriveDirection.West: return DriveDirection.NorthWest;
				case DriveDirection.NorthWest: return DriveDirection.North;
				case DriveDirection.North: return DriveDirection.NorthEast;
				case DriveDirection.NorthEast: return DriveDirection.East;

				default:
					return 0;
			}
		}


		public static int GetX(this DriveDirection dd)
		{
			return (int) ((uint) dd << 30) >> 30; // <- Fill sign bit
		}

		public static int GetZ(this DriveDirection dd)
		{
			return (int) ((uint) dd << 28) >> 30; // <- Fill sign bit
		}


		public static DriveDirection MakeDriveDirection(int x, int z)
		{
			return (DriveDirection) (((uint) x & 3u) | (((uint) z & 3u) << 2));
		}

		public static DriveDirection GetDriveDirection(int x, int z, DriveDirection previous,
			NavRegionProjection projection)
		{
			// This incredible switch construct is so that we can jump directly to the previous value, saving us a lot of code in the general case
			// And, in particular, allowing us to avoid shuffling our large arguments between stacks, as the dopey JIT won't inline the large comparisons used here

			// The use of non-short-circuiting is a speculative optimisation against expected cost of branch mis-prediction (NOTE: inspected JIT but haven't profiled yet -AR)
			// This slightly expands our output code size (~5 icache lines total), but perhaps not a big deal due to the switch behaviour in the typical case.

			switch (previous)
			{
				default:
					previous = 0; // Reset
					goto case DriveDirection.East;

				case DriveDirection.East:
				{
					if (((x < projection.x.min) & (projection.z.min <= z) & (z <= projection.z.max)) |
					    ((x < projection.negative.min) & (x < projection.positive.min)))
						return DriveDirection.East;
				}
					if (previous == 0)
						goto case DriveDirection.West;
					else
						goto default;

				case DriveDirection.West:
				{
					if (((x > projection.x.max) & (projection.z.min <= z) & (z <= projection.z.max)) |
					    ((x > projection.negative.max) & (x > projection.positive.max)))
						return DriveDirection.West;
				}
					if (previous == 0)
						goto case DriveDirection.North;
					else
						goto default;

				case DriveDirection.North:
				{
					if (((z < projection.z.min) & (projection.x.min <= x) & (x <= projection.x.max)) |
					    ((x < projection.negative.min) & (x > projection.positive.max)))
						return DriveDirection.North;
				}
					if (previous == 0)
						goto case DriveDirection.South;
					else
						goto default;

				case DriveDirection.South:
				{
					if (((z > projection.z.max) & (projection.x.min <= x) & (x <= projection.x.max)) |
					    ((x > projection.negative.max) & (x < projection.positive.min)))
						return DriveDirection.South;
				}
					if (previous == 0)
						goto case DriveDirection.NorthEast;
					else
						goto default;

				case DriveDirection.NorthEast:
				{
					if (((x <= projection.x.max) & (z <= projection.z.max) & (projection.positive.min <= x) &
					     (x <= projection.positive.max))
					    | ((x < projection.x.min) & (z < projection.z.min)))
						return DriveDirection.NorthEast;
				}
					if (previous == 0)
						goto case DriveDirection.NorthWest;
					else
						goto default;

				case DriveDirection.NorthWest:
				{
					if (((x >= projection.x.min) & (z <= projection.z.max) & (projection.negative.min <= x) &
					     (x <= projection.negative.max))
					    | ((x > projection.x.max) & (z < projection.z.min)))
						return DriveDirection.NorthWest;
				}
					if (previous == 0)
						goto case DriveDirection.SouthEast;
					else
						goto default;

				case DriveDirection.SouthEast:
				{
					if (((x <= projection.x.max) & (z >= projection.z.min) & (projection.negative.min <= x) &
					     (x <= projection.negative.max))
					    | ((x < projection.x.min) & (z > projection.z.max)))
						return DriveDirection.SouthEast;
				}
					if (previous == 0)
						goto case DriveDirection.SouthWest;
					else
						goto default;

				case DriveDirection.SouthWest:
				{
					if (((x >= projection.x.min) & (z >= projection.z.min) & (projection.positive.min <= x) &
					     (x <= projection.positive.max))
					    | ((x > projection.x.max) & (z > projection.z.max)))
						return DriveDirection.SouthWest;
				}
					if (previous == 0)
						return 0; // Nowhere to go :(
					else
						goto default;
			}
		}
	}
}