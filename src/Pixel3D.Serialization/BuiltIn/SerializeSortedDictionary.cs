// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Collections.Generic;
using System.IO;
using Pixel3D.Serialization.Context;

namespace Pixel3D.Serialization.BuiltIn
{
	internal static class SerializeSortedDictionary
	{
		[CustomSerializer]
		public static void Serialize<TKey, TValue>(SerializeContext context, BinaryWriter bw,
			SortedDictionary<TKey, TValue> dictionary)
		{
			// TODO: Add support for custom comparers
			if (!dictionary.Comparer.Equals(Sample<TKey, TValue>.sample.Comparer))
				throw new NotImplementedException("Serializing a custom comparer is not implemented");

			context.VisitObject(dictionary);

			bw.WriteSmallInt32(dictionary.Count);

			foreach (var item in dictionary)
			{
				var key = item.Key;
				var value = item.Value;
				Field.Serialize(context, bw, ref key);
				Field.Serialize(context, bw, ref value);
			}

			context.LeaveObject();
		}

		[CustomSerializer]
		public static void Deserialize<TKey, TValue>(DeserializeContext context, BinaryReader br,
			SortedDictionary<TKey, TValue> dictionary)
		{
			context.VisitObject(dictionary);

			var count = br.ReadSmallInt32();

			dictionary.Clear();

			for (var i = 0; i < count; i++)
			{
				var key = default(TKey);
				var value = default(TValue);
				Field.Deserialize(context, br, ref key);
				Field.Deserialize(context, br, ref value);
				dictionary.Add(key, value);
			}
		}

		[CustomInitializer]
		public static SortedDictionary<TKey, TValue> Initialize<TKey, TValue>()
		{
			return new SortedDictionary<TKey, TValue>();
		}

		// Because we want to know what comparer we get by default:
		private static class Sample<TKey, TValue>
		{
			internal static readonly SortedDictionary<TKey, TValue> sample = Initialize<TKey, TValue>();
		}
	}
}