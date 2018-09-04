using System;
using System.IO;

namespace Pixel3D.Animations.Serialization
{
	public static class AnimationSerializeContextExtensions
	{
		#region TagSet

		// NOTE: Pass-through the animation serializer to a simple binary serializer (the format of `TagSet` is *really* stable, and some folks need to directly serialize us)

		public static void SerializeTagSet(this TagSet tagSet, AnimationSerializeContext context)
		{
			tagSet.Serialize(context.bw);
		}

		public static TagSet DeserializeTagSet(this AnimationDeserializeContext context)
		{
			return new TagSet(context.br);
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

		#region OrderedDictionary

		public static void SerializeOrderedDictionary<T>(this OrderedDictionary<string, T> dictionary, AnimationSerializeContext context, Action<T> serializeValue)
		{
			context.bw.WriteSmallInt32(dictionary.Count);

			foreach (var item in dictionary)
			{
				string key = item.Key;
				T value = item.Value;
				context.bw.Write(key);
				serializeValue(value);
			}
		}

		public static OrderedDictionary<string, T> DeserializeOrderedDictionary<T>(this AnimationDeserializeContext context, Func<T> deserializeValue)
		{
			var dictionary = new OrderedDictionary<string, T>();

			int count = context.br.ReadSmallInt32();

			for (int i = 0; i < count; i++)
			{
				var key = context.br.ReadString();
				var value = deserializeValue();
				dictionary.Add(key, value);
			}

			return dictionary;
		}


		#endregion
	}
}