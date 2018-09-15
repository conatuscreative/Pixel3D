// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;

namespace Pixel3D
{
	// NOTE: This type must match what is replicated in Pixel3D.Network.Rollback.InputStateExtensions

	[Serializable]
	[Flags]
	public enum InputState : uint
	{
		None = 0x0,

		Input0 = 0x1,
		Input1 = 0x2,
		Input2 = 0x4,
		Input3 = 0x8,
		Input4 = 0x10,
		Input5 = 0x20,
		Input6 = 0x40,
		Input7 = 0x80,
		Input8 = 0x100,
		Input9 = 0x200,
		Input10 = 0x400,
		Input11 = 0x800,
		Input12 = 0x1000,
		Input13 = 0x2000,
		Input14 = 0x4000,
		Input15 = 0x8000,
		Input16 = 0x10000,
		Input17 = 0x20000,
		Input18 = 0x40000,
		Input19 = 0x80000,
		Input20 = 0x100000,
		Input21 = 0x200000,
		Input22 = 0x400000,
		Input23 = 0x800000,
		Input24 = 0x1000000,
		Input25 = 0x2000000,
		Input26 = 0x4000000,
		Input27 = 0x8000000,
		Input28 = 0x10000000,
		Input29 = 0x20000000,
		Input30 = 0x40000000,
		Input31 = 0x80000000,

		All = uint.MaxValue
	}
}