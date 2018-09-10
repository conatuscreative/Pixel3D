// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Reflection.Emit;

namespace Pixel3D.Serialization.Discovery.ReadIL
{
	internal static class OpCodeExtensions
	{
		// Improve check performance by comparing to hard-coded opcode values:
		// (because CLR equality check for OpCode is very much sub-optimal)

		public static bool IsLdarg(this OpCode opCode)
		{
			var value = (ushort) opCode.Value;
			return value >= OpCodeValues.Ldarg_0 && value <= OpCodeValues.Ldarg_3
			       || value == OpCodeValues.Ldarg_S
			       || value == OpCodeValues.Ldarg;
		}

		public static bool IsLdloc(this OpCode opCode)
		{
			var value = (ushort) opCode.Value;
			return value >= OpCodeValues.Ldloc_0 && value <= OpCodeValues.Ldloc_3
			       || value == OpCodeValues.Ldloc_S
			       || value == OpCodeValues.Ldloc;
		}
	}
}