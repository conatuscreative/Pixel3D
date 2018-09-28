// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.IO;
using Pixel3D.Serialization.Context;

namespace Pixel3D.Serialization.BuiltIn
{
	// In generated serializers, these are inlined.
	// In normal custom serializers, you'll probably want to inline them too.
	// But for generic custom serializers, we need to be able to provide these methods when <T> is a primitive.

	internal static class SerializePrimitives
	{
		[CustomSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ref bool value)
		{
			bw.Write(value);
		}

		[CustomSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ref byte value)
		{
			bw.Write(value);
		}

		[CustomSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ref sbyte value)
		{
			bw.Write(value);
		}

		[CustomSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ref short value)
		{
			bw.Write(value);
		}

		[CustomSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ref ushort value)
		{
			bw.Write(value);
		}

		[CustomSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ref int value)
		{
			bw.Write(value);
		}

		[CustomSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ref uint value)
		{
			bw.Write(value);
		}

		[CustomSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ref long value)
		{
			bw.Write(value);
		}

		[CustomSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ref ulong value)
		{
			bw.Write(value);
		}

		[CustomSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ref char value)
		{
			bw.Write(value);
		}

		[CustomSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ref double value)
		{
			bw.Write(value);
		}

		[CustomSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ref float value)
		{
			bw.Write(value);
		}

		[CustomSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref bool value)
		{
			value = br.ReadBoolean();
		}

		[CustomSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref byte value)
		{
			value = br.ReadByte();
		}

		[CustomSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref sbyte value)
		{
			value = br.ReadSByte();
		}

		[CustomSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref short value)
		{
			value = br.ReadInt16();
		}

		[CustomSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref ushort value)
		{
			value = br.ReadUInt16();
		}

		[CustomSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref int value)
		{
			value = br.ReadInt32();
		}

		[CustomSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref uint value)
		{
			value = br.ReadUInt32();
		}

		[CustomSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref long value)
		{
			value = br.ReadInt64();
		}

		[CustomSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref ulong value)
		{
			value = br.ReadUInt64();
		}

		[CustomSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref char value)
		{
			value = br.ReadChar();
		}

		[CustomSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref double value)
		{
			value = br.ReadDouble();
		}

		[CustomSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref float value)
		{
			value = br.ReadSingle();
		}
	}
}