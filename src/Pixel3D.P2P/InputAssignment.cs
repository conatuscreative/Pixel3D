// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;

namespace Pixel3D.P2P
{
	[Flags]
	public enum InputAssignment
	{
		None = 0x0,

		Player1 = 0x1,
		Player2 = 0x2,
		Player3 = 0x4,
		Player4 = 0x8,

		All = 0xF,
		Full = All
	}
}