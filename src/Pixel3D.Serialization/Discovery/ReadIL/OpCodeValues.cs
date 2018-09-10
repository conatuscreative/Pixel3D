// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

namespace Pixel3D.Serialization.Discovery.ReadIL
{
	internal static class OpCodeValues
	{
		// Incomplete list at the moment (just using these for optimising "hot" paths)
		// (because CLR equality check for OpCode is very much sub-optimal)

		public const ushort Ldarg_0 = 0x02;
		public const ushort Ldarg_1 = 0x03;
		public const ushort Ldarg_2 = 0x04;
		public const ushort Ldarg_3 = 0x05;
		public const ushort Ldarg_S = 0x0e;
		public const ushort Ldarg = 0xfe09;

		public const ushort Ldloc_0 = 0x06;
		public const ushort Ldloc_1 = 0x07;
		public const ushort Ldloc_2 = 0x08;
		public const ushort Ldloc_3 = 0x09;
		public const ushort Ldloc_S = 0x11;
		public const ushort Ldloc = 0xfe0c;

		public const ushort Dup = 0x25;
		public const ushort Ldnull = 0x14;
		public const ushort Newobj = 0x73;
		public const ushort Ldfld = 0x7b;
		public const ushort Ldftn = 0xfe06;
		public const ushort Ldvirtftn = 0xfe07;
	}
}