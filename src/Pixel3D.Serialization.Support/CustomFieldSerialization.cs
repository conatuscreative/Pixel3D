// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.IO;
using Pixel3D.Animations;
using Pixel3D.Animations.Serialization;
using Pixel3D.Serialization;
using Pixel3D.Serialization.Context;

namespace Pixel3D
{
	public static partial class CustomFieldSerialization
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

		public static void SerializeTagLookup<T>(this TagLookup<T> tagLookup, AnimationSerializeContext context,
			Action<T> serializeValue)
		{
			tagLookup.Serialize(context.bw, serializeValue);
		}

		/// <summary>Deserialize into new object instance</summary>
		public static TagLookup<T> DeserializeTagLookup<T>(this AnimationDeserializeContext context,
			Func<T> deserializeValue)
		{
			return new TagLookup<T>(context.br, deserializeValue);
		}

		#endregion

		#region TagLookup

		// Due to a limitation in the serializer generator, this needs to be in a different class to TagLookup<T>
		// because the generic parameters need to be on the Method and NOT on the Type. (Maybe we should fix this so they can be on either.)

		// At last count, TagLookup and its TagSet rules and their strings were taking up ~70% of definition objects.
		// These are definition-only objects that the game state should never even store references to.
		// Only thing the game state may care about is the contents of the lookup (values) - so we store that.

		[CustomFieldSerializer]
		public static void Serialize<T>(SerializeContext context, BinaryWriter bw, TagLookup<T> value)
		{
			for (var i = 0; i < value.Count; i++)
				Field.Serialize(context, bw, ref value.values[i]);
		}

		[CustomFieldSerializer]
		public static void Deserialize<T>(DeserializeContext context, BinaryReader br, ref TagLookup<T> value)
		{
			throw new InvalidOperationException();
		}

		#endregion

		#region TagSet

		// Definition-only at the field level (don't even bother storing it) - see TagLookup
		[CustomFieldSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, TagSet value) { }

		[CustomFieldSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref TagSet value)
		{
			throw new InvalidOperationException();
		}

		#endregion

		#region ImageBundle

		[CustomFieldSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ImageBundle value)
		{
			throw new InvalidOperationException();
		}

		[CustomFieldSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref ImageBundle value)
		{
			throw new InvalidOperationException();
		}

		#endregion

		#region ImageBundleManager

		[CustomFieldSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ImageBundleManager value)
		{
			throw new InvalidOperationException();
		}

		[CustomFieldSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref ImageBundleManager value)
		{
			throw new InvalidOperationException();
		}

		#endregion
	}
}