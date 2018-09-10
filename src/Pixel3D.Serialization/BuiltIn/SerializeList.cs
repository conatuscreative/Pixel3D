// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Collections.Generic;
using System.IO;
using Pixel3D.Serialization.Context;

namespace Pixel3D.Serialization.BuiltIn
{
	internal static class SerializeList
	{
		[CustomSerializer]
		public static void Serialize<T>(SerializeContext context, BinaryWriter bw, List<T> list)
		{
			context.VisitObject(list);

			bw.WriteSmallInt32(list.Count);
			for (var i = 0; i < list.Count; i++)
			{
				var item = list[i];
				Field.Serialize(context, bw, ref item);
			}

			context.LeaveObject();
		}

		[CustomSerializer]
		public static void Deserialize<T>(DeserializeContext context, BinaryReader br, List<T> list)
		{
			context.VisitObject(list);

			var count = br.ReadSmallInt32();

			list.Clear();
			if (list.Capacity < count)
				list.Capacity = count;

			for (var i = 0; i < count; i++)
			{
				var item = default(T);
				Field.Deserialize(context, br, ref item);
				list.Add(item);
			}
		}

		[CustomInitializer]
		public static List<T> Initialize<T>()
		{
			return new List<T>();
		}
	}
}