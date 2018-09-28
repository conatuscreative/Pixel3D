// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.Collections.Generic;
using System.IO;
using Pixel3D.Serialization.Context;

namespace Pixel3D.Serialization.BuiltIn
{
	internal static class SerializeQueue
	{
		[CustomSerializer]
		public static void Serialize<T>(SerializeContext context, BinaryWriter bw, Queue<T> queue)
		{
			context.VisitObject(queue);

			bw.WriteSmallInt32(queue.Count);
			foreach (var entry in queue)
			{
				var item = entry;
				Field.Serialize(context, bw, ref item);
			}

			context.LeaveObject();
		}

		[CustomSerializer]
		public static void Deserialize<T>(DeserializeContext context, BinaryReader br, Queue<T> queue)
		{
			context.VisitObject(queue);

			var count = br.ReadSmallInt32();

			queue.Clear();

			for (var i = 0; i < count; i++)
			{
				var item = default(T);
				Field.Deserialize(context, br, ref item);
				queue.Enqueue(item);
			}
		}

		[CustomInitializer]
		public static Queue<T> Initialize<T>()
		{
			return new Queue<T>();
		}
	}
}