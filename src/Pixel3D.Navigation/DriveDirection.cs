// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;

namespace Pixel3D.Navigation
{
	// Packing two signed 2-bit numbers (X in low, Z in high)
	// Using 2 bits as it allows minimal jump tables while retaining simple unpack behaviour
	[Flags] // <- ... or something to that effect
	public enum DriveDirection : byte
	{
		None = 0,

		East = 1, // 0b01 =  1
		West = 3, // 0b11 = -1
		North = East << 2,
		South = West << 2,

		NorthEast = North | East,
		NorthWest = North | West,
		SouthEast = South | East,
		SouthWest = South | West
	}
}