// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Pixel3D.Serialization.Context;
using Pixel3D.Serialization.MethodProviders;

namespace Pixel3D.Serialization.BuiltIn
{
	internal static class SerializeArray
	{
		// NOTE: Do *not* mark these as [CustomFieldSerializer] (will get rejected anyway)
		//       They are accessed directly by the serializer and generator (via ArrayFieldMethodProvider)

		public static void SerializeArrayField<T>(SerializeContext context, BinaryWriter bw, T[] array)
		{
			if (!context.Walk(array)) // null check
				return;

			context.VisitObject(array);

			bw.WriteSmallInt32(array.Length);
			for (var i = 0; i < array.Length; i++) Field.Serialize(context, bw, ref array[i]);

			context.LeaveObject();
		}

		public static void DeserializeArrayField<T>(DeserializeContext context, BinaryReader br, ref T[] array)
		{
			if (!context.Walk(ref array))
				return;

			array = new T[br.ReadSmallInt32()];
			context.VisitObject(array);
			for (var i = 0; i < array.Length; i++) Field.Deserialize(context, br, ref array[i]);
		}

		public static void SerializeArray2DField<T>(SerializeContext context, BinaryWriter bw, T[,] array)
		{
			if (!context.Walk(array)) // null check
				return;

			context.VisitObject(array);

			int length0, length1;
			bw.WriteSmallInt32(length0 = array.GetLength(0));
			bw.WriteSmallInt32(length1 = array.GetLength(1));

			for (var i = 0; i < length0; i++)
			for (var j = 0; j < length1; j++) // NOTE: Arrays are contiguious on their rightmost (last) dimension
				Field.Serialize(context, bw, ref array[i, j]);

			context.LeaveObject();
		}

		public static void DeserializeArray2DField<T>(DeserializeContext context, BinaryReader br, ref T[,] array)
		{
			if (!context.Walk(ref array))
				return;

			int length0, length1;
			array = new T[length0 = br.ReadSmallInt32(), length1 = br.ReadSmallInt32()];

			context.VisitObject(array);

			for (var i = 0; i < length0; i++)
			for (var j = 0; j < length1; j++) // NOTE: Arrays are contiguious on their rightmost (last) dimension
				Field.Deserialize(context, br, ref array[i, j]);
		}

		public static void SerializeArray3DField<T>(SerializeContext context, BinaryWriter bw, T[,,] array)
		{
			if (!context.Walk(array)) // null check
				return;

			context.VisitObject(array);

			int length0, length1, length2;
			bw.WriteSmallInt32(length0 = array.GetLength(0));
			bw.WriteSmallInt32(length1 = array.GetLength(1));
			bw.WriteSmallInt32(length2 = array.GetLength(2));

			for (var i = 0; i < length0; i++)
			for (var j = 0; j < length1; j++)
			for (var k = 0; k < length2; k++)
				Field.Serialize(context, bw, ref array[i, j, k]);

			context.LeaveObject();
		}

		public static void DeserializeArray3DField<T>(DeserializeContext context, BinaryReader br, ref T[,,] array)
		{
			if (!context.Walk(ref array))
				return;

			int length0, length1, length2;
			array = new T[length0 = br.ReadSmallInt32(), length1 = br.ReadSmallInt32(), length2 = br.ReadSmallInt32()];

			context.VisitObject(array);

			for (var i = 0; i < length0; i++)
			for (var j = 0; j < length1; j++)
			for (var k = 0; k < length2; k++)
				Field.Deserialize(context, br, ref array[i, j, k]);
		}

		#region Method Provider

		public static SerializationMethodProviders CreateSerializationMethodProviders()
		{
			var serialize = new[]
			{
				typeof(SerializeArray).GetMethod("SerializeArrayField"),
				typeof(SerializeArray).GetMethod("SerializeArray2DField"),
				typeof(SerializeArray).GetMethod("SerializeArray3DField")
			};

			var deserialize = new[]
			{
				typeof(SerializeArray).GetMethod("DeserializeArrayField"),
				typeof(SerializeArray).GetMethod("DeserializeArray2DField"),
				typeof(SerializeArray).GetMethod("DeserializeArray3DField")
			};

			return new SerializationMethodProviders(
				new EmptyMethodProvider(),
				new EmptyMethodProvider(),
				new EmptyMethodProvider(),
				new EmptyMethodProvider(),
				new ArrayFieldMethodProvider(serialize),
				new ArrayFieldMethodProvider(deserialize),
				new EmptyMethodProvider());
		}

		internal class ArrayFieldMethodProvider : MethodProvider
		{
			private readonly MethodInfo[] arraySerializeMethods;

			// Expects the methods in SerializeArray, sorted by array rank:
			internal ArrayFieldMethodProvider(MethodInfo[] arraySerializeMethods)
			{
				this.arraySerializeMethods = arraySerializeMethods;
			}

			public override MethodInfo GetMethodForType(Type type)
			{
				if (!type.IsArray) return null;

				var lookup = type.GetArrayRank() - 1;
				if (lookup < 0 || lookup >= arraySerializeMethods.Length)
				{
					Debug.Assert(
						false); // <- Trying to serialize a multidimensional array we don't support (add it? add arbitrary dimension support?)
					return null;
				}

				return arraySerializeMethods[lookup].MakeGenericMethod(type.GetElementType());
			}
		}

		#endregion
	}
}