// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;

namespace Pixel3D.Serialization.Discovery.ReadIL
{
	internal static class BinaryReadOpCodeExtensions
	{
		private static readonly OpCode[] singleByteOpCodes = new OpCode[0x100];
		private static readonly OpCode[] multiByteOpCodes = new OpCode[0x100];

		static BinaryReadOpCodeExtensions()
		{
			var allOpCodes = typeof(OpCodes).GetFields().Select(f => (OpCode) f.GetValue(null));
			foreach (var opCode in allOpCodes)
				if (opCode.Size == 1)
					singleByteOpCodes[opCode.Value] = opCode;
				else if (opCode.Size == 2 && (opCode.Value & 0xFF00) == 0xFE00)
					multiByteOpCodes[opCode.Value & 0xFF] = opCode;
				else
					throw new InvalidOperationException("Unknown opcode!");
		}


		public static OpCode ReadOpCode(this BinaryReader br)
		{
			int b = br.ReadByte();
			var opCode = b == 0xFE ? multiByteOpCodes[br.ReadByte()] : singleByteOpCodes[b];

			if (opCode.Size == 0) // Not initialized
				throw new Exception("Invalid OpCode");
			return opCode;
		}

		public static void SkipOperand(this BinaryReader br, OperandType operandType)
		{
			switch (operandType)
			{
				case OperandType.InlineSwitch:
					long size = br.ReadUInt32();
					br.BaseStream.Position += size * 4;
					break;

				case OperandType.InlineI8:
				case OperandType.InlineR:
					br.ReadInt64();
					break;

				case OperandType.InlineBrTarget:
				case OperandType.InlineField:
				case OperandType.InlineI:
				case OperandType.InlineMethod:
				case OperandType.InlineString:
				case OperandType.InlineTok:
				case OperandType.InlineType:
				case OperandType.ShortInlineR:
					br.ReadInt32();
					break;

				case OperandType.InlineVar:
					br.ReadInt16();
					break;

				case OperandType.ShortInlineBrTarget:
				case OperandType.ShortInlineI:
				case OperandType.ShortInlineVar:
					br.ReadByte();
					break;

				case OperandType.InlineNone:
					break;

				default:
					throw new Exception("Unknown operand type!"); // Shouldn't happen
			}
		}

		public static int ReadIndexOperandLdarg(this BinaryReader br, ushort opCodeValue)
		{
			switch (opCodeValue)
			{
				case OpCodeValues.Ldarg: return br.ReadUInt16();
				case OpCodeValues.Ldarg_0: return 0;
				case OpCodeValues.Ldarg_1: return 1;
				case OpCodeValues.Ldarg_2: return 2;
				case OpCodeValues.Ldarg_3: return 3;
				case OpCodeValues.Ldarg_S: return br.ReadByte();

				default:
					Debug.Assert(false);
					return -1; // Shouldn't happen!
			}
		}

		public static int ReadIndexOperandLdloc(this BinaryReader br, ushort opCodeValue)
		{
			switch (opCodeValue)
			{
				case OpCodeValues.Ldloc: return br.ReadUInt16();
				case OpCodeValues.Ldloc_0: return 0;
				case OpCodeValues.Ldloc_1: return 1;
				case OpCodeValues.Ldloc_2: return 2;
				case OpCodeValues.Ldloc_3: return 3;
				case OpCodeValues.Ldloc_S: return br.ReadByte();

				default:
					Debug.Assert(false);
					return -1; // Shouldn't happen!
			}
		}
	}
}