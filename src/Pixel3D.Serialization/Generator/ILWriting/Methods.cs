// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using Pixel3D.Serialization.Context;
using Pixel3D.Serialization.MethodProviders;
using Pixel3D.Serialization.Static;

namespace Pixel3D.Serialization.Generator.ILWriting
{
	// Provide cached access to methods used in generated IL
	internal static class Methods
	{
		public static readonly MethodInfo StaticDispatchTable_SerializationDispatcher =
			typeof(StaticDispatchTable).GetMethod("SerializationDispatcher");

		public static readonly MethodInfo SerializeContext_VisitObject =
			typeof(SerializeContext).GetMethod("VisitObject", new[] {typeof(object)});

		public static readonly MethodInfo SerializeContext_LeaveObject =
			typeof(SerializeContext).GetMethod("LeaveObject", Type.EmptyTypes);

		public static readonly MethodInfo DeserializeContext_VisitObject =
			typeof(DeserializeContext).GetMethod("VisitObject", new[] {typeof(object)});

		public static readonly MethodInfo SerializeContext_Walk =
			typeof(SerializeContext).GetMethod("Walk", new[] {typeof(object)});


		public static readonly MethodInfo Type_GetTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");

		public static readonly MethodInfo MethodBase_GetMethodFromHandle =
			typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] {typeof(RuntimeMethodHandle)});

		public static readonly MethodInfo FormatterServices_GetUninitializedObject =
			typeof(FormatterServices).GetMethod("GetUninitializedObject");


		public static readonly MethodInfo BinaryWriter_WriteInt32 =
			typeof(BinaryWriter).GetMethod("Write", new[] {typeof(int)});

		public static readonly MethodInfo BinaryReader_ReadInt32 = typeof(BinaryReader).GetMethod("ReadInt32");

		public static readonly MethodInfo BinaryWriter_WriteByte =
			typeof(BinaryWriter).GetMethod("Write", new[] {typeof(byte)});

		public static readonly MethodInfo BinaryReader_ReadByte = typeof(BinaryReader).GetMethod("ReadByte");

		public static readonly LookupMethodProvider BinaryWriterPrimitive = new LookupMethodProvider(
			new Dictionary<Type, MethodInfo>
			{
				{typeof(bool), typeof(BinaryWriter).GetMethod("Write", new[] {typeof(bool)})},
				{typeof(byte), typeof(BinaryWriter).GetMethod("Write", new[] {typeof(byte)})},
				{typeof(sbyte), typeof(BinaryWriter).GetMethod("Write", new[] {typeof(sbyte)})},
				{typeof(short), typeof(BinaryWriter).GetMethod("Write", new[] {typeof(short)})},
				{typeof(ushort), typeof(BinaryWriter).GetMethod("Write", new[] {typeof(ushort)})},
				{typeof(int), typeof(BinaryWriter).GetMethod("Write", new[] {typeof(int)})},
				{typeof(uint), typeof(BinaryWriter).GetMethod("Write", new[] {typeof(uint)})},
				{typeof(long), typeof(BinaryWriter).GetMethod("Write", new[] {typeof(long)})},
				{typeof(ulong), typeof(BinaryWriter).GetMethod("Write", new[] {typeof(ulong)})},
				{typeof(char), typeof(BinaryWriter).GetMethod("Write", new[] {typeof(char)})},
				{typeof(double), typeof(BinaryWriter).GetMethod("Write", new[] {typeof(double)})},
				{typeof(float), typeof(BinaryWriter).GetMethod("Write", new[] {typeof(float)})}
			});

		public static readonly LookupMethodProvider BinaryReaderPrimitive = new LookupMethodProvider(
			new Dictionary<Type, MethodInfo>
			{
				{typeof(bool), typeof(BinaryReader).GetMethod("ReadBoolean")},
				{typeof(byte), typeof(BinaryReader).GetMethod("ReadByte")},
				{typeof(sbyte), typeof(BinaryReader).GetMethod("ReadSByte")},
				{typeof(short), typeof(BinaryReader).GetMethod("ReadInt16")},
				{typeof(ushort), typeof(BinaryReader).GetMethod("ReadUInt16")},
				{typeof(int), typeof(BinaryReader).GetMethod("ReadInt32")},
				{typeof(uint), typeof(BinaryReader).GetMethod("ReadUInt32")},
				{typeof(long), typeof(BinaryReader).GetMethod("ReadInt64")},
				{typeof(ulong), typeof(BinaryReader).GetMethod("ReadUInt64")},
				{typeof(char), typeof(BinaryReader).GetMethod("ReadChar")},
				{typeof(double), typeof(BinaryReader).GetMethod("ReadDouble")},
				{typeof(float), typeof(BinaryReader).GetMethod("ReadSingle")}
			});


#if DEBUG
		public static readonly MethodInfo SerializeContext_DebugTrace =
			typeof(SerializeContext).GetMethod("DebugTrace", new[] {typeof(string)});

		public static readonly MethodInfo DeserializeContext_DebugTrace =
			typeof(DeserializeContext).GetMethod("DebugTrace", new[] {typeof(string)});
#endif
	}
}