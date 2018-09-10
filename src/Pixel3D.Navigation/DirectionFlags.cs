// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;

namespace Pixel3D.Navigation
{
	/// <summary>Flags for eight possible directions</summary>
	[Flags]
	public enum DirectionFlags : byte
	{
		East = (byte) 1u << 0,
		NorthEast = (byte) 1u << 1,
		North = (byte) 1u << 2,
		NorthWest = (byte) 1u << 3,
		West = (byte) 1u << 4,
		SouthWest = (byte) 1u << 5,
		South = (byte) 1u << 6,
		SouthEast = (byte) 1u << 7,

		None = 0,
		All = 0xFF
	}
}