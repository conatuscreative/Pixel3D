using System;

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

		#region TagLookup<T>

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