// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.IO;

namespace Pixel3D // <- Put in the same namespace as BinaryReader, etc
{
	public static class BinaryReaderWriterExtensions
	{
		public static void WriteSmallUInt32(this BinaryWriter bw, uint value)
		{
			while (value >= 0x80u)
			{
				bw.Write((byte) (value | 0x80u)); // <- Write lower 7 bits and more data flag
				value >>= 7;
			}

			bw.Write((byte) value); // <- Write remaining bits
		}

		public static uint ReadSmallUInt32(this BinaryReader br)
		{
			var offset = 0;
			uint result = 0;
			byte v;
			while (((v = br.ReadByte()) & 0x80u) != 0)
			{
				result |= (v & 0x7Fu) << offset;
				offset += 7;
			}

			result |= (uint) v << offset;
			return result;
		}

		public static void WriteSmallInt32(this BinaryWriter bw, int value)
		{
			WriteSmallUInt32(bw, (uint) value);
		}

		public static int ReadSmallInt32(this BinaryReader br)
		{
			return (int) ReadSmallUInt32(br);
		}
	}
}