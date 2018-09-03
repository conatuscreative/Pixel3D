namespace Pixel3D.Animations.Serialization
{
	public static class AnimationSerializeContextExtensions
	{
		// NOTE: Pass-through the animation serializer to a simple binary serializer (the format of `TagSet` is *really* stable, and some folks need to directly serialize us)

		public static void Serialize(this TagSet tagSet, AnimationSerializeContext context)
		{
			tagSet.Serialize(context.bw);
		}

		public static TagSet DeserializeTagSet(this AnimationDeserializeContext context)
		{
			return new TagSet(context.br);
		}
	}
}