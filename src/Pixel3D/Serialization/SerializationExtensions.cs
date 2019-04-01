// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using Pixel3D.Animations;

namespace Pixel3D.Serialization
{
	public static class SerializationExtensions
	{
		#region OrderedDictionary

		public static void SerializeOrderedDictionary<T>(this OrderedDictionary<string, T> dictionary, AnimationSerializeContext context, Action<T> serializeValue)
		{
			context.bw.WriteSmallInt32(dictionary.Count);

			foreach (var item in dictionary)
			{
				var key = item.Key;
				var value = item.Value;
				context.bw.Write(key);
				serializeValue(value);
			}
		}

		public static OrderedDictionary<string, T> DeserializeOrderedDictionary<T>(this AnimationDeserializeContext context, Func<T> deserializeValue)
		{
			var dictionary = new OrderedDictionary<string, T>();

			int count = context.br.ReadSmallInt32();

			for (var i = 0; i < count; i++)
			{
				var key = context.br.ReadString();
				var value = deserializeValue();
				dictionary.Add(key, value);
			}

			return dictionary;
		}

		#endregion

		#region TagLookup

		// NOTE: Pass-through the animation serializer to a simple binary serializer (the format of `TagLookup` is *really* stable, and some folks need to directly serialize us)

		public static void SerializeTagLookup<T>(this TagLookup<T> tagLookup, AnimationSerializeContext context, Action<T> serializeValue)
		{
			tagLookup.Serialize(context.bw, serializeValue);
		}

		/// <summary>Deserialize into new object instance</summary>
		public static TagLookup<T> DeserializeTagLookup<T>(this AnimationDeserializeContext context, Func<T> deserializeValue)
		{
			return new TagLookup<T>(context.br, deserializeValue);
		}

		#endregion
	}
}