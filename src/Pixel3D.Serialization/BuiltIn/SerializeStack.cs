// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.Collections.Generic;
using System.IO;
using Pixel3D.Serialization.Context;

namespace Pixel3D.Serialization.BuiltIn
{
	internal static class SerializeStack
	{
		[CustomSerializer]
		public static void Serialize<T>(SerializeContext context, BinaryWriter bw, Stack<T> stack)
		{
			context.VisitObject(stack);

			bw.WriteSmallInt32(stack.Count);
			foreach (var entry in stack)
			{
				var item = entry;
				Field.Serialize(context, bw, ref item);
			}

			context.LeaveObject();
		}

		[CustomSerializer]
		public static void Deserialize<T>(DeserializeContext context, BinaryReader br, Stack<T> stack)
		{
			context.VisitObject(stack);

			var count = br.ReadSmallInt32();

			stack.Clear();

			for (var i = 0; i < count; i++)
			{
				var item = default(T);
				Field.Deserialize(context, br, ref item);
				stack.Push(item);
			}
		}

		[CustomInitializer]
		public static Stack<T> Initialize<T>()
		{
			return new Stack<T>();
		}
	}
}