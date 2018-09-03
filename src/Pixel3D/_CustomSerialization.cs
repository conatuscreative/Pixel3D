using System;
using System.IO;
using Pixel3D.Serialization;
using Pixel3D.Serialization.Context;

namespace Pixel3D
{
	public static class CustomSerialization
	{
		#region Tag Set

		// Definition-only at the field level (don't even bother storing it) - see TagLookup
		[CustomFieldSerializer] public static void Serialize(SerializeContext context, BinaryWriter bw, TagSet value) { }
		[CustomFieldSerializer] public static void Deserialize(DeserializeContext context, BinaryReader br, ref TagSet value) { throw new InvalidOperationException(); }

		#endregion

		#region Tag Lookup

		// Due to a limitation in the serializer generator, this needs to be in a different class to TagLookup<T>
		// because the generic parameters need to be on the Method and NOT on the Type. (Maybe we should fix this so they can be on either.)

		public class CustomSerializerForTagLookup
		{
			// At last count, TagLookup and its TagSet rules and their strings were taking up ~70% of definition objects.
			// These are definition-only objects that the game state should never even store references to.
			// Only thing the game state may care about is the contents of the lookup (values) - so we store that.

			[CustomFieldSerializer]
			public static void Serialize<T>(SerializeContext context, BinaryWriter bw, TagLookup<T> value)
			{
				for (int i = 0; i < value.Count; i++)
					Field.Serialize(context, bw, ref value.values[i]);
			}

			[CustomFieldSerializer]
			public static void Deserialize<T>(DeserializeContext context, BinaryReader br, ref TagLookup<T> value)
			{
				throw new InvalidOperationException();
			}
		}

		#endregion
	}
}